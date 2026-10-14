# Optimization profiles

A profile (`OptimizationProfile`, a ScriptableObject) is an ordered list of rules. Create one with *Assets > Create > JP > Audio Import Optimizer > Optimization Profile* or from the window. Duplicate a profile with Ctrl/Cmd+D; starter rules are only ever appended, never replace your rules.

## Rule fields
- **Identity**: name (unique), enabled, category (free text: Music, UI, Voice Over…).
- **Precedence**: priority (higher wins), mismatch severity (Info/Warning/Error).
- **Criteria** (leave empty to ignore): folders, name patterns, extensions, labels, min/max file size in KB.
- **Policy**: default policy and optional per-platform policies.

## Matching
All non-empty criteria must match (AND); values inside one list are alternatives (OR).
- *Folders*: project-relative prefix on a folder boundary, case-insensitive (`Assets/Audio` matches `Assets/Audio/a.wav`, not `Assets/AudioX/a.wav`).
- *Name patterns*: `*` and `?` wildcards against the file name including extension, case-insensitive.
- *Extensions*: include the dot, case-insensitive.
- *Labels*: Unity asset labels; the asset needs at least one. Labels are optional; folders and names work without them.
- *File size*: source file size in KB — not clip duration. A rule with a size range never matches an asset whose size is unknown.
A rule without criteria matches everything (validator reports Info); give it a low priority as a fallback.

## Precedence
1. Higher `priority`. 2. More criteria (specificity). 3. Earlier list position. If steps 1 and 2 tie between the top two rules, finding AIO005 reports the conflict. The Results tab states which rule won and why.

## Policies
Each setting has an *Apply* toggle; if off the setting is left as it is. Supported:
- Asset-level (Default policy only): Force To Mono, Load In Background, Ambisonic.
- Sample-level (Default and per platform): Load Type, Compression Format, Quality (0–1), Sample Rate Setting, Sample Rate Override (Hz; compared only when the effective mode is *Override Sample Rate*), Preload Audio Data (Unity stores it per platform in the sample settings; `AudioImporter.preloadAudioData` is obsolete in Unity 6000.6).
- *Disallowed compression*: formats that raise an Error finding.
Not supported: Normalize (no verified public `AudioImporter` property).

## Platform policies
Importer platform names: `Standalone`, `iPhone`, `Android`, `WebGL` are verified; others are flagged by the validator and are not inspected by the adapter. *Enable Override* on: the override is created from Default if missing, then its values are compared. Off: an existing override is removed — this discards its values (they are in the snapshot), so the change is marked not auto-safe. Windows, macOS and Linux all use `Standalone`. Player Settings are never touched.

## Safe profile design
Start with few rules, check the Changes tab, keep priorities well separated (10, 20, 30), prefer folders over names, treat file-size rules as hints, and review Warnings before Errors. Settings are recommendations: a streaming music track and a UI click need different choices, and mobile and desktop may need different formats.

## Validation
The inspector and Profile tab show: empty profile, unnamed or duplicate rules, rules without criteria, inverted size ranges, bad extensions, unknown or duplicate platforms, out-of-range quality or sample rate, enforced format that is also disallowed, overridden rate without override mode, ambisonic+mono.
