// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;
using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;

namespace JP.AudioImportOptimizer.Core.Planning
{
    /// <summary>Reads the current value of a change's target setting from an asset state. Used for no-op detection and post-apply verification.</summary>
    public static class ChangeEvaluator
    {
        /// <summary>Tries to read the value a change targets.</summary>
        /// <param name="asset">Fresh asset state.</param>
        /// <param name="change">The change.</param>
        /// <param name="value">The current value when found.</param>
        /// <returns>False when the target does not exist yet (for example a platform override that has not been created).</returns>
        /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
        public static bool TryGetCurrent(AudioAssetInfo asset, ProposedChange change, out SettingValue value)
        {
            if (asset == null) throw new ArgumentNullException("asset");
            if (change == null) throw new ArgumentNullException("change");
            value = default(SettingValue);
            switch (change.Kind)
            {
                case SettingKind.ForceToMono:
                case SettingKind.LoadInBackground:
                case SettingKind.Ambisonic:
                    value = asset.GetAssetLevel(change.Kind);
                    return true;
                case SettingKind.PlatformOverride:
                    var ps = asset.FindPlatform(change.Platform);
                    value = SettingValue.FromBool(ps != null && ps.hasOverride);
                    return true;
                default:
                    if (change.Platform == Profiles.PlatformCatalog.DefaultPlatform) { value = asset.Default.Get(change.Kind); return true; }
                    var state = asset.FindPlatform(change.Platform);
                    if (state == null || !state.hasOverride || state.settings == null) return false;
                    value = state.settings.Get(change.Kind);
                    return true;
            }
        }

        /// <summary>Selects the changes that still need to be applied and explains the ones that must be dropped.</summary>
        /// <param name="fresh">Fresh asset state read just before applying.</param>
        /// <param name="requested">Changes the user included.</param>
        /// <param name="notes">Receives one sentence per dropped change.</param>
        /// <returns>Changes that would actually modify the asset. Empty means the asset is unchanged.</returns>
        public static List<ProposedChange> SelectEffective(AudioAssetInfo fresh, IEnumerable<ProposedChange> requested, List<string> notes)
        {
            var all = new List<ProposedChange>(requested);
            var result = new List<ProposedChange>();
            foreach (var c in all)
            {
                SettingValue cur;
                bool exists = TryGetCurrent(fresh, c, out cur);
                if (exists && cur.Equals(c.Proposed)) continue; // already in the desired state: no reimport needed

                bool sampleOnPlatform = c.Platform != Profiles.PlatformCatalog.DefaultPlatform
                    && c.Kind != SettingKind.PlatformOverride;
                if (sampleOnPlatform && !exists)
                {
                    bool creationIncluded = all.Exists(o => o.Kind == SettingKind.PlatformOverride && o.Platform == c.Platform && o.Proposed.Number > 0.5);
                    if (!creationIncluded)
                    {
                        if (notes != null) notes.Add(c.Kind + " on " + c.Platform + " was skipped because the platform override does not exist and its creation was excluded.");
                        continue;
                    }
                }
                result.Add(c);
            }
            return result;
        }
    }
}
