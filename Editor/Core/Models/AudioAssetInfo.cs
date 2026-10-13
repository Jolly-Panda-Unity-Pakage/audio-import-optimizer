// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;

namespace JP.AudioImportOptimizer.Core.Models
{
    /// <summary>Platform override state of one build target for one asset.</summary>
    [Serializable]
    public sealed class PlatformState
    {
        /// <summary>Importer platform name, for example "Android" or "Standalone".</summary>
        public string platform;

        /// <summary>True when an explicit override exists; false when the asset inherits Default.</summary>
        public bool hasOverride;

        /// <summary>The override values; null when <see cref="hasOverride"/> is false.</summary>
        public SampleSettingsValues settings;
    }

    /// <summary>Everything the analysis needs to know about one audio asset. Contains no Unity object references.</summary>
    public sealed class AudioAssetInfo
    {
        /// <summary>Project-relative asset path using forward slashes.</summary>
        public string Path;

        /// <summary>Asset GUID, empty when unknown.</summary>
        public string Guid = string.Empty;

        /// <summary>Source file size in bytes, -1 when unavailable.</summary>
        public long FileSizeBytes = -1;

        /// <summary>Asset labels (AssetDatabase.GetLabels).</summary>
        public List<string> Labels = new List<string>();

        /// <summary>Null when the importer was read successfully; otherwise a description of the failure.</summary>
        public string ReadError;

        /// <summary>AudioImporter.forceToMono.</summary>
        public bool ForceToMono;

        /// <summary>AudioImporter.loadInBackground.</summary>
        public bool LoadInBackground;

        /// <summary>AudioImporter.ambisonic.</summary>
        public bool Ambisonic;

        /// <summary>AudioImporter.preloadAudioData.</summary>
        public bool PreloadAudioData;

        /// <summary>Default sample settings.</summary>
        public SampleSettingsValues Default = new SampleSettingsValues();

        /// <summary>Per-platform override state for every platform the adapter inspected.</summary>
        public List<PlatformState> Platforms = new List<PlatformState>();

        /// <summary>Lower-case extension including the dot, for example ".wav".</summary>
        public string Extension
        {
            get { return System.IO.Path.GetExtension(Path ?? string.Empty).ToLowerInvariant(); }
        }

        /// <summary>Reads an asset-level (non-sample) setting.</summary>
        /// <param name="kind">One of ForceToMono, LoadInBackground, Ambisonic, PreloadAudioData.</param>
        /// <returns>The current value.</returns>
        /// <exception cref="ArgumentException">Thrown for other kinds.</exception>
        public SettingValue GetAssetLevel(SettingKind kind)
        {
            switch (kind)
            {
                case SettingKind.ForceToMono: return SettingValue.FromBool(ForceToMono);
                case SettingKind.LoadInBackground: return SettingValue.FromBool(LoadInBackground);
                case SettingKind.Ambisonic: return SettingValue.FromBool(Ambisonic);
                case SettingKind.PreloadAudioData: return SettingValue.FromBool(PreloadAudioData);
                default: throw new ArgumentException("Not an asset-level setting: " + kind, "kind");
            }
        }

        /// <summary>Finds the state for a platform.</summary>
        /// <param name="platform">Importer platform name.</param>
        /// <returns>The state, or null if the platform was not inspected.</returns>
        public PlatformState FindPlatform(string platform)
        {
            for (int i = 0; i < Platforms.Count; i++)
            {
                if (string.Equals(Platforms[i].platform, platform, StringComparison.Ordinal)) return Platforms[i];
            }
            return null;
        }
    }
}
