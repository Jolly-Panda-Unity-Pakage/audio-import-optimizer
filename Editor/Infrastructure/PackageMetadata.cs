// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

namespace JP.AudioImportOptimizer.Editor.Infrastructure
{
    /// <summary>
    /// Single place for author and contact details shown in the UI and in reports.
    /// Keep in sync with package.json when the contact mailbox changes.
    /// </summary>
    public static class PackageMetadata
    {
        /// <summary>Product name.</summary>
        public const string DisplayName = "Audio Import Optimizer";
        /// <summary>Author and copyright holder.</summary>
        public const string Author = "Usef Farahmand";
        /// <summary>Author website.</summary>
        public const string Website = "https://useffarahmand.com";
        /// <summary>Support contact (proposed address; update here and in package.json if it changes).</summary>
        public const string Contact = "contact@useffarahmand.com";
        /// <summary>Package name used by the Package Manager.</summary>
        public const string PackageName = "com.jollypanda.audio-import-optimizer";
        /// <summary>Fallback version used when the package is not installed through the Package Manager. Keep equal to package.json.</summary>
        public const string FallbackVersion = "0.1.0-preview.1";

        /// <summary>Returns the installed package version, or the fallback when it cannot be resolved.</summary>
        /// <returns>A version string.</returns>
        public static string GetVersion()
        {
            var info = GetPackageInfo();
            return info != null ? info.version : FallbackVersion;
        }

        /// <summary>Returns the package folder on disk when installed through the Package Manager.</summary>
        /// <returns>The absolute folder, or null.</returns>
        public static string GetPackageFolder()
        {
            var info = GetPackageInfo();
            return info != null ? info.resolvedPath : null;
        }

        private static UnityEditor.PackageManager.PackageInfo GetPackageInfo()
        {
            return UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PackageMetadata).Assembly);
        }
    }
}
