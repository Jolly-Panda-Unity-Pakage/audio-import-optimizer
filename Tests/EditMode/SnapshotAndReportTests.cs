// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Matching;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Planning;
using JP.AudioImportOptimizer.Core.Profiles;
using JP.AudioImportOptimizer.Core.Reporting;
using JP.AudioImportOptimizer.Core.Snapshots;
using NUnit.Framework;

namespace JP.AudioImportOptimizer.Tests.EditMode
{
    /// <summary>Tests snapshot validation, snapshot equality and report generation (pure logic).</summary>
    [TestFixture]
    public sealed class SnapshotAndReportTests
    {
        private static SettingsSnapshot ValidSnapshot()
        {
            var s = new SettingsSnapshot { id = "test-1", createdUtc = "2026-01-01T00:00:00Z" };
            s.entries.Add(SnapshotEntry.FromAsset(TestFactory.Asset("Assets/a.wav")));
            return s;
        }

        [Test]
        public void ValidSnapshot_HasNoProblems()
        {
            Assert.AreEqual(0, SnapshotValidator.Validate(ValidSnapshot()).Count);
        }

        [Test]
        public void NullFutureSchemaEmptyAndOutOfRange_AreRejected()
        {
            Assert.AreEqual(1, SnapshotValidator.Validate(null).Count);
            var s = ValidSnapshot(); s.schemaVersion = 99;
            Assert.GreaterOrEqual(SnapshotValidator.Validate(s).Count, 1);
            s = ValidSnapshot(); s.entries.Clear();
            Assert.GreaterOrEqual(SnapshotValidator.Validate(s).Count, 1);
            s = ValidSnapshot(); s.entries[0].defaultSettings.quality = 5f;
            Assert.AreEqual(1, SnapshotValidator.Validate(s).Count);
            s = ValidSnapshot(); s.entries.Add(SnapshotEntry.FromAsset(TestFactory.Asset("Assets/a.wav")));
            Assert.IsTrue(SnapshotValidator.Validate(s).Exists(p => p.Contains("twice")));
        }

        [Test]
        public void Entry_MatchesItsSourceAndDetectsDifferences()
        {
            var asset = TestFactory.Asset("Assets/a.wav");
            var entry = SnapshotEntry.FromAsset(asset);
            Assert.IsTrue(entry.MatchesAsset(asset));
            asset.Default.quality = 0.1f;
            Assert.IsFalse(entry.MatchesAsset(asset));
        }

        [Test]
        public void Report_JsonContainsSummaryAndEscapesText()
        {
            var assets = new List<AudioAssetInfo> { TestFactory.Asset("Assets/C/1.wav") };
            var plan = PlanBuilder.Build(assets, new List<OptimizationRule> { TestFactory.Rule("Cat", 0, "Assets/C") });
            var report = new AnalysisReport { PackageVersion = "0.1.0", UnityVersion = "6000.6", Scope = "Assets/\"A\"", Plan = plan };
            report.ApplyResults.Add(new AssetApplyResult { AssetPath = "Assets/C/1.wav", Outcome = ApplyOutcome.Failed, Message = "line\nbreak <x>" });
            string json = ReportWriter.ToJson(report);
            StringAssert.Contains("\"totalAssets\": 1", json);
            StringAssert.Contains("\\\"A\\\"", json);
            StringAssert.Contains("\"failed\": 1", json);
            string html = ReportWriter.ToHtml(report);
            StringAssert.Contains("&lt;x&gt;", html);
            Assert.IsFalse(html.Contains("<script"));
        }

        [Test]
        public void Report_JsonIsStructurallyBalanced()
        {
            string json = ReportWriter.ToJson(new AnalysisReport());
            int depth = 0; bool inString = false, escaped = false;
            foreach (char c in json)
            {
                if (escaped) { escaped = false; continue; }
                if (inString) { if (c == '\\') escaped = true; else if (c == '"') inString = false; continue; }
                if (c == '"') inString = true; else if (c == '{' || c == '[') depth++; else if (c == '}' || c == ']') depth--;
            }
            Assert.AreEqual(0, depth);
            Assert.IsFalse(inString);
        }
    }
}
