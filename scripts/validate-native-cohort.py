"""Verify immutable local evidence and replay the bounded native geometry cohort."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import subprocess
import time


def checksum(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def vectors(value, path=''):
    if isinstance(value, dict):
        if set(value) == {'x', 'y', 'z'}:
            yield path, [value[k] for k in ('x', 'y', 'z')]
        else:
            for k, v in value.items():
                yield from vectors(v, path + '/' + k)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('manifest', 'dll', 'burst-binary', 'output'):
        p.add_argument('--' + name, required=True)
    a = p.parse_args()
    manifest = json.loads(Path(a.manifest).read_text())
    if manifest['schemaVersion'] != 1:
        raise ValueError('Unknown cohort schema')
    # The manifest contains local paths, hashes and aggregate measurements only.
    for entry in manifest['immutableInputs']:
        if checksum(entry['path']) != entry['sha256'].upper():
            raise ValueError('Historical evidence changed: ' + entry['path'])
    if checksum(a.burst_binary) != manifest['burstSha256']:
        raise ValueError('Changed Burst binary requires a new qualification cohort')
    out = Path(a.output)
    out.mkdir(parents=True, exist_ok=False)
    reports, runs = {}, []
    for case in manifest['fixtures']:
        destination = out / (case['id'] + '.json')
        started = time.perf_counter()
        result = subprocess.run(['dotnet', a.dll, '--pipeline-native', case['trace'],
                                 a.burst_binary, str(destination)], capture_output=True, text=True)
        runs.append(dict(id=case['id'], seconds=time.perf_counter()-started,
                         exitCode=result.returncode, stdout=result.stdout, stderr=result.stderr))
        (out/'runs.json').write_text(json.dumps(runs, indent=2))
        if result.returncode:
            raise RuntimeError('Replay failed: ' + case['id'])
        reports[case['id']] = json.loads(destination.read_text())
    pair = manifest['anchorPair']
    preview, permanent = (reports[pair[k]] for k in ('preview', 'permanent'))
    def edges(report):
        result = {}
        for e in report['computedEdges']:
            key = tuple(e['original'])
            if key in result:
                raise ValueError('Ambiguous original identity')
            result[key] = e
        return result
    before, after = edges(preview), edges(permanent)
    differences = []
    for native in pair['nativeSurfaceDifferences']:
        key = tuple(native['edge'])
        x, y = (dict(vectors(world[key]['edgeGeometry'])) for world in (before, after))
        # Bounds are not surface control points.
        x = {k:v for k,v in x.items() if '/m_Bounds/' not in k}
        y = {k:v for k,v in y.items() if '/m_Bounds/' not in k}
        if not x or x.keys() != y.keys():
            raise ValueError('Mismatched paired geometry domain')
        delta = max(abs(x[k][1]-y[k][1]) for k in x)
        residual = abs(delta-native['maxY'])
        if not math.isfinite(residual) or residual > pair['toleranceMetres']:
            raise AssertionError('Native preview/permanent difference not reproduced')
        differences.append(dict(edge=key, computedMaxY=delta, nativeMaxY=native['maxY'],
                                residualMetres=residual))
    report = dict(schemaVersion=1, passed=True, harnessSha256=checksum(a.dll),
                  manifestSha256=checksum(a.manifest), runs=runs, anchorDifferences=differences,
                  limits='Captured query membership and identity-dependent native lookup; no general scheduler or lane prediction')
    (out/'summary.json').write_text(json.dumps(report, indent=2))
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
