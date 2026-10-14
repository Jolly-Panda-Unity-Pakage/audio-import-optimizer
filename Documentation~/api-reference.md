# API Reference

Generated from the actual source. All APIs live in Editor-only assemblies (there is no runtime code). Only public types and members are listed.


## `JP.AudioImportOptimizer.Core.Matching`


### class `GlobMatcher`

`public static class GlobMatcher`

Wildcard matching with * (any run) and ? (any one character), case-insensitive.

- `public static bool IsMatch(string pattern, string text)` — Tests a text against a wildcard pattern. Pattern with * and ? wildcards. Text to test. True when the whole text matches.

### class `RuleMatch`

`public sealed class RuleMatch`

A successful rule match with human-readable reasons.

- `public OptimizationRule Rule` — The matching rule.
- `public int Index` — Index of the rule in the profile (used as the last tie-breaker).
- `public int Specificity` — Number of criteria the rule defines.
- `public List<string> Reasons = new List<string>()` — One sentence per satisfied criterion.

### class `RuleSelection`

`public sealed class RuleSelection`

The outcome of selecting a rule for one asset.

- `public RuleMatch Selected` — The winning match, or null when no rule matched.
- `public List<RuleMatch> Candidates = new List<RuleMatch>()` — All matching rules ordered from best to worst.
- `public bool HasConflict` — True when the two best candidates tie on priority and specificity (list order decided).
- `public string Explanation` — Explains why the selected rule won; empty when nothing matched.

### class `RuleMatcher`

`public static class RuleMatcher`

Matches assets against rules. Precedence: higher priority, then more criteria, then earlier list position.

- `public static RuleMatch Match(OptimizationRule rule, AudioAssetInfo asset, int index)` — Tests one rule against one asset. The rule. The asset. Rule index in the profile. The match, or null if the rule is disabled or does not match. Thrown when an argument is null.
- `public static RuleSelection Select(IReadOnlyList<OptimizationRule> rules, AudioAssetInfo asset)` — Selects the winning rule for an asset. Profile rules in list order. The asset. The selection;  is null when nothing matched.

## `JP.AudioImportOptimizer.Core.Models`


### class `PlatformState`

`public sealed class PlatformState`

Platform override state of one build target for one asset.

- `public string platform` — Importer platform name, for example "Android" or "Standalone".
- `public bool hasOverride` — True when an explicit override exists; false when the asset inherits Default.
- `public SampleSettingsValues settings` — The override values; null when  is false.

### class `AudioAssetInfo`

`public sealed class AudioAssetInfo`

Everything the analysis needs to know about one audio asset. Contains no Unity object references.

- `public string Path` — Project-relative asset path using forward slashes.
- `public string Guid` — Asset GUID, empty when unknown.
- `public long FileSizeBytes` — Source file size in bytes, -1 when unavailable.
- `public List<string> Labels = new List<string>()` — Asset labels (AssetDatabase.GetLabels).
- `public string ReadError` — Null when the importer was read successfully; otherwise a description of the failure.
- `public bool ForceToMono` — AudioImporter.forceToMono.
- `public bool LoadInBackground` — AudioImporter.loadInBackground.
- `public bool Ambisonic` — AudioImporter.ambisonic.
- `public SampleSettingsValues Default = new SampleSettingsValues()` — Default sample settings.
- `public List<PlatformState> Platforms = new List<PlatformState>()` — Per-platform override state for every platform the adapter inspected.
- `public string Extension` — Lower-case extension including the dot, for example ".wav".
- `public SettingValue GetAssetLevel(SettingKind kind)` — Reads an asset-level (non-sample) setting. One of ForceToMono, LoadInBackground, Ambisonic. The current value. Thrown for other kinds.
- `public PlatformState FindPlatform(string platform)` — Finds the state for a platform. Importer platform name. The state, or null if the platform was not inspected.

### enum `Severity`

`public enum Severity`

Importance of a finding produced by the analysis.

- `Info = 0` — Informational only; no action is required.
- `Warning = 1` — The asset deviates from a profile policy.
- `Error = 2` — The asset is broken or violates a policy that must be fixed.

### enum `SettingKind`

`public enum SettingKind`

Importer settings the package can read, compare, change, snapshot and restore.

