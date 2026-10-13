// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;
using UnityEngine;

namespace JP.AudioImportOptimizer.Core.Profiles
{
    /// <summary>
    /// One matching rule plus the policy applied to matching assets.
    /// All non-empty criteria must match (AND); entries inside one list are alternatives (OR).
    /// </summary>
    [Serializable]
    public sealed class OptimizationRule
    {
        [Header("Identity")]
        [Tooltip("Unique, human-readable rule name shown in the preview and reports.")]
        public string ruleName = "New Rule";

        [Tooltip("Disabled rules are ignored by matching and validation.")]
        public bool enabled = true;

        [Tooltip("Free-form category (for example Music or UI). Used for grouping and consistency checks.")]
        public string category = "Uncategorized";

        [Header("Precedence")]
        [Tooltip("Higher priority wins. Ties are broken by the number of criteria (more specific wins), then by list order.")]
        public int priority;

        [Tooltip("Severity of findings caused by a mismatch with this rule's policy.")]
        public Severity mismatchSeverity = Severity.Warning;

        [Header("Matching criteria (leave a list empty to ignore it)")]
        [Tooltip("Project-relative folders, for example Assets/Audio/Music. Matches the folder and everything below it (case-insensitive).")]
        public List<string> folders = new List<string>();

        [Tooltip("File name patterns using * and ? wildcards, matched case-insensitively against the file name including extension, for example bgm_*.")]
        public List<string> namePatterns = new List<string>();

        [Tooltip("File extensions including the dot, for example .wav. Case-insensitive.")]
        public List<string> extensions = new List<string>();

        [Tooltip("Unity asset labels. The asset needs at least one of them.")]
        public List<string> labels = new List<string>();

        [Min(0)]
        [Tooltip("Minimum source file size in KB. 0 = no minimum. File size is a cheap stand-in for clip length; it is not the clip duration.")]
        public int minFileSizeKb;

        [Min(0)]
        [Tooltip("Maximum source file size in KB. 0 = no maximum.")]
        public int maxFileSizeKb;

        [Header("Policy")]
        [Tooltip("Settings enforced on the Default sample settings.")]
        public SettingsPolicy defaultPolicy = new SettingsPolicy();

        [Tooltip("Optional per-platform override policies.")]
        public List<PlatformPolicy> platformPolicies = new List<PlatformPolicy>();

        /// <summary>Counts the non-empty matching criteria (the rule's specificity).</summary>
        /// <returns>A number between 0 and 5.</returns>
        public int CountCriteria()
        {
            int n = 0;
            if (HasItems(folders)) n++;
            if (HasItems(namePatterns)) n++;
            if (HasItems(extensions)) n++;
            if (HasItems(labels)) n++;
            if (minFileSizeKb > 0 || maxFileSizeKb > 0) n++;
            return n;
        }

        private static bool HasItems(List<string> list)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++) if (!string.IsNullOrWhiteSpace(list[i])) return true;
            return false;
        }
    }
}
