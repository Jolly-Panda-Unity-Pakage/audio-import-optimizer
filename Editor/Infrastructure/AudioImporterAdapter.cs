// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using System.IO;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Planning;
using JP.AudioImportOptimizer.Core.Profiles;
using JP.AudioImportOptimizer.Core.Snapshots;
using UnityEditor;
using UnityEngine;

namespace JP.AudioImportOptimizer.Editor.Infrastructure
{
    /// <summary>
    /// The only class that talks to AudioImporter. Converts between importer state and the pure Core models.
    /// Reading never modifies an asset; writing never reimports (the caller decides when).
    /// </summary>
    public static class AudioImporterAdapter
    {
        /// <summary>Reads the importer state of an asset without loading the clip.</summary>
        /// <param name="assetPath">Project-relative asset path.</param>
        /// <returns>The asset info. <see cref="AudioAssetInfo.ReadError"/> is set when reading failed.</returns>
        public static AudioAssetInfo Read(string assetPath)
        {
            var info = new AudioAssetInfo { Path = assetPath ?? string.Empty };
            try
            {
                if (string.IsNullOrEmpty(assetPath)) throw new ArgumentException("The asset path is empty.");
                info.Guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(info.Guid)) throw new FileNotFoundException("The asset does not exist in the AssetDatabase (deleted or moved).");
                var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (importer == null) throw new InvalidOperationException("The asset has no AudioImporter.");
                ReadInto(importer, info);
            }
            catch (Exception ex)
            {
                info.ReadError = ex.GetType().Name + ": " + ex.Message;
            }
            return info;
        }