- `ForceToMono = 0` — AudioImporter.forceToMono.
- `LoadInBackground = 1` — AudioImporter.loadInBackground.
- `Ambisonic = 2` — AudioImporter.ambisonic.
- `PreloadAudioData = 3` — AudioImporterSampleSettings.preloadAudioData (a per-platform setting).
- `LoadType = 4` — AudioImporterSampleSettings.loadType.
- `CompressionFormat = 5` — AudioImporterSampleSettings.compressionFormat.
- `Quality = 6` — AudioImporterSampleSettings.quality (0..1).
- `SampleRateSetting = 7` — AudioImporterSampleSettings.sampleRateSetting.
- `SampleRateOverride = 8` — AudioImporterSampleSettings.sampleRateOverride (Hz).
- `PlatformOverride = 9` — Whether a platform override block exists (enabled) or is inherited from Default.

### enum `LoadMode`

`public enum LoadMode`

Mirror of UnityEngine.AudioClipLoadType kept here so Core stays free of importer types.

- `DecompressOnLoad = 0` — Decompress the clip when it is loaded.
- `CompressedInMemory = 1` — Keep the clip compressed in memory.
- `Streaming = 2` — Stream the clip from disk.

### enum `CompressionKind`

`public enum CompressionKind`

Mirror of UnityEngine.AudioCompressionFormat.

- `PCM = 0` — Uncompressed PCM.
- `Vorbis = 1` — Ogg Vorbis.
- `ADPCM = 2` — ADPCM.
- `MP3 = 3` — MP3.
- `VAG = 4` — VAG (PlayStation).
- `HEVAG = 5` — HEVAG (PlayStation).
- `XMA = 6` — XMA (Xbox).
- `AAC = 7` — AAC.
- `GCADPCM = 8` — GCADPCM (Nintendo).
- `ATRAC9 = 9` — ATRAC9 (PlayStation).

### enum `SampleRateMode`

`public enum SampleRateMode`

Mirror of UnityEngine.AudioSampleRateSetting.

- `PreserveSampleRate = 0` — Keep the source sample rate.
- `OptimizeSampleRate = 1` — Let Unity pick the lowest rate that preserves the content.
- `OverrideSampleRate = 2` — Use an explicit sample rate.

### class `SampleSettingsValues`

`public sealed class SampleSettingsValues`

Plain-data copy of AudioImporterSampleSettings used by analysis and snapshots.

- `public LoadMode loadType` — Clip load type.
- `public CompressionKind compressionFormat` — Compression format.
- `public float quality` — Quality in the range 0..1 (only meaningful for lossy formats).
- `public SampleRateMode sampleRateSetting` — Sample rate mode.
- `public uint sampleRateOverride` — Sample rate override in Hz (only used when the mode is OverrideSampleRate).
- `public bool preloadAudioData` — Preload audio data. Unity stores this per platform inside the sample settings.
- `public SampleSettingsValues Clone()` — Creates a deep copy. A new instance with identical values.
- `public SettingValue Get(SettingKind kind)` — Reads one setting as a . A sample-level setting kind. The current value. Thrown when  is not a sample-level setting.
- `public void Set(SettingKind kind, SettingValue value)` — Writes one setting from a . A sample-level setting kind. The value to store. Thrown when  is not a sample-level setting.

### struct `SettingValue`

`public readonly struct SettingValue : IEquatable<SettingValue>`

A typed, display-ready value for a single importer setting.

- `public readonly double Number` — Numeric representation (bool: 0/1, enum: underlying int, float/Hz: the number).
- `public readonly string Display` — Human-readable text used by the preview and reports.
- `public static SettingValue FromBool(bool value) { return new SettingValue(value ? 1 : 0, value ? "On" : "Off"); }` — Creates a boolean value. The flag. The value.
- `public static SettingValue FromEnum(int number, string display) { return new SettingValue(number, display); }` — Creates an enum value. Underlying integer. Enum member name. The value.
- `public static SettingValue FromNumber(double number, string display) { return new SettingValue(number, display); }` — Creates a numeric value. The number. Display text. The value.
- `public bool Equals(SettingValue other) { return Math.Abs(Number - other.Number) < 0.0005; }`
- `public override bool Equals(object obj) { return obj is SettingValue other && Equals(other); }`
- `public override int GetHashCode() { return Number.GetHashCode(); }`
- `public override string ToString() { return Display; }`

## `JP.AudioImportOptimizer.Core.Planning`


