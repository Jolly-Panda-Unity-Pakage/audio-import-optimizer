// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Planning;
using UnityEditor;
using UnityEngine;

namespace JP.AudioImportOptimizer.Editor.UI
{
    /// <summary>Results and Changes tabs of the main window.</summary>
    public sealed partial class AudioImportOptimizerWindow
    {
        private enum SortMode { Path, Severity, Category, PendingChanges }
        private enum SeverityFilter { All, WarningsAndErrors, ErrorsOnly }

        private sealed class ChangeRow
        {
            internal AssetPlan Asset;
            internal ProposedChange Change;
        }

        // Results tab state.
        private string _search = string.Empty;
        private SortMode _sort = SortMode.Severity;
        private SeverityFilter _severityFilter = SeverityFilter.All;
        private bool _onlyWithChanges;
        private readonly HashSet<string> _selected = new HashSet<string>(StringComparer.Ordinal);
        private List<AssetPlan> _filtered = new List<AssetPlan>();
        private bool _filteredDirty = true;
        private AssetPlan _focused;
        private Vector2 _resultsScroll;
        private Vector2 _detailScroll;

        // Changes tab state.
        private string _changeSearch = string.Empty;
        private bool _onlySelectedAssets;
        private List<ChangeRow> _changeRows = new List<ChangeRow>();
        private bool _changeRowsDirty = true;
        private Vector2 _changesScroll;

        private void ResetViewCaches()
        {
            _filteredDirty = true;
            _changeRowsDirty = true;
            _focused = null;
            _selected.Clear();
        }

        // ---------- results ----------

        private void DrawResults()
        {
            if (_plan == null)
            {
                EditorGUILayout.HelpBox(_assets.Count == 0
                    ? "No results yet. Run a scan from the Scan tab."
                    : "Select a profile in the Profile tab to analyze the scanned assets.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
                _severityFilter = (SeverityFilter)EditorGUILayout.EnumPopup(_severityFilter, EditorStyles.toolbarPopup, GUILayout.Width(130f));
                _sort = (SortMode)EditorGUILayout.EnumPopup(_sort, EditorStyles.toolbarPopup, GUILayout.Width(110f));
                _onlyWithChanges = GUILayout.Toggle(_onlyWithChanges, new GUIContent("Has changes", "Show only assets with proposed changes."), EditorStyles.toolbarButton, GUILayout.Width(80f));
                if (EditorGUI.EndChangeCheck()) _filteredDirty = true;
            }
            EnsureFiltered();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(_filtered.Count + " of " + _plan.Assets.Count + " assets, " + _selected.Count + " selected", EditorStyles.miniLabel);
                if (GUILayout.Button("Select all shown", EditorStyles.miniButton, GUILayout.Width(100f))) foreach (var a in _filtered) _selected.Add(a.Asset.Path);
                if (GUILayout.Button("Clear selection", EditorStyles.miniButton, GUILayout.Width(100f))) _selected.Clear();
                if (GUILayout.Button(new GUIContent("Exclude selected", "Excluded assets are skipped by Apply."), EditorStyles.miniButton, GUILayout.Width(110f))) SetExcluded(true);
                if (GUILayout.Button("Include selected", EditorStyles.miniButton, GUILayout.Width(100f))) SetExcluded(false);
            }

            float listHeight = Mathf.Max(120f, position.height * 0.42f);
            DrawVirtualList(_filtered.Count, ref _resultsScroll, listHeight, DrawResultRow);
            DrawDetails();
        }

        private void DrawResultRow(Rect row, int index)
        {
            var plan = _filtered[index];
            string path = plan.Asset.Path;
            if (plan == _focused) EditorGUI.DrawRect(row, new Color(0.24f, 0.49f, 0.9f, 0.25f));
            bool sel = _selected.Contains(path);
            bool now = GUI.Toggle(new Rect(row.x + 2f, row.y, 18f, row.height), sel, GUIContent.none);
            if (now != sel) { if (now) _selected.Add(path); else _selected.Remove(path); }

            GUI.Label(new Rect(row.x + 22f, row.y, 20f, row.height), plan.Findings.Count > 0 ? SeverityIcon(plan.WorstSeverity) : GUIContent.none);
            float w = row.width - 46f;
            var pathRect = new Rect(row.x + 44f, row.y, w * 0.5f, row.height);
            var catRect = new Rect(pathRect.xMax, row.y, w * 0.2f, row.height);
            var infoRect = new Rect(catRect.xMax, row.y, w * 0.3f, row.height);
            GUI.Label(pathRect, new GUIContent((plan.Excluded ? "(excluded) " : string.Empty) + path, path), _clip);
            GUI.Label(catRect, plan.Category, _clip);
            GUI.Label(infoRect, plan.PendingChangeCount + " change(s), " + plan.Findings.Count + " finding(s)", _clip);

            if (Event.current.type == EventType.MouseDown && pathRect.Contains(Event.current.mousePosition))
            {
                _focused = plan;
                Event.current.Use();
                Repaint();
            }
        }

