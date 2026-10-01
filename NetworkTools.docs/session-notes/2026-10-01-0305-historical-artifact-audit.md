# 2026-10-01 0305 — Historical raw-artifact audit

User authorized a separate cleanup commit in PR #12 for the rest of the repository.

Inventory: 5,485 tracked captures (~86.4 MiB) remained after the terrain-only
cleanup. Outside captures, the largest entries were legitimate source assets,
package lock/type definitions and explanatory plots. The tracked JSONL files are
geometry fixtures. Retained these; no incidental dependency/submodule cleanup.

Archived all 5,485 tracked captures at 77782cc in historical-captures-20261001.zip
(8,069,466 bytes), attached alongside the terrain ZIP on the diagnostic release.
Downloaded again; whole-archive hash and every file's manifest hash passed before
untracking. The exact source revision and per-file paths/hashes are in the ZIP.

Untracked 5,371 files, leaving 114 curated captures (~5.52 MiB), including 36 inputs
used directly by Python regressions and the previously retained terrain inputs.
Local originals remain ignored. A retention list records the kept paths. Session
notes retain their historical paths; the archive index/session-notes README explain
where to recover absent files. This is a tree cleanup, not a main-history rewrite.

Verification: exported only the staged tracked tree, excluding all local untracked
captures. Initial unittest discovery found zero tests because the script names
contain hyphens; this was not treated as a pass. Ran each test script directly:
all 13 passed. Added a reusable runner that fails if no scripts exist or any fails.
No game access, build, deployment, production changes or weakened tests.

The archive tool now supports --all-tracked and uses one git cat-file batch process
instead of thousands of subprocesses; full archive creation/verification passed.
See [archive index](../diagnostic-archives.md). Earlier raw files already in main
remain in its historical objects; reclaiming those would require separate explicit
history-rewrite work, which was not done here.
