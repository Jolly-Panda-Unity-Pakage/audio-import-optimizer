// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Matching;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Profiles;

namespace JP.AudioImportOptimizer.Core.Planning
{
    /// <summary>Stable identifiers of the built-in findings.</summary>
    public static class FindingCodes
    {
        /// <summary>A Default setting differs from the matched rule's policy.</summary>
        public const string DefaultMismatch = "AIO001";
        /// <summary>A disallowed compression format is in use.</summary>
        public const string DisallowedCompression = "AIO002";
        /// <summary>The importer could not be read.</summary>
        public const string ImporterReadFailed = "AIO003";
        /// <summary>No rule matches the asset.</summary>
        public const string NoRuleMatched = "AIO004";
        /// <summary>Two rules tie on priority and specificity.</summary>
        public const string RuleConflict = "AIO005";
        /// <summary>The asset differs from the majority of its category.</summary>
        public const string InconsistentCategory = "AIO006";
        /// <summary>A platform override differs from the policy.</summary>
        public const string PlatformMismatch = "AIO007";
        /// <summary>A platform override must be created or removed.</summary>
        public const string PlatformOverrideState = "AIO008";
    }

    /// <summary>One concrete importer change the user can accept or exclude.</summary>
    public sealed class ProposedChange
    {
        /// <summary>Project-relative asset path.</summary>
        public string AssetPath;
        /// <summary>"Default" or an importer platform name.</summary>
        public string Platform = PlatformCatalog.DefaultPlatform;
        /// <summary>Which setting changes.</summary>
        public SettingKind Kind;
        /// <summary>Current value.</summary>
        public SettingValue Current;
        /// <summary>Proposed value.</summary>
        public SettingValue Proposed;
        /// <summary>Name of the rule that requested the change.</summary>
        public string RuleName;
        /// <summary>Why the change is proposed.</summary>
        public string Reason;
        /// <summary>Severity of the underlying finding.</summary>
        public Severity Severity;
        /// <summary>True when the change can be applied without extra caution (it never discards an override).</summary>
        public bool SafeToAutoApply = true;
        /// <summary>Every importer change triggers a reimport of the asset.</summary>
        public bool TriggersReimport = true;
        /// <summary>User toggle; excluded changes are never applied.</summary>
        public bool Included = true;
    }

    /// <summary>An explained observation about an asset.</summary>
    public sealed class Finding
    {
        /// <summary>Stable code, see <see cref="FindingCodes"/>.</summary>
        public string Code;
        /// <summary>Severity.</summary>
        public Severity Severity;
        /// <summary>Project-relative asset path.</summary>
        public string AssetPath;
        /// <summary>Short title.</summary>
        public string Title;
        /// <summary>What was detected.</summary>
        public string Detected;
        /// <summary>Why it matters.</summary>
        public string WhyItMatters;
        /// <summary>The setting involved, or empty.</summary>
        public string Setting = string.Empty;
        /// <summary>The proposed change in words, or empty when manual action is needed.</summary>
        public string ProposedSummary = string.Empty;
        /// <summary>True when the proposed change is safe to apply automatically.</summary>
        public bool SafeToApply;
        /// <summary>The attached change, or null for informational findings.</summary>
        public ProposedChange Change;
    }

    /// <summary>Analysis result for one asset.</summary>
    public sealed class AssetPlan
    {
        /// <summary>The inspected asset.</summary>
        public AudioAssetInfo Asset;
        /// <summary>Rule selection and explanation.</summary>
        public RuleSelection Selection = new RuleSelection();
        /// <summary>Findings for this asset.</summary>
        public List<Finding> Findings = new List<Finding>();
        /// <summary>Changes proposed for this asset.</summary>
        public List<ProposedChange> Changes = new List<ProposedChange>();
        /// <summary>User toggle; an excluded asset is skipped entirely.</summary>
        public bool Excluded;

        /// <summary>Category of the selected rule, or "Unmatched".</summary>
        public string Category { get { return Selection.Selected != null ? Selection.Selected.Rule.category : "Unmatched"; } }

        /// <summary>Highest severity among the findings.</summary>
        public Severity WorstSeverity
        {
            get { var s = Severity.Info; foreach (var f in Findings) if (f.Severity > s) s = f.Severity; return s; }
        }

        /// <summary>Number of changes that would be applied right now.</summary>
        public int PendingChangeCount
        {
            get { if (Excluded) return 0; int n = 0; foreach (var c in Changes) if (c.Included) n++; return n; }
        }
    }

    /// <summary>The full dry-run result for a scan.</summary>
    public sealed class OptimizationPlan
    {
        /// <summary>Per-asset results in scan order.</summary>
        public List<AssetPlan> Assets = new List<AssetPlan>();

        /// <summary>Total number of findings.</summary>
        public int FindingCount { get { int n = 0; foreach (var a in Assets) n += a.Findings.Count; return n; } }

        /// <summary>Counts findings of one severity.</summary>
        /// <param name="severity">Severity to count.</param>
        /// <returns>The count.</returns>
        public int CountFindings(Severity severity)
        {
            int n = 0;
            foreach (var a in Assets) foreach (var f in a.Findings) if (f.Severity == severity) n++;
            return n;
        }

        /// <summary>Number of assets that currently have at least one pending change.</summary>
        public int AssetsWithPendingChanges { get { int n = 0; foreach (var a in Assets) if (a.PendingChangeCount > 0) n++; return n; } }

        /// <summary>Total number of pending changes.</summary>
        public int PendingChangeCount { get { int n = 0; foreach (var a in Assets) n += a.PendingChangeCount; return n; } }
    }
}