        private void DrawDetails()
        {
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            if (_focused == null)
            {
                EditorGUILayout.LabelField("Click an asset to see which rule matched and why, plus every finding.", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField(_focused.Asset.Path, EditorStyles.boldLabel);
                if (GUILayout.Button("Ping asset in Project window", EditorStyles.miniButton, GUILayout.Width(190f)))
                    EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(_focused.Asset.Path)); // Loads the clip only on this explicit click.
                var sel = _focused.Selection;
                if (sel.Selected == null) EditorGUILayout.LabelField("Rule: none matched.", _wrapLabel);
                else
                {
                    EditorGUILayout.LabelField("Rule: " + sel.Selected.Rule.ruleName + " (category " + sel.Selected.Rule.category + ", priority " + sel.Selected.Rule.priority + ")", EditorStyles.boldLabel);
                    foreach (var reason in sel.Selected.Reasons) EditorGUILayout.LabelField("• " + reason, _wrapLabel);
                    if (!string.IsNullOrEmpty(sel.Explanation)) EditorGUILayout.LabelField("Precedence: " + sel.Explanation, _wrapLabel);
                }
                EditorGUILayout.Space();
                foreach (var f in _focused.Findings)
                {
                    var type = f.Severity == Severity.Error ? MessageType.Error : f.Severity == Severity.Warning ? MessageType.Warning : MessageType.Info;
                    EditorGUILayout.HelpBox(
                        "[" + f.Code + "] " + f.Title + "\nDetected: " + f.Detected + "\nWhy it matters: " + f.WhyItMatters +
                        (string.IsNullOrEmpty(f.ProposedSummary) ? string.Empty : "\nProposed: " + f.ProposedSummary) +
                        "\nSafe to apply automatically: " + (f.SafeToApply ? "yes" : "no"), type);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void SetExcluded(bool excluded)
        {
            foreach (var a in _plan.Assets) if (_selected.Contains(a.Asset.Path)) a.Excluded = excluded;
            _changeRowsDirty = true;
        }

        private void EnsureFiltered()
        {
            if (!_filteredDirty) return;
            _filteredDirty = false;
            _filtered = new List<AssetPlan>();
            string q = (_search ?? string.Empty).Trim();
            foreach (var a in _plan.Assets)
            {
                if (q.Length > 0 && a.Asset.Path.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 && a.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (_severityFilter == SeverityFilter.WarningsAndErrors && a.WorstSeverity < Severity.Warning) continue;
                if (_severityFilter == SeverityFilter.ErrorsOnly && a.WorstSeverity < Severity.Error) continue;
                if ((_severityFilter != SeverityFilter.All) && a.Findings.Count == 0) continue;
                if (_onlyWithChanges && a.Changes.Count == 0) continue;
                _filtered.Add(a);
            }
            Comparison<AssetPlan> cmp;
            switch (_sort)
            {
                case SortMode.Severity: cmp = (x, y) => y.WorstSeverity.CompareTo(x.WorstSeverity) != 0 ? y.WorstSeverity.CompareTo(x.WorstSeverity) : string.Compare(x.Asset.Path, y.Asset.Path, StringComparison.OrdinalIgnoreCase); break;
                case SortMode.Category: cmp = (x, y) => string.Compare(x.Category, y.Category, StringComparison.OrdinalIgnoreCase) != 0 ? string.Compare(x.Category, y.Category, StringComparison.OrdinalIgnoreCase) : string.Compare(x.Asset.Path, y.Asset.Path, StringComparison.OrdinalIgnoreCase); break;
                case SortMode.PendingChanges: cmp = (x, y) => y.Changes.Count != x.Changes.Count ? y.Changes.Count.CompareTo(x.Changes.Count) : string.Compare(x.Asset.Path, y.Asset.Path, StringComparison.OrdinalIgnoreCase); break;
                default: cmp = (x, y) => string.Compare(x.Asset.Path, y.Asset.Path, StringComparison.OrdinalIgnoreCase); break;
            }
            _filtered.Sort(cmp);
        }

        // ---------- changes (dry run preview) ----------

        private void DrawChanges()
        {
            if (_plan == null)
            {
                EditorGUILayout.HelpBox("No dry run available. Scan and select a profile first.", MessageType.Info);
                return;
            }
            EnsureChangeRows();
            EditorGUILayout.HelpBox("This is a preview. Nothing has been changed. Untick a change or an asset to exclude it; Apply only writes ticked changes. Every change triggers a reimport of its asset.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _changeSearch = GUILayout.TextField(_changeSearch, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
                _onlySelectedAssets = GUILayout.Toggle(_onlySelectedAssets, new GUIContent("Selected assets only", "Show only assets ticked in the Results tab."), EditorStyles.toolbarButton, GUILayout.Width(130f));
                if (EditorGUI.EndChangeCheck()) _changeRowsDirty = true;
                if (GUILayout.Button("Include all", EditorStyles.toolbarButton, GUILayout.Width(80f))) SetAllIncluded(false);
                if (GUILayout.Button(new GUIContent("Include safe only", "Includes only changes that never discard a platform override."), EditorStyles.toolbarButton, GUILayout.Width(110f))) SetAllIncluded(true);
                if (GUILayout.Button("Exclude all", EditorStyles.toolbarButton, GUILayout.Width(80f))) SetAllExcluded();
            }
            EditorGUILayout.LabelField(_changeRows.Count + " change(s) shown, " + _plan.PendingChangeCount + " pending in total", EditorStyles.miniLabel);

            float height = Mathf.Max(160f, position.height - 150f);
            DrawVirtualList(_changeRows.Count, ref _changesScroll, height, DrawChangeRow);
        }

        private void DrawChangeRow(Rect row, int index)
        {
            var r = _changeRows[index];
            var c = r.Change;
            bool assetIncluded = !r.Asset.Excluded;
            bool a = GUI.Toggle(new Rect(row.x + 2f, row.y, 16f, row.height), assetIncluded, new GUIContent(string.Empty, "Include this asset"));
            if (a != assetIncluded) r.Asset.Excluded = !a;
            using (new EditorGUI.DisabledScope(r.Asset.Excluded))
            {
                c.Included = GUI.Toggle(new Rect(row.x + 20f, row.y, 16f, row.height), c.Included, new GUIContent(string.Empty, "Include this change"));
            }
            float w = row.width - 42f;
            float x = row.x + 40f;
            GUI.Label(new Rect(x, row.y, w * 0.34f, row.height), new GUIContent(c.AssetPath, c.AssetPath + "\n" + c.Reason), _clip);
            GUI.Label(new Rect(x + w * 0.34f, row.y, w * 0.10f, row.height), c.Platform, _clip);
            GUI.Label(new Rect(x + w * 0.44f, row.y, w * 0.16f, row.height), c.Kind.ToString(), _clip);
            GUI.Label(new Rect(x + w * 0.60f, row.y, w * 0.22f, row.height), c.Current.Display + "  →  " + c.Proposed.Display, _clip);
            GUI.Label(new Rect(x + w * 0.82f, row.y, w * 0.18f, row.height),
                new GUIContent((c.SafeToAutoApply ? string.Empty : "(!) ") + c.RuleName, (c.SafeToAutoApply ? string.Empty : "Not auto-safe: removes a platform override. ") + "Reimport: " + (c.TriggersReimport ? "yes" : "no")), _clip);
        }

        private void EnsureChangeRows()
        {
            if (!_changeRowsDirty) return;
            _changeRowsDirty = false;
            _changeRows = new List<ChangeRow>();
            string q = (_changeSearch ?? string.Empty).Trim();
            foreach (var a in _plan.Assets)
            {
                if (_onlySelectedAssets && !_selected.Contains(a.Asset.Path)) continue;
                foreach (var c in a.Changes)
                {
                    if (q.Length > 0 && c.AssetPath.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 && c.Kind.ToString().IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 && c.Platform.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    _changeRows.Add(new ChangeRow { Asset = a, Change = c });
                }
            }
        }

        private void SetAllIncluded(bool safeOnly)
        {
            foreach (var a in _plan.Assets) { a.Excluded = false; foreach (var c in a.Changes) c.Included = !safeOnly || c.SafeToAutoApply; }
        }

        private void SetAllExcluded()
        {
            foreach (var a in _plan.Assets) foreach (var c in a.Changes) c.Included = false;
        }
    }
}
