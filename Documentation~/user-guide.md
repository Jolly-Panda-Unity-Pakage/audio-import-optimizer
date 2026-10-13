# User guide

## Safety model
- Scan, analysis, preview and report export are read-only.
- Nothing is applied when a profile is selected. Apply needs a button press and a confirmation dialog.
- Before the first write, a JSON snapshot of the affected assets is saved to `ProjectSettings/JP/AudioImportOptimizer/Snapshots/`. If it cannot be saved, nothing is changed.
- Assets whose effective settings already match are reported *Unchanged* and are not reimported.
- After the editing block closes, every applied asset is re-read; mismatches are downgraded to *Failed* with an explanation.
- `AssetDatabase.StartAssetEditing/StopAssetEditing` wrap the batch, with `StopAssetEditing` in a `finally` block.

## Tabs
**Overview** — total assets, assets matching a rule, warnings, errors, pending changes, last scan time, stale-results warning (shown when audio assets changed after the scan).

**Scan** — *Folders* (one per line; missing folders are reported), *Exclude* (folder prefixes), *Extensions* (empty = every audio format Unity imports as an AudioClip), *Include Packages* (off by default; package assets are often read-only). The scan is time-sliced (about 10 ms per editor tick) so the Editor stays responsive.

**Profile** — object field, *Create starter profile…*, *Edit in Inspector*, *Re-run analysis* (re-evaluates existing scan data without rescanning), rule summary and validation messages.

**Results** — search box (path or category), severity filter, sort (path, severity, category, pending changes), *Has changes* toggle, per-row selection checkbox, *Select all shown*, *Clear selection*, *Exclude selected*, *Include selected*. The list is virtualized, so thousands of assets stay smooth. Clicking an asset shows rule, precedence explanation and findings; *Ping asset* loads the clip only on that click.

**Changes** — dry-run table with two checkboxes per row (asset, change). *Include all*, *Include safe only* (excludes changes that remove a platform override), *Exclude all*. A `(!)` marks changes that are not auto-safe. Every change triggers a reimport of its asset.

**Apply & Restore** — apply scope (all ticked or only assets selected in Results), pending counts, warnings (profile errors, incomplete scan, stale results block or flag Apply), last-operation list with outcomes *Applied / Skipped / Failed / Unchanged*, snapshot list, restore. *Reveal snapshot folder* opens the recovery folder.

**Reports** — JSON and HTML. Contents: package and Unity version, scan time (UTC), scope, profile name, totals, warning/error counts, assets requiring changes, applied/skipped/failed/unchanged counts, all findings with current and proposed values. Only project-relative paths are written. Files go where you choose.

**Help** — workflow summary, documentation, website, author and contact.

## Findings
| Code | Severity | Meaning |
|---|---|---|
| AIO001 | rule's mismatch severity | A Default setting differs from the rule |
| AIO002 | Error | A disallowed compression format is used |
| AIO003 | Error | The importer could not be read |
| AIO004 | Info | No rule matches |
| AIO005 | Warning | Two rules tie on priority and specificity |
| AIO006 | Info | Asset differs from the majority compression of its category (≥ 3 assets, strict majority) |
| AIO007 | rule's mismatch severity | A platform override differs from the rule |
| AIO008 | rule's mismatch severity | A platform override must be created or removed |

## Performance notes
Scanning reads importer metadata only (no clip loading). Applying reimports each changed asset; large batches can take minutes. File size is a cheap stand-in for clip length; the package does not estimate memory or build-size savings because that requires profiling or building.

## Files written
| Location | Content | When |
|---|---|---|
| `ProjectSettings/JP/AudioImportOptimizer/Snapshots/snapshot-<id>.json` | Recovery snapshots | Before apply and before restore |
| `*.tmp` next to a snapshot | Temporary file moved into place | During a save only |
| Location you choose | Reports | On export |
| Profile `.asset` | Profiles | On *Create starter profile…* |
No caches, telemetry or network requests exist. Audio source files are never deleted or modified.
