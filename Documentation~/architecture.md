# Architecture

All code is Editor-only; there is no runtime assembly. Namespaces start with `JP`.

| Assembly | Folder | Depends on | Content |
|---|---|---|---|
| `JP.AudioImportOptimizer.Core` | `Editor/Core` | UnityEngine only (serialization attributes) | Models, rules, matching, planning, snapshot models and validation, reports. No `UnityEditor`. |
| `JP.AudioImportOptimizer.Editor` | `Editor` (excluding Core) | Core | `Infrastructure` (adapter, scanner, snapshot store, metadata), `Profiles` (ScriptableObject + inspector), `Services` (batch apply, restore), `UI` (window). |
| `JP.AudioImportOptimizer.Tests.EditMode` | `Tests/EditMode` | Core, Editor, test runner | NUnit tests. |

## Data flow
Scan (`ScanJob`, `AudioImporterAdapter.Read`) → `AudioAssetInfo` list → `PlanBuilder.Build` (`RuleMatcher.Select`, `ChangePlanner.Plan`, consistency pass) → `OptimizationPlan` (dry run, user toggles) → `BatchApplier.Apply` (fresh read → snapshot → `ChangeEvaluator.SelectEffective` → write → verify) → `AnalysisReport`.

## Decisions
- **No Undo for importers**: importer settings are persisted by `SaveAndReimport`; Undo cannot reliably revert them, so snapshots and version control are the recovery path.
- **Core separated from Unity importer types**: own enums mirror Unity's, and the adapter maps them by name so a member missing in a Unity version gives a clear error, not a wrong value.
- **IMGUI** window with a virtualized list: simpler and more reliable for thousands of rows.
- **Service layer is named `Services`**, not `Application`, because a namespace called `Application` would shadow `UnityEngine.Application`.
- **Domain reload**: the window subscribes to `EditorApplication.update` in `OnEnable` and unsubscribes in `OnDisable`; the only static state is a change counter used for stale-result warnings. No `[InitializeOnLoad]`.
- **Threading**: everything runs on the main thread, time-sliced.
- **Extension points**: add rules via profiles; reuse `RuleMatcher`, `ChangePlanner`, `ReportWriter` from your own editor code; add platforms to `PlatformCatalog.KnownPlatforms`.
