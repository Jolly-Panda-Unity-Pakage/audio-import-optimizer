// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Matching;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Planning;
using JP.AudioImportOptimizer.Core.Profiles;
using NUnit.Framework;

namespace JP.AudioImportOptimizer.Tests.EditMode
{
    /// <summary>Tests current-versus-proposed comparison, no-op detection, severities and conflicts.</summary>
    [TestFixture]
    public sealed class PlanningTests
    {
        private static AssetPlan Plan(AudioAssetInfo asset, params OptimizationRule[] rules)
        {
            var list = new List<OptimizationRule>(rules);
            return ChangePlanner.Plan(asset, RuleMatcher.Select(list, asset));
        }

        [Test]
        public void MismatchingSettings_ProduceChanges()
        {
            var rule = TestFactory.Rule("Music", 0, "Assets/Audio");
            rule.defaultPolicy.applyLoadType = true; rule.defaultPolicy.loadType = LoadMode.Streaming;
            rule.defaultPolicy.applyForceToMono = true; rule.defaultPolicy.forceToMono = true;
            Assert.AreEqual(2, Plan(TestFactory.Asset("Assets/Audio/m.wav"), rule).Changes.Count);
        }

        [Test]
        public void MatchingSettings_ProduceNoChangesAndNoFindings()
        {
            var rule = TestFactory.Rule("Music", 0, "Assets/Audio");
            rule.defaultPolicy.applyLoadType = true; rule.defaultPolicy.loadType = LoadMode.Streaming;
            var asset = TestFactory.Asset("Assets/Audio/m.wav");
            asset.Default.loadType = LoadMode.Streaming;
            var plan = Plan(asset, rule);
            Assert.AreEqual(0, plan.Changes.Count);
            Assert.AreEqual(0, plan.Findings.Count);
        }

        [Test]
        public void PreloadAudioData_IsASampleLevelSettingPerPlatform()
        {
            var rule = TestFactory.Rule("Preload");
            rule.defaultPolicy.applyPreloadAudioData = true; rule.defaultPolicy.preloadAudioData = false;
            var asset = TestFactory.Asset("Assets/a.wav");
            var plan = Plan(asset, rule);
            Assert.AreEqual(1, plan.Changes.Count);
            Assert.AreEqual(SettingKind.PreloadAudioData, plan.Changes[0].Kind);
            asset.Default.preloadAudioData = false;
            Assert.AreEqual(0, Plan(asset, rule).Changes.Count);
        }

        [Test]
        public void SettingsNotEnforced_AreNeverChanged()
        {
            var rule = TestFactory.Rule("Empty");
            Assert.AreEqual(0, Plan(TestFactory.Asset("Assets/a.wav"), rule).Changes.Count);
        }

        [Test]
        public void MismatchSeverity_ComesFromTheRule()
        {
            var rule = TestFactory.Rule("R");
            rule.mismatchSeverity = Severity.Error;
            rule.defaultPolicy.applyForceToMono = true; rule.defaultPolicy.forceToMono = true;
            Assert.AreEqual(Severity.Error, Plan(TestFactory.Asset("Assets/a.wav"), rule).WorstSeverity);
        }

        [Test]
        public void DisallowedCompression_IsAnErrorAndNotSafeWithoutAReplacement()
        {
            var rule = TestFactory.Rule("D");
            rule.defaultPolicy.disallowedCompression.Add(CompressionKind.MP3);
            var plan = Plan(TestFactory.Asset("Assets/a.mp3", 100, CompressionKind.MP3), rule);
            Assert.AreEqual(1, plan.Findings.Count);
            Assert.AreEqual(Severity.Error, plan.Findings[0].Severity);
            Assert.IsFalse(plan.Findings[0].SafeToApply);
        }

        [Test]
        public void MissingPlatformOverride_ProposesCreationThenComparesAgainstDefault()
        {
            var rule = TestFactory.Rule("P");
            rule.platformPolicies.Add(new PlatformPolicy { platform = "Android", enableOverride = true, policy = new SettingsPolicy { applyCompressionFormat = true, compressionFormat = CompressionKind.Vorbis } });
            var plan = Plan(TestFactory.Asset("Assets/a.wav"), rule);
            Assert.AreEqual(2, plan.Changes.Count);
            Assert.AreEqual(SettingKind.PlatformOverride, plan.Changes[0].Kind);
        }

