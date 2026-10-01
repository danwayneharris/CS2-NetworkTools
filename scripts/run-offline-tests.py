"""Run the real offline suites without deploying or contacting the game.

Requires .NET 8; the Slope suite additionally requires the installed CS2 toolchain.
Writes local build outputs and a JSON/log directory. Native/visual behavior is not tested.
"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import platform
import re
import shutil
import subprocess
import sys
import time


def suite_specs(root, output):
    dotnet = shutil.which('dotnet') or 'dotnet'
    powershell = shutil.which('pwsh') or shutil.which('powershell') or 'powershell'
    ps = [powershell, '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File']
    return [
        ('geometry', [dotnet, 'run', '--project', 'NetworkTools.Geometry.Tests'],
         'NetworkTools.Geometry.Tests/NetworkTools.Geometry.Tests.csproj', r'PASS: analytic solution'),
        ('path-selection', [dotnet, 'run', '--project', 'NetworkTools.PathSelection.Tests'],
         'NetworkTools.PathSelection.Tests/NetworkTools.PathSelection.Tests.csproj', r'Path selection production-source tests passed: [1-9][0-9]* assertions'),
        ('slope-production', ps + ['scripts/test-slope.ps1'],
         'scripts/test-slope.ps1', r'PASS [1-9][0-9]* assertions against production code'),
        ('original-input', ps + ['scripts/test-original-input-comparison.ps1'],
         'scripts/test-original-input-comparison.ps1', r'11 original-input comparison checks passed'),
        ('python', [sys.executable, 'scripts/run-offline-python-tests.py', '--output', str(output / 'python.json')],
         'scripts/run-offline-python-tests.py', r'[1-9][0-9]*/[1-9][0-9]* test scripts passed'),
    ]


def run_suite(name, command, required_file, marker, root, output, timeout, run=subprocess.run):
    row = {'suite': name, 'command': command, 'configuration': 'Debug', 'status': 'blocked'}
    if not (root / required_file).is_file():
        row['reason'] = 'Expected suite entry point missing: ' + required_file
        return row
    started = time.monotonic()
    try:
        child = run(command, cwd=root, capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=timeout)
        text = child.stdout + '\n' + child.stderr
        row['exitCode'] = child.returncode
        row['status'] = 'passed' if child.returncode == 0 else 'failed'
        if child.returncode == 0 and not re.search(marker, text):
            row.update(status='failed', reason='Expected execution/coverage marker absent; zero exit is insufficient')
    except (OSError, subprocess.TimeoutExpired) as exc:
        row['reason'] = str(exc)
        text = str(exc)
    row['elapsedSeconds'] = round(time.monotonic() - started, 3)
    log = output / (name + '.log')
    log.write_text(text, encoding='utf-8')
    row['log'] = str(log)
    return row


def aggregate_status(rows):
    if not rows:
        return 'failed'
    if any(row['status'] == 'failed' for row in rows):
        return 'failed'
    if any(row['status'] != 'passed' for row in rows):
        return 'blocked'
    return 'passed'


def probe(command, root):
    try:
        result = subprocess.run(command, cwd=root, capture_output=True, text=True,
                                encoding='utf-8', errors='replace', timeout=20)
        return {'exitCode': result.returncode, 'value': result.stdout.strip(), 'error': result.stderr.strip()}
    except (OSError, subprocess.TimeoutExpired) as exc:
        return {'error': str(exc)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--output', type=Path, default=Path('artifacts/offline-tests'))
    parser.add_argument('--timeout', type=int, default=900, help='Maximum seconds per offline suite')
    args = parser.parse_args()
    if args.timeout < 1:
        parser.error('--timeout must be positive')
    root = args.root.resolve()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    summary = {
        'startedUtc': datetime.now(timezone.utc).isoformat(),
        'scope': 'offline; no deployment, game queries, native execution or live mutation',
        'root': str(root), 'python': sys.version, 'platform': platform.platform(),
        'revision': probe(['git', '-c', 'safe.directory=' + str(root), 'rev-parse', 'HEAD'], root),
        'workingTree': probe(['git', '-c', 'safe.directory=' + str(root), 'status', '--porcelain'], root),
        'dotnet': probe(['dotnet', '--version'], root),
        'policy': 'Suites retain their stated mathematical/assertion tolerances; live geometry policy is not applied to offline analytical precision.',
        'notRun': ['optional trace/captured-replay command modes', 'Release/Burst execution', 'native preview/Apply', 'vehicle traversal', 'human visual review'],
        'results': [],
    }
    specs = suite_specs(root, output)
    for name, command, required_file, marker in specs:
        row = run_suite(name, command, required_file, marker, root, output, args.timeout)
        summary['results'].append(row)
        summary['status'] = aggregate_status(summary['results'])
        (output / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
        print(name + ': ' + row['status'], flush=True)
        if row.get('reason'):
            print('  ' + row['reason'], flush=True)
    summary['status'] = aggregate_status(summary['results'])
    summary['completedUtc'] = datetime.now(timezone.utc).isoformat()
    (output / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
    print('Offline aggregate: ' + summary['status'] + '; report: ' + str(output / 'summary.json'))
    return 0 if summary['status'] == 'passed' else 1


if __name__ == '__main__':
    sys.exit(main())
