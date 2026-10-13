// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Profiles;
using UnityEngine;

namespace JP.AudioImportOptimizer.Editor.Profiles
{
    /// <summary>
    /// A reusable set of optimization rules stored as a ScriptableObject asset. Duplicate it (Ctrl/Cmd+D) to create variants.
    /// A profile never changes any audio asset by itself; changes are only applied after a reviewed dry run.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioOptimizationProfile", menuName = "JP/Audio Import Optimizer/Optimization Profile", order = 0)]
    public sealed class OptimizationProfile : ScriptableObject
    {
        [Header("Description")]
        [TextArea(2, 5)]
        [Tooltip("Free text describing what this profile is for, for example 'Mobile build, 2026 Q1'.")]
        [SerializeField] private string description = string.Empty;

        [Header("Rules")]
        [Tooltip("Rules are evaluated for every asset. The winner is chosen by priority, then specificity, then list order.")]
        [SerializeField] private List<OptimizationRule> rules = new List<OptimizationRule>();

        /// <summary>Free-text description.</summary>
        public string Description { get { return description; } }

        /// <summary>The rules in list order.</summary>
        public IReadOnlyList<OptimizationRule> Rules { get { return rules; } }

        /// <summary>Appends rules (used by the "Add default rules" button; never replaces existing rules).</summary>
        /// <param name="toAdd">Rules to append.</param>
        public void AppendRules(IEnumerable<OptimizationRule> toAdd)
        {
            rules.AddRange(toAdd);
        }
    }

    /// <summary>Provides the built-in starter rules. They are examples: edit folders and values for your project.</summary>
    public static class DefaultRules
    {
        /// <summary>Creates a fresh copy of the starter rules.</summary>
        /// <returns>New rule instances.</returns>
        public static List<OptimizationRule> Create()
        {
            var longForm = new OptimizationRule
            {
                ruleName = "Long-Form Audio (2 MB and larger)", category = "Long-Form Audio", priority = 10, minFileSizeKb = 2048,
                defaultPolicy = new SettingsPolicy
                {
                    applyLoadType = true, loadType = LoadMode.Streaming,
                    applyCompressionFormat = true, compressionFormat = CompressionKind.Vorbis,
                    applyQuality = true, quality = 0.7f
                }
            };
            var oneShot = new OptimizationRule
            {
                ruleName = "Short One-Shot Sounds (200 KB and smaller)", category = "Short One-Shot Sounds", priority = 5, maxFileSizeKb = 200,
                defaultPolicy = new SettingsPolicy
                {
                    applyLoadType = true, loadType = LoadMode.DecompressOnLoad,
                    applyCompressionFormat = true, compressionFormat = CompressionKind.ADPCM,
                    applyPreloadAudioData = true, preloadAudioData = true
                }
            };
            var voice = new OptimizationRule
            {
                ruleName = "Voice Over", category = "Voice Over", priority = 20,
                defaultPolicy = new SettingsPolicy
                {
                    applyForceToMono = true, forceToMono = true,
                    applyLoadType = true, loadType = LoadMode.CompressedInMemory,
                    applyCompressionFormat = true, compressionFormat = CompressionKind.Vorbis,
                    applyQuality = true, quality = 0.5f,
                    applyLoadInBackground = true, loadInBackground = true
                }
            };
            voice.folders.Add("Assets/Audio/VO");
            var ui = new OptimizationRule
            {
                ruleName = "UI Sounds", category = "UI Sounds", priority = 20,
                defaultPolicy = new SettingsPolicy
                {
                    applyForceToMono = true, forceToMono = true,
                    applyLoadType = true, loadType = LoadMode.DecompressOnLoad,
                    applyCompressionFormat = true, compressionFormat = CompressionKind.ADPCM,
                    applyPreloadAudioData = true, preloadAudioData = true
                }
            };
            ui.folders.Add("Assets/Audio/UI");
            var music = new OptimizationRule
            {
                ruleName = "Music", category = "Music", priority = 20,
                defaultPolicy = new SettingsPolicy
                {
                    applyLoadType = true, loadType = LoadMode.Streaming,
                    applyCompressionFormat = true, compressionFormat = CompressionKind.Vorbis,
                    applyQuality = true, quality = 0.7f
                }
            };
            music.folders.Add("Assets/Audio/Music");
            music.platformPolicies.Add(new PlatformPolicy
            {
                platform = "Android", enableOverride = true,
                policy = new SettingsPolicy { applyCompressionFormat = true, compressionFormat = CompressionKind.Vorbis, applyQuality = true, quality = 0.5f }
            });
            return new List<OptimizationRule> { longForm, oneShot, voice, ui, music };
        }
    }
}
