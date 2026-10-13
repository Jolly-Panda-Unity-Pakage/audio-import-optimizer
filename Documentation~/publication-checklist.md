# Publication checklist (nothing here has been done yet)

Unity's Asset Store submission requirements change; verify the current ones in the Asset Store publisher documentation before submitting. This package has not been submitted, approved or published.

- [ ] Package compiles in Unity 6000.6 without errors or warnings; fix anything listed in *Testing → NOT executed*.
- [ ] EditMode tests pass in the Unity Test Runner; record results.
- [ ] Choose and add a license (`LICENSE.md` currently states the decision is open). Do not publish without it.
- [ ] Confirm the contact mailbox works; update `package.json` and `Editor/Infrastructure/PackageMetadata.cs` if it changes.
- [ ] Set the release version and date in `CHANGELOG.md` (no date is invented).
- [ ] Capture real screenshots from the Editor (README placeholders), promotional images, short video if desired.
- [ ] Decide the Asset Store layout (typically `Assets/<PublisherName>/AudioImportOptimizer/...` with `Editor`, `Documentation`, `Tests` optional). Rename `Documentation~` to a normal folder (`Documentation`) there, because `~` folders are not importable. Exclude `.gitignore`, `CONTRIBUTING.md`, `Tests` if desired.
- [ ] Re-import the package into a clean project; check no stray files and that removal leaves nothing behind (`ProjectSettings/JP/...` snapshots stay by design).
- [ ] Test on a large project and on Windows and macOS.
- [ ] Review known limitations in the README and the store description; make no performance or size-saving claims.
- [ ] Do not commit, push or upload without deciding to.
