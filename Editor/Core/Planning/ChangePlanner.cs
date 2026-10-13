// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using System.Globalization;
using JP.AudioImportOptimizer.Core.Matching;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Profiles;

namespace JP.AudioImportOptimizer.Core.Planning
{
    /// <summary>Compares asset state with the selected rule and produces findings and proposed changes. Pure logic; never touches assets.</summary>
    public static class ChangePlanner
    {
        private static readonly SettingKind[] AssetKinds =
        {
            SettingKind.ForceToMono, SettingKind.LoadInBackground, SettingKind.Ambisonic, SettingKind.PreloadAudioData
        };

        private static readonly SettingKind[] SampleKinds =
        {
            SettingKind.LoadType, SettingKind.CompressionFormat, SettingKind.Quality,
            SettingKind.SampleRateSetting, SettingKind.SampleRateOverride
        };

        /// <summary>Plans one asset.</summary>
        /// <param name="asset">The asset state.</param>
        /// <param name="selection">The rule selection for the asset.</param>
        /// <returns>The asset plan. Contains no changes when the asset already satisfies its rule.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public static AssetPlan Plan(AudioAssetInfo asset, RuleSelection selection)
        {
            if (asset == null) throw new ArgumentNullException("asset");
            if (selection == null) throw new ArgumentNullException("selection");
            var plan = new AssetPlan { Asset = asset, Selection = selection };

            if (!string.IsNullOrEmpty(asset.ReadError))
            {
                plan.Findings.Add(new Finding
                {
                    Code = FindingCodes.ImporterReadFailed, Severity = Severity.Error, AssetPath = asset.Path,
                    Title = "Importer could not be read",
                    Detected = asset.ReadError,
                    WhyItMatters = "Without a readable AudioImporter the asset cannot be analyzed or optimized.",
                    ProposedSummary = "Reimport the asset or check that the file is a valid audio file."
                });
                return plan;
            }

            if (selection.Selected == null)
            {
                plan.Findings.Add(new Finding
                {
                    Code = FindingCodes.NoRuleMatched, Severity = Severity.Info, AssetPath = asset.Path,
                    Title = "No rule matches this asset",
                    Detected = "None of the enabled rules matched the folder, name, extension, labels or size.",
                    WhyItMatters = "Unmatched assets keep their current settings and are not optimized.",
                    ProposedSummary = "Add a rule or widen an existing one if this asset should be managed."
                });
                return plan;
            }

            var rule = selection.Selected.Rule;
            if (selection.HasConflict)
            {
                plan.Findings.Add(new Finding
                {
                    Code = FindingCodes.RuleConflict, Severity = Severity.Warning, AssetPath = asset.Path,
                    Title = "Rules conflict",
                    Detected = selection.Explanation,
                    WhyItMatters = "Two rules tie on priority and specificity; list order decided. Reordering rules would change the result.",
                    ProposedSummary = "Give one rule a higher priority or make its criteria more specific."
                });
            }

            PlanSampleScope(plan, asset, rule, rule.defaultPolicy, PlatformCatalog.DefaultPlatform, asset.Default, FindingCodes.DefaultMismatch);
            PlanAssetLevel(plan, asset, rule);

            if (rule.platformPolicies != null)
            {
                foreach (var pp in rule.platformPolicies)
                {
                    if (pp == null || string.IsNullOrWhiteSpace(pp.platform)) continue;
                    PlanPlatform(plan, asset, rule, pp);
                }
            }
            return plan;
        }

        private static void PlanAssetLevel(AssetPlan plan, AudioAssetInfo asset, OptimizationRule rule)
        {
            if (rule.defaultPolicy == null) return;
            foreach (var kind in AssetKinds)
            {
                SettingValue desired;
                if (!rule.defaultPolicy.TryGetAssetLevel(kind, out desired)) continue;
                var current = asset.GetAssetLevel(kind);
                if (current.Equals(desired)) continue;
                AddChange(plan, asset, rule, PlatformCatalog.DefaultPlatform, kind, current, desired, FindingCodes.DefaultMismatch, true);
            }
        }

        private static void PlanSampleScope(AssetPlan plan, AudioAssetInfo asset, OptimizationRule rule, SettingsPolicy policy,
            string platform, SampleSettingsValues current, string code)
        {
            if (policy == null || current == null) return;

            if (policy.disallowedCompression != null && policy.disallowedCompression.Contains(current.compressionFormat))
            {
                SettingValue wanted;
                bool canFix = policy.TryGetSampleLevel(SettingKind.CompressionFormat, out wanted) && !wanted.Equals(current.Get(SettingKind.CompressionFormat));
                plan.Findings.Add(new Finding
                {
                    Code = FindingCodes.DisallowedCompression, Severity = Severity.Error, AssetPath = asset.Path,
                    Title = "Disallowed compression format (" + platform + ")",
                    Detected = platform + " uses " + current.compressionFormat + ", which rule '" + rule.ruleName + "' disallows.",
                    WhyItMatters = "The profile forbids this format, for example because it is unsupported or undesirable on the target platform.",
                    Setting = SettingKind.CompressionFormat.ToString(),
                    ProposedSummary = canFix ? "Change to " + wanted.Display + "." : "Enforce an allowed compression format in the rule or change it manually.",
                    SafeToApply = canFix
                });
            }

            foreach (var kind in SampleKinds)
            {
                SettingValue desired;
                if (!policy.TryGetSampleLevel(kind, out desired)) continue;
                if (kind == SettingKind.SampleRateOverride)
                {
                    // The override value is only meaningful when the effective mode is Override Sample Rate.
                    SettingValue mode;
                    var effectiveMode = policy.TryGetSampleLevel(SettingKind.SampleRateSetting, out mode) ? (SampleRateMode)(int)mode.Number : current.sampleRateSetting;
                    if (effectiveMode != SampleRateMode.OverrideSampleRate) continue;
                }
                var cur = current.Get(kind);
                if (cur.Equals(desired)) continue;
                AddChange(plan, asset, rule, platform, kind, cur, desired, code, true);
            }
        }

