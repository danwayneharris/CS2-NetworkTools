# Runtime original-input observation

Added Debug-only instance-owned original input copies at Smooth Curve preview
submission, compared after ModificationEndBarrier. Covers path nodes, sorted incident
edge identities, full Edge/Curve values, optional PrefabRef/Composition/Upgraded/
Elevation components, and both endpoint Node values. Includes unselected incident
edges; limits 128 path nodes, 64 incidents/node and 512 unique edges. Missing,
deleted, temporary or nonfinite inputs report unavailable. Exact struct comparison
is conservative. Reports originalInputs=matches/changed/unavailable independently
of revisionMatches. No CanApply or geometry behavior changed.

Scope differs from offline JSON prototype: no copied lane graph or expanded prefab
contents. This detects changes between submission and observation, not a stale
path cache already present at submission. Far-node incident sets are not traversed.
Typed boxed snapshots allocate while the Debug probe is active; performance remains
to be measured. Full production coverage and an acceptance gate remain unfinished.

Verification: MSBuild /t:Compile Debug passed against installed assemblies, without
AfterBuild/postprocessing/UI/deployment. Runtime capture/equality behavior needs
in-game verification; Python prototype tests are not tests of this C# implementation.
Next: full build with game closed, stationary preview expects matches, then an
external edit while a candidate remains pending (if tools allow it) should report
changed/unavailable. Switching tools may instead cancel the candidate; that is not
proof the changed-input path ran. No live edits or deployment this session.

## Deployment

User confirmed game closed; no Cities2 process present. Full Debug build, postprocessing, UI build and deployment passed (0 errors, 33 compiler warnings). Installed DLL SHA256: 3EFDFB97CEBB8742C3C64B93D43975D5ADAB09A6F226C5E045ACCE22067E5BB5. Package-lock changes retained for review after automated approval rejected restoration due to possible pre-existing edits. Untracked programmatic-mod-ui note preserved. Live originalInputs verification pending; next stationary preview at 0.5.


## Live stationary original-input check

User selected branch endpoint 85028:1 -> junction 85022:1 at 0.5. Submission 1,
input/submitted revision 16, revisionMatches=True and originalInputs=matches.
All four selected curves match, no ambiguity/missing lane buffers; bridge resolves
53266:3, complete with no errors and four junction track lanes. Captures stored
under captures/original-inputs-20260928. This verifies the unchanged-input path;
changed/unavailable runtime paths remain untested. Avoid asking for MoveIt edits
that cancel the tool and therefore replace the baseline instead of testing it.
Next independent work should exercise the actual comparison with controlled stale
copies/offline harness before planning a non-destructive live mismatch test.
