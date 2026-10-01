# Offline Slope regression

Run `scripts/test-slope.ps1` from the repository root. It compiles Debug without
postprocessing or deployment, then runs against that exact production assembly.
The game may stay open. Requires the installed managed assemblies and .NET 8.

The formula fixtures (flat, hillside, ridge, valley) adopt the deterministic
terrain idea from Morgan Touverey Quilling's PR #74 ProvingGround, without its
Tunnel implementation or native test-map loader. They check both traversal
orientations, physical endpoint grades, endpoint/node offsets and unchanged XZ.
An initial failing test demonstrated a 20 m end handle being treated as 27.889 m.

These are numeric regressions, not a simulated terrain renderer. They do not test
terrain deformation, grade limits, lane construction, preview timing, vehicle
traversal or native allocation. Live tests remain in `exercise-tool-provider.py`.
