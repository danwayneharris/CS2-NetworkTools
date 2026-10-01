# Diagnostic archives

Keep source, tests, small result summaries, and necessary offline regression inputs
in Git. Write new raw runs to ignored `artifacts/`; full evidence belongs in a
checksummed diagnostic release attachment, not source commits. Existing tracked
historical captures and curated fixtures remain tracked despite the ignore rule.

## Terrain investigation ? PR #12, 2026-10-01

[Download the diagnostic prerelease](https://github.com/danwayneharris/CS2-NetworkTools/releases/tag/diagnostics-terrain-pr12-20261001).
This is evidence, **not a playable mod release**. Use its named ZIP attachment,
not GitHub's automatic Source code archives.

- Asset: `terrain-pr12-captures-20261001.zip` (6,580,110 bytes).
- SHA-256: `1624bcd738b5069ad9b762bf42deae64d623ea5a785b0df04b93ec3d27dc08cd`.
- Contains all 4,346 newly added captures at source commit
  `a1643a4c26f318fe78526c986bad43a93d580ff6`, including failures and intermediate runs.
- `manifest.json` records exact source/base commits and every file's path, size,
  and SHA-256. Capture paths retain their original repository-relative names.
- Uploaded ZIP was downloaded again and checked byte-for-byte, then all manifest
  entries were verified before removing raw captures from the PR's tracked tree.
- The release tag points to the existing main baseline, not the capture-heavy
  branch tip; the manifest identifies the source revision of the evidence.

The PR retains 41 compact reports/plots and replay inputs (about 301 KiB).
In particular, the Slope replay's `last-curve-apply.log` and
`007-get_network_edges.json` under `session-notes/captures/offramp-grade-audit/`
remain available without downloading anything. No older main-branch fixtures were
removed. Some reports reference raw request/response files; find those in the ZIP.
Historical session-note capture paths refer to this archive when absent from Git.

## Verify or recover a raw run

Download the ZIP and its `.sha256` attachment. From the repository root:

```powershell
Get-FileHash '<downloaded ZIP>' -Algorithm SHA256
uv run --no-project python scripts/archive-diagnostic-captures.py --verify '<downloaded ZIP>'
Expand-Archive -LiteralPath '<downloaded ZIP>' -DestinationPath '<new empty extraction directory>'
```

Compare the whole-ZIP hash with the value above (or checksum attachment); the
Python verification then checks every contained file. Extract separately to avoid
overwriting working captures. No game or save is needed to inspect this evidence.

For future archives, `scripts/archive-diagnostic-captures.py --base <base> --ref
<source> --output <new.zip>` packages newly added committed captures. Use `--all-tracked` to include every tracked capture at the source revision. It never
removes files. Untracked future runs under artifacts need their own explicit
packaging step; this helper does not silently include them. Verify uploads before
any cleanup. Squash-merge the cleaned PR to keep raw capture commits out of main.

## Historical capture audit ? 2026-10-01

The same [diagnostic release](https://github.com/danwayneharris/CS2-NetworkTools/releases/tag/diagnostics-terrain-pr12-20261001)
also contains `historical-captures-20261001.zip` (8,069,466 bytes).
SHA-256: `13023e9856ee130d829a3fbf840ed790ecdf6d6cd5d2003685c5f8f3caf1d1d6`.
Its manifest covers all 5,485 tracked captures at source commit
`77782cc736e13278c5d19db5961a6e85a2bbcf0d`. It includes the 41 curated terrain
captures; use the first archive for that sprint's complete raw runs.

This follow-up removes 5,371 older raw files from tracking while retaining 114
captures: direct regression inputs plus reports and the previous terrain curation.
See [the retention list](../scripts/fixtures/retained-diagnostic-captures.json) for
all retained paths and the 36 inputs used directly by Python tests. Inputs stay at
their existing paths; test behavior is unchanged. Curated explanatory plots,
geometry fixtures, scripts, session notes, assets, UI type definitions and package
lockfiles remain source-controlled. No vendored dependency or submodule was changed.

Historical session notes can refer to files now in this ZIP. Extract to a separate
directory; their paths are relative to the archive root. Both archives retain
failures and intermediate attempts, not just passing results. Local original files
also remain on disk but are ignored.

These older files already exist in main's history. This commit cleans the checkout;
it does not shrink historical Git objects. No main history rewrite is performed.

## Deferred housekeeping: remove archived captures from Git history

The maintainer wants to revisit a history rewrite to reclaim the raw-capture bloat
already present in historical commits. This is explicitly non-urgent; the current
cleanup removes files from the tracked tree but does not purge historical objects.
No history rewrite is authorized by this reminder alone.

When prioritizing this work:

- Measure reachable historical capture blobs and actual clone size first.
- Reverify the diagnostic release ZIPs and checksums; retain an independent offline
  repository backup before rewriting. Do not rely solely on the refs being rewritten.
- Inventory branches, tags, archived sprint refs and open PR dependencies. Keeping
  old history reachable under archive refs in the same repository can retain the
  very objects we intend to remove; decide which archives should live outside Git.
- Scope filtering to archived raw captures while preserving source, session notes,
  curated reports and test inputs. Dry-run in an isolated clone and compare final
  trees and offline test results before updating any shared refs.
- Coordinate force updates, branch protection, stale clones/worktrees and changed
  commit links. GitHub PR refs/server retention may affect when space is reclaimed;
  verify actual results rather than assuming a force-push immediately shrinks storage.

Keep this separate from terrain, slope and gameplay development priorities.
