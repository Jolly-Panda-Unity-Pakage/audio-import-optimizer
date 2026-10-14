// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using System.IO;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Profiles;
using JP.AudioImportOptimizer.Core.Reporting;
using JP.AudioImportOptimizer.Core.Snapshots;
using JP.AudioImportOptimizer.Editor.Infrastructure;
using JP.AudioImportOptimizer.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace JP.AudioImportOptimizer.Editor.UI
{
    /// <summary>Apply, Restore and Reports tabs of the main window.</summary>
    public sealed partial class AudioImportOptimizerWindow
    {
        private bool _applySelectedOnly;
        private List<SnapshotInfo> _snapshots;
        private int _snapshotIndex = -1;

        // ---------- apply and restore ----------

        private void DrawApplyAndRestore()
        {
            EditorGUILayout.LabelField("Apply", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Apply writes the ticked changes to the AudioImporter of each asset and reimports it. This can take time in large projects. " +
                "Unity's Undo cannot reliably revert importer settings, so a JSON snapshot of the original values is saved first. " +
                "Apply is NOT transactional: if an asset fails or you cancel, assets processed earlier stay changed. Changes are also recoverable through version control (.meta files).",
                MessageType.Warning);

            bool canApply = _plan != null && _plan.PendingChangeCount > 0 && (_job == null || _job.IsDone);
            if (_plan == null) EditorGUILayout.LabelField("Scan with a profile first.", EditorStyles.miniLabel);
            else
            {
                EditorGUILayout.LabelField(_plan.PendingChangeCount + " change(s) in " + _plan.AssetsWithPendingChanges + " asset(s) will trigger " + _plan.AssetsWithPendingChanges + " reimport(s).");
                _applySelectedOnly = EditorGUILayout.ToggleLeft(new GUIContent("Only assets selected in the Results tab (" + _selected.Count + ")", "Restrict Apply to the assets ticked in Results."), _applySelectedOnly);
                bool profileErrors = false;
                if (_profile != null) foreach (var i in ProfileValidator.Validate(_profile.Rules)) if (i.Severity == Severity.Error) profileErrors = true;
                if (profileErrors) { EditorGUILayout.HelpBox("The profile has errors. Fix them in the Profile tab before applying.", MessageType.Error); canApply = false; }
                if (_lastScanIncomplete) EditorGUILayout.HelpBox("The last scan was cancelled; the plan is incomplete.", MessageType.Warning);
                if (AudioAssetChangeWatcher.ChangeVersion != _changeVersionAtScan) { EditorGUILayout.HelpBox("Assets changed since the scan. Scan again before applying.", MessageType.Warning); canApply = false; }
            }
            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("Apply ticked changes…", GUILayout.Height(30f))) ConfirmAndApply();
            }

            DrawBatchSummary();
            EditorGUILayout.Space(12f);
            DrawRestore();
        }

        private void ConfirmAndApply()
        {
            ISet<string> only = _applySelectedOnly ? new HashSet<string>(_selected, StringComparer.Ordinal) : null;
            int assets = 0, changes = 0;
            foreach (var a in _plan.Assets)
            {
                if (only != null && !only.Contains(a.Asset.Path)) continue;
                if (a.PendingChangeCount > 0) { assets++; changes += a.PendingChangeCount; }
            }
            if (assets == 0) { SetStatus("Nothing to apply for the current selection.", MessageType.Info); return; }
            if (!EditorUtility.DisplayDialog("Apply import settings?",
                changes + " change(s) in " + assets + " asset(s) will be written and reimported.\n\nA snapshot is saved first. This operation is not transactional and Undo cannot revert it.",
                "Apply", "Cancel")) return;

            try
            {
                _lastBatch = BatchApplier.Apply(_plan, only, "Apply " + DateTime.Now.ToString("g"), ReportProgress);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            SummarizeBatch("Apply", _lastBatch);
            _pendingBatchStatus = _status;
            _pendingBatchType = _statusType;
            StartScan(); // Refresh the plan so it reflects the new importer state.
        }

        private static bool ReportProgress(string title, string info, float progress)
        {
            return EditorUtility.DisplayCancelableProgressBar(title, info, progress);
        }

        private void SummarizeBatch(string verb, BatchResult b)
        {
            if (b.FatalError != null) { SetStatus(verb + " refused: " + b.FatalError, MessageType.Error); return; }
            string text = verb + " finished: " + b.Count(ApplyOutcome.Applied) + " applied, " + b.Count(ApplyOutcome.Unchanged) + " unchanged, " +
                b.Count(ApplyOutcome.Skipped) + " skipped, " + b.Count(ApplyOutcome.Failed) + " failed." + (b.Cancelled ? " Cancelled by user." : string.Empty) +
                (b.SnapshotId != null ? " Snapshot: " + b.SnapshotId + "." : string.Empty);
            SetStatus(text, b.Count(ApplyOutcome.Failed) > 0 || b.Cancelled ? MessageType.Warning : MessageType.Info);
            _snapshots = null;
        }

        private Vector2 _batchScroll;

        private void DrawBatchSummary()
        {
            if (_lastBatch == null) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last operation", EditorStyles.boldLabel);
            _batchScroll = EditorGUILayout.BeginScrollView(_batchScroll, GUILayout.MaxHeight(160f));
            int shown = 0;
            foreach (var r in _lastBatch.Results)
            {
                if (r.Outcome == ApplyOutcome.Unchanged && _lastBatch.Results.Count > 50) continue; // Keep long lists readable.
                if (shown++ >= 200) { EditorGUILayout.LabelField("… more results are in the exported report.", EditorStyles.miniLabel); break; }
                EditorGUILayout.LabelField(r.Outcome + "  " + r.AssetPath, r.Message, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawRestore()
        {
            EditorGUILayout.LabelField("Restore", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "A snapshot stores Force To Mono, Load In Background, Ambisonic, Preload Audio Data, the Default sample settings and the override state of Standalone, iPhone, Android and WebGL. " +
                "Nothing else is captured. Restoring is never automatic; a safety backup of the current state is saved first.", MessageType.None);
            if (_snapshots == null) _snapshots = SnapshotStore.List();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh list", GUILayout.Width(100f))) _snapshots = SnapshotStore.List();
                if (GUILayout.Button("Reveal snapshot folder", GUILayout.Width(160f)))
                {
                    Directory.CreateDirectory(SnapshotStore.Folder);
                    EditorUtility.RevealInFinder(SnapshotStore.Folder);
                }
            }
            if (_snapshots.Count == 0) { EditorGUILayout.LabelField("No snapshots yet. One is created automatically before every Apply.", EditorStyles.miniLabel); return; }

            var names = new string[_snapshots.Count];
            for (int i = 0; i < names.Length; i++) names[i] = FormatCreated(_snapshots[i].CreatedUtc) + "  —  " + _snapshots[i].AssetCount + " asset(s)  —  " + _snapshots[i].Label;
            _snapshotIndex = Mathf.Clamp(_snapshotIndex, 0, names.Length - 1);
            _snapshotIndex = EditorGUILayout.Popup("Snapshot", _snapshotIndex, names);

            var chosen = SnapshotStore.Load(_snapshots[_snapshotIndex].Id);
            var problems = SnapshotValidator.Validate(chosen);
            foreach (var p in problems) EditorGUILayout.HelpBox(p, MessageType.Error);
            using (new EditorGUI.DisabledScope(problems.Count > 0))
            {
                if (GUILayout.Button("Restore this snapshot…", GUILayout.Height(26f)))
                {
                    if (EditorUtility.DisplayDialog("Restore snapshot?",
                        "Importer settings of " + chosen.entries.Count + " asset(s) will be overwritten with the values stored in this snapshot and the assets will be reimported.\n\nA safety backup of the current state is saved first.",
                        "Restore", "Cancel"))
                    {
                        try { _lastBatch = RestoreService.Restore(chosen, ReportProgress); }
                        finally { EditorUtility.ClearProgressBar(); }
                        SummarizeBatch("Restore", _lastBatch);
                    }
                }
            }
        }

        /// <summary>Formats the stored UTC timestamp as local time for the snapshot list.</summary>
        private static string FormatCreated(string isoUtc)
        {
            DateTime parsed;
            if (DateTime.TryParse(isoUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out parsed)) return parsed.ToLocalTime().ToString("g");
            return string.IsNullOrEmpty(isoUtc) ? "(unknown time)" : isoUtc;
        }

        // ---------- reports ----------

        private void DrawReports()
        {
            EditorGUILayout.LabelField("Reports", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Reports contain project-relative asset paths, settings and counts only. They are written where you choose and are never uploaded.", MessageType.None);
            using (new EditorGUI.DisabledScope(_plan == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Export JSON…", GUILayout.Height(26f))) Export("json");
                    if (GUILayout.Button("Export HTML…", GUILayout.Height(26f))) Export("html");
                }
            }
            if (_plan == null) EditorGUILayout.LabelField("Scan with a profile first.", EditorStyles.miniLabel);
        }

        private void Export(string extension)
        {
            string path = EditorUtility.SaveFilePanel("Export report", string.Empty, "AudioImportOptimizerReport", extension);
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var report = new AnalysisReport
                {
                    PackageVersion = PackageMetadata.GetVersion(), UnityVersion = UnityEngine.Application.unityVersion, ScanTimeUtc = _lastScanUtc,
                    Scope = _lastScope, ProfileName = _profile != null ? _profile.name : string.Empty, ScanIncomplete = _lastScanIncomplete, Plan = _plan
                };
                if (_lastBatch != null) report.ApplyResults.AddRange(_lastBatch.Results);
                File.WriteAllText(path, extension == "json" ? ReportWriter.ToJson(report) : ReportWriter.ToHtml(report));
                SetStatus("Report written to " + path, MessageType.Info);
            }
            catch (Exception ex)
            {
                SetStatus("The report could not be written: " + ex.Message, MessageType.Error);
            }
        }
    }
}
