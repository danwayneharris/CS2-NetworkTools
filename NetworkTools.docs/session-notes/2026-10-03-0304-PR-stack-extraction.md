# Offline execution PR extraction ? 2026-10-03

## Scope and preservation

User authorized separating the offline game execution infrastructure from PR #15
and publishing the companion Bridge PR. Preserved source tip: `3e4f947`.
Research tip: `c198a7c`; original ancestry is retained on an archive branch.

The standalone harness, research docs/compact manifests and generic capture
scripts are extracted onto current main. Feature-specific selection/Apply
helpers and the runtime correction stay in #15. Shared timeout support and
explicit research-test discovery come with the infrastructure so its ordinary
test aggregate remains usable without pretending research CLIs ran.

No live game actions or deployment are part of this reorganization. Existing
raw captures remain local and unchanged; the unrelated UI lockfile remains
in its original worktree. Qualification and publication results follow below.

## Extraction verification

- Standalone native harness build: zero warnings/errors.
- Ordinary Python aggregate: 18/18 scripts pass; four research CLIs explicitly
  not run there and separately exercised below.
- Immutable native cohort: all five cases pass; 1.844445 m native discrepancy
  reproduced to about 0.005 mm residual (not corrected by this infrastructure).
- Pipeline contracts: 16 pass; original-binary value stage cases: seven pass;
  synthetic world capture contracts: 16 pass; raw-edge capture checks: seven pass.
- Compiled Bridge scheduler: eight native scheduling signatures preserved;
  missing/extra sites rejected. Initial invocation used the wrong CLI switch;
  rejected without game effects, rerun successfully with --schedule-transpiler-tests.
- Bridge ordinary/research compilation and installed contract checks pass.
- No new live tests: existing dated captures establish the bounded scope only.

Reports are local under artifacts/pr-split-*; raw captures and generated game
source/binaries are ignored and are not part of the PR.
