# Getting started

## What it does and why
Audio Import Optimizer lets you check and change the import settings of many audio clips at once, safely: scans and previews never modify anything, and every apply is preceded by a recovery snapshot.

## Install
1. Copy the package folder into your project's `Packages/` folder (embedded package), or use *Window > Package Manager > + > Add package from disk…* and select `package.json`. A Git URL also works once the repository is published.
2. Requires Unity 6000.6. There are no dependencies.
3. To run the tests, add `"testables": ["com.jollypanda.audio-import-optimizer"]` to your project's `Packages/manifest.json`.

## Open
`Tools > JP > Audio Import Optimizer`.

## Typical workflow
1. **Profile tab** — press *Create starter profile…* (a unique file name is always used; nothing is overwritten) or assign an existing profile. The starter rules refer to folders such as `Assets/Audio/Music`; edit them in the Inspector to match your project.
2. **Scan tab** — set folders, optional exclusions and extensions, press *Scan*. A progress bar and *Cancel scan* are shown. Only importer metadata is read.
3. **Results tab** — search, filter, sort; click an asset to see the matched rule, why it won, and each finding (what, why it matters, proposed change, whether it is safe).
4. **Changes tab** — the dry run: asset, platform, setting, current → proposed value, rule. Untick assets or single changes to exclude them.
5. **Apply & Restore tab** — *Apply ticked changes…* shows a confirmation, saves a snapshot, writes the changes, reimports changed assets, verifies the result and refreshes the scan.
6. **Restore** — pick a snapshot and press *Restore this snapshot…* (confirmation required; a safety backup is saved first).
7. **Reports tab** — export JSON or HTML.

## Limitations in one paragraph
Apply is not transactional; Undo cannot revert importer settings (use snapshots or version control); *Normalize* is not exposed because `AudioImporter` has no public property for it that this package could verify; only the platforms Standalone, iPhone, Android and WebGL are inspected.
