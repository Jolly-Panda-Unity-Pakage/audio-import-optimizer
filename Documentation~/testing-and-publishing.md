# Step-by-step: test and publish

Part A comes first; do not start Part B before Part A passes. Already done by the author in Unity 6000.6: compile, window, profile, scan, dry run and one apply (see `testing.md`). The automated tests and the remaining manual checks below still need to be recorded.

## Part A — Test in Unity 6000.6

### A1. Prepare
1. Install Unity 6000.6 with Unity Hub (if the exact version is unavailable, note which version you used and adjust `"unity"` in `package.json` later).
2. Create a **new empty project** (any template). Do not test in a real project.
3. Keep the repository in a normal folder, for example `C:\Dev\audio-import-optimizer`.

### A2. Install the package from disk
1. In the project open `Packages/manifest.json`.
2. Add the package and the `testables` entry (use your real path; forward slashes):
   ```json
   {
     "dependencies": {
       "com.jollypanda.audio-import-optimizer": "file:C:/Dev/audio-import-optimizer"
     },
     "testables": [ "com.jollypanda.audio-import-optimizer" ]
   }
   ```
   (Or *Window > Package Manager > + > Add package from disk…* → select `package.json`, then add `testables` by hand.)
3. Return to Unity and wait for the compile.

### A3. Compile
1. Open the Console. The expected result is **zero errors**.
2. If there are errors, fix them and repeat. The most likely places are `AudioImporter` / `AudioImporterSampleSettings` member names in `Editor/Infrastructure/AudioImporterAdapter.cs` and `UnityEditor.PackageManager.PackageInfo` in `PackageMetadata.cs`. Fix the code, do not remove the feature.
3. Check *Window > Package Manager* shows "Audio Import Optimizer 0.1.0-preview.1" and that `Tools > JP > Audio Import Optimizer` exists.
4. Confirm no assembly warnings about the three `JP.AudioImportOptimizer.*` asmdefs.

### A4. Run the automated tests
1. *Window > General > Test Runner* → **EditMode** tab → **Run All**.
2. Expected: 43 tests (33 pure-logic, 3 snapshot serialization, 7 importer integration). Write down the real numbers: passed, failed, skipped.
3. The integration tests create `Assets/__JP_AIO_Tests__` and delete it afterwards; make sure it is gone after the run. Any snapshot they wrote is deleted too.
4. Fix every failure (code or test) and rerun until all pass. Do not report a pass that did not happen.

### A5. Manual test pass
Create test content: `Assets/Audio/Music`, `Assets/Audio/UI`, `Assets/Audio/VO` and put a few real WAV, MP3 and OGG files in them (one larger than 2 MB, several tiny). Commit the project to Git first so you can see exactly what changes.

| # | Step | Expected |
|---|---|---|
| 1 | Open the window; Overview with no scan | Empty-state message, no errors |
| 2 | Profile tab → *Create starter profile…* | Asset created with a unique name; five rules; no validation errors |
| 3 | Create a second profile at the same path | New file gets a suffix; the first is untouched |
| 4 | Scan tab → Scan | Progress bar; completion message; **no `.meta` file changes in Git** |
| 5 | Scan with a missing folder added | Diagnostic message, other folders still scanned |
| 6 | Cancel a scan on a large folder | "Scan cancelled… incomplete" message |
| 7 | Results: search, filters, sort, multi-select | Works; clicking an asset shows rule, reasons, precedence text, findings |
| 8 | Changes tab | Current → proposed shown; untick an asset and a single change |
| 9 | Apply & Restore → Apply | Confirmation dialog; progress; summary; snapshot file in `ProjectSettings/JP/AudioImportOptimizer/Snapshots` |
| 10 | Inspect an applied clip in the normal Inspector | Values equal the proposal; only included changes were written |
| 11 | Apply again with the same plan | Result "Unchanged"; no reimport |
| 12 | Restore the snapshot | Values return to the originals; a safety snapshot is created |
| 13 | Android override rule | Override created; values correct on the Android tab of the importer |
| 14 | Delete a clip after scanning, then Apply | That clip *Failed*, others applied, Editor still healthy |
| 15 | Export JSON and HTML | Files open outside Unity; only project-relative paths; counts match the UI |
| 16 | Rename a profile rule to a duplicate name | Validation warning; Apply blocked only by *errors* |
| 17 | `git status` after the run | Only the `.meta` files of changed clips and the snapshot files changed |
| 18 | Large project (thousands of clips) | Window stays responsive; list scrolls smoothly |
| 19 | *Project Settings > Editor > Enter Play Mode Options*, Reload Domain **off**; enter/exit Play Mode with the window open; open/close it several times | No duplicate scans, no errors, no stale state |
| 20 | Remove the package | Project compiles; only snapshot files remain (by design) |

