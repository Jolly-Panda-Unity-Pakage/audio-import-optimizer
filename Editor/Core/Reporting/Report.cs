// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Planning;

namespace JP.AudioImportOptimizer.Core.Reporting
{
    /// <summary>Result category for one asset in a batch apply.</summary>
    public enum ApplyOutcome
    {
        /// <summary>At least one setting was changed and the asset was reimported.</summary>
        Applied = 0,
        /// <summary>The asset was skipped (excluded, cancelled, moved or deleted).</summary>
        Skipped = 1,
        /// <summary>Applying failed; the message explains why.</summary>
        Failed = 2,
        /// <summary>The effective settings already matched; no reimport was triggered.</summary>
        Unchanged = 3
    }

    /// <summary>Apply result for one asset.</summary>
    public sealed class AssetApplyResult
    {
        /// <summary>Project-relative path.</summary>
        public string AssetPath;
        /// <summary>Outcome.</summary>
        public ApplyOutcome Outcome;
        /// <summary>Explanation, for example the exception message.</summary>
        public string Message = string.Empty;
    }

    /// <summary>Everything a report contains. Only project-relative paths are included.</summary>
    public sealed class AnalysisReport
    {
        /// <summary>Package version.</summary>
        public string PackageVersion = string.Empty;
        /// <summary>Unity version.</summary>
        public string UnityVersion = string.Empty;
        /// <summary>UTC time of the scan.</summary>
        public DateTime ScanTimeUtc = DateTime.UtcNow;
        /// <summary>Human-readable scan scope, for example "Assets/Audio".</summary>
        public string Scope = string.Empty;
        /// <summary>Name of the profile used.</summary>
        public string ProfileName = string.Empty;
        /// <summary>True when the scan was cancelled before completion.</summary>
        public bool ScanIncomplete;
        /// <summary>The dry-run plan.</summary>
        public OptimizationPlan Plan = new OptimizationPlan();
        /// <summary>Apply results, empty if nothing was applied yet.</summary>
        public List<AssetApplyResult> ApplyResults = new List<AssetApplyResult>();

        /// <summary>Counts apply results of one outcome.</summary>
        /// <param name="outcome">Outcome to count.</param>
        /// <returns>The count.</returns>
        public int CountApply(ApplyOutcome outcome)
        {
            int n = 0; foreach (var r in ApplyResults) if (r.Outcome == outcome) n++; return n;
        }
    }

    /// <summary>Serializes reports to JSON and self-contained HTML without any dependency.</summary>
    public static class ReportWriter
    {
        /// <summary>Report format version written into the JSON.</summary>
        public const int FormatVersion = 1;

        /// <summary>Writes the report as indented JSON.</summary>
        /// <param name="report">The report.</param>
        /// <returns>JSON text.</returns>
        /// <exception cref="ArgumentNullException">Thrown when report is null.</exception>
        public static string ToJson(AnalysisReport report)
        {
            if (report == null) throw new ArgumentNullException("report");
            var j = new Json();
            j.Begin();
            j.Prop("formatVersion", FormatVersion);
            j.Prop("packageVersion", report.PackageVersion);
            j.Prop("unityVersion", report.UnityVersion);
            j.Prop("scanTimeUtc", report.ScanTimeUtc.ToString("o", CultureInfo.InvariantCulture));
            j.Prop("scope", report.Scope);
            j.Prop("profile", report.ProfileName);
            j.Prop("scanIncomplete", report.ScanIncomplete);
            j.BeginObj("summary");
            j.Prop("totalAssets", report.Plan.Assets.Count);
            j.Prop("findings", report.Plan.FindingCount);
            j.Prop("infos", report.Plan.CountFindings(Severity.Info));
            j.Prop("warnings", report.Plan.CountFindings(Severity.Warning));
            j.Prop("errors", report.Plan.CountFindings(Severity.Error));
            j.Prop("assetsRequiringChanges", report.Plan.AssetsWithPendingChanges);
            j.Prop("pendingChanges", report.Plan.PendingChangeCount);
            j.Prop("applied", report.CountApply(ApplyOutcome.Applied));
            j.Prop("skipped", report.CountApply(ApplyOutcome.Skipped));
            j.Prop("failed", report.CountApply(ApplyOutcome.Failed));
            j.Prop("unchanged", report.CountApply(ApplyOutcome.Unchanged));
            j.End();
            j.BeginArr("findings");
            foreach (var a in report.Plan.Assets)
                foreach (var f in a.Findings)
                {
                    j.BeginObj(null);
                    j.Prop("code", f.Code); j.Prop("severity", f.Severity.ToString()); j.Prop("asset", f.AssetPath);
                    j.Prop("category", a.Category); j.Prop("rule", a.Selection.Selected != null ? a.Selection.Selected.Rule.ruleName : "");
                    j.Prop("title", f.Title); j.Prop("detected", f.Detected); j.Prop("whyItMatters", f.WhyItMatters);
                    j.Prop("setting", f.Setting); j.Prop("proposed", f.ProposedSummary); j.Prop("safeToApply", f.SafeToApply);
                    if (f.Change != null)
                    {
                        j.Prop("platform", f.Change.Platform); j.Prop("current", f.Change.Current.Display); j.Prop("proposedValue", f.Change.Proposed.Display);
                        j.Prop("triggersReimport", f.Change.TriggersReimport);
                    }
                    j.End();
                }
            j.End();
            j.BeginArr("applyResults");
            foreach (var r in report.ApplyResults)
            {
                j.BeginObj(null); j.Prop("asset", r.AssetPath); j.Prop("outcome", r.Outcome.ToString()); j.Prop("message", r.Message); j.End();
            }
            j.End();
            j.EndAll();
            return j.ToString();
        }

