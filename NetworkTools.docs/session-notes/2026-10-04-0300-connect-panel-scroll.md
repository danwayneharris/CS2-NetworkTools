# 2026-10-04 03:00 — Connect panel overflow

Read-only bridge inspection of clean Debug d78c172 found profile_approach_required at the departure junction and lane_choice_required at arrival. No candidate had been accepted. Dan reports bottom controls clipped with no scrollbar. Gameface CDP was unavailable in this launch; no direct screenshot/layout measurement was obtained.

Panel source had no overall height cap or scrolling container, while the newly expanded approach/lane controls increased height. Use the installed cs2/ui Scrollable contract (types/ui.d.ts), bound the panel to viewport minus existing top/HUD reservation, retain a fixed header and Apply footer, and let the outer scroll area own choice-list scrolling. Acceptance policy, game selection and geometry are unchanged.

BUG-005 tracks this fix pending in-game review. Validate non-deploying Debug package, then review scroll wheel/track access to both endpoint lane lists and persistent Apply at the user's UI scale. Also check a shorter tool, switching tools, and the prefab picker. Browser/build success cannot establish Gameface layout behavior.

Validation: full Debug PackageOnly passed (31 existing warnings, zero errors), including postprocessing and webpack. No deployment occurred; Cities2 remains running. Package identity is clean 3d8434c. Native layout/scrollbar review pending game closure and deployment. npm regenerated the initially clean lockfile; preserve its full build-only version in ignored artifacts before restoring it.

Deployment follow-up (October 4): verified Cities2 was closed, then full Debug build/deployment passed (31 warnings, zero errors). Installed identity `1.5.7+g531d970a8c1a.clean.Debug` must be read from the manifest; exact identity is recorded in the local build log/manifest. All 81 deployed artifact hashes verified. Native scrollbar behavior awaits Dan's review. Initially clean npm lockfile churn preserved in artifacts/connect-panel-deploy-package-lock.json before restoration.