Record results in a table like this and keep it with the release notes.

## Part B — Publish

### B1. Before any release
1. Part A fully passed; record the Unity version used.
2. The license is MIT (`LICENSE.md`, © 2026 Usef Farahmand). If you sell on the Asset Store, check how the MIT license sits alongside the Asset Store EULA.
3. Confirm the contact mailbox; if it changes update `package.json` and `Editor/Infrastructure/PackageMetadata.cs`.
4. `"unity"` in `package.json` is `6000.6`, the only version tested; add others only after testing them.
5. Set the version (semantic versioning) in `package.json` **and** `PackageMetadata.FallbackVersion`, and write the release date in `CHANGELOG.md`.
6. Screenshots from Unity 6000.6 are already in `Documentation~/images` and the README; retake them if the UI changes.

### B2. Publish through Git (Package Manager install)
```bash
git add -A
git commit -m "feat: add Audio Import Optimizer 0.1.0-preview.1 Unity Editor package"
git push origin main
git tag 0.1.0-preview.1
git push origin 0.1.0-preview.1
```
1. On GitHub create a **Release** from that tag with the changelog text. Publishing a release triggers `.github/workflows/notify-profile.yml`, which needs the organization secret `PROFILE_DISPATCH_TOKEN`; without it that step fails harmlessly.
2. Users install with *Package Manager > + > Add package from git URL…*:
   `https://github.com/Jolly-Panda-Unity-Pakage/audio-import-optimizer.git` — or pin a version with `#0.1.0-preview.1`.
3. Test that install in a clean project before announcing it.

### B3. Publish on the Unity Asset Store
The steps below follow Unity's current Asset Store documentation as found while writing this guide; Unity changes them, so re-read the Publisher documentation before you start.
1. Create a publisher account at publisher.unity.com and accept the Provider Agreement.
2. In the **Publisher Portal** create a product draft. For a UPM package the draft reserves a *technical name*, and your `package.json` `name` must match it (Unity's example form is `com.company.product.tools`). If the portal gives you a different name, change `name` in `package.json` and `PackageMetadata.PackageName`.
3. **Check whether paid UPM packages are allowed.** Unity's Unity 6.0 manual says paid UPM packages were not available on the Publisher Portal and only free ones could be published. Confirm the current rule in the portal. If a paid UPM package is not possible, publish as an *asset package* instead:
   - Create a clean project and copy the package contents into `Assets/<YourFolder>/AudioImportOptimizer/` (rename `Documentation~` to `Documentation`; leave out `package.json`, `.github`, `.gitignore`, `CONTRIBUTING.md`, and `Tests` if you prefer).
   - Re-run Part A there (the asmdefs work in `Assets/`; the Help tab's "Open documentation" button reports a missing folder and the version falls back to `PackageMetadata.FallbackVersion`, which is why that constant must be kept correct).
4. Install **Asset Store Publishing Tools** (Package Manager → My Assets, or "Open in Unity" from its Asset Store page).
5. Run the **Validator** (menu *Tools > Asset Store > Validator*, or *Window > Tools > Asset Store > Validator* in newer versions). For UPM choose *UPM* as the validation type; for an asset package add the folder path. Fix every error and re-run.
6. Run the **Uploader** (same menu). For UPM use the *UPM Packages* tab → Upload; for an asset package choose the draft → *Export and Upload*. UPM packages are limited to 550 MB (this one is far smaller).
7. In the Publisher Portal complete metadata: title, description, category, price, version, supported Unity versions (only what you tested), release notes, key images and **real** screenshots, support contact, license information. This package ships no audio files, so no audio samples are required.
8. Submit for approval, wait for the review, and fix anything the reviewers report. Never state in the listing that the package is approved until it is.

### B4. After release
Tag every release, keep `CHANGELOG.md` current, bump the version for each update, and re-run Part A before each submission.