### class `ChangeEvaluator`

`public static class ChangeEvaluator`

Reads the current value of a change's target setting from an asset state. Used for no-op detection and post-apply verification.

- `public static bool TryGetCurrent(AudioAssetInfo asset, ProposedChange change, out SettingValue value)` — Tries to read the value a change targets. Fresh asset state. The change. The current value when found. False when the target does not exist yet (for example a platform override that has not been created). Thrown when an argument is null.
- `public static List<ProposedChange> SelectEffective(AudioAssetInfo fresh, IEnumerable<ProposedChange> requested, List<string> notes)` — Selects the changes that still need to be applied and explains the ones that must be dropped. Fresh asset state read just before applying. Changes the user included. Receives one sentence per dropped change. Changes that would actually modify the asset. Empty means the asset is unchanged.

### class `ChangePlanner`

`public static class ChangePlanner`

Compares asset state with the selected rule and produces findings and proposed changes. Pure logic; never touches assets.

- `public static AssetPlan Plan(AudioAssetInfo asset, RuleSelection selection)` — Plans one asset. The asset state. The rule selection for the asset. The asset plan. Contains no changes when the asset already satisfies its rule. Thrown when an argument is null.

### class `PlanBuilder`

`public static class PlanBuilder`

Builds a full  from scanned assets and rules.

- `public const int MinimumCategorySize` — Minimum number of assets in a category before consistency checks run.
- `public static OptimizationPlan Build(IEnumerable<AudioAssetInfo> assets, IReadOnlyList<OptimizationRule> rules)` — Selects rules, plans every asset and adds category consistency findings. Scanned assets. Profile rules. The plan. Thrown when an argument is null.

### class `FindingCodes`

`public static class FindingCodes`

Stable identifiers of the built-in findings.

- `public const string DefaultMismatch` — A Default setting differs from the matched rule's policy.
- `public const string DisallowedCompression` — A disallowed compression format is in use.
- `public const string ImporterReadFailed` — The importer could not be read.
- `public const string NoRuleMatched` — No rule matches the asset.
- `public const string RuleConflict` — Two rules tie on priority and specificity.
- `public const string InconsistentCategory` — The asset differs from the majority of its category.
- `public const string PlatformMismatch` — A platform override differs from the policy.
- `public const string PlatformOverrideState` — A platform override must be created or removed.

### class `ProposedChange`

`public sealed class ProposedChange`

One concrete importer change the user can accept or exclude.

- `public string AssetPath` — Project-relative asset path.
- `public string Platform` — "Default" or an importer platform name.
- `public SettingKind Kind` — Which setting changes.
- `public SettingValue Current` — Current value.
- `public SettingValue Proposed` — Proposed value.
- `public string RuleName` — Name of the rule that requested the change.
- `public string Reason` — Why the change is proposed.
- `public Severity Severity` — Severity of the underlying finding.
- `public bool SafeToAutoApply` — True when the change can be applied without extra caution (it never discards an override).
- `public bool TriggersReimport` — Every importer change triggers a reimport of the asset.
- `public bool Included` — User toggle; excluded changes are never applied.

### class `Finding`

`public sealed class Finding`

An explained observation about an asset.

- `public string Code` — Stable code, see .
- `public Severity Severity` — Severity.
- `public string AssetPath` — Project-relative asset path.
- `public string Title` — Short title.
- `public string Detected` — What was detected.
- `public string WhyItMatters` — Why it matters.
- `public string Setting` — The setting involved, or empty.
- `public string ProposedSummary` — The proposed change in words, or empty when manual action is needed.
- `public bool SafeToApply` — True when the proposed change is safe to apply automatically.
- `public ProposedChange Change` — The attached change, or null for informational findings.

### class `AssetPlan`

`public sealed class AssetPlan`

Analysis result for one asset.

- `public AudioAssetInfo Asset` — The inspected asset.
- `public RuleSelection Selection = new RuleSelection()` — Rule selection and explanation.
- `public List<Finding> Findings = new List<Finding>()` — Findings for this asset.
- `public List<ProposedChange> Changes = new List<ProposedChange>()` — Changes proposed for this asset.
- `public bool Excluded` — User toggle; an excluded asset is skipped entirely.
- `public string Category` — Category of the selected rule, or "Unmatched".
- `public Severity WorstSeverity` — Highest severity among the findings.
- `public int PendingChangeCount` — Number of changes that would be applied right now.

