# Audio Import Optimizer

> Unity Editor tool that analyzes audio import settings and applies configurable optimization profiles with dry-run previews and snapshots.

**Status:** 🧪 Preview `0.1.0-preview.1` — implemented, but **not yet compiled or run inside Unity** (see [Testing](Documentation~/testing.md)). Not submitted to or approved by the Asset Store.

## Why

Audio import settings are easy to get inconsistent across a project (load type, compression, sample rate, mono), which wastes memory and build size. Fixing it by hand is slow and risky.

## Who it is for

Indie developers and small studios shipping to mobile/WebGL/console where audio memory and size matter.

## Features

* Editor window `Tools > JP > Audio Import Optimizer`: overview, scan, profile, results, changes, apply & restore, reports, help
* Read-only, cancellable, time-sliced audio asset scanner (metadata only; no clip loading, no reimport)
* Configurable optimization profiles (`OptimizationProfile` ScriptableObjects) matched by folder, file name pattern, extension, label and file size, with documented precedence and an explanation of why a rule won
* Settings handled: Force To Mono, Load In Background, Ambisonic, Preload Audio Data, Load Type, Compression Format, Quality, Sample Rate Setting, Sample Rate Override
* Platform-specific overrides (Standalone, iPhone, Android, WebGL)
* Validation findings that explain what was detected, why it matters, the proposed change and whether it is safe to auto-apply
* Dry-run previews with per-asset and per-change exclusion; nothing changes until you confirm
* Batch processing with a pre-apply JSON snapshot, no-op detection (no needless reimports), post-apply verification and per-asset results
* Import settings snapshots and recovery (restore with validation)
* Reports: JSON and HTML export
* Fully offline: no telemetry, no network requests

## First milestone (MVP)

Scan a folder, apply one profile with dry-run diff, one-click revert from snapshot, report export. *(Implemented; report formats are JSON and HTML rather than CSV/Markdown.)*

## Compatibility

Declared in `package.json`: Unity `6000.6`. Editor-only, no dependencies, no runtime code. Support for 2021.3 LTS through earlier Unity 6 releases is a goal of the project README but has **not** been verified, so it is not declared yet.

## Installation

Git URL (once the repository is published): *Window > Package Manager > + > Add package from git URL…*

```
https://github.com/Jolly-Panda-Unity-Pakage/audio-import-optimizer.git
```

Or copy the repository folder into your project's `Packages/` folder, or use *Add package from disk…* and select `package.json`. Details: [Getting started](Documentation~/getting-started.md).

## Quick start

1. `Tools > JP > Audio Import Optimizer` → **Profile** → *Create starter profile…* (edit the folders to fit your project).
2. **Scan** → *Scan*.
3. Review **Results** and **Changes** (the dry run).
4. **Apply & Restore** → *Apply ticked changes…*; restore from a snapshot if needed.

## Screenshots

_Placeholders — real screenshots must be captured from a Unity Editor session._

- [ ] Overview  - [ ] Results with rule explanation  - [ ] Changes (dry run)  - [ ] Apply & Restore

## Documentation

English and Persian: [Index](Documentation~/index.md) ([فارسی](Documentation~/index.fa.md)) · [Getting started](Documentation~/getting-started.md) · [User guide](Documentation~/user-guide.md) · [Profiles](Documentation~/optimization-profiles.md) · [Troubleshooting](Documentation~/troubleshooting.md) · [Architecture](Documentation~/architecture.md) · [API](Documentation~/api-reference.md) · [Testing](Documentation~/testing.md) · [Publication checklist](Documentation~/publication-checklist.md)

Persian README: [README.fa.md](README.fa.md).

## Testing

EditMode tests are in `Tests/EditMode`. Outside Unity, the Core logic was compiled with Mono and 32 pure-logic test methods passed through a stub runner; the Unity-dependent tests have **not** been run. Details: [testing.md](Documentation~/testing.md).

## Known limitations

* Apply is not transactional; Undo cannot revert importer settings (use snapshots / version control)
* Normalize is not supported (no verified public importer property)
* Only Standalone, iPhone, Android and WebGL overrides are inspected
* File size, not clip duration, is used for size rules; no memory or build-size savings are estimated
* Snapshots capture only the settings listed above

## Author

Usef Farahmand — https://useffarahmand.com — contact@useffarahmand.com (proposed address). © Usef Farahmand. License: see [LICENSE.md](LICENSE.md) (decision pending).

---

Part of [Jolly Panda Unity Packages](https://github.com/Jolly-Panda-Unity-Pakage). Documentation is provided in English and Persian.
