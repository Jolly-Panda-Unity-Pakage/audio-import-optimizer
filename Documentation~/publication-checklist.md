# Publication checklist (nothing here has been done yet)

Unity's Asset Store submission requirements change; verify the current ones in the Asset Store publisher documentation before submitting. This package has not been submitted, approved or published.

- [x] Package compiles in Unity 6000.6 (author-tested). [ ] Check the Console for warnings too.
- [ ] EditMode tests pass in the Unity Test Runner; record results.
- [x] License: MIT (`LICENSE.md`). [ ] Check it against the Asset Store EULA if selling there.
- [ ] Confirm the contact mailbox works; update `package.json` and `Editor/Infrastructure/PackageMetadata.cs` if it changes.
- [ ] Set the release version and date in `CHANGELOG.md` (no date is invented).
- [x] Real screenshots from Unity 6000.6 are in the README. [ ] Promotional/key images and an optional short video.
- [ ] Decide the Asset Store layout (typically `Assets/<PublisherName>/AudioImportOptimizer/...` with `Editor`, `Documentation`, `Tests` optional). Rename `Documentation~` to a normal folder (`Documentation`) there, because `~` folders are not importable. Exclude `.gitignore`, `CONTRIBUTING.md`, `Tests` if desired.
- [ ] Re-import the package into a clean project; check no stray files and that removal leaves nothing behind (`ProjectSettings/JP/...` snapshots stay by design).
- [ ] Test on a large project and on Windows and macOS.
- [ ] Review known limitations in the README and the store description; make no performance or size-saving claims.
- [ ] Do not commit, push or upload without deciding to.
