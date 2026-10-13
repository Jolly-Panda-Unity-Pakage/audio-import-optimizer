// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Profiles;
using NUnit.Framework;

namespace JP.AudioImportOptimizer.Tests.EditMode
{
    /// <summary>Tests profile validation.</summary>
    [TestFixture]
    public sealed class ProfileValidatorTests
    {
        [Test]
        public void EmptyProfile_WarnsOnce()
        {
            var issues = ProfileValidator.Validate(new List<OptimizationRule>());
            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(Severity.Warning, issues[0].Severity);
        }

        [Test]
        public void UnnamedRuleAndInvertedSizeRange_AreErrors()
        {
            var bad = TestFactory.Rule(string.Empty);
            bad.minFileSizeKb = 10; bad.maxFileSizeKb = 5;
            var errors = ProfileValidator.Validate(new List<OptimizationRule> { bad }).FindAll(i => i.Severity == Severity.Error);
            Assert.GreaterOrEqual(errors.Count, 2);
        }

        [Test]
        public void DuplicateNames_AreReported()
        {
            var issues = ProfileValidator.Validate(new List<OptimizationRule> { TestFactory.Rule("Dup"), TestFactory.Rule("Dup") });
            Assert.IsTrue(issues.Exists(i => i.Message.Contains("same name")));
        }

        [Test]
        public void EnforcedFormatThatIsAlsoDisallowed_IsAnError()
        {
            var rule = TestFactory.Rule("CF");
            rule.defaultPolicy.applyCompressionFormat = true; rule.defaultPolicy.compressionFormat = CompressionKind.MP3;
            rule.defaultPolicy.disallowedCompression.Add(CompressionKind.MP3);
            Assert.IsTrue(ProfileValidator.Validate(new List<OptimizationRule> { rule }).Exists(i => i.Severity == Severity.Error));
        }

        [Test]
        public void DuplicatePlatformsAndUnknownPlatforms_AreReported()
        {
            var rule = TestFactory.Rule("P");
            rule.platformPolicies.Add(new PlatformPolicy { platform = "Android" });
            rule.platformPolicies.Add(new PlatformPolicy { platform = "Android" });
            rule.platformPolicies.Add(new PlatformPolicy { platform = "Dreamcast" });
            var issues = ProfileValidator.Validate(new List<OptimizationRule> { rule });
            Assert.IsTrue(issues.Exists(i => i.Severity == Severity.Error && i.Message.Contains("twice")));
            Assert.IsTrue(issues.Exists(i => i.Severity == Severity.Warning && i.Message.Contains("Dreamcast")));
        }

        [Test]
        public void DefaultStarterShapedRule_HasNoErrors()
        {
            var rule = TestFactory.Rule("OK", 1, "Assets/Audio");
            rule.defaultPolicy.applyLoadType = true;
            Assert.IsFalse(ProfileValidator.Validate(new List<OptimizationRule> { rule }).Exists(i => i.Severity == Severity.Error));
        }
    }
}