        [Test]
        public void RemovingAnOverride_IsNotSafeToAutoApply()
        {
            var rule = TestFactory.Rule("P");
            rule.platformPolicies.Add(new PlatformPolicy { platform = "Android", enableOverride = false });
            var asset = TestFactory.Asset("Assets/a.wav");
            asset.Platforms[0].hasOverride = true; asset.Platforms[0].settings = new SampleSettingsValues();
            var plan = Plan(asset, rule);
            Assert.AreEqual(1, plan.Changes.Count);
            Assert.IsFalse(plan.Changes[0].SafeToAutoApply);
        }

        [Test]
        public void SampleRateOverride_IsIgnoredUnlessTheEffectiveModeIsOverride()
        {
            var rule = TestFactory.Rule("SR");
            rule.defaultPolicy.applySampleRateOverride = true; rule.defaultPolicy.sampleRateOverrideHz = 22050;
            Assert.AreEqual(0, Plan(TestFactory.Asset("Assets/a.wav"), rule).Changes.Count);
        }

        [Test]
        public void UnmatchedAndUnreadableAssets_GetExplainedFindings()
        {
            var plan = Plan(TestFactory.Asset("Assets/z.wav"));
            Assert.AreEqual(FindingCodes.NoRuleMatched, plan.Findings[0].Code);
            var bad = TestFactory.Asset("Assets/bad.wav");
            bad.ReadError = "boom";
            Assert.AreEqual(FindingCodes.ImporterReadFailed, Plan(bad, TestFactory.Rule("R")).Findings[0].Code);
        }

        [Test]
        public void ConflictingRules_AreReported()
        {
            var plan = Plan(TestFactory.Asset("Assets/X/a.wav"), TestFactory.Rule("A", 1, "Assets/X"), TestFactory.Rule("B", 1, "Assets/X"));
            Assert.IsTrue(plan.Findings.Exists(f => f.Code == FindingCodes.RuleConflict));
        }

        [Test]
        public void Consistency_FlagsTheMinorityInsideACategory()
        {
            var assets = new List<AudioAssetInfo>
            {
                TestFactory.Asset("Assets/C/1.wav", 1, CompressionKind.Vorbis), TestFactory.Asset("Assets/C/2.wav", 1, CompressionKind.Vorbis),
                TestFactory.Asset("Assets/C/3.wav", 1, CompressionKind.Vorbis), TestFactory.Asset("Assets/C/4.wav", 1, CompressionKind.PCM)
            };
            var plan = PlanBuilder.Build(assets, new List<OptimizationRule> { TestFactory.Rule("Cat", 0, "Assets/C") });
            Assert.AreEqual(1, plan.CountFindings(Severity.Info));
            Assert.AreEqual(1, plan.Assets[3].Findings.Count);
        }

        [Test]
        public void Evaluator_DropsNoOpsAndOrphanPlatformChanges()
        {
            var asset = TestFactory.Asset("Assets/e.wav");
            var noOp = new ProposedChange { AssetPath = asset.Path, Kind = SettingKind.LoadType, Proposed = SettingValue.FromEnum((int)LoadMode.DecompressOnLoad, "DecompressOnLoad") };
            Assert.AreEqual(0, ChangeEvaluator.SelectEffective(asset, new[] { noOp }, null).Count);

            var orphan = new ProposedChange { AssetPath = asset.Path, Platform = "Android", Kind = SettingKind.Quality, Proposed = SettingValue.FromNumber(0.5, "0.5") };
            var notes = new List<string>();
            Assert.AreEqual(0, ChangeEvaluator.SelectEffective(asset, new[] { orphan }, notes).Count);
            Assert.AreEqual(1, notes.Count);

            var create = new ProposedChange { AssetPath = asset.Path, Platform = "Android", Kind = SettingKind.PlatformOverride, Proposed = SettingValue.FromBool(true) };
            Assert.AreEqual(2, ChangeEvaluator.SelectEffective(asset, new[] { create, orphan }, null).Count);
        }
    }
}
