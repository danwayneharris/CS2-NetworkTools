# 2026-10-04 — PR 23 merge and PR 21 review deployment

Squash-merged PR #23 as `7ca0b8a`, using the live GitHub title/body including Dan's added warning that this is not yet every noted bug and a fuller reconciliation will follow. Verified the complete message after normalizing line endings; an initial one-sided newline comparison falsely reported a mismatch.

Rebased #21 onto main and #22 onto #21. Runtime trees exactly match their preceding tips. Original tips retained in `archive/pr21-before-backlog-merge-20261004` and the corresponding PR22 archive. Original-checkout user stash left untouched.

With Cities2 absent, built/deployed PR21 `d78c17258c829f8f5824dda5c2c5113644708934`: `1.5.7+gd78c17258c82.clean.Debug`. Full C#/postprocessing/UI build succeeded, 31 warnings and zero errors. Verified all 81 deployed artifacts against build-manifest.json. Log: local ignored `artifacts/pr21-review-build.log`. No live behavior inferred; no launch/mutation performed.

npm changed the initially clean package lock. Automatic review first blocked restoring it pending preservation evidence. Inspected the diff, copied the complete changed file to ignored `artifacts/pr21-build-package-lock.json`, verified equal SHA256, then restored the build-only churn with approval. No user edits were discarded.

Dan's review: load a toy Connect baseline, select endpoints/approaches, enable Lane-aware direction, choose eligible departure/arrival lanes or contiguous groups, and inspect preview then Apply. This controls direction, not lateral alignment. Record review findings in BUG-BACKLOG.md. PR22 remains draft and is not deployed. This note is documentation only after the identified build.
