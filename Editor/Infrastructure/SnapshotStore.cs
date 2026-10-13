// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using JP.AudioImportOptimizer.Core.Snapshots;
using UnityEngine;

namespace JP.AudioImportOptimizer.Editor.Infrastructure
{
    /// <summary>A snapshot file found on disk.</summary>
    public sealed class SnapshotInfo
    {
        /// <summary>Snapshot id.</summary>
        public string Id;
        /// <summary>Label stored in the file.</summary>
        public string Label;
        /// <summary>Creation time (UTC, ISO 8601).</summary>
        public string CreatedUtc;
        /// <summary>Number of assets captured.</summary>
        public int AssetCount;
        /// <summary>Absolute file path.</summary>
        public string FilePath;
    }

    /// <summary>
    /// Persists snapshots as JSON under ProjectSettings/JP/AudioImportOptimizer/Snapshots (outside Assets, so no import happens,
    /// and inside version control by default). Writes go to a temporary file first and are moved into place.
    /// </summary>
    public static class SnapshotStore
    {
        private static readonly Regex SafeId = new Regex("^[A-Za-z0-9-]{1,64}$", RegexOptions.Compiled);

        /// <summary>Absolute folder that holds snapshot files.</summary>
        public static string Folder
        {
            get
            {
                string root = Directory.GetParent(Application.dataPath).FullName;
                return Path.Combine(root, "ProjectSettings", "JP", "AudioImportOptimizer", "Snapshots");
            }
        }

        /// <summary>Creates an empty snapshot with a fresh id and metadata.</summary>
        /// <param name="label">Optional label.</param>
        /// <returns>A new snapshot without entries.</returns>
        public static SettingsSnapshot Create(string label)
        {
            var now = DateTime.UtcNow;
            return new SettingsSnapshot
            {
                id = now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                label = label ?? string.Empty,
                createdUtc = now.ToString("o", CultureInfo.InvariantCulture),
                packageVersion = PackageMetadata.GetVersion(),
                unityVersion = Application.unityVersion
            };
        }

        /// <summary>Writes a snapshot to disk.</summary>
        /// <param name="snapshot">A valid snapshot.</param>
        /// <returns>The absolute file path.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the snapshot is invalid.</exception>
        /// <exception cref="IOException">Thrown when the file cannot be written.</exception>
        public static string Save(SettingsSnapshot snapshot)
        {
            var problems = SnapshotValidator.Validate(snapshot);
            if (problems.Count > 0) throw new InvalidOperationException("Refusing to save an invalid snapshot: " + problems[0]);
            if (!SafeId.IsMatch(snapshot.id)) throw new InvalidOperationException("Invalid snapshot id.");

            Directory.CreateDirectory(Folder);
            string target = PathFor(snapshot.id);
            string temp = target + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(snapshot, true));
            if (File.Exists(target)) File.Delete(target);
            File.Move(temp, target);
            return target;
        }

        /// <summary>Loads a snapshot by id. The result is not validated; call <see cref="SnapshotValidator.Validate"/>.</summary>
        /// <param name="id">Snapshot id.</param>
        /// <returns>The snapshot, or null when the file is missing or unreadable.</returns>
        public static SettingsSnapshot Load(string id)
        {
            if (string.IsNullOrEmpty(id) || !SafeId.IsMatch(id)) return null;
            try
            {
                string path = PathFor(id);
                return File.Exists(path) ? JsonUtility.FromJson<SettingsSnapshot>(File.ReadAllText(path)) : null;
            }
            catch (Exception)
            {
                return null; // Corrupt JSON is reported by the caller as an unreadable snapshot.
            }
        }

        /// <summary>Lists snapshots, newest first. Unreadable files are listed with a label that says so.</summary>
        /// <returns>The snapshot infos.</returns>
        public static List<SnapshotInfo> List()
        {
            var result = new List<SnapshotInfo>();
            if (!Directory.Exists(Folder)) return result;
            foreach (var file in Directory.GetFiles(Folder, "snapshot-*.json"))
            {
                string id = Path.GetFileNameWithoutExtension(file).Substring("snapshot-".Length);
                var snap = Load(id);
                result.Add(new SnapshotInfo
                {
                    Id = id, FilePath = file,
                    Label = snap != null ? snap.label : "(unreadable snapshot)",
                    CreatedUtc = snap != null ? snap.createdUtc : string.Empty,
                    AssetCount = snap != null && snap.entries != null ? snap.entries.Count : 0
                });
            }
            result.Sort((a, b) => string.CompareOrdinal(b.Id, a.Id));
            return result;
        }

        private static string PathFor(string id)
        {
            return Path.Combine(Folder, "snapshot-" + id + ".json");
        }
    }
}
