# 2026-10-01 0255 — Move raw PR captures to a diagnostic release

User requested full cleanup and archival of PR #12's thousands of diagnostic files.

- Packaged all 4,346 newly added captures at a1643a4 with original paths, exact
  source/base revisions and per-file sizes/SHA-256 hashes in manifest.json.
- Uploaded the ZIP (6,580,110 bytes) and whole-archive checksum to a dedicated
  diagnostic prerelease. Downloaded it again, compared its SHA-256, then verified
  every member against the manifest before untracking any files.
- Release tag uses the existing main baseline, avoiding an additional tag that
  retains the capture-heavy branch ancestry. It is not a mod release or Latest.
- Removed 4,305 files from the index only. Original local files remain; no older
  main fixtures were removed. Retained 41 reports/plots and offline replay inputs.
- Added archive index/recovery instructions, historical-note links and ignore
  rules for new raw captures/artifacts. Reusable archive verification script retained.
- One documentation pass encountered an old CP1252 note. Retried preserving each
  note's encoding; archive creation and verification were unaffected.
- Three terrain metric tests, 96 Slope assertions and captured Slope replay passed.
  Replay still agrees with permanent curves within 0.061 mm. Used the existing
  production assembly; no production source changed or game build/deploy occurred.

Full evidence and checksum: [diagnostic archive](../diagnostic-archives.md).
No live game operations were needed. Squash-merge the cleaned PR so intermediate
raw-capture commits are not introduced into main's ancestry. No main history rewrite.
