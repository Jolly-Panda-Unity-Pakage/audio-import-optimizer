// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Profiles;

namespace JP.AudioImportOptimizer.Core.Matching
{
    /// <summary>Wildcard matching with * (any run) and ? (any one character), case-insensitive.</summary>
    public static class GlobMatcher
    {
        /// <summary>Tests a text against a wildcard pattern.</summary>
        /// <param name="pattern">Pattern with * and ? wildcards.</param>
        /// <param name="text">Text to test.</param>
        /// <returns>True when the whole text matches.</returns>
        public static bool IsMatch(string pattern, string text)
        {
            if (pattern == null || text == null) return false;
            int p = 0, t = 0, star = -1, mark = 0;
            while (t < text.Length)
            {
                if (p < pattern.Length && (pattern[p] == '?' || char.ToLowerInvariant(pattern[p]) == char.ToLowerInvariant(text[t]))) { p++; t++; }
                else if (p < pattern.Length && pattern[p] == '*') { star = p++; mark = t; }
                else if (star >= 0) { p = star + 1; t = ++mark; }
                else return false;
            }
            while (p < pattern.Length && pattern[p] == '*') p++;
            return p == pattern.Length;
        }
    }

    /// <summary>A successful rule match with human-readable reasons.</summary>
    public sealed class RuleMatch
    {
        /// <summary>The matching rule.</summary>
        public OptimizationRule Rule;

        /// <summary>Index of the rule in the profile (used as the last tie-breaker).</summary>
        public int Index;

        /// <summary>Number of criteria the rule defines.</summary>
        public int Specificity;

        /// <summary>One sentence per satisfied criterion.</summary>
        public List<string> Reasons = new List<string>();
    }

    /// <summary>The outcome of selecting a rule for one asset.</summary>
    public sealed class RuleSelection
    {
        /// <summary>The winning match, or null when no rule matched.</summary>
        public RuleMatch Selected;

        /// <summary>All matching rules ordered from best to worst.</summary>
        public List<RuleMatch> Candidates = new List<RuleMatch>();

        /// <summary>True when the two best candidates tie on priority and specificity (list order decided).</summary>
        public bool HasConflict;

        /// <summary>Explains why the selected rule won; empty when nothing matched.</summary>
        public string Explanation = string.Empty;
    }

    /// <summary>
    /// Matches assets against rules. Precedence: higher priority, then more criteria, then earlier list position.
    /// </summary>
    public static class RuleMatcher
    {
        /// <summary>Tests one rule against one asset.</summary>
        /// <param name="rule">The rule.</param>
        /// <param name="asset">The asset.</param>
        /// <param name="index">Rule index in the profile.</param>
        /// <returns>The match, or null if the rule is disabled or does not match.</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public static RuleMatch Match(OptimizationRule rule, AudioAssetInfo asset, int index)
        {
            if (rule == null) throw new ArgumentNullException("rule");
            if (asset == null) throw new ArgumentNullException("asset");
            if (!rule.enabled) return null;

            var m = new RuleMatch { Rule = rule, Index = index, Specificity = rule.CountCriteria() };
            string path = (asset.Path ?? string.Empty).Replace('\\', '/');
            string file = System.IO.Path.GetFileName(path);

            if (Has(rule.folders))
            {
                string hit = null;
                foreach (var f in rule.folders)
                {
                    if (string.IsNullOrWhiteSpace(f)) continue;
                    string folder = f.Trim().Replace('\\', '/').TrimEnd('/');
                    if (path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)) { hit = folder; break; }
                }
                if (hit == null) return null;
                m.Reasons.Add("Folder '" + hit + "' contains the asset.");
            }
            if (Has(rule.namePatterns))
            {
                string hit = null;
                foreach (var n in rule.namePatterns) if (!string.IsNullOrWhiteSpace(n) && GlobMatcher.IsMatch(n.Trim(), file)) { hit = n.Trim(); break; }
                if (hit == null) return null;
                m.Reasons.Add("File name '" + file + "' matches pattern '" + hit + "'.");
            }
            if (Has(rule.extensions))
            {
                string ext = asset.Extension, hit = null;
                foreach (var e in rule.extensions) if (!string.IsNullOrWhiteSpace(e) && string.Equals(e.Trim(), ext, StringComparison.OrdinalIgnoreCase)) { hit = e.Trim(); break; }
                if (hit == null) return null;
                m.Reasons.Add("Extension " + ext + " is allowed.");
            }
            if (Has(rule.labels))
            {
                string hit = null;
                foreach (var l in rule.labels)
                {
                    if (string.IsNullOrWhiteSpace(l) || asset.Labels == null) continue;
                    foreach (var al in asset.Labels) if (string.Equals(al, l.Trim(), StringComparison.OrdinalIgnoreCase)) { hit = l.Trim(); break; }
                    if (hit != null) break;
                }
                if (hit == null) return null;
                m.Reasons.Add("Asset has label '" + hit + "'.");
            }
            if (rule.minFileSizeKb > 0 || rule.maxFileSizeKb > 0)
            {
                if (asset.FileSizeBytes < 0) return null;
                double kb = asset.FileSizeBytes / 1024.0;
                if (rule.minFileSizeKb > 0 && kb < rule.minFileSizeKb) return null;
                if (rule.maxFileSizeKb > 0 && kb > rule.maxFileSizeKb) return null;
                m.Reasons.Add("File size " + kb.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " KB is inside the configured range.");
            }
            if (m.Specificity == 0) m.Reasons.Add("The rule has no criteria and matches every asset.");
            return m;
        }

        /// <summary>Selects the winning rule for an asset.</summary>
        /// <param name="rules">Profile rules in list order.</param>
        /// <param name="asset">The asset.</param>
        /// <returns>The selection; <see cref="RuleSelection.Selected"/> is null when nothing matched.</returns>
        public static RuleSelection Select(IReadOnlyList<OptimizationRule> rules, AudioAssetInfo asset)
        {
            var result = new RuleSelection();
            if (rules == null) return result;
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] == null) continue;
                var m = Match(rules[i], asset, i);
                if (m != null) result.Candidates.Add(m);
            }
            result.Candidates.Sort(Compare);
            if (result.Candidates.Count == 0) return result;

            result.Selected = result.Candidates[0];
            if (result.Candidates.Count > 1)
            {
                var a = result.Candidates[0]; var b = result.Candidates[1];
                result.HasConflict = a.Rule.priority == b.Rule.priority && a.Specificity == b.Specificity;
                string why = a.Rule.priority != b.Rule.priority ? "higher priority (" + a.Rule.priority + " vs " + b.Rule.priority + ")"
                    : a.Specificity != b.Specificity ? "more specific (" + a.Specificity + " vs " + b.Specificity + " criteria)"
                    : "listed first (priority and specificity tie)";
                result.Explanation = "'" + a.Rule.ruleName + "' won over '" + b.Rule.ruleName + "': " + why + ".";
            }
            else result.Explanation = "'" + result.Selected.Rule.ruleName + "' is the only matching rule.";
            return result;
        }

        private static int Compare(RuleMatch a, RuleMatch b)
        {
            int c = b.Rule.priority.CompareTo(a.Rule.priority);
            if (c != 0) return c;
            c = b.Specificity.CompareTo(a.Specificity);
            return c != 0 ? c : a.Index.CompareTo(b.Index);
        }

        private static bool Has(List<string> list)
        {
            if (list == null) return false;
            foreach (var s in list) if (!string.IsNullOrWhiteSpace(s)) return true;
            return false;
        }
    }
}
