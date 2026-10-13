# Testing

## Test assemblies
`Tests/EditMode` only. No PlayMode assembly exists because the package has no runtime functionality; adding one would be artificial.

## Test files
- `RuleMatchingTests`, `PlanningTests`, `ProfileValidatorTests`, `SnapshotAndReportTests` — pure logic.
- `SnapshotSerializationTests` — `JsonUtility` round trip and `SnapshotStore`.
- `AudioImporterIntegrationTests` — real importers on temporary WAV files in `Assets/__JP_AIO_Tests__` (read, read-only scan, apply scope, no-op reimport, restore, Android override, deleted asset). Temporary assets and snapshots are removed in `TearDown`.

## What was actually executed (during development, outside Unity)
The Core assembly was compiled with Mono `mcs` against minimal stubs for four `UnityEngine` attributes, and:
- a 34-check console harness passed;
- the 32 test methods in the four pure-logic test files above were run through a small reflection runner with a stub of NUnit's `Assert` — 32 passed, 0 failed.
This is **not** the Unity Test Runner.

## NOT executed
- Compilation of the `Editor` assembly, the window, the tests that need Unity, and the whole package under Unity 6000.6.
- `SnapshotSerializationTests` and `AudioImporterIntegrationTests` (need Unity).
- Domain-reload-disabled behavior, UI behavior, large-project performance.

## How to complete verification
1. Open a clean Unity 6000.6 project, install the package, add it to `testables`.
2. Fix any compile errors (most likely spots: `AudioImporter`/`AudioImporterSampleSettings` member names and `UnityEditor.PackageManager.PackageInfo`).
3. *Window > General > Test Runner > EditMode > Run All*.
4. Test with *Enter Play Mode Options* with Domain Reload disabled; open and close the window repeatedly.
5. Scan a project with thousands of clips and watch responsiveness.
