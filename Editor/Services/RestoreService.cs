// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Reporting;
using JP.AudioImportOptimizer.Core.Snapshots;
using JP.AudioImportOptimizer.Editor.Infrastructure;
using UnityEditor;

namespace JP.AudioImportOptimizer.Editor.Services
{
    /// <summary>
    /// Restores the importer settings captured by a snapshot: Force To Mono, Load In Background, Ambisonic, Preload Audio Data,
    /// the Default sample settings and the override state of the platforms the package manages. Other importer data is untouched.
    /// </summary>
    public static class RestoreService
    {
        /// <summary>Restores a snapshot. Must only be called after explicit user confirmation.</summary>
        /// <param name="snapshot">The snapshot to restore.</param>
        /// <param name="progress">Optional progress callback.</param>
        /// <returns>The result. <see cref="BatchResult.FatalError"/> is set when the snapshot was refused.</returns>
        /// <exception cref="ArgumentNullException">Thrown when snapshot is null.</exception>
        public static BatchResult Restore(SettingsSnapshot snapshot, ProgressCallback progress)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            var result = new BatchResult();
            var problems = SnapshotValidator.Validate(snapshot);
            if (problems.Count > 0) { result.FatalError = "The snapshot is invalid: " + string.Join(" ", problems); return result; }

            // Resolve each entry to a current path (the asset may have moved) and read its present state.
            var work = new List<KeyValuePair<SnapshotEntry, Core.Models.AudioAssetInfo>>();
            var safety = SnapshotStore.Create("Automatic backup before restoring " + snapshot.id);
            foreach (var e in snapshot.entries)
            {
                string path = e.assetPath;
                if (!(AssetImporter.GetAtPath(path) is AudioImporter) && !string.IsNullOrEmpty(e.guid))
                {
                    string moved = AssetDatabase.GUIDToAssetPath(e.guid);
                    if (!string.IsNullOrEmpty(moved)) path = moved;
                }
                var info = AudioImporterAdapter.Read(path);
                if (!string.IsNullOrEmpty(info.ReadError))
                {
                    result.Results.Add(new AssetApplyResult { AssetPath = e.assetPath, Outcome = ApplyOutcome.Skipped, Message = "Asset was deleted or can no longer be read: " + info.ReadError });
                    continue;
                }
                work.Add(new KeyValuePair<SnapshotEntry, Core.Models.AudioAssetInfo>(e, info));
                safety.entries.Add(SnapshotEntry.FromAsset(info));
            }
            if (work.Count == 0) return result;

            try { SnapshotStore.Save(safety); result.SnapshotId = safety.id; }
            catch (Exception ex)
            {
                result.FatalError = "The safety backup could not be saved, so nothing was restored: " + ex.Message;
                return result;
            }

            var changed = new List<KeyValuePair<SnapshotEntry, string>>();
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < work.Count; i++)
                {
                    var entry = work[i].Key; var info = work[i].Value;
                    if (progress != null && progress("Restoring audio import settings", info.Path, (float)i / work.Count))
                    {
                        result.Cancelled = true;
                        for (int j = i; j < work.Count; j++)
                            result.Results.Add(new AssetApplyResult { AssetPath = work[j].Value.Path, Outcome = ApplyOutcome.Skipped, Message = "Cancelled before this asset was processed." });
                        break;
                    }
                    try
                    {
                        if (entry.MatchesAsset(info)) { result.Results.Add(new AssetApplyResult { AssetPath = info.Path, Outcome = ApplyOutcome.Unchanged, Message = "Already matches the snapshot." }); continue; }
                        var importer = AssetImporter.GetAtPath(info.Path) as AudioImporter;
                        if (importer == null) throw new InvalidOperationException("The AudioImporter is no longer available.");
                        AudioImporterAdapter.WriteEntry(importer, entry);
                        importer.SaveAndReimport();
                        changed.Add(new KeyValuePair<SnapshotEntry, string>(entry, info.Path));
                        result.Results.Add(new AssetApplyResult { AssetPath = info.Path, Outcome = ApplyOutcome.Applied, Message = "Restored." });
                    }
                    catch (Exception ex)
                    {
                        result.Results.Add(new AssetApplyResult { AssetPath = info.Path, Outcome = ApplyOutcome.Failed, Message = "Could not restore under this Unity version: " + ex.Message });
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            foreach (var c in changed)
            {
                var info = AudioImporterAdapter.Read(c.Value);
                if (!string.IsNullOrEmpty(info.ReadError) || !c.Key.MatchesAsset(info))
                {
                    var r = result.Results.Find(x => x.AssetPath == c.Value);
                    if (r != null) { r.Outcome = ApplyOutcome.Failed; r.Message = "Verification failed: Unity did not persist every restored value."; }
                }
            }
            return result;
        }
    }
}
