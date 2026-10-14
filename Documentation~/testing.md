# Testing

## Test assemblies
`Tests/EditMode` only. No PlayMode assembly exists because the package has no runtime functionality; adding one would be artificial.

## Test files
- `RuleMatchingTests`, `PlanningTests`, `ProfileValidatorTests`, `SnapshotAndReportTests` — pure logic.
- `SnapshotSerializationTests` — `JsonUtility` round trip and `SnapshotStore`.
- `AudioImporterIntegrationTests` — real importers on temporary WAV files in `Assets/__JP_AIO_Tests__` (read, read-only scan, apply scope, no-op reimport, restore, Android override, deleted asset). Temporary assets and snapshots are removed in `TearDown`.

## What was actually executed
**By the author in Unity 6000.6 (manual, with screenshots in `Documentation~/images`):**
- the package compiles (one error, the obsolete `AudioImporter.preloadAudioData`, was found and fixed; Preload Audio Data is now handled in the per-platform sample settings);
- the window opens; a starter profile is created; a scan reads 16 clips; Results, Changes (12 proposed changes in 3 assets) and Apply & Restore render; Apply wrote 3 assets with 0 failures and a snapshot was created.

**Outside Unity (development sandbox):** the Core assembly was compiled with Mono against stubs, a 34-check harness passed, and the pure-logic test files ran through a small stub runner (all passed). This is not the Unity Test Runner.

## NOT yet recorded
- A Unity Test Runner result for the EditMode tests (including `SnapshotSerializationTests` and `AudioImporterIntegrationTests`).
- Manual checks of Restore, platform overrides in the importer Inspector, cancelling a scan, Domain Reload disabled, and a large project.
- Any Unity version other than 6000.6.

## How to complete verification
1. Open a clean Unity 6000.6 project, install the package, add it to `testables`.
2. Fix any compile errors (most likely spots: `AudioImporter`/`AudioImporterSampleSettings` member names and `UnityEditor.PackageManager.PackageInfo`).
3. *Window > General > Test Runner > EditMode > Run All*.
4. Test with *Enter Play Mode Options* with Domain Reload disabled; open and close the window repeatedly.
5. Scan a project with thousands of clips and watch responsiveness.