        private static void PlanPlatform(AssetPlan plan, AudioAssetInfo asset, OptimizationRule rule, PlatformPolicy pp)
        {
            var state = asset.FindPlatform(pp.platform);
            bool has = state != null && state.hasOverride;

            if (pp.enableOverride && !has)
            {
                AddChange(plan, asset, rule, pp.platform, SettingKind.PlatformOverride, SettingValue.FromBool(false), SettingValue.FromBool(true),
                    FindingCodes.PlatformOverrideState, true);
            }
            else if (!pp.enableOverride && has)
            {
                AddChange(plan, asset, rule, pp.platform, SettingKind.PlatformOverride, SettingValue.FromBool(true), SettingValue.FromBool(false),
                    FindingCodes.PlatformOverrideState, false);
                return;
            }
            if (!pp.enableOverride) return;

            // A missing override starts as a copy of Default, so compare against that.
            var baseValues = has ? state.settings : asset.Default;
            PlanSampleScope(plan, asset, rule, pp.policy, pp.platform, baseValues, FindingCodes.PlatformMismatch);
        }

        private static void AddChange(AssetPlan plan, AudioAssetInfo asset, OptimizationRule rule, string platform, SettingKind kind,
            SettingValue current, SettingValue desired, string code, bool safe)
        {
            var change = new ProposedChange
            {
                AssetPath = asset.Path, Platform = platform, Kind = kind, Current = current, Proposed = desired,
                RuleName = rule.ruleName, Severity = rule.mismatchSeverity, SafeToAutoApply = safe,
                Reason = "Rule '" + rule.ruleName + "' (" + rule.category + ") expects " + kind + " = " + desired.Display + "."
            };
            plan.Changes.Add(change);
            plan.Findings.Add(new Finding
            {
                Code = code, Severity = rule.mismatchSeverity, AssetPath = asset.Path,
                Title = kind + " differs from policy (" + platform + ")",
                Detected = kind + " is " + current.Display + " but rule '" + rule.ruleName + "' expects " + desired.Display + ".",
                WhyItMatters = "Assets in the '" + rule.category + "' category are meant to share this setting; the difference may increase memory use, load time or CPU cost depending on the platform. The size of the effect cannot be calculated without profiling.",
                Setting = kind.ToString(),
                ProposedSummary = "Change " + kind + " from " + current.Display + " to " + desired.Display + " on " + platform + ".",
                SafeToApply = safe, Change = change
            });
        }
    }

    /// <summary>Builds a full <see cref="OptimizationPlan"/> from scanned assets and rules.</summary>
    public static class PlanBuilder
    {
        /// <summary>Minimum number of assets in a category before consistency checks run.</summary>
        public const int MinimumCategorySize = 3;

        /// <summary>Selects rules, plans every asset and adds category consistency findings.</summary>
        /// <param name="assets">Scanned assets.</param>
        /// <param name="rules">Profile rules.</param>
        /// <returns>The plan.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public static OptimizationPlan Build(IEnumerable<AudioAssetInfo> assets, IReadOnlyList<OptimizationRule> rules)
        {
            if (assets == null) throw new ArgumentNullException("assets");
            if (rules == null) throw new ArgumentNullException("rules");
            var result = new OptimizationPlan();
            foreach (var asset in assets)
                result.Assets.Add(ChangePlanner.Plan(asset, RuleMatcher.Select(rules, asset)));
            AddConsistencyFindings(result);
            return result;
        }

        private static void AddConsistencyFindings(OptimizationPlan plan)
        {
            var groups = new Dictionary<string, List<AssetPlan>>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in plan.Assets)
            {
                if (a.Selection.Selected == null || !string.IsNullOrEmpty(a.Asset.ReadError)) continue;
                List<AssetPlan> list;
                if (!groups.TryGetValue(a.Category, out list)) groups[a.Category] = list = new List<AssetPlan>();
                list.Add(a);
            }
            foreach (var kv in groups)
            {
                if (kv.Value.Count < MinimumCategorySize) continue;
                var counts = new Dictionary<CompressionKind, int>();
                foreach (var a in kv.Value)
                {
                    int n; counts.TryGetValue(a.Asset.Default.compressionFormat, out n); counts[a.Asset.Default.compressionFormat] = n + 1;
                }
                CompressionKind majority = default(CompressionKind); int best = 0;
                foreach (var c in counts) if (c.Value > best) { best = c.Value; majority = c.Key; }
                if (best * 2 <= kv.Value.Count) continue; // no strict majority, nothing to compare against
                foreach (var a in kv.Value)
                {
                    if (a.Asset.Default.compressionFormat == majority) continue;
                    a.Findings.Add(new Finding
                    {
                        Code = FindingCodes.InconsistentCategory, Severity = Severity.Info, AssetPath = a.Asset.Path,
                        Title = "Differs from the rest of '" + kv.Key + "'",
                        Detected = "Default compression is " + a.Asset.Default.compressionFormat + " while " + best + " of " + kv.Value.Count + " assets in '" + kv.Key + "' use " + majority + ".",
                        WhyItMatters = "Mixed formats inside one category are often accidental and make runtime cost harder to predict. It may also be intentional.",
                        Setting = SettingKind.CompressionFormat.ToString(),
                        ProposedSummary = "Review manually; no change is proposed because the rule does not enforce a format."
                    });
                }
            }
        }
    }
}
