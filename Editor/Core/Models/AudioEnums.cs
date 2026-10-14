// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

namespace JP.AudioImportOptimizer.Core.Models
{
    /// <summary>Importance of a finding produced by the analysis.</summary>
    public enum Severity
    {
        /// <summary>Informational only; no action is required.</summary>
        Info = 0,

        /// <summary>The asset deviates from a profile policy.</summary>
        Warning = 1,

        /// <summary>The asset is broken or violates a policy that must be fixed.</summary>
        Error = 2
    }

    /// <summary>Importer settings the package can read, compare, change, snapshot and restore.</summary>
    public enum SettingKind
    {
        /// <summary>AudioImporter.forceToMono.</summary>
        ForceToMono = 0,

        /// <summary>AudioImporter.loadInBackground.</summary>
        LoadInBackground = 1,

        /// <summary>AudioImporter.ambisonic.</summary>
        Ambisonic = 2,

        /// <summary>
        /// AudioImporterSampleSettings.preloadAudioData.
        /// This setting is platform-specific.
        /// </summary>
        PreloadAudioData = 3,

        /// <summary>AudioImporterSampleSettings.loadType.</summary>
        LoadType = 4,

        /// <summary>AudioImporterSampleSettings.compressionFormat.</summary>
        CompressionFormat = 5,

        /// <summary>AudioImporterSampleSettings.quality (0..1).</summary>
        Quality = 6,

        /// <summary>AudioImporterSampleSettings.sampleRateSetting.</summary>
        SampleRateSetting = 7,

        /// <summary>AudioImporterSampleSettings.sampleRateOverride (Hz).</summary>
        SampleRateOverride = 8,

        /// <summary>Whether a platform override block exists (enabled) or is inherited from Default.</summary>
        PlatformOverride = 9
    }

    /// <summary>Mirror of UnityEngine.AudioClipLoadType kept here so Core stays free of importer types.</summary>
    public enum LoadMode
    {
        /// <summary>Decompress the clip when it is loaded.</summary>
        DecompressOnLoad = 0,

        /// <summary>Keep the clip compressed in memory.</summary>
        CompressedInMemory = 1,

        /// <summary>Stream the clip from disk.</summary>
        Streaming = 2
    }

    /// <summary>Mirror of UnityEngine.AudioCompressionFormat.</summary>
    public enum CompressionKind
    {
        /// <summary>Uncompressed PCM.</summary>
        PCM = 0,

        /// <summary>Ogg Vorbis.</summary>
        Vorbis = 1,

        /// <summary>ADPCM.</summary>
        ADPCM = 2,

        /// <summary>MP3.</summary>
        MP3 = 3,

        /// <summary>VAG (PlayStation).</summary>
        VAG = 4,

        /// <summary>HEVAG (PlayStation).</summary>
        HEVAG = 5,

        /// <summary>XMA (Xbox).</summary>
        XMA = 6,

        /// <summary>AAC.</summary>
        AAC = 7,

        /// <summary>GCADPCM (Nintendo).</summary>
        GCADPCM = 8,

        /// <summary>ATRAC9 (PlayStation).</summary>
        ATRAC9 = 9
    }

    /// <summary>Mirror of UnityEngine.AudioSampleRateSetting.</summary>
    public enum SampleRateMode
    {
        /// <summary>Keep the source sample rate.</summary>
        PreserveSampleRate = 0,

        /// <summary>Let Unity pick the lowest rate that preserves the content.</summary>
        OptimizeSampleRate = 1,

        /// <summary>Use an explicit sample rate.</summary>
        OverrideSampleRate = 2
    }
}