### class `OptimizationPlan`

`public sealed class OptimizationPlan`

The full dry-run result for a scan.

- `public List<AssetPlan> Assets = new List<AssetPlan>()` — Per-asset results in scan order.
- `public int FindingCount` — Total number of findings.
- `public int CountFindings(Severity severity)` — Counts findings of one severity. Severity to count. The count.
- `public int AssetsWithPendingChanges` — Number of assets that currently have at least one pending change.
- `public int PendingChangeCount` — Total number of pending changes.

## `JP.AudioImportOptimizer.Core.Profiles`


### class `OptimizationRule`

`public sealed class OptimizationRule`

One matching rule plus the policy applied to matching assets. All non-empty criteria must match (AND); entries inside one list are alternatives (OR).

- `public string ruleName`
- `public bool enabled`
- `public string category`
- `public int priority`
- `public Severity mismatchSeverity`
- `public List<string> folders = new List<string>()`
- `public List<string> namePatterns = new List<string>()`
- `public List<string> extensions = new List<string>()`
- `public List<string> labels = new List<string>()`
- `public int minFileSizeKb`
- `public int maxFileSizeKb`
- `public SettingsPolicy defaultPolicy = new SettingsPolicy()`
- `public List<PlatformPolicy> platformPolicies = new List<PlatformPolicy>()`
- `public int CountCriteria()` — Counts the non-empty matching criteria (the rule's specificity). A number between 0 and 5.

### class `ValidationIssue`

`public sealed class ValidationIssue`

A problem found in a profile.

- `public Severity Severity` — How serious the problem is.
- `public string RuleName` — Name of the rule involved, or empty for profile-level issues.
- `public string Message` — What is wrong and how to fix it.
- `public ValidationIssue(Severity severity, string ruleName, string message)` — Creates an issue. Severity. Rule name or empty. Message.

### class `ProfileValidator`

`public static class ProfileValidator`

Validates a list of rules without touching any asset.

- `public static List<ValidationIssue> Validate(IReadOnlyList<OptimizationRule> rules)` — Validates rules and returns every issue found. The rules to check; null is treated as empty. The issues, possibly empty.

### class `SettingsPolicy`

`public sealed class SettingsPolicy`

Desired importer settings. A setting is only enforced when its matching "apply" toggle is on; everything else is left exactly as the asset currently has it.

- `public const int MaxSampleRateHz` — Highest sample rate Unity accepts for an override, used for validation.
- `public bool applyForceToMono`
- `public bool forceToMono`
- `public bool applyLoadInBackground`
- `public bool loadInBackground`
- `public bool applyAmbisonic`
- `public bool ambisonic`
- `public bool applyLoadType`
- `public LoadMode loadType`
- `public bool applyCompressionFormat`
- `public CompressionKind compressionFormat`
- `public bool applyQuality`
- `public float quality`
- `public bool applySampleRateSetting`
- `public SampleRateMode sampleRateSetting`
- `public bool applySampleRateOverride`
- `public int sampleRateOverrideHz`
- `public bool applyPreloadAudioData`
- `public bool preloadAudioData`
- `public List<CompressionKind> disallowedCompression = new List<CompressionKind>()`
- `public bool HasAnyEnforcement()` — Returns true when at least one setting or disallow list entry is enforced. True when the policy does something.
- `public bool TryGetAssetLevel(SettingKind kind, out SettingValue value)` — Returns the desired value of an asset-level setting if it is enforced. ForceToMono, LoadInBackground or Ambisonic. The desired value when enforced. True when the setting is enforced.
- `public bool TryGetSampleLevel(SettingKind kind, out SettingValue value)` — Returns the desired value of a sample-level setting if it is enforced. LoadType, CompressionFormat, Quality, SampleRateSetting, SampleRateOverride or PreloadAudioData. The desired value when enforced. True when the setting is enforced.

### class `PlatformPolicy`

`public sealed class PlatformPolicy`

Policy for one platform override block.

- `public string platform` — Importer platform name (see ).
- `public bool enableOverride`
- `public SettingsPolicy policy = new SettingsPolicy()`

### class `PlatformCatalog`

`public static class PlatformCatalog`

Known importer platform names. Other names are accepted but flagged as unverified.

- `public const string DefaultPlatform` — Name used in findings and changes for the Default sample settings.
- `public static readonly string[] KnownPlatforms` — Platform names verified to work with AudioImporter override APIs.
- `public static bool IsKnown(string platform)` — Checks whether a platform name is one of . Platform name. True when known.

## `JP.AudioImportOptimizer.Core.Reporting`


### enum `ApplyOutcome`

`public enum ApplyOutcome`

Result category for one asset in a batch apply.

- `Applied = 0` — At least one setting was changed and the asset was reimported.
- `Skipped = 1` — The asset was skipped (excluded, cancelled, moved or deleted).
- `Failed = 2` — Applying failed; the message explains why.
- `Unchanged = 3` — The effective settings already matched; no reimport was triggered.

### class `AssetApplyResult`

`public sealed class AssetApplyResult`

Apply result for one asset.

- `public string AssetPath` — Project-relative path.
- `public ApplyOutcome Outcome` — Outcome.
- `public string Message` — Explanation, for example the exception message.

### class `AnalysisReport`

`public sealed class AnalysisReport`

Everything a report contains. Only project-relative paths are included.

- `public string PackageVersion` — Package version.
- `public string UnityVersion` — Unity version.
- `public DateTime ScanTimeUtc` — UTC time of the scan.
- `public string Scope` — Human-readable scan scope, for example "Assets/Audio".
- `public string ProfileName` — Name of the profile used.
- `public bool ScanIncomplete` — True when the scan was cancelled before completion.
- `public OptimizationPlan Plan = new OptimizationPlan()` — The dry-run plan.
- `public List<AssetApplyResult> ApplyResults = new List<AssetApplyResult>()` — Apply results, empty if nothing was applied yet.
- `public int CountApply(ApplyOutcome outcome)` — Counts apply results of one outcome. Outcome to count. The count.

### class `ReportWriter`

`public static class ReportWriter`

Serializes reports to JSON and self-contained HTML without any dependency.

- `public const int FormatVersion` — Report format version written into the JSON.
- `public static string ToJson(AnalysisReport report)` — Writes the report as indented JSON. The report. JSON text. Thrown when report is null.
- `public static string ToHtml(AnalysisReport report)` — Writes the report as a standalone HTML page (no scripts, no external resources). The report. HTML text. Thrown when report is null.

## `JP.AudioImportOptimizer.Core.Snapshots`


### class `SnapshotEntry`

`public sealed class SnapshotEntry`

One asset's captured importer state.

- `public string assetPath` — Project-relative path at capture time.
- `public string guid` — GUID at capture time; used to find the asset if it moved.
- `public bool forceToMono` — AudioImporter.forceToMono.
- `public bool loadInBackground` — AudioImporter.loadInBackground.
- `public bool ambisonic` — AudioImporter.ambisonic.
- `public SampleSettingsValues defaultSettings = new SampleSettingsValues()` — Default sample settings.
- `public List<PlatformState> platforms = new List<PlatformState>()` — Platform override states for the platforms the package manages.
- `public static SnapshotEntry FromAsset(AudioAssetInfo asset)` — Captures the current state of an asset. A successfully read asset. A deep copy suitable for serialization. Thrown when asset is null.
- `public bool MatchesAsset(AudioAssetInfo asset)` — Checks whether an asset's current state already equals this entry (for the platforms the entry lists). The current asset state. True when restoring would change nothing. Thrown when asset is null.

### class `SettingsSnapshot`

`public sealed class SettingsSnapshot`

A recoverable record of importer settings taken before a batch apply.

- `public const int CurrentSchemaVersion` — Schema version written by this package.
- `public int schemaVersion` — Schema version of this file.
- `public string id` — Unique id (also used in the file name).
- `public string label` — Optional label shown in the restore list.
- `public string createdUtc` — UTC creation time, ISO 8601.
- `public string packageVersion` — Package version that wrote the file.
- `public string unityVersion` — Unity version that wrote the file.
- `public List<SnapshotEntry> entries = new List<SnapshotEntry>()` — Captured assets.

### class `SnapshotValidator`

`public static class SnapshotValidator`

Validates a snapshot before any restore is attempted.

- `public static List<string> Validate(SettingsSnapshot snapshot)` — Checks structure, schema version and value ranges. The snapshot to check; null yields an error. Error and warning messages. An empty list means the snapshot is structurally valid.

## `JP.AudioImportOptimizer.Editor.Infrastructure`


### class `ScanOptions`

`public sealed class ScanOptions`

User-configurable scan scope.

- `public List<string> includeFolders` — Folders to search. Defaults to the whole Assets folder.
- `public List<string> excludeFolders = new List<string>()` — Folders skipped even when inside an included folder.
- `public bool includePackages` — When true the Packages folder is searched too. Package assets are often read-only, so applying changes there can fail.
- `public List<string> extensions = new List<string>()` — Optional extension filter (for example .wav). Empty means every audio asset Unity can import.

### class `ScanJob`

`public sealed class ScanJob`

A time-sliced, cancellable, read-only scan. Call  repeatedly (for example from EditorApplication.update) until  is true. Only importer metadata is read; no clip is loaded and nothing is reimported.

- `public readonly List<AudioAssetInfo> Results = new List<AudioAssetInfo>()` — Assets read so far.
- `public readonly List<string> Diagnostics = new List<string>()` — Problems with the scan configuration (missing folders and similar).
- `public bool IsDone` — True once every asset has been read or the job was cancelled.
- `public bool WasCancelled` — True when  was called before completion.
- `public float Progress` — Progress between 0 and 1.
- `public int TotalCount` — Total number of assets that will be read.
- `public ScanJob(ScanOptions options)` — Starts a scan by enumerating candidate asset paths (cheap index query, no importer access). The scope. Thrown when options is null.
- `public void Step(double budgetMilliseconds)` — Reads assets until the time budget is used up. Maximum time to spend in this call.
- `public void Cancel()` — Stops the scan at the next safe boundary; results read so far are kept.

### class `AudioAssetChangeWatcher`

`public sealed class AudioAssetChangeWatcher : AssetPostprocessor`

Tracks whether audio assets changed after the last scan so the UI can warn that results may be stale. It only increments a counter; it never touches assets.

- `public static int ChangeVersion` — Incremented whenever an imported, deleted or moved asset has a common audio extension.

### class `AudioImporterAdapter`

`public static class AudioImporterAdapter`

The only class that talks to AudioImporter. Converts between importer state and the pure Core models. Reading never modifies an asset; writing never reimports (the caller decides when).

- `public static AudioAssetInfo Read(string assetPath)` — Reads the importer state of an asset without loading the clip. Project-relative asset path. The asset info.  is set when reading failed.
- `public static void ReadInto(AudioImporter importer, AudioAssetInfo info)` — Fills an  from an importer. The importer. The info object to fill.
- `public static void ApplyChanges(AudioImporter importer, IEnumerable<ProposedChange> changes)` — Applies proposed changes to an importer in memory. Does not save or reimport. The importer. Changes for this asset only. Thrown when a value cannot be represented by this Unity version.
- `public static void WriteEntry(AudioImporter importer, SnapshotEntry entry)` — Writes a complete snapshot entry back to an importer in memory. Does not save or reimport. The importer. The entry to restore. Thrown when a value cannot be represented by this Unity version.

### class `PackageMetadata`

`public static class PackageMetadata`

Single place for author and contact details shown in the UI and in reports. Keep in sync with package.json when the contact mailbox changes.

- `public const string DisplayName` — Product name.
- `public const string Author` — Author and copyright holder.
- `public const string Website` — Author website.
- `public const string Contact` — Support contact (proposed address; update here and in package.json if it changes).
- `public const string PackageName` — Package name used by the Package Manager.
- `public const string FallbackVersion` — Fallback version used when the package is not installed through the Package Manager. Keep equal to package.json.
- `public static string GetVersion()` — Returns the installed package version, or the fallback when it cannot be resolved. A version string.
- `public static string GetPackageFolder()` — Returns the package folder on disk when installed through the Package Manager. The absolute folder, or null.

### class `SnapshotInfo`

`public sealed class SnapshotInfo`

A snapshot file found on disk.

- `public string Id` — Snapshot id.
- `public string Label` — Label stored in the file.
- `public string CreatedUtc` — Creation time (UTC, ISO 8601).
- `public int AssetCount` — Number of assets captured.
- `public string FilePath` — Absolute file path.

### class `SnapshotStore`

`public static class SnapshotStore`

Persists snapshots as JSON under ProjectSettings/JP/AudioImportOptimizer/Snapshots (outside Assets, so no import happens, and inside version control by default). Writes go to a temporary file first and are moved into place.

- `public static string Folder` — Absolute folder that holds snapshot files.
- `public static SettingsSnapshot Create(string label)` — Creates an empty snapshot with a fresh id and metadata. Optional label. A new snapshot without entries.
- `public static string Save(SettingsSnapshot snapshot)` — Writes a snapshot to disk. A valid snapshot. The absolute file path. Thrown when the snapshot is invalid. Thrown when the file cannot be written.
- `public static SettingsSnapshot Load(string id)` — Loads a snapshot by id. The result is not validated; call . Snapshot id. The snapshot, or null when the file is missing or unreadable.
- `public static List<SnapshotInfo> List()` — Lists snapshots, newest first. Unreadable files are listed with a label that says so. The snapshot infos.

## `JP.AudioImportOptimizer.Editor.Profiles`


### class `OptimizationProfile`

`public sealed class OptimizationProfile : ScriptableObject`

A reusable set of optimization rules stored as a ScriptableObject asset. Duplicate it (Ctrl/Cmd+D) to create variants. A profile never changes any audio asset by itself; changes are only applied after a reviewed dry run.

- `public string Description` — Free-text description.
- `public IReadOnlyList<OptimizationRule> Rules` — The rules in list order.
- `public void AppendRules(IEnumerable<OptimizationRule> toAdd)` — Appends rules (used by the "Add default rules" button; never replaces existing rules). Rules to append.

### class `DefaultRules`

`public static class DefaultRules`

Provides the built-in starter rules. They are examples: edit folders and values for your project.

- `public static List<OptimizationRule> Create()` — Creates a fresh copy of the starter rules. New rule instances.

### class `OptimizationProfileEditor`

`public sealed class OptimizationProfileEditor : UnityEditor.Editor`

Inspector for : shows live validation and explains what the profile does and does not do.

- `public override void OnInspectorGUI()`

## `JP.AudioImportOptimizer.Editor.Services`


### delegate `bool ProgressCallback`

`public delegate bool ProgressCallback(string title, string info, float progress);`

Reports progress and returns true when the user asked to cancel. Operation title. Current item description. Progress between 0 and 1. True to cancel at the next safe boundary.


### class `BatchResult`

`public sealed class BatchResult`

Outcome of a batch apply or restore.

- `public readonly List<AssetApplyResult> Results = new List<AssetApplyResult>()` — Per-asset results.
- `public string SnapshotId` — Id of the snapshot written before changing anything; null when nothing was captured.
- `public bool Cancelled` — True when the user cancelled; remaining assets are reported as skipped.
- `public string FatalError` — Set when the whole operation was refused (for example the snapshot could not be saved). No asset was modified.
- `public int Count(ApplyOutcome outcome) { int n = 0; foreach (var r in Results) if (r.Outcome == outcome) n++; return n; }` — Counts results with the given outcome. Outcome to count. The count.

### class `BatchApplier`

`public static class BatchApplier`

Applies the included changes of a plan. Not transactional: each asset is saved independently, so a failure or cancellation leaves earlier assets changed. A snapshot of the original values is written first and is the recovery path.

- `public static BatchResult Apply(OptimizationPlan plan, ISet<string> onlyAssets, string label, ProgressCallback progress)` — Applies included changes. The reviewed plan. Optional set of asset paths to restrict the operation to; null applies to every non-excluded asset. Label stored in the snapshot. Optional progress callback. The result. Never null. Thrown when plan is null.

### class `RestoreService`

`public static class RestoreService`

Restores the importer settings captured by a snapshot: Force To Mono, Load In Background, Ambisonic, Preload Audio Data, the Default sample settings and the override state of the platforms the package manages. Other importer data is untouched.

- `public static BatchResult Restore(SettingsSnapshot snapshot, ProgressCallback progress)` — Restores a snapshot. Must only be called after explicit user confirmation. The snapshot to restore. Optional progress callback. The result.  is set when the snapshot was refused. Thrown when snapshot is null.

## `JP.AudioImportOptimizer.Editor.UI`


### class `AudioImportOptimizerWindow`

`public sealed partial class AudioImportOptimizerWindow`

Apply, Restore and Reports tabs of the main window.

- `public static void Open()` — Opens the window.
