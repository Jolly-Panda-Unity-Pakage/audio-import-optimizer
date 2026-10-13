// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;

namespace JP.AudioImportOptimizer.Core.Snapshots
{
    /// <summary>One asset's captured importer state.</summary>
    [Serializable]
    public sealed class SnapshotEntry
    {
        /// <summary>Project-relative path at capture time.</summary>
        public string assetPath;
        /// <summary>GUID at capture time; used to find the asset if it moved.</summary>
        public string guid;
        /// <summary>AudioImporter.forceToMono.</summary>
        public bool forceToMono;
        /// <summary>AudioImporter.loadInBackground.</summary>
        public bool loadInBackground;
        /// <summary>AudioImporter.ambisonic.</summary>
        public bool ambisonic;
        /// <summary>AudioImporter.preloadAudioData.</summary>
        public bool preloadAudioData;
        /// <summary>Default sample settings.</summary>
        public SampleSettingsValues defaultSettings = new SampleSettingsValues();
        /// <summary>Platform override states for the platforms the package manages.</summary>
        public List<PlatformState> platforms = new List<PlatformState>();

        /// <summary>Captures the current state of an asset.</summary>
        /// <param name="asset">A successfully read asset.</param>
        /// <returns>A deep copy suitable for serialization.</returns>
        /// <exception cref="ArgumentNullException">Thrown when asset is null.</exception>
        public static SnapshotEntry FromAsset(AudioAssetInfo asset)
        {
            if (asset == null) throw new ArgumentNullException("asset");
            var e = new SnapshotEntry
            {
                assetPath = asset.Path, guid = asset.Guid, forceToMono = asset.ForceToMono, loadInBackground = asset.LoadInBackground,
                ambisonic = asset.Ambisonic, preloadAudioData = asset.PreloadAudioData, defaultSettings = asset.Default.Clone()
            };
            foreach (var p in asset.Platforms)
                e.platforms.Add(new PlatformState { platform = p.platform, hasOverride = p.hasOverride, settings = p.settings != null ? p.settings.Clone() : null });
            return e;
        }

        /// <summary>Checks whether an asset's current state already equals this entry (for the platforms the entry lists).</summary>
        /// <param name="asset">The current asset state.</param>
        /// <returns>True when restoring would change nothing.</returns>
        /// <exception cref="ArgumentNullException">Thrown when asset is null.</exception>
        public bool MatchesAsset(AudioAssetInfo asset)
        {
            if (asset == null) throw new ArgumentNullException("asset");
            if (forceToMono != asset.ForceToMono || loadInBackground != asset.LoadInBackground
                || ambisonic != asset.Ambisonic || preloadAudioData != asset.PreloadAudioData) return false;
            if (!SameSample(defaultSettings, asset.Default)) return false;
            foreach (var p in platforms)
            {
                var cur = asset.FindPlatform(p.platform);
                bool curHas = cur != null && cur.hasOverride;
                if (curHas != p.hasOverride) return false;
                if (p.hasOverride && !SameSample(p.settings, cur.settings)) return false;
            }
            return true;
        }

        private static bool SameSample(SampleSettingsValues a, SampleSettingsValues b)
        {
            if (a == null || b == null) return a == b;
            return a.loadType == b.loadType && a.compressionFormat == b.compressionFormat
                && Math.Abs(a.quality - b.quality) < 0.0005f && a.sampleRateSetting == b.sampleRateSetting
                && a.sampleRateOverride == b.sampleRateOverride;
        }
    }

    /// <summary>A recoverable record of importer settings taken before a batch apply.</summary>
    [Serializable]
    public sealed class SettingsSnapshot
    {
        /// <summary>Schema version written by this package.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Schema version of this file.</summary>
        public int schemaVersion = CurrentSchemaVersion;
        /// <summary>Unique id (also used in the file name).</summary>
        public string id;
        /// <summary>Optional label shown in the restore list.</summary>
        public string label;
        /// <summary>UTC creation time, ISO 8601.</summary>
        public string createdUtc;
        /// <summary>Package version that wrote the file.</summary>
        public string packageVersion;
        /// <summary>Unity version that wrote the file.</summary>
        public string unityVersion;
        /// <summary>Captured assets.</summary>
        public List<SnapshotEntry> entries = new List<SnapshotEntry>();
    }

    /// <summary>Validates a snapshot before any restore is attempted.</summary>
    public static class SnapshotValidator
    {
        /// <summary>Checks structure, schema version and value ranges.</summary>
        /// <param name="snapshot">The snapshot to check; null yields an error.</param>
        /// <returns>Error and warning messages. An empty list means the snapshot is structurally valid.</returns>
        public static List<string> Validate(SettingsSnapshot snapshot)
        {
            var problems = new List<string>();
            if (snapshot == null) { problems.Add("Error: the snapshot could not be read."); return problems; }
            if (snapshot.schemaVersion < 1) problems.Add("Error: invalid schema version " + snapshot.schemaVersion + ".");
            if (snapshot.schemaVersion > SettingsSnapshot.CurrentSchemaVersion)
                problems.Add("Error: the snapshot uses schema " + snapshot.schemaVersion + " which is newer than supported (" + SettingsSnapshot.CurrentSchemaVersion + "). Update the package.");
            if (string.IsNullOrWhiteSpace(snapshot.id)) problems.Add("Error: the snapshot has no id.");
            if (snapshot.entries == null || snapshot.entries.Count == 0) { problems.Add("Error: the snapshot contains no assets."); return problems; }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < snapshot.entries.Count; i++)
            {
                var e = snapshot.entries[i];
                if (e == null || string.IsNullOrWhiteSpace(e.assetPath)) { problems.Add("Error: entry #" + (i + 1) + " has no asset path."); continue; }
                if (!seen.Add(e.assetPath)) problems.Add("Error: asset '" + e.assetPath + "' appears twice.");
                if (e.defaultSettings == null) { problems.Add("Error: '" + e.assetPath + "' has no default settings."); continue; }
                CheckSettings(e.assetPath, "Default", e.defaultSettings, problems);
                if (e.platforms == null) continue;
                foreach (var p in e.platforms)
                {
                    if (p == null || string.IsNullOrWhiteSpace(p.platform)) { problems.Add("Error: '" + e.assetPath + "' has a platform entry without a name."); continue; }
                    if (p.hasOverride && p.settings == null) problems.Add("Error: '" + e.assetPath + "' platform " + p.platform + " has an override without values.");
                    else if (p.hasOverride) CheckSettings(e.assetPath, p.platform, p.settings, problems);
                }
            }
            return problems;
        }

        private static void CheckSettings(string path, string scope, SampleSettingsValues s, List<string> problems)
        {
            if (s.quality < 0f || s.quality > 1f) problems.Add("Error: '" + path + "' " + scope + " quality is out of range.");
            if (!Enum.IsDefined(typeof(LoadMode), s.loadType)) problems.Add("Error: '" + path + "' " + scope + " has an unknown load type.");
            if (!Enum.IsDefined(typeof(CompressionKind), s.compressionFormat)) problems.Add("Error: '" + path + "' " + scope + " has an unknown compression format.");
            if (!Enum.IsDefined(typeof(SampleRateMode), s.sampleRateSetting)) problems.Add("Error: '" + path + "' " + scope + " has an unknown sample rate setting.");
        }
    }
}
