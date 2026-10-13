// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using System.IO;
using JP.AudioImportOptimizer.Core.Matching;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Planning;
using JP.AudioImportOptimizer.Core.Profiles;
using JP.AudioImportOptimizer.Core.Reporting;
using JP.AudioImportOptimizer.Editor.Infrastructure;
using JP.AudioImportOptimizer.Editor.Services;
using NUnit.Framework;
using UnityEditor;

namespace JP.AudioImportOptimizer.Tests.EditMode
{
    /// <summary>
    /// Integration tests against real AudioImporters. They create their own temporary WAV files under
    /// Assets/__JP_AIO_Tests__ and delete them (and any snapshots they wrote) afterwards. Real project audio is never touched.
    /// </summary>
    [TestFixture]
    public sealed class AudioImporterIntegrationTests
    {
        private const string Folder = "Assets/__JP_AIO_Tests__";
        private const string TargetPath = Folder + "/target.wav";
        private const string BystanderPath = Folder + "/bystander.wav";
        private readonly List<string> _snapshotIds = new List<string>();

        [SetUp]
        public void SetUp()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "__JP_AIO_Tests__");
            WriteWav(TargetPath);
            WriteWav(BystanderPath);
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                AssetDatabase.DeleteAsset(Folder);
                foreach (var id in _snapshotIds)
                {
                    string file = Path.Combine(SnapshotStore.Folder, "snapshot-" + id + ".json");
                    if (File.Exists(file)) File.Delete(file);
                }
            }
            finally
            {
                _snapshotIds.Clear();
                AssetDatabase.Refresh();
            }
        }

        private static void WriteWav(string assetPath)
        {
            // 0.1 s of 8 kHz 16-bit mono silence: the smallest valid PCM WAV Unity will import.
            const int sampleRate = 8000, samples = 800, bytes = samples * 2;
            using (var w = new BinaryWriter(File.Create(Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), assetPath))))
            {
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes); w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(sampleRate); w.Write(sampleRate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes); w.Write(new byte[bytes]);
            }
        }

        private static string MetaText(string assetPath)
        {
            return File.ReadAllText(Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), assetPath + ".meta"));
        }

        private static OptimizationPlan PlanFor(OptimizationRule rule, params string[] paths)
        {
            var infos = new List<AudioAssetInfo>();
            foreach (var p in paths) infos.Add(AudioImporterAdapter.Read(p));
            return PlanBuilder.Build(infos, new List<OptimizationRule> { rule });
        }

        private static OptimizationRule MonoRule(bool desired)
        {
            var rule = TestFactory.Rule("Mono", 0, Folder);
            rule.defaultPolicy.applyForceToMono = true;
            rule.defaultPolicy.forceToMono = desired;
            return rule;
        }

        private BatchResult Apply(OptimizationPlan plan)
        {
            var result = BatchApplier.Apply(plan, null, "integration test", null);
            if (result.SnapshotId != null) _snapshotIds.Add(result.SnapshotId);
            return result;
        }

        [Test]
        public void Read_ReturnsImporterStateWithoutError()
        {
            var info = AudioImporterAdapter.Read(TargetPath);
            Assert.IsNull(info.ReadError);
            Assert.IsFalse(string.IsNullOrEmpty(info.Guid));
            Assert.Greater(info.FileSizeBytes, 0L);
            Assert.AreEqual(PlatformCatalog.KnownPlatforms.Length, info.Platforms.Count);
        }

        [Test]
        public void Scan_IsReadOnly()
        {
            string before = MetaText(TargetPath);
            var job = new ScanJob(new ScanOptions { includeFolders = new List<string> { Folder } });
            while (!job.IsDone) job.Step(50);
            Assert.AreEqual(2, job.Results.Count);
            Assert.AreEqual(before, MetaText(TargetPath));
        }

        [Test]
        public void Scan_ReportsMissingFoldersInsteadOfFailing()
        {
            var job = new ScanJob(new ScanOptions { includeFolders = new List<string> { "Assets/__JP_AIO_DoesNotExist__" } });
            Assert.IsTrue(job.IsDone);
            Assert.GreaterOrEqual(job.Diagnostics.Count, 1);
        }

        [Test]
        public void Apply_ChangesOnlyTheTargetAndSecondApplyIsUnchanged()
        {
            var current = AudioImporterAdapter.Read(TargetPath);
            var plan = PlanFor(MonoRule(!current.ForceToMono), TargetPath);
            // Restrict to the target even though the bystander shares the folder.
            var only = new HashSet<string> { TargetPath };
            string bystanderBefore = MetaText(BystanderPath);

            var first = BatchApplier.Apply(plan, only, "integration test", null);
            if (first.SnapshotId != null) _snapshotIds.Add(first.SnapshotId);
            Assert.IsNull(first.FatalError);
            Assert.AreEqual(1, first.Count(ApplyOutcome.Applied));
            Assert.AreEqual(!current.ForceToMono, AudioImporterAdapter.Read(TargetPath).ForceToMono);
            Assert.AreEqual(bystanderBefore, MetaText(BystanderPath));

            // The same (now stale) plan must not trigger another reimport.
            var second = BatchApplier.Apply(plan, only, "integration test", null);
            if (second.SnapshotId != null) _snapshotIds.Add(second.SnapshotId);
            Assert.AreEqual(1, second.Count(ApplyOutcome.Unchanged));
            Assert.AreEqual(0, second.Count(ApplyOutcome.Applied));
        }

        [Test]
        public void Restore_BringsBackTheOriginalValue()
        {
            bool original = AudioImporterAdapter.Read(TargetPath).ForceToMono;
            var result = Apply(PlanFor(MonoRule(!original), TargetPath, BystanderPath));
            Assert.IsNotNull(result.SnapshotId);

            var snapshot = SnapshotStore.Load(result.SnapshotId);
            var restore = RestoreService.Restore(snapshot, null);
            if (restore.SnapshotId != null) _snapshotIds.Add(restore.SnapshotId);
            Assert.IsNull(restore.FatalError);
            Assert.AreEqual(0, restore.Count(ApplyOutcome.Failed));
            Assert.AreEqual(original, AudioImporterAdapter.Read(TargetPath).ForceToMono);
        }

        [Test]
        public void PlatformOverride_CanBeCreatedAndIsVerified()
        {
            var rule = TestFactory.Rule("Android", 0, Folder);
            rule.platformPolicies.Add(new PlatformPolicy
            {
                platform = "Android", enableOverride = true,
                policy = new SettingsPolicy { applyCompressionFormat = true, compressionFormat = CompressionKind.Vorbis, applyQuality = true, quality = 0.5f }
            });
            var result = Apply(PlanFor(rule, TargetPath));
            Assert.AreEqual(0, result.Count(ApplyOutcome.Failed), result.Results.Count > 0 ? result.Results[0].Message : string.Empty);
            var android = AudioImporterAdapter.Read(TargetPath).FindPlatform("Android");
            Assert.IsTrue(android.hasOverride);
            Assert.AreEqual(CompressionKind.Vorbis, android.settings.compressionFormat);
        }

        [Test]
        public void DeletedAssetDuringApply_IsReportedAsFailedAndEditingStateStaysValid()
        {
            var current = AudioImporterAdapter.Read(TargetPath);
            var plan = PlanFor(MonoRule(!current.ForceToMono), TargetPath);
            AssetDatabase.DeleteAsset(TargetPath);

            var result = Apply(plan);
            Assert.AreEqual(1, result.Count(ApplyOutcome.Failed));
            Assert.IsNull(result.FatalError);
            // The AssetDatabase must still work: a normal import has to succeed afterwards.
            Assert.DoesNotThrow(() => AssetDatabase.ImportAsset(BystanderPath));
        }
    }
}
