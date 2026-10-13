// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System;

namespace JP.AudioImportOptimizer.Core.Models
{
    /// <summary>Plain-data copy of AudioImporterSampleSettings used by analysis and snapshots.</summary>
    [Serializable]
    public sealed class SampleSettingsValues
    {
        /// <summary>Clip load type.</summary>
        public LoadMode loadType;

        /// <summary>Compression format.</summary>
        public CompressionKind compressionFormat;

        /// <summary>Quality in the range 0..1 (only meaningful for lossy formats).</summary>
        public float quality = 1f;

        /// <summary>Sample rate mode.</summary>
        public SampleRateMode sampleRateSetting;

        /// <summary>Sample rate override in Hz (only used when the mode is OverrideSampleRate).</summary>
        public uint sampleRateOverride = 44100;

        /// <summary>Creates a deep copy.</summary>
        /// <returns>A new instance with identical values.</returns>
        public SampleSettingsValues Clone()
        {
            return (SampleSettingsValues)MemberwiseClone();
        }

        /// <summary>Reads one setting as a <see cref="SettingValue"/>.</summary>
        /// <param name="kind">A sample-level setting kind.</param>
        /// <returns>The current value.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="kind"/> is not a sample-level setting.</exception>
        public SettingValue Get(SettingKind kind)
        {
            switch (kind)
            {
                case SettingKind.LoadType: return SettingValue.FromEnum((int)loadType, loadType.ToString());
                case SettingKind.CompressionFormat: return SettingValue.FromEnum((int)compressionFormat, compressionFormat.ToString());
                case SettingKind.Quality: return SettingValue.FromNumber(Math.Round(quality, 3), Math.Round(quality, 3).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                case SettingKind.SampleRateSetting: return SettingValue.FromEnum((int)sampleRateSetting, sampleRateSetting.ToString());
                case SettingKind.SampleRateOverride: return SettingValue.FromNumber(sampleRateOverride, sampleRateOverride + " Hz");
                default: throw new ArgumentException("Not a sample-level setting: " + kind, "kind");
            }
        }

        /// <summary>Writes one setting from a <see cref="SettingValue"/>.</summary>
        /// <param name="kind">A sample-level setting kind.</param>
        /// <param name="value">The value to store.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="kind"/> is not a sample-level setting.</exception>
        public void Set(SettingKind kind, SettingValue value)
        {
            switch (kind)
            {
                case SettingKind.LoadType: loadType = (LoadMode)(int)value.Number; break;
                case SettingKind.CompressionFormat: compressionFormat = (CompressionKind)(int)value.Number; break;
                case SettingKind.Quality: quality = (float)value.Number; break;
                case SettingKind.SampleRateSetting: sampleRateSetting = (SampleRateMode)(int)value.Number; break;
                case SettingKind.SampleRateOverride: sampleRateOverride = (uint)value.Number; break;
                default: throw new ArgumentException("Not a sample-level setting: " + kind, "kind");
            }
        }
    }

    /// <summary>A typed, display-ready value for a single importer setting.</summary>
    public readonly struct SettingValue : IEquatable<SettingValue>
    {
        /// <summary>Numeric representation (bool: 0/1, enum: underlying int, float/Hz: the number).</summary>
        public readonly double Number;

        /// <summary>Human-readable text used by the preview and reports.</summary>
        public readonly string Display;

        private SettingValue(double number, string display)
        {
            Number = number;
            Display = display;
        }

        /// <summary>Creates a boolean value.</summary>
        /// <param name="value">The flag.</param>
        /// <returns>The value.</returns>
        public static SettingValue FromBool(bool value) { return new SettingValue(value ? 1 : 0, value ? "On" : "Off"); }

        /// <summary>Creates an enum value.</summary>
        /// <param name="number">Underlying integer.</param>
        /// <param name="display">Enum member name.</param>
        /// <returns>The value.</returns>
        public static SettingValue FromEnum(int number, string display) { return new SettingValue(number, display); }

        /// <summary>Creates a numeric value.</summary>
        /// <param name="number">The number.</param>
        /// <param name="display">Display text.</param>
        /// <returns>The value.</returns>
        public static SettingValue FromNumber(double number, string display) { return new SettingValue(number, display); }

        /// <inheritdoc/>
        public bool Equals(SettingValue other) { return Math.Abs(Number - other.Number) < 0.0005; }

        /// <inheritdoc/>
        public override bool Equals(object obj) { return obj is SettingValue other && Equals(other); }

        /// <inheritdoc/>
        public override int GetHashCode() { return Number.GetHashCode(); }

        /// <inheritdoc/>
        public override string ToString() { return Display; }
    }
}
