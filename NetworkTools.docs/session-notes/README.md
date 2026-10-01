# Session evidence

Session notes record incremental experiments, including failed attempts. Full raw
captures are preserved in [diagnostic archives](../diagnostic-archives.md) when
absent from this checkout. Original relative capture paths are retained inside
those ZIPs. Tests keep their necessary inputs locally; downloading archives is not
required for the curated offline suite.

For new investigations, write raw requests/responses and polling output to ignored
`artifacts/`. Commit useful conclusions, reproducible scripts and small deliberate
fixtures. Archive full evidence with a manifest and checksum, then verify a fresh
download before removing it from source control.
