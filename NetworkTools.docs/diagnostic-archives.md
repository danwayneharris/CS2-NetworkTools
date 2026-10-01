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
<source> --output <new.zip>` packages newly added committed captures. It never
removes files. Untracked future runs under artifacts need their own explicit
packaging step; this helper does not silently include them. Verify uploads before
any cleanup. Squash-merge the cleaned PR to keep raw capture commits out of main.
