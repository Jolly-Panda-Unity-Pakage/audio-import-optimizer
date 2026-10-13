// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Planning;
using JP.AudioImportOptimizer.Core.Reporting;
using JP.AudioImportOptimizer.Core.Snapshots;
using JP.AudioImportOptimizer.Editor.Infrastructure;
using UnityEditor;

namespace JP.AudioImportOptimizer.Editor.Services
{
    /// <summary>Reports progress and returns true when the user asked to cancel.</summary>
    /// <param name="title">Operation title.</param>
    /// <param name="info">Current item description.</param>
    /// <param name="progress">Progress between 0 and 1.</param>
    /// <returns>True to cancel at the next safe boundary.</returns>
    public delegate bool ProgressCallback(string title, string info, float progress);

    /// <summary>Outcome of a batch apply or restore.</summary>
    public sealed class BatchResult
    {
        /// <summary>Per-asset results.</summary>
        public readonly List<AssetApplyResult> Results = new List<AssetApplyResult>();
        /// <summary>Id of the snapshot written before changing anything; null when nothing was captured.</summary>
        public string SnapshotId;
        /// <summary>True when the user cancelled; remaining assets are reported as skipped.</summary>
        public bool Cancelled;
        /// <summary>Set when the whole operation was refused (for example the snapshot could not be saved). No asset was modified.</summary>
        public string FatalError;

        /// <summary>Counts results with the given outcome.</summary>
        /// <param name="outcome">Outcome to count.</param>
        /// <returns>The count.</returns>
        public int Count(ApplyOutcome outcome) { int n = 0; foreach (var r in Results) if (r.Outcome == outcome) n++; return n; }
    }

    /// <summary>
    /// Applies the included changes of a plan. Not transactional: each asset is saved independently, so a failure or cancellation
    /// leaves earlier assets changed. A snapshot of the original values is written first and is the recovery path.
    /// </summary>
    public static class BatchApplier
    {
        /// <summary>Applies included changes.</summary>
        /// <param name="plan">The reviewed plan.</param>
        /// <param name="onlyAssets">Optional set of asset paths to restrict the operation to; null applies to every non-excluded asset.</param>
        /// <param name="label">Label stored in the snapshot.</param>
        /// <param name="progress">Optional progress callback.</param>
        /// <returns>The result. Never null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when plan is null.</exception>
        public static BatchResult Apply(OptimizationPlan plan, ISet<string> onlyAssets, string label, ProgressCallback progress)
        {
            if (plan == null) throw new ArgumentNullException("plan");
            var result = new BatchResult();

            var targets = new List<AssetPlan>();
            foreach (var a in plan.Assets)
            {
                if (a.Excluded || a.PendingChangeCount == 0) continue;
                if (onlyAssets != null && !onlyAssets.Contains(a.Asset.Path)) continue;
                targets.Add(a);
            }
            if (targets.Count == 0) return result;

            // Read fresh state and capture the snapshot BEFORE any modification.
            var fresh = new Dictionary<string, AudioAssetInfo>(StringComparer.Ordinal);
            var snapshot = SnapshotStore.Create(label);
            foreach (var t in targets)
            {
                var info = AudioImporterAdapter.Read(t.Asset.Path);
                fresh[t.Asset.Path] = info;
                if (string.IsNullOrEmpty(info.ReadError)) snapshot.entries.Add(SnapshotEntry.FromAsset(info));
            }
            if (snapshot.entries.Count > 0)
            {
                try { SnapshotStore.Save(snapshot); result.SnapshotId = snapshot.id; }
                catch (Exception ex)
                {
                    result.FatalError = "The recovery snapshot could not be saved, so nothing was changed: " + ex.Message;
                    foreach (var t in targets) result.Results.Add(new AssetApplyResult { AssetPath = t.Asset.Path, Outcome = ApplyOutcome.Skipped, Message = "Not applied: snapshot could not be saved." });
                    return result;
                }
            }

            var applied = new List<KeyValuePair<AssetPlan, List<ProposedChange>>>();
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    var t = targets[i];
                    if (progress != null && progress("Applying audio import settings", t.Asset.Path, (float)i / targets.Count))
                    {
                        result.Cancelled = true;
                        for (int j = i; j < targets.Count; j++)
                            result.Results.Add(new AssetApplyResult { AssetPath = targets[j].Asset.Path, Outcome = ApplyOutcome.Skipped, Message = "Cancelled before this asset was processed." });
                        break;
                    }
                    ApplyOne(t, fresh[t.Asset.Path], result, applied);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing(); // Always restore the AssetDatabase editing state.
            }

            Verify(applied, result);
            return result;
        }

        private static void ApplyOne(AssetPlan target, AudioAssetInfo info, BatchResult result, List<KeyValuePair<AssetPlan, List<ProposedChange>>> applied)
        {
            string path = target.Asset.Path;
            try
            {
                if (!string.IsNullOrEmpty(info.ReadError))
                {
                    result.Results.Add(new AssetApplyResult { AssetPath = path, Outcome = ApplyOutcome.Failed, Message = info.ReadError });
                    return;
                }
                var requested = target.Changes.FindAll(c => c.Included);
                var notes = new List<string>();
                var effective = ChangeEvaluator.SelectEffective(info, requested, notes);
                if (effective.Count == 0)
                {
                    result.Results.Add(new AssetApplyResult { AssetPath = path, Outcome = ApplyOutcome.Unchanged, Message = notes.Count > 0 ? string.Join(" ", notes) : "Settings already match; no reimport was triggered." });
                    return;
                }
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) throw new InvalidOperationException("The AudioImporter is no longer available.");
                AudioImporterAdapter.ApplyChanges(importer, effective);
                importer.SaveAndReimport();
                applied.Add(new KeyValuePair<AssetPlan, List<ProposedChange>>(target, effective));
                result.Results.Add(new AssetApplyResult { AssetPath = path, Outcome = ApplyOutcome.Applied, Message = effective.Count + " change(s) saved." + (notes.Count > 0 ? " " + string.Join(" ", notes) : string.Empty) });
            }
            catch (Exception ex)
            {
                result.Results.Add(new AssetApplyResult { AssetPath = path, Outcome = ApplyOutcome.Failed, Message = ex.GetType().Name + ": " + ex.Message });
            }
        }

        /// <summary>Re-reads every applied asset after the editing block closed and downgrades results whose values did not persist.</summary>
        private static void Verify(List<KeyValuePair<AssetPlan, List<ProposedChange>>> applied, BatchResult result)
        {
            foreach (var pair in applied)
            {
                var info = AudioImporterAdapter.Read(pair.Key.Asset.Path);
                string problem = null;
                if (!string.IsNullOrEmpty(info.ReadError)) problem = "could not be re-read: " + info.ReadError;
                else
                {
                    foreach (var c in pair.Value)
                    {
                        Core.Models.SettingValue cur;
                        if (!ChangeEvaluator.TryGetCurrent(info, c, out cur) || !cur.Equals(c.Proposed)) { problem = c.Kind + " on " + c.Platform + " is " + (ChangeEvaluator.TryGetCurrent(info, c, out cur) ? cur.Display : "missing") + " instead of " + c.Proposed.Display + "."; break; }
                    }
                }
                if (problem == null) continue;
                var r = result.Results.Find(x => x.AssetPath == pair.Key.Asset.Path);
                if (r != null) { r.Outcome = ApplyOutcome.Failed; r.Message = "Verification failed: " + problem + " Unity may have rejected the value for this platform or format."; }
            }
        }
    }
}
