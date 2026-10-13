# Troubleshooting

| Problem | Cause and fix |
|---|---|
| Menu item missing | The package did not compile. Fix Console errors; check Unity is 6000.6 or newer. |
| Tests not listed | Add the package to `testables` in `Packages/manifest.json`. |
| "No audio assets were found" | Wrong folders, an exclusion, or an extension filter. Folders must be project-relative (`Assets/...`). |
| "Folder does not exist" | Typo or deleted folder; it is skipped, other folders still scan. |
| Result says "No rule matched" | Check criteria are AND-combined; empty a list to ignore it; look at folder spelling. |
| Wrong rule wins | Read the precedence text in Results; raise `priority` or add criteria. |
| Apply button disabled | No pending changes, scan running, profile has errors, or assets changed since the scan (scan again). |
| Result *Failed*: "Verification failed" | Unity did not keep the value (format unsupported for that platform). Choose another format for that platform. |
| Result *Failed* on `Packages/...` | Package assets are read-only. Exclude Packages or copy the clip to `Assets`. |
| Platform setting skipped | Its override does not exist and the creation change was excluded. Include the *PlatformOverride* change. |
| Many reimports | Every changed asset reimports. Apply in smaller batches (select assets in Results). |
| Restore reports *Skipped* | The asset was deleted. Moved assets are found by GUID. |
| Restore reports *Failed* | A stored value is not available in the current Unity version; the message names it. |
| Snapshot invalid | The file was edited or written by a newer package version. Do not hand-edit snapshots. |
| Undo does nothing | Undo cannot revert importer settings; restore a snapshot or use version control. |
