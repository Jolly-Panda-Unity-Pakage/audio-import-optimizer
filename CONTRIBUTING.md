# Contributing

- Every namespace must start with `JP`, matching folders and asmdefs.
- All source is English; every public type and member has XML documentation; every serialized user setting has a `[Tooltip]`.
- Keep Core free of `UnityEditor`; keep the package Editor-only.
- Scans and previews must never modify assets; applying must require explicit user confirmation.
- Add or update EditMode tests, and update the English documentation in `Documentation~/`.
- Never claim a test passed unless it was executed.
