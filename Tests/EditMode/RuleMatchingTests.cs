// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Matching;
using JP.AudioImportOptimizer.Core.Profiles;
using NUnit.Framework;

namespace JP.AudioImportOptimizer.Tests.EditMode
{
    /// <summary>Tests wildcard matching, rule criteria and deterministic precedence.</summary>
    [TestFixture]
    public sealed class RuleMatchingTests
    {
        [Test]
        public void Glob_MatchesStarAndQuestionCaseInsensitively()
        {
            Assert.IsTrue(GlobMatcher.IsMatch("bgm_*", "BGM_forest.wav"));
            Assert.IsTrue(GlobMatcher.IsMatch("a?c.wav", "abc.wav"));
            Assert.IsFalse(GlobMatcher.IsMatch("bgm_*", "sfx_a.wav"));
        }

        [Test]
        public void Folder_MatchesPrefixOnFolderBoundaryOnly()
        {
            var rules = new List<OptimizationRule> { TestFactory.Rule("F", 0, "Assets/Aud") };
            Assert.IsNull(RuleMatcher.Select(rules, TestFactory.Asset("Assets/Audio/a.wav")).Selected);
            rules[0].folders[0] = "Assets/Audio";
            Assert.IsNotNull(RuleMatcher.Select(rules, TestFactory.Asset("Assets/Audio/a.wav")).Selected);
        }

        [Test]
        public void Extension_IsCaseInsensitive()
        {
            var rules = new List<OptimizationRule> { TestFactory.Rule("E", 0, null, ".WAV") };
            Assert.IsNotNull(RuleMatcher.Select(rules, TestFactory.Asset("Assets/a.wav")).Selected);
            Assert.IsNull(RuleMatcher.Select(rules, TestFactory.Asset("Assets/a.ogg")).Selected);
        }

        [Test]
        public void Labels_MatchAnyListedLabel()
        {
            var rule = TestFactory.Rule("L");
            rule.labels.Add("Loop");
            var asset = TestFactory.Asset("Assets/a.wav");
            Assert.IsNull(RuleMatcher.Select(new List<OptimizationRule> { rule }, asset).Selected);
            asset.Labels.Add("loop");
            Assert.IsNotNull(RuleMatcher.Select(new List<OptimizationRule> { rule }, asset).Selected);
        }

        [Test]
        public void FileSize_RespectsMinimum()
        {
            var rule = TestFactory.Rule("S");
            rule.minFileSizeKb = 500;
            Assert.IsNull(RuleMatcher.Select(new List<OptimizationRule> { rule }, TestFactory.Asset("Assets/a.wav", 100)).Selected);
            Assert.IsNotNull(RuleMatcher.Select(new List<OptimizationRule> { rule }, TestFactory.Asset("Assets/a.wav", 600)).Selected);
        }

        [Test]
        public void DisabledRules_AreIgnored()
        {
            var rule = TestFactory.Rule("D");
            rule.enabled = false;
            Assert.IsNull(RuleMatcher.Select(new List<OptimizationRule> { rule }, TestFactory.Asset("Assets/a.wav")).Selected);
        }

        [Test]
        public void Precedence_MoreSpecificRuleWinsOnEqualPriority()
        {
            var rules = new List<OptimizationRule>
            {
                TestFactory.Rule("Music", 0, "Assets/Audio/Music"),
                TestFactory.Rule("Wav", 0, null, ".wav"),
                TestFactory.Rule("MusicWav", 0, "Assets/Audio/Music", ".wav")
            };
            var s = RuleMatcher.Select(rules, TestFactory.Asset("Assets/Audio/Music/a.wav"));
            Assert.AreEqual("MusicWav", s.Selected.Rule.ruleName);
            Assert.IsFalse(s.HasConflict);
        }

        [Test]
        public void Precedence_HigherPriorityBeatsSpecificity()
        {
            var rules = new List<OptimizationRule>
            {
                TestFactory.Rule("Music", 5, "Assets/Audio/Music"),
                TestFactory.Rule("MusicWav", 0, "Assets/Audio/Music", ".wav")
            };
            Assert.AreEqual("Music", RuleMatcher.Select(rules, TestFactory.Asset("Assets/Audio/Music/a.wav")).Selected.Rule.ruleName);
        }

        [Test]
        public void Precedence_FullTieUsesListOrderAndFlagsConflict()
        {
            var rules = new List<OptimizationRule> { TestFactory.Rule("A", 1, "Assets/X"), TestFactory.Rule("B", 1, "Assets/X") };
            var s = RuleMatcher.Select(rules, TestFactory.Asset("Assets/X/a.wav"));
            Assert.AreEqual("A", s.Selected.Rule.ruleName);
            Assert.IsTrue(s.HasConflict);
        }
    }
}
