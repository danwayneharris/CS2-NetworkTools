"""Inventory immutable source evidence and assembly identities; no game connection.

Bulk captures stay at their local paths. A later consumer must verify SHA256 before
using them. This manifest is an inventory, not a complete stage-input contract.
"""
import argparse
import datetime
import hashlib
import json
from pathlib import Path


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest().upper()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--source', type=Path, required=True, help='Original investigation worktree')
    p.add_argument('--decompile', type=Path, required=True)
    p.add_argument('--managed', type=Path, required=True)
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    if a.output.exists(): raise FileExistsError('Refusing to overwrite frozen manifest')
    manifest_path = a.decompile / 'source-manifest.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    names = {'Game.dll', 'Unity.Entities.dll', 'Unity.Collections.dll',
             'Unity.Mathematics.dll', 'Colossal.Mathematics.dll'}
    assemblies = []
    for row in manifest['assemblies']:
        if row['name'] not in names: continue
        path = a.managed / row['name']
        actual = digest(path)
        if actual != row['sha256'].upper(): raise ValueError('Source mismatch: ' + row['name'])
        assemblies.append(dict(name=row['name'], path=str(path.resolve()), sha256=actual))
    if {r['name'] for r in assemblies} != names: raise ValueError('Incomplete assembly manifest')
    relative = [
        'NetworkTools.docs/session-notes/2026-10-02-0050-junction-inputs.md',
        'NetworkTools.docs/session-notes/2026-10-02-junction-cut-flatten-fixture.json',
        'scripts/replay-junction-flattening.py', 'scripts/prepare-native-edge-cut-probe.py',
        'artifacts/junction-native-cut-results.json', 'artifacts/junction-cut-expressions.json',
        'artifacts/junction-cut-apply/before.json',
        'artifacts/junction-cut-apply/after-immediate.json',
        'artifacts/junction-cut-apply/after-settled.json',
        'artifacts/junction-cut-apply/after-later.json',
        'artifacts/junction-cut-apply/comparison.json',
    ]
    files = []
    for name in relative:
        path = a.source / name
        files.append(dict(path=str(path.resolve()), bytes=path.stat().st_size, sha256=digest(path)))
    source_files = []
    for name in ['GeometrySystem.cs', 'EdgeIterator.cs', 'NetUtils.cs']:
        path = a.decompile / 'src/Game/Game.Net' / name
        source_files.append(dict(path=str(path.resolve()), sha256=digest(path)))
    before = json.loads((a.source / 'artifacts/junction-cut-apply/before.json').read_text())
    report = dict(schemaVersion=1, utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
        scope='Evidence inventory, not coherent pre-job capture or live runtime identity',
        decompileManifestSha256=digest(manifest_path), assemblies=assemblies,
        sourceFiles=source_files, evidenceFiles=files, recordedCity=before['city'],
        recordedToolState=before['state'],
        captureGaps=['Actual per-stage participant list/query membership',
                     'Retention sentinels at execution, not later observations',
                     'Pre/post Flatten height-map entries and subsequent writer history',
                     'Complete prefabs and terrain domain for new-input execution',
                     'Independent native control and held-out qualification'])
    a.output.parent.mkdir(parents=True, exist_ok=True)
    with a.output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2)
    print(f'Frozen {len(files)} evidence files, {len(source_files)} source hashes, {len(assemblies)} matching assemblies')


if __name__ == '__main__': main()