        /// <summary>Fills an <see cref="AudioAssetInfo"/> from an importer.</summary>
        /// <param name="importer">The importer.</param>
        /// <param name="info">The info object to fill.</param>
        public static void ReadInto(AudioImporter importer, AudioAssetInfo info)
        {
            info.ForceToMono = importer.forceToMono;
            info.LoadInBackground = importer.loadInBackground;
            info.Ambisonic = importer.ambisonic;
            info.PreloadAudioData = importer.defaultSampleSettings.preloadAudioData;
            info.Default = ToValues(importer.defaultSampleSettings);
            info.Platforms.Clear();
            foreach (var platform in PlatformCatalog.KnownPlatforms)
            {
                var state = new PlatformState { platform = platform, hasOverride = importer.ContainsSampleSettingsOverride(platform) };
                if (state.hasOverride) state.settings = ToValues(importer.GetOverrideSampleSettings(platform));
                info.Platforms.Add(state);
            }
            info.Labels.Clear();
            info.Labels.AddRange(AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(info.Path)));
            info.FileSizeBytes = GetFileSize(info.Path);
        }

        /// <summary>Applies proposed changes to an importer in memory. Does not save or reimport.</summary>
        /// <param name="importer">The importer.</param>
        /// <param name="changes">Changes for this asset only.</param>
        /// <exception cref="ArgumentException">Thrown when a value cannot be represented by this Unity version.</exception>
        public static void ApplyChanges(AudioImporter importer, IEnumerable<ProposedChange> changes)
        {
            var list = new List<ProposedChange>(changes);
            // Platform override creation/removal must happen before values are written into the block.
            list.Sort((a, b) => (a.Kind == SettingKind.PlatformOverride ? 0 : 1).CompareTo(b.Kind == SettingKind.PlatformOverride ? 0 : 1));

            foreach (var c in list)
            {
                if (c.Kind == SettingKind.PlatformOverride)
                {
                    if (c.Proposed.Number > 0.5)
                    {
                        if (!importer.ContainsSampleSettingsOverride(c.Platform)) importer.SetOverrideSampleSettings(c.Platform, importer.defaultSampleSettings);
                    }
                    else importer.ClearSampleSettingOverride(c.Platform);
                    continue;
                }
                if (c.Kind == SettingKind.ForceToMono) { importer.forceToMono = c.Proposed.Number > 0.5; continue; }
                if (c.Kind == SettingKind.LoadInBackground) { importer.loadInBackground = c.Proposed.Number > 0.5; continue; }
                if (c.Kind == SettingKind.Ambisonic) { importer.ambisonic = c.Proposed.Number > 0.5; continue; }

                bool isDefault = c.Platform == PlatformCatalog.DefaultPlatform;
                var values = ToValues(isDefault ? importer.defaultSampleSettings : importer.GetOverrideSampleSettings(c.Platform));
                values.Set(c.Kind, c.Proposed);
                var native = ToNative(values, isDefault ? importer.defaultSampleSettings : importer.GetOverrideSampleSettings(c.Platform));
                if (isDefault) importer.defaultSampleSettings = native;
                else importer.SetOverrideSampleSettings(c.Platform, native);
            }
        }

        /// <summary>Writes a complete snapshot entry back to an importer in memory. Does not save or reimport.</summary>
        /// <param name="importer">The importer.</param>
        /// <param name="entry">The entry to restore.</param>
        /// <exception cref="ArgumentException">Thrown when a value cannot be represented by this Unity version.</exception>
        public static void WriteEntry(AudioImporter importer, SnapshotEntry entry)
        {
            importer.forceToMono = entry.forceToMono;
            importer.loadInBackground = entry.loadInBackground;
            importer.ambisonic = entry.ambisonic;
            importer.defaultSampleSettings = ToNative(entry.defaultSettings, importer.defaultSampleSettings);
            foreach (var p in entry.platforms)
            {
                if (p.hasOverride)
                {
                    var current = importer.ContainsSampleSettingsOverride(p.platform) ? importer.GetOverrideSampleSettings(p.platform) : importer.defaultSampleSettings;
                    importer.SetOverrideSampleSettings(p.platform, ToNative(p.settings, current));
                }
                else if (importer.ContainsSampleSettingsOverride(p.platform)) importer.ClearSampleSettingOverride(p.platform);
            }
        }

        private static SampleSettingsValues ToValues(AudioImporterSampleSettings s)
        {
            return new SampleSettingsValues
            {
                loadType = MapByName<LoadMode>(s.loadType),
                compressionFormat = MapByName<CompressionKind>(s.compressionFormat),
                quality = s.quality,
                sampleRateSetting = MapByName<SampleRateMode>(s.sampleRateSetting),
                sampleRateOverride = s.sampleRateOverride,
                preloadAudioData = s.preloadAudioData
            };
        }

        /// <summary>Builds native settings from values, starting from an existing struct so unknown future fields are preserved.</summary>
        private static AudioImporterSampleSettings ToNative(SampleSettingsValues v,AudioImporterSampleSettings baseline)
        {
            baseline.loadType = MapByName<AudioClipLoadType>(v.loadType);
            baseline.compressionFormat =
                MapByName<AudioCompressionFormat>(v.compressionFormat);
            baseline.quality = v.quality;
            baseline.sampleRateSetting =
                MapByName<AudioSampleRateSetting>(v.sampleRateSetting);
            baseline.sampleRateOverride = v.sampleRateOverride;
            baseline.preloadAudioData = v.preloadAudioData;

            return baseline;
        }

        /// <summary>Maps enums by member name so a missing member in this Unity version produces a clear error instead of a wrong value.</summary>
        private static TTo MapByName<TTo>(object from) where TTo : struct
        {
            TTo result;
            if (!Enum.TryParse<TTo>(from.ToString(), out result))
                throw new ArgumentException("'" + from + "' is not available as " + typeof(TTo).Name + " in this Unity version.");
            return result;
        }

        private static long GetFileSize(string assetPath)
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                var file = new FileInfo(Path.Combine(projectRoot, assetPath));
                return file.Exists ? file.Length : -1;
            }
            catch (Exception)
            {
                return -1; // Size is optional metadata; rules that need it simply do not match.
            }
        }
    }
}