        /// <summary>Writes the report as a standalone HTML page (no scripts, no external resources).</summary>
        /// <param name="report">The report.</param>
        /// <returns>HTML text.</returns>
        /// <exception cref="ArgumentNullException">Thrown when report is null.</exception>
        public static string ToHtml(AnalysisReport report)
        {
            if (report == null) throw new ArgumentNullException("report");
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>Audio Import Optimizer Report</title>");
            sb.Append("<style>body{font-family:sans-serif;margin:24px;color:#222}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ccc;padding:4px 8px;text-align:left;font-size:13px}th{background:#eee}.Error{color:#b00020}.Warning{color:#a15c00}.Info{color:#1a5fb4}</style></head><body>");
            sb.Append("<h1>Audio Import Optimizer Report</h1>");
            sb.Append("<p>Package ").Append(E(report.PackageVersion)).Append(" &middot; Unity ").Append(E(report.UnityVersion))
              .Append(" &middot; ").Append(E(report.ScanTimeUtc.ToString("u", CultureInfo.InvariantCulture)))
              .Append("<br>Scope: ").Append(E(report.Scope)).Append(" &middot; Profile: ").Append(E(report.ProfileName)).Append("</p>");
            if (report.ScanIncomplete) sb.Append("<p class=\"Warning\"><b>The scan was cancelled; results are incomplete.</b></p>");
            sb.Append("<ul><li>Assets examined: ").Append(report.Plan.Assets.Count)
              .Append("</li><li>Findings: ").Append(report.Plan.FindingCount).Append(" (errors ").Append(report.Plan.CountFindings(Severity.Error))
              .Append(", warnings ").Append(report.Plan.CountFindings(Severity.Warning)).Append(", info ").Append(report.Plan.CountFindings(Severity.Info))
              .Append(")</li><li>Assets requiring changes: ").Append(report.Plan.AssetsWithPendingChanges)
              .Append("</li><li>Applied ").Append(report.CountApply(ApplyOutcome.Applied)).Append(", skipped ").Append(report.CountApply(ApplyOutcome.Skipped))
              .Append(", failed ").Append(report.CountApply(ApplyOutcome.Failed)).Append(", unchanged ").Append(report.CountApply(ApplyOutcome.Unchanged)).Append("</li></ul>");
            sb.Append("<h2>Findings</h2><table><tr><th>Severity</th><th>Asset</th><th>Rule</th><th>Detected</th><th>Why it matters</th><th>Proposed</th></tr>");
            foreach (var a in report.Plan.Assets)
                foreach (var f in a.Findings)
                    sb.Append("<tr><td class=\"").Append(f.Severity).Append("\">").Append(f.Severity).Append("</td><td>").Append(E(f.AssetPath)).Append("</td><td>")
                      .Append(E(a.Selection.Selected != null ? a.Selection.Selected.Rule.ruleName : "-")).Append("</td><td>").Append(E(f.Detected)).Append("</td><td>")
                      .Append(E(f.WhyItMatters)).Append("</td><td>").Append(E(f.ProposedSummary)).Append("</td></tr>");
            sb.Append("</table>");
            if (report.ApplyResults.Count > 0)
            {
                sb.Append("<h2>Apply results</h2><table><tr><th>Asset</th><th>Outcome</th><th>Message</th></tr>");
                foreach (var r in report.ApplyResults)
                    sb.Append("<tr><td>").Append(E(r.AssetPath)).Append("</td><td>").Append(r.Outcome).Append("</td><td>").Append(E(r.Message)).Append("</td></tr>");
                sb.Append("</table>");
            }
            sb.Append("<p><small>Generated locally by Audio Import Optimizer. No data was uploaded.</small></p></body></html>");
            return sb.ToString();
        }

        private static string E(string s) { return WebUtility.HtmlEncode(s ?? string.Empty); }

        /// <summary>Minimal JSON writer that handles commas and escaping.</summary>
        private sealed class Json
        {
            private readonly StringBuilder _sb = new StringBuilder();
            private readonly Stack<bool> _first = new Stack<bool>(); // true while the current container is still empty
            private readonly Stack<char> _closers = new Stack<char>();

            internal void Begin() { _sb.Append('{'); _first.Push(true); _closers.Push('}'); }
            internal void BeginObj(string name) { Key(name); _sb.Append('{'); _first.Push(true); _closers.Push('}'); }
            internal void BeginArr(string name) { Key(name); _sb.Append('['); _first.Push(true); _closers.Push(']'); }
            internal void End() { _first.Pop(); _sb.Append('\n').Append(_closers.Pop()); }
            internal void EndAll() { while (_closers.Count > 0) End(); }
            internal void Prop(string name, string v) { Key(name); Str(v); }
            internal void Prop(string name, int v) { Key(name); _sb.Append(v.ToString(CultureInfo.InvariantCulture)); }
            internal void Prop(string name, bool v) { Key(name); _sb.Append(v ? "true" : "false"); }
            public override string ToString() { return _sb.ToString(); }

            private void Key(string name)
            {
                if (_first.Peek()) { _first.Pop(); _first.Push(false); } else _sb.Append(',');
                _sb.Append('\n');
                if (name != null) { Str(name); _sb.Append(": "); }
            }

            private void Str(string v)
            {
                _sb.Append('"');
                foreach (char c in v ?? string.Empty)
                {
                    switch (c)
                    {
                        case '"': _sb.Append("\\\""); break;
                        case '\\': _sb.Append("\\\\"); break;
                        case '\n': _sb.Append("\\n"); break;
                        case '\r': _sb.Append("\\r"); break;
                        case '\t': _sb.Append("\\t"); break;
                        default:
                            if (c < 0x20) _sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else _sb.Append(c);
                            break;
                    }
                }
                _sb.Append('"');
            }
        }
    }
}
