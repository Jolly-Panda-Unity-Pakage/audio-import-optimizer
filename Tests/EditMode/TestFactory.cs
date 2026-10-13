// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Profiles;

namespace JP.AudioImportOptimizer.Tests.EditMode
{
    /// <summary>Builds small in-memory assets and rules for pure-logic tests.</summary>
    internal static class TestFactory
    {
        public static AudioAssetInfo Asset(string path, long sizeKb = 100, CompressionKind compression = CompressionKind.PCM)
        {
            var a = new AudioAssetInfo { Path = path, FileSizeBytes = sizeKb * 1024 };
            a.Default.compressionFormat = compression;
            a.Default.loadType = LoadMode.DecompressOnLoad;
            a.Platforms.Add(new PlatformState { platform = "Android", hasOverride = false });
            return a;
        }

        public static OptimizationRule Rule(string name, int priority = 0, string folder = null, string extension = null)
        {
            var r = new OptimizationRule { ruleName = name, priority = priority, category = name };
            if (folder != null) r.folders.Add(folder);
            if (extension != null) r.extensions.Add(extension);
            return r;
        }
    }
}
