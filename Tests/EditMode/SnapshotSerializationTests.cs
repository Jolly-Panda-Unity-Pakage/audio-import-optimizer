// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System.IO;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Snapshots;
using JP.AudioImportOptimizer.Editor.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace JP.AudioImportOptimizer.Tests.EditMode
{
    /// <summary>Tests JSON serialization of snapshots with Unity's JsonUtility and the on-disk store.</summary>
    [TestFixture]
    public sealed class SnapshotSerializationTests
    {
        private string _savedPath;

        [TearDown]
        public void TearDown()
        {
            if (_savedPath != null && File.Exists(_savedPath)) File.Delete(_savedPath);
            _savedPath = null;
        }

        private static SettingsSnapshot Sample()
        {
            var snapshot = SnapshotStore.Create("unit test");
            var asset = TestFactory.Asset("Assets/__JP_AIO_Test__/never-exists.wav");
            asset.ForceToMono = true;
            asset.Default.quality = 0.42f;
            asset.Default.compressionFormat = CompressionKind.Vorbis;
            asset.Platforms[0].hasOverride = true;
            asset.Platforms[0].settings = new SampleSettingsValues { compressionFormat = CompressionKind.ADPCM, loadType = LoadMode.Streaming, quality = 0.25f };
            snapshot.entries.Add(SnapshotEntry.FromAsset(asset));
            return snapshot;
        }

        [Test]
        public void JsonRoundTrip_PreservesValuesAndStaysValid()
        {
            var original = Sample();
            var copy = JsonUtility.FromJson<SettingsSnapshot>(JsonUtility.ToJson(original, true));
            Assert.AreEqual(0, SnapshotValidator.Validate(copy).Count);
            Assert.AreEqual(original.id, copy.id);
            Assert.AreEqual(SettingsSnapshot.CurrentSchemaVersion, copy.schemaVersion);
            Assert.IsTrue(copy.entries[0].forceToMono);
            Assert.IsTrue(copy.entries[0].MatchesAsset(AssetFromEntry(original.entries[0])));
            Assert.AreEqual(CompressionKind.ADPCM, copy.entries[0].platforms[0].settings.compressionFormat);
        }

        [Test]
        public void Store_SavesLoadsAndListsWithoutLeavingTempFiles()
        {
            var snapshot = Sample();
            _savedPath = SnapshotStore.Save(snapshot);
            Assert.IsTrue(File.Exists(_savedPath));
            Assert.IsFalse(File.Exists(_savedPath + ".tmp"));
            var loaded = SnapshotStore.Load(snapshot.id);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(snapshot.id, loaded.id);
            Assert.IsTrue(SnapshotStore.List().Exists(i => i.Id == snapshot.id));
        }

        [Test]
        public void Store_RejectsInvalidSnapshotsAndUnsafeIds()
        {
            Assert.Throws<System.InvalidOperationException>(() => SnapshotStore.Save(new SettingsSnapshot { id = "x" }));
            Assert.IsNull(SnapshotStore.Load("../escape"));
            Assert.IsNull(SnapshotStore.Load(string.Empty));
        }

        private static AudioAssetInfo AssetFromEntry(SnapshotEntry e)
        {
            var a = new AudioAssetInfo { Path = e.assetPath, ForceToMono = e.forceToMono, LoadInBackground = e.loadInBackground, Ambisonic = e.ambisonic, Default = e.defaultSettings.Clone() };
            foreach (var p in e.platforms) a.Platforms.Add(new PlatformState { platform = p.platform, hasOverride = p.hasOverride, settings = p.settings != null ? p.settings.Clone() : null });
            return a;
        }
    }
}
