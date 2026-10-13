// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JP.AudioImportOptimizer.Core.Models;
using UnityEditor;

namespace JP.AudioImportOptimizer.Editor.Infrastructure
{
    /// <summary>User-configurable scan scope.</summary>
    [Serializable]
    public sealed class ScanOptions
    {
        /// <summary>Folders to search. Defaults to the whole Assets folder.</summary>
        public List<string> includeFolders = new List<string> { "Assets" };

        /// <summary>Folders skipped even when inside an included folder.</summary>
        public List<string> excludeFolders = new List<string>();

        /// <summary>When true the Packages folder is searched too. Package assets are often read-only, so applying changes there can fail.</summary>
        public bool includePackages;

        /// <summary>Optional extension filter (for example .wav). Empty means every audio asset Unity can import.</summary>
        public List<string> extensions = new List<string>();
    }

    /// <summary>
    /// A time-sliced, cancellable, read-only scan. Call <see cref="Step"/> repeatedly (for example from EditorApplication.update)
    /// until <see cref="IsDone"/> is true. Only importer metadata is read; no clip is loaded and nothing is reimported.
    /// </summary>
    public sealed class ScanJob
    {
        private readonly ScanOptions _options;
        private readonly List<string> _paths = new List<string>();
        private int _index;

        /// <summary>Assets read so far.</summary>
        public readonly List<AudioAssetInfo> Results = new List<AudioAssetInfo>();

        /// <summary>Problems with the scan configuration (missing folders and similar).</summary>
        public readonly List<string> Diagnostics = new List<string>();

        /// <summary>True once every asset has been read or the job was cancelled.</summary>
        public bool IsDone { get; private set; }

        /// <summary>True when <see cref="Cancel"/> was called before completion.</summary>
        public bool WasCancelled { get; private set; }

        /// <summary>Progress between 0 and 1.</summary>
        public float Progress { get { return _paths.Count == 0 ? 1f : (float)_index / _paths.Count; } }

        /// <summary>Total number of assets that will be read.</summary>
        public int TotalCount { get { return _paths.Count; } }

        /// <summary>Starts a scan by enumerating candidate asset paths (cheap index query, no importer access).</summary>
        /// <param name="options">The scope.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public ScanJob(ScanOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            _options = options;
            Enumerate();
            if (_paths.Count == 0) IsDone = true;
        }

        /// <summary>Reads assets until the time budget is used up.</summary>
        /// <param name="budgetMilliseconds">Maximum time to spend in this call.</param>
        public void Step(double budgetMilliseconds)
        {
            if (IsDone) return;
            var clock = Stopwatch.StartNew();
            while (_index < _paths.Count && clock.Elapsed.TotalMilliseconds < budgetMilliseconds)
                Results.Add(AudioImporterAdapter.Read(_paths[_index++]));
            if (_index >= _paths.Count) IsDone = true;
        }

        /// <summary>Stops the scan at the next safe boundary; results read so far are kept.</summary>
        public void Cancel()
        {
            if (IsDone) return;
            WasCancelled = true;
            IsDone = true;
        }

        private void Enumerate()
        {
            var roots = new List<string>();
            foreach (var f in _options.includeFolders)
            {
                if (string.IsNullOrWhiteSpace(f)) continue;
                string folder = f.Trim().Replace('\\', '/').TrimEnd('/');
                if (AssetDatabase.IsValidFolder(folder)) roots.Add(folder);
                else Diagnostics.Add("Folder '" + folder + "' does not exist and was skipped.");
            }
            if (_options.includePackages && !roots.Contains("Packages")) roots.Add("Packages");
            if (roots.Count == 0) { Diagnostics.Add("No valid folder to scan. Add a folder such as 'Assets'."); return; }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", roots.ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !seen.Add(path)) continue;
                if (IsExcluded(path) || !ExtensionAllowed(path)) continue;
                _paths.Add(path);
            }
            _paths.Sort(StringComparer.OrdinalIgnoreCase);
        }

        private bool IsExcluded(string path)
        {
            foreach (var e in _options.excludeFolders)
            {
                if (string.IsNullOrWhiteSpace(e)) continue;
                string folder = e.Trim().Replace('\\', '/').TrimEnd('/');
                if (path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private bool ExtensionAllowed(string path)
        {
            bool any = false;
            string ext = System.IO.Path.GetExtension(path);
            foreach (var e in _options.extensions)
            {
                if (string.IsNullOrWhiteSpace(e)) continue;
                any = true;
                if (string.Equals(e.Trim(), ext, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return !any;
        }
    }

    /// <summary>
    /// Tracks whether audio assets changed after the last scan so the UI can warn that results may be stale.
    /// It only increments a counter; it never touches assets.
    /// </summary>
    public sealed class AudioAssetChangeWatcher : AssetPostprocessor
    {
        /// <summary>Incremented whenever an imported, deleted or moved asset has a common audio extension.</summary>
        public static int ChangeVersion { get; private set; }

        private static readonly string[] AudioExtensions = { ".wav", ".mp3", ".ogg", ".aif", ".aiff", ".flac", ".mod", ".it", ".s3m", ".xm" };

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (AnyAudio(imported) || AnyAudio(deleted) || AnyAudio(moved) || AnyAudio(movedFrom)) ChangeVersion++;
        }

        private static bool AnyAudio(string[] paths)
        {
            foreach (var p in paths)
                foreach (var e in AudioExtensions)
                    if (p.EndsWith(e, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
