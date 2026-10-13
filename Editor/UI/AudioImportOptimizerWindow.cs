// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Planning;
using JP.AudioImportOptimizer.Core.Profiles;
using JP.AudioImportOptimizer.Core.Reporting;
using JP.AudioImportOptimizer.Editor.Infrastructure;
using JP.AudioImportOptimizer.Editor.Profiles;
using JP.AudioImportOptimizer.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace JP.AudioImportOptimizer.Editor.UI
{
    /// <summary>
    /// The main Audio Import Optimizer window (Tools > JP > Audio Import Optimizer).
    /// Scanning and analysis are read-only; importer settings change only after the user confirms Apply or Restore.
    /// </summary>
    public sealed partial class AudioImportOptimizerWindow : EditorWindow
    {
        private enum Tab { Overview, Scan, Profile, Results, Changes, Apply, Reports, Help }

        private static readonly string[] TabLabels = { "Overview", "Scan", "Profile", "Results", "Changes", "Apply & Restore", "Reports", "Help" };
        private const float RowHeight = 20f;
        private const double ScanBudgetMilliseconds = 10.0; // Per editor tick, keeps the UI responsive during scans.

        // Persisted across domain reloads (window serialization).
        [SerializeField] private Tab _tab;
        [SerializeField] private OptimizationProfile _profile;
        [SerializeField] private string _includeText = "Assets";
        [SerializeField] private string _excludeText = string.Empty;
        [SerializeField] private string _extensionText = string.Empty;
        [SerializeField] private bool _includePackages;

        // Runtime-only state; rebuilt by scanning again.
        private ScanJob _job;
        private OptimizationPlan _plan;
        private List<AudioAssetInfo> _assets = new List<AudioAssetInfo>();
        private DateTime _lastScanUtc;
        private bool _lastScanIncomplete;
        private string _lastScope = string.Empty;
        private int _changeVersionAtScan;
        private string _status = string.Empty;
        private MessageType _statusType = MessageType.None;
        private BatchResult _lastBatch;
        private string _pendingBatchStatus;
        private MessageType _pendingBatchType;
        private Vector2 _pageScroll;
        private GUIStyle _clip;
        private GUIStyle _wrapLabel;

        /// <summary>Opens the window.</summary>
        [MenuItem("Tools/JP/Audio Import Optimizer")]
        public static void Open()
        {
            var w = GetWindow<AudioImportOptimizerWindow>("Audio Import Optimizer");
            w.minSize = new Vector2(640f, 420f);
            w.Show();
        }

        private void OnEnable()
        {
            // Subscribed here and removed in OnDisable so Enter Play Mode without Domain Reload never leaves duplicate handlers.
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            if (_job != null) _job.Cancel();
            _job = null;
        }

        private void OnEditorUpdate()
        {
            if (_job == null) return;
            if (!_job.IsDone) _job.Step(ScanBudgetMilliseconds);
            if (_job.IsDone) FinishScan();
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            _tab = (Tab)GUILayout.Toolbar((int)_tab, TabLabels, GUILayout.Height(24f));
            DrawStatus();
            switch (_tab)
            {
                case Tab.Overview: DrawPage(DrawOverview); break;
                case Tab.Scan: DrawPage(DrawScan); break;
                case Tab.Profile: DrawPage(DrawProfile); break;
                case Tab.Results: DrawResults(); break;
                case Tab.Changes: DrawChanges(); break;
                case Tab.Apply: DrawPage(DrawApplyAndRestore); break;
                case Tab.Reports: DrawPage(DrawReports); break;
                case Tab.Help: DrawPage(DrawHelp); break;
            }
        }

        private void EnsureStyles()
        {
            if (_clip != null) return;
            _clip = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Clip };
            _wrapLabel = new GUIStyle(EditorStyles.label) { wordWrap = true };
        }

        private void DrawPage(Action body)
        {
            _pageScroll = EditorGUILayout.BeginScrollView(_pageScroll);
            body();
            EditorGUILayout.EndScrollView();
        }

        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, _statusType);
            if (_plan != null && AudioAssetChangeWatcher.ChangeVersion != _changeVersionAtScan)
                EditorGUILayout.HelpBox("Audio assets changed after the last scan. Results may be outdated; scan again before applying.", MessageType.Warning);
        }

        private void SetStatus(string message, MessageType type) { _status = message; _statusType = type; }

        // ---------- scanning ----------

        private ScanOptions BuildScanOptions()
        {
            var o = new ScanOptions { includePackages = _includePackages };
            o.includeFolders.AddRange(SplitLines(_includeText));
            o.excludeFolders.AddRange(SplitLines(_excludeText));
            foreach (var e in _extensionText.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)) o.extensions.Add(e.StartsWith(".") ? e : "." + e);
            return o;
        }

        private static IEnumerable<string> SplitLines(string text)
        {
            foreach (var line in (text ?? string.Empty).Split('\n')) if (!string.IsNullOrWhiteSpace(line)) yield return line.Trim();
        }

        private void StartScan()
        {
            if (_job != null && !_job.IsDone) return;
            var options = BuildScanOptions();
            _lastScope = string.Join(", ", options.includeFolders) + (options.includePackages ? ", Packages" : string.Empty);
            _job = new ScanJob(options);
            SetStatus(_job.TotalCount + " audio asset(s) found. Reading importer settings…", MessageType.Info);
            if (_job.IsDone) FinishScan();
        }

        private void FinishScan()
        {
            var job = _job;
            _job = null;
            if (job == null) return;
            _assets = job.Results;
            _lastScanUtc = DateTime.UtcNow;
            _lastScanIncomplete = job.WasCancelled;
            _changeVersionAtScan = AudioAssetChangeWatcher.ChangeVersion;
            RebuildPlan();
            string diag = job.Diagnostics.Count > 0 ? " " + string.Join(" ", job.Diagnostics) : string.Empty;
            if (job.WasCancelled) SetStatus("Scan cancelled after " + _assets.Count + " asset(s). Results are incomplete." + diag, MessageType.Warning);
            else if (_assets.Count == 0) SetStatus("No audio assets were found in the configured scope." + diag, MessageType.Warning);
            else SetStatus("Scan complete: " + _assets.Count + " asset(s) read. No import settings were modified." + diag, MessageType.Info);

            if (_pendingBatchStatus != null)
            {
                // Keep the result of the Apply that triggered this refresh scan visible.
                _status = _pendingBatchStatus + "\n" + _status;
                if (_pendingBatchType != MessageType.Info) _statusType = _pendingBatchType;
                _pendingBatchStatus = null;
            }
        }

        private void RebuildPlan()
        {
            ResetViewCaches();
            if (_profile == null) { _plan = null; return; }
            try { _plan = PlanBuilder.Build(_assets, _profile.Rules); }
            catch (Exception ex)
            {
                _plan = null;
                SetStatus("The analysis failed: " + ex.Message, MessageType.Error);
                UnityEngine.Debug.LogException(ex);
            }
        }

        // ---------- overview ----------

        private void DrawOverview()
        {
            EditorGUILayout.LabelField("Project overview", EditorStyles.boldLabel);
            if (_assets.Count == 0)
            {
                EditorGUILayout.HelpBox("No scan results yet. Choose a profile, then press Scan. Scanning only reads importer settings and never modifies assets.", MessageType.Info);
            }
            else
            {
                int matched = 0;
                if (_plan != null) foreach (var a in _plan.Assets) if (a.Selection.Selected != null) matched++;
                Stat("Total audio assets", _assets.Count.ToString());
                if (_plan != null)
                {
                    Stat("Assets matching a rule", matched.ToString());
                    Stat("Warnings", _plan.CountFindings(Severity.Warning).ToString());
                    Stat("Errors", _plan.CountFindings(Severity.Error).ToString());
                    Stat("Pending changes", _plan.PendingChangeCount + " in " + _plan.AssetsWithPendingChanges + " asset(s)");
                }
                else EditorGUILayout.HelpBox("Select a profile in the Profile tab to analyze the scanned assets.", MessageType.Warning);
                Stat("Last scan", _lastScanUtc.ToLocalTime().ToString("g") + (_lastScanIncomplete ? " (incomplete)" : string.Empty));
            }
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan now", GUILayout.Height(28f))) { _tab = Tab.Scan; StartScan(); }
                if (GUILayout.Button("Review changes", GUILayout.Height(28f))) _tab = Tab.Changes;
            }
        }

        private static void Stat(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(180f));
                EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            }
        }

        // ---------- scan tab ----------

        private void DrawScan()
        {
            EditorGUILayout.LabelField("Scan configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("A scan lists audio assets and reads their importer settings. It does not load clips, reimport, or modify anything.", MessageType.None);
            EditorGUILayout.LabelField(new GUIContent("Folders to scan (one per line)", "Project-relative folders such as Assets/Audio. Missing folders are reported and skipped."));
            _includeText = EditorGUILayout.TextArea(_includeText, GUILayout.MinHeight(40f));
            EditorGUILayout.LabelField(new GUIContent("Folders to exclude (one per line)", "Assets inside these folders (and their subfolders) are ignored."));
            _excludeText = EditorGUILayout.TextArea(_excludeText, GUILayout.MinHeight(30f));
            _extensionText = EditorGUILayout.TextField(new GUIContent("Extensions (optional)", "Comma separated, for example .wav, .ogg. Empty = every audio format Unity imports as an AudioClip."), _extensionText);
            _includePackages = EditorGUILayout.Toggle(new GUIContent("Include Packages", "Also scan the Packages folder. Package assets are often read-only, so applying changes there can fail."), _includePackages);
            EditorGUILayout.Space();

            if (_job != null && !_job.IsDone)
            {
                var r = GUILayoutUtility.GetRect(1f, 20f, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(r, _job.Progress, _job.Results.Count + " / " + _job.TotalCount);
                if (GUILayout.Button("Cancel scan")) _job.Cancel();
            }
            else if (GUILayout.Button("Scan", GUILayout.Height(28f))) StartScan();
        }

        // ---------- profile tab ----------

        private void DrawProfile()
        {
            EditorGUILayout.LabelField("Optimization profile", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _profile = (OptimizationProfile)EditorGUILayout.ObjectField(new GUIContent("Profile", "The profile whose rules are used for analysis. Selecting a profile never changes assets."), _profile, typeof(OptimizationProfile), false);
            if (EditorGUI.EndChangeCheck() && _assets.Count > 0) RebuildPlan();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Create starter profile…", "Creates a new profile asset with example rules. An existing file is never overwritten; a unique name is used instead."))) CreateStarterProfile();
                using (new EditorGUI.DisabledScope(_profile == null))
                {
                    if (GUILayout.Button("Edit in Inspector")) { Selection.activeObject = _profile; EditorGUIUtility.PingObject(_profile); }
                    if (GUILayout.Button(new GUIContent("Re-run analysis", "Re-evaluates the already scanned assets against the profile without scanning again.")) && _assets.Count > 0) RebuildPlan();
                }
            }
            if (_profile == null)
            {
                EditorGUILayout.HelpBox("No profile selected. Create a starter profile or assign an existing one. Example rules use folders such as Assets/Audio/Music; edit them to match your project.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rules (" + _profile.Rules.Count + ")", EditorStyles.boldLabel);
            foreach (var rule in _profile.Rules)
            {
                if (rule == null) continue;
                EditorGUILayout.LabelField((rule.enabled ? string.Empty : "(disabled) ") + rule.ruleName,
                    "category: " + rule.category + "  priority: " + rule.priority + "  criteria: " + rule.CountCriteria());
            }
            EditorGUILayout.Space();
            foreach (var issue in ProfileValidator.Validate(_profile.Rules))
            {
                var type = issue.Severity == Severity.Error ? MessageType.Error : issue.Severity == Severity.Warning ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.HelpBox((string.IsNullOrEmpty(issue.RuleName) ? string.Empty : "[" + issue.RuleName + "] ") + issue.Message, type);
            }
        }

        private void CreateStarterProfile()
        {
            string chosen = EditorUtility.SaveFilePanelInProject("Create optimization profile", "AudioOptimizationProfile", "asset", "Choose where to save the new profile.");
            if (string.IsNullOrEmpty(chosen)) return;
            string path = AssetDatabase.GenerateUniqueAssetPath(chosen); // Never overwrite an existing profile.
            var profile = CreateInstance<OptimizationProfile>();
            profile.AppendRules(DefaultRules.Create());
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            _profile = profile;
            if (_assets.Count > 0) RebuildPlan();
            SetStatus("Created profile '" + path + "'. Review its rules before applying anything.", MessageType.Info);
        }

        // ---------- help ----------

        private void DrawHelp()
        {
            EditorGUILayout.LabelField(PackageMetadata.DisplayName + " " + PackageMetadata.GetVersion(), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Workflow: 1) pick or create a profile, 2) scan, 3) review Results and Changes (dry run), 4) Apply, 5) restore from a snapshot if needed. " +
                "Scans and dry runs never modify import settings. Apply reimports each changed asset; Undo cannot revert importer settings, so a JSON snapshot is saved first in " +
                "ProjectSettings/JP/AudioImportOptimizer/Snapshots. Apply is not transactional: if it fails or is cancelled, earlier assets stay changed.", _wrapLabel);
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open documentation"))
                {
                    string folder = PackageMetadata.GetPackageFolder();
                    string index = folder == null ? null : System.IO.Path.Combine(folder, "Documentation~", "index.md");
                    if (index != null && System.IO.File.Exists(index)) EditorUtility.OpenWithDefaultApp(index);
                    else EditorUtility.DisplayDialog("Documentation not found", "The Documentation~ folder was not found next to the installed package. Open index.md from the package source instead.", "OK");
                }
                if (GUILayout.Button("Website")) Application.OpenURL(PackageMetadata.Website);
            }
            EditorGUILayout.LabelField("Author", PackageMetadata.Author);
            EditorGUILayout.LabelField("Contact", PackageMetadata.Contact);
            EditorGUILayout.HelpBox("This tool works fully offline. It sends no data and makes no network requests. The Website button only opens your browser when you press it.", MessageType.None);
        }

        // ---------- shared list helper ----------

        /// <summary>Draws only the visible rows of a fixed-height-row list so thousands of assets stay responsive.</summary>
        private void DrawVirtualList(int count, ref Vector2 scroll, float height, Action<Rect, int> drawRow)
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(height));
            Rect total = GUILayoutUtility.GetRect(1f, Mathf.Max(count, 1) * RowHeight, GUILayout.ExpandWidth(true));
            if (count == 0) GUI.Label(total, "Nothing to show.", EditorStyles.centeredGreyMiniLabel);
            int first = Mathf.Clamp((int)(scroll.y / RowHeight), 0, Mathf.Max(0, count - 1));
            int last = Mathf.Min(count, first + (int)(height / RowHeight) + 2);
            for (int i = first; i < last; i++) drawRow(new Rect(total.x, total.y + i * RowHeight, total.width, RowHeight), i);
            GUILayout.EndScrollView();
        }

        private static GUIContent SeverityIcon(Severity s)
        {
            return EditorGUIUtility.IconContent(s == Severity.Error ? "console.erroricon.sml" : s == Severity.Warning ? "console.warnicon.sml" : "console.infoicon.sml");
        }
    }
}
