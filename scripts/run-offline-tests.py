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
        ('parameters', [dotnet, 'run', '--project', 'NetworkTools.Parameter.Tests'],
         'NetworkTools.Parameter.Tests/NetworkTools.Parameter.Tests.csproj', r'Parameter production-source tests passed: [1-9][0-9]* assertions'),
        ('connect-profile', [dotnet, 'run', '--project', 'NetworkTools.ConnectProfile.Tests'],
         'NetworkTools.ConnectProfile.Tests/NetworkTools.ConnectProfile.Tests.csproj', r'Connect vertical profile: [1-9][0-9]* assertions passed'),
        ('connect-coverage', [dotnet, 'run', '--project', 'NetworkTools.ConnectCoverage.Tests'],
         'NetworkTools.ConnectCoverage.Tests/NetworkTools.ConnectCoverage.Tests.csproj', r'Connect profile coverage: [1-9][0-9]* assertions passed'),
        ('lane-direction', [dotnet, 'run', '--project', 'NetworkTools.LaneDirection.Tests'],
         'NetworkTools.LaneDirection.Tests/NetworkTools.LaneDirection.Tests.csproj', r'Lane direction policy: [1-9][0-9]* assertions passed'),
        ('lane-connection', [dotnet, 'run', '--project', 'NetworkTools.LaneConnection.Tests'],
         'NetworkTools.LaneConnection.Tests/NetworkTools.LaneConnection.Tests.csproj', r'LaneConnectionProof: [1-9][0-9]* assertions passed'),
        ('lane-alignment', [dotnet, 'run', '--project', 'NetworkTools.LaneAlignment.Tests'],
         'NetworkTools.LaneAlignment.Tests/NetworkTools.LaneAlignment.Tests.csproj', r'ConnectLaneAlignment: [1-9][0-9]* assertions passed'),
        ('lane-plane', [dotnet, 'run', '--project', 'NetworkTools.LanePlane.Tests'],
         'NetworkTools.LanePlane.Tests/NetworkTools.LanePlane.Tests.csproj', r'LanePlaneIntersection: [1-9][0-9]* assertions passed'),
        ('connect-candidate', [dotnet, 'run', '--project', 'NetworkTools.Connect.Tests'],
         'NetworkTools.Connect.Tests/NetworkTools.Connect.Tests.csproj', r'Connect candidate production-source tests passed: [1-9][0-9]* assertions'),
        ('codegen', ps + ['scripts/test-codegen.ps1'],
         'scripts/test-codegen.ps1', r'Codegen regression checks passed\.'),
        ('slope-production', ps + ['scripts/test-slope.ps1'],
         'scripts/test-slope.ps1', r'PASS [1-9][0-9]* assertions against production code'),
        ('original-input', ps + ['scripts/test-original-input-comparison.ps1'],
         'scripts/test-original-input-comparison.ps1', r'[1-9][0-9]* original-input comparison checks passed'),
        ('python', [sys.executable, 'scripts/run-offline-python-tests.py', '--output', str(output / 'python.json')],
         'scripts/run-offline-python-tests.py', r'[1-9][0-9]*/[1-9][0-9]* test scripts passed'),
    ]


def prerequisite_issues(name, root, run=subprocess.run, which=shutil.which):
    """Inspect tools/installed inputs separately from compiling or executing tests."""
    issues = []
    if name in ('geometry', 'path-selection', 'parameters', 'connect-candidate', 'connect-profile', 'connect-coverage', 'lane-direction', 'lane-connection', 'lane-alignment', 'lane-plane', 'codegen', 'slope-production'):
        dotnet = which('dotnet')
        if not dotnet:
            issues.append('Missing dotnet executable / .NET 8 SDK')
        else:
            try:
                result = run([dotnet, '--list-sdks'], capture_output=True, text=True, timeout=20)
                if result.returncode or not re.search(r'^8\.', result.stdout, re.M):
                    issues.append('Required .NET 8 SDK unavailable')
            except (OSError, subprocess.TimeoutExpired) as exc:
                issues.append('Cannot inspect .NET SDK: ' + str(exc))
    if name in ('codegen', 'slope-production', 'original-input'):
        ps = which('pwsh') or which('powershell')
        if not ps:
            issues.append('Missing PowerShell executable')
        elif name == 'slope-production':
            query = "@{managed=[Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH','User');tool=[Environment]::GetEnvironmentVariable('CSII_TOOLPATH','User')} | ConvertTo-Json -Compress"
            try:
                result = run([ps, '-NoProfile', '-Command', query], capture_output=True, text=True, timeout=20)
                if result.returncode:
                    raise ValueError(result.stderr)
                paths = json.loads(result.stdout)
                for key, files in {'tool': ('Mod.props', 'Mod.targets'), 'managed': (
                        'Game.dll', 'Unity.Mathematics.dll', 'Colossal.Mathematics.dll',
                        'Unity.Entities.dll', 'Unity.Collections.dll', 'UnityEngine.CoreModule.dll', 'Unity.Burst.dll')}.items():
                    base = paths.get(key)
                    for filename in files:
                        if not base or not (Path(base) / filename).is_file():
                            issues.append('Missing Slope installed input: ' + key + '/' + filename)
            except (OSError, subprocess.TimeoutExpired, ValueError) as exc:
                issues.append('Cannot inspect User-scoped CS2 toolchain paths: ' + str(exc))
        if name == 'slope-production':
            for filename in ('LucaModsCommon.props', 'LucaModsCommon.targets'):
                if not (root / 'NetworkTools.Mod/Common' / filename).is_file():
                    issues.append('Missing pinned Common submodule input: ' + filename)
    return issues


def run_suite(name, command, required_file, marker, root, output, timeout, run=subprocess.run, prerequisites=None):
    row = {'suite': name, 'command': command, 'configuration': 'Debug', 'status': 'blocked'}
    if prerequisites:
        row.update(reason='Missing prerequisites', prerequisites=prerequisites)
        return row
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
    except subprocess.TimeoutExpired as exc:
        row.update(status='failed', reason='Suite timed out: ' + str(exc))
        text = str(exc)
    except OSError as exc:
        row['reason'] = 'Cannot launch suite: ' + str(exc)
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
    (output / 'python.json').unlink(missing_ok=True)
    specs = suite_specs(root, output)
    for name, command, required_file, marker in specs:
        row = run_suite(name, command, required_file, marker, root, output, args.timeout,
                        prerequisites=prerequisite_issues(name, root))
        if name == 'python' and (output / 'python.json').is_file():
            try:
                details = json.loads((output / 'python.json').read_text(encoding='utf-8'))
                row['notRunResearch'] = details.get('notRunResearch', [])
                if details.get('status') == 'blocked' and row.get('exitCode') == 1:
                    row.update(status='blocked', reason='Python child prerequisites unavailable')
            except (OSError, ValueError) as exc:
                row.update(status='failed', reason='Invalid Python summary: ' + str(exc))
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
