// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;
using UnityEngine;

namespace JP.AudioImportOptimizer.Core.Profiles
{
    /// <summary>
    /// Desired importer settings. A setting is only enforced when its matching "apply" toggle is on;
    /// everything else is left exactly as the asset currently has it.
    /// </summary>
    [Serializable]
    public sealed class SettingsPolicy
    {
        /// <summary>Highest sample rate Unity accepts for an override, used for validation.</summary>
        public const int MaxSampleRateHz = 192000;

        [Header("Asset-level settings")]
        [Tooltip("Enforce the Force To Mono value. Off = leave the asset unchanged.")]
        public bool applyForceToMono;
        [Tooltip("Desired Force To Mono. Halves the channel count of stereo clips.")]
        public bool forceToMono;

        [Tooltip("Enforce the Load In Background value. Off = leave the asset unchanged.")]
        public bool applyLoadInBackground;
        [Tooltip("Desired Load In Background. Loads the clip on a separate thread so scene loading is not blocked.")]
        public bool loadInBackground;

        [Tooltip("Enforce the Ambisonic value. Off = leave the asset unchanged.")]
        public bool applyAmbisonic;
        [Tooltip("Desired Ambisonic flag. Only enable for real ambisonic source files.")]
        public bool ambisonic;


        [Header("Sample settings")]
        [Tooltip("Enforce the Load Type. Off = leave the asset unchanged.")]
        public bool applyLoadType;
        [Tooltip("Desired Load Type. Streaming suits long music; Decompress On Load suits very short effects.")]
        public LoadMode loadType = LoadMode.CompressedInMemory;

        [Tooltip("Enforce the Compression Format. Off = leave the asset unchanged.")]
        public bool applyCompressionFormat;
        [Tooltip("Desired Compression Format. Availability depends on the platform.")]
        public CompressionKind compressionFormat = CompressionKind.Vorbis;

        [Tooltip("Enforce the Quality value. Off = leave the asset unchanged.")]
        public bool applyQuality;
        [Range(0f, 1f)]
        [Tooltip("Desired quality from 0 (smallest) to 1 (best). Ignored by lossless formats.")]
        public float quality = 0.7f;

        [Tooltip("Enforce the Sample Rate Setting. Off = leave the asset unchanged.")]
        public bool applySampleRateSetting;
        [Tooltip("Desired Sample Rate Setting.")]
        public SampleRateMode sampleRateSetting = SampleRateMode.PreserveSampleRate;

        [Tooltip("Enforce the Sample Rate Override. Only compared when the effective Sample Rate Setting is Override Sample Rate.")]
        public bool applySampleRateOverride;
        [Min(1)]
        [Tooltip("Desired sample rate in Hz when the setting is Override Sample Rate. Typical values: 22050, 44100, 48000.")]
        public int sampleRateOverrideHz = 44100;

        [Tooltip("Enforce Preload Audio Data. Unity stores it per platform. Off = leave the asset unchanged.")]
        public bool applyPreloadAudioData;
        [Tooltip("Desired Preload Audio Data. When off, scripts must call AudioClip.LoadAudioData before playing.")]
        public bool preloadAudioData = true;

        [Header("Disallowed formats")]
        [Tooltip("Compression formats that must not be used. An asset using one of them is reported as an Error.")]
        public List<CompressionKind> disallowedCompression = new List<CompressionKind>();

        /// <summary>Returns true when at least one setting or disallow list entry is enforced.</summary>
        /// <returns>True when the policy does something.</returns>
        public bool HasAnyEnforcement()
        {
            return applyForceToMono || applyLoadInBackground || applyAmbisonic || applyPreloadAudioData
                || applyLoadType || applyCompressionFormat || applyQuality || applySampleRateSetting
                || applySampleRateOverride || (disallowedCompression != null && disallowedCompression.Count > 0);
        }

        /// <summary>Returns the desired value of an asset-level setting if it is enforced.</summary>
        /// <param name="kind">ForceToMono, LoadInBackground or Ambisonic.</param>
        /// <param name="value">The desired value when enforced.</param>
        /// <returns>True when the setting is enforced.</returns>
        public bool TryGetAssetLevel(SettingKind kind, out SettingValue value)
        {
            switch (kind)
            {
                case SettingKind.ForceToMono: value = SettingValue.FromBool(forceToMono); return applyForceToMono;
                case SettingKind.LoadInBackground: value = SettingValue.FromBool(loadInBackground); return applyLoadInBackground;
                case SettingKind.Ambisonic: value = SettingValue.FromBool(ambisonic); return applyAmbisonic;
                default: value = default(SettingValue); return false;
            }
        }

        /// <summary>Returns the desired value of a sample-level setting if it is enforced.</summary>
        /// <param name="kind">LoadType, CompressionFormat, Quality, SampleRateSetting, SampleRateOverride or PreloadAudioData.</param>
        /// <param name="value">The desired value when enforced.</param>
        /// <returns>True when the setting is enforced.</returns>
        public bool TryGetSampleLevel(SettingKind kind, out SettingValue value)
        {
            var probe = new SampleSettingsValues
            {
                loadType = loadType,
                compressionFormat = compressionFormat,
                quality = quality,
                sampleRateSetting = sampleRateSetting,
                sampleRateOverride = (uint)Math.Max(1, sampleRateOverrideHz),
                preloadAudioData = preloadAudioData
            };
            switch (kind)
            {
                case SettingKind.LoadType: value = probe.Get(kind); return applyLoadType;
                case SettingKind.CompressionFormat: value = probe.Get(kind); return applyCompressionFormat;
                case SettingKind.Quality: value = probe.Get(kind); return applyQuality;
                case SettingKind.SampleRateSetting: value = probe.Get(kind); return applySampleRateSetting;
                case SettingKind.SampleRateOverride: value = probe.Get(kind); return applySampleRateOverride;
                case SettingKind.PreloadAudioData: value = probe.Get(kind); return applyPreloadAudioData;
                default: value = default(SettingValue); return false;
            }
        }
    }

    /// <summary>Policy for one platform override block.</summary>
    [Serializable]
    public sealed class PlatformPolicy
    {
        /// <summary>Importer platform name (see <see cref="PlatformCatalog"/>).</summary>
        [Tooltip("Importer platform name, for example Standalone, iPhone, Android or WebGL.")]
        public string platform = "Android";

        [Tooltip("On = the asset must have an explicit override for this platform (it is created from Default if missing). Off = the asset must inherit Default (an existing override is removed after confirmation).")]
        public bool enableOverride = true;

        [Tooltip("Settings enforced on the override block. Ignored when Enable Override is off.")]
        public SettingsPolicy policy = new SettingsPolicy();
    }

    /// <summary>Known importer platform names. Other names are accepted but flagged as unverified.</summary>
    public static class PlatformCatalog
    {
        /// <summary>Name used in findings and changes for the Default sample settings.</summary>
        public const string DefaultPlatform = "Default";

        /// <summary>Platform names verified to work with AudioImporter override APIs.</summary>
        public static readonly string[] KnownPlatforms = { "Standalone", "iPhone", "Android", "WebGL" };

        /// <summary>Checks whether a platform name is one of <see cref="KnownPlatforms"/>.</summary>
        /// <param name="platform">Platform name.</param>
        /// <returns>True when known.</returns>
        public static bool IsKnown(string platform)
        {
            return Array.IndexOf(KnownPlatforms, platform) >= 0;
        }
    }
}
