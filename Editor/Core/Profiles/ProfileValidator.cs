// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;

namespace JP.AudioImportOptimizer.Core.Profiles
{
    /// <summary>A problem found in a profile.</summary>
    public sealed class ValidationIssue
    {
        /// <summary>How serious the problem is.</summary>
        public Severity Severity;

        /// <summary>Name of the rule involved, or empty for profile-level issues.</summary>
        public string RuleName;

        /// <summary>What is wrong and how to fix it.</summary>
        public string Message;

        /// <summary>Creates an issue.</summary>
        /// <param name="severity">Severity.</param>
        /// <param name="ruleName">Rule name or empty.</param>
        /// <param name="message">Message.</param>
        public ValidationIssue(Severity severity, string ruleName, string message)
        {
            Severity = severity;
            RuleName = ruleName ?? string.Empty;
            Message = message;
        }
    }

    /// <summary>Validates a list of rules without touching any asset.</summary>
    public static class ProfileValidator
    {
        /// <summary>Validates rules and returns every issue found.</summary>
        /// <param name="rules">The rules to check; null is treated as empty.</param>
        /// <returns>The issues, possibly empty.</returns>
        public static List<ValidationIssue> Validate(IReadOnlyList<OptimizationRule> rules)
        {
            var issues = new List<ValidationIssue>();
            if (rules == null || rules.Count == 0)
            {
                issues.Add(new ValidationIssue(Severity.Warning, "", "The profile has no rules. Add at least one rule before scanning."));
                return issues;
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rules.Count; i++)
            {
                var r = rules[i];
                if (r == null) { issues.Add(new ValidationIssue(Severity.Error, "", "Rule #" + (i + 1) + " is empty. Remove it.")); continue; }
                if (!r.enabled) continue;

                string name = string.IsNullOrWhiteSpace(r.ruleName) ? "(rule #" + (i + 1) + ")" : r.ruleName;
                if (string.IsNullOrWhiteSpace(r.ruleName)) issues.Add(new ValidationIssue(Severity.Error, name, "The rule has no name. Give it a unique name."));
                else if (!names.Add(r.ruleName)) issues.Add(new ValidationIssue(Severity.Warning, name, "Another rule uses the same name. Names should be unique so reports are unambiguous."));

                if (r.CountCriteria() == 0)
                    issues.Add(new ValidationIssue(Severity.Info, name, "The rule has no matching criteria and matches every audio asset. Use a low priority if it is meant as a fallback."));
                if (r.minFileSizeKb < 0 || r.maxFileSizeKb < 0)
                    issues.Add(new ValidationIssue(Severity.Error, name, "File size limits cannot be negative."));
                if (r.maxFileSizeKb > 0 && r.minFileSizeKb > r.maxFileSizeKb)
                    issues.Add(new ValidationIssue(Severity.Error, name, "Minimum file size is larger than maximum file size, so the rule can never match."));
                if (r.extensions != null)
                    foreach (var e in r.extensions)
                        if (!string.IsNullOrWhiteSpace(e) && !e.Trim().StartsWith(".", StringComparison.Ordinal))
                            issues.Add(new ValidationIssue(Severity.Warning, name, "Extension '" + e + "' should start with a dot (for example .wav)."));

                ValidatePolicy(r.defaultPolicy, name, "Default", issues);
                if (r.defaultPolicy != null && !r.defaultPolicy.HasAnyEnforcement() && (r.platformPolicies == null || r.platformPolicies.Count == 0))
                    issues.Add(new ValidationIssue(Severity.Info, name, "The rule enforces no settings; it only classifies assets."));

                if (r.platformPolicies != null)
                {
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var p in r.platformPolicies)
                    {
                        if (p == null || string.IsNullOrWhiteSpace(p.platform)) { issues.Add(new ValidationIssue(Severity.Error, name, "A platform policy has no platform name.")); continue; }
                        if (!seen.Add(p.platform)) issues.Add(new ValidationIssue(Severity.Error, name, "Platform '" + p.platform + "' is configured twice."));
                        if (!PlatformCatalog.IsKnown(p.platform))
                            issues.Add(new ValidationIssue(Severity.Warning, name, "Platform '" + p.platform + "' is not in the verified list (" + string.Join(", ", PlatformCatalog.KnownPlatforms) + "). It may be ignored or rejected by Unity."));
                        if (p.enableOverride) ValidatePolicy(p.policy, name, p.platform, issues);
                    }
                }
            }
            return issues;
        }

        private static void ValidatePolicy(SettingsPolicy p, string rule, string scope, List<ValidationIssue> issues)
        {
            if (p == null) { issues.Add(new ValidationIssue(Severity.Error, rule, scope + " policy is missing.")); return; }
            if (p.applyQuality && (p.quality < 0f || p.quality > 1f))
                issues.Add(new ValidationIssue(Severity.Error, rule, scope + ": quality must be between 0 and 1."));
            if (p.applySampleRateOverride && (p.sampleRateOverrideHz < 1 || p.sampleRateOverrideHz > SettingsPolicy.MaxSampleRateHz))
                issues.Add(new ValidationIssue(Severity.Error, rule, scope + ": sample rate override must be between 1 and " + SettingsPolicy.MaxSampleRateHz + " Hz."));
            if (p.applySampleRateOverride && p.applySampleRateSetting && p.sampleRateSetting != SampleRateMode.OverrideSampleRate)
                issues.Add(new ValidationIssue(Severity.Warning, rule, scope + ": a sample rate override is enforced but the Sample Rate Setting is not Override Sample Rate, so the override value is ignored."));
            if (p.applyCompressionFormat && p.disallowedCompression != null && p.disallowedCompression.Contains(p.compressionFormat))
                issues.Add(new ValidationIssue(Severity.Error, rule, scope + ": the enforced compression format " + p.compressionFormat + " is also in the disallowed list."));
            if (p.applyAmbisonic && p.ambisonic && p.applyForceToMono && p.forceToMono)
                issues.Add(new ValidationIssue(Severity.Warning, rule, scope + ": Ambisonic clips should not be forced to mono."));
        }
    }
}
