# Non-deploying offline aggregate

Audit sprint follow-up for F14 and test-discovery gaps.

- Added `scripts/run-offline-tests.py`: explicitly invokes the geometry executable,
  production-source path-search executable, compiled-production Slope suite, actual
  original-input comparison helper, and existing explicit Python test runner.
- A missing expected entry point/tool or timeout is blocked; a child failure or
  absent execution marker is failed. Any non-pass and an empty aggregate exit nonzero.
  Each completed child produces a log; summary records commands, revision/dirty state,
  Python/platform/.NET context, duration, status, and excluded verification layers.
- `bootstrap.ps1 -OfflineTest` is standalone, routes before deployment prerequisites,
  and rejects combination with install/build/decompile/legacy test/bridge or Release.
  It uses uv when available. Legacy `-Test` behavior remains, now with a deployment
  and incomplete-suite warning. No global environment/settings are changed.
- This is non-deploying, not file-write-free: Slope compiles Debug locally using the
  installed game assemblies; other projects also produce bin/obj outputs. No game
  query, native execution, deployment, or live mutation occurs.

## Verification

Six no-game fake-child tests passed: real execution marker required, nonzero wins
against a success marker, missing entry blocks without launching, missing tool and
 timeout block, empty/failed/blocked/skipped aggregates cannot pass, and explicit
non-deploying stage inventory. PowerShell parser accepted the modified bootstrap.
The aggregate itself has not been run in this subtask to avoid concurrent compilation;
parent runs it after all changes are integrated.

Optional trace/captured-replay modes, Release/Burst, native preview/Apply, vehicle
traversal and human visuals remain explicitly not run by this command. Existing
Python runner is reused rather than duplicated. Mathematical analytical tolerances
are not replaced with the live 5 cm geometric-displacement policy.

## Integration follow-up

Added required standalone parameter and codegen suites after their agents provided
entry points and completion markers. The original-input marker now accepts its
positive reported count (parent expanded 11 to 16 checks) rather than hardcoding
an obsolete count. All seven required stages still reject zero/absent execution
evidence. Six orchestration unit tests passed again; no production builds run by
this subtask.
