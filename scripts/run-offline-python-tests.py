"""Run hyphen-named offline Python tests with positive executed coverage."""
import argparse
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys

RESEARCH = {
    'test-finish-height-preparation.py': 'requires captured cohort and pinned native binaries',
    'test-native-pipeline.py': 'requires a captured native schedule trace and harness',
    'test-native-replay.py': 'requires compiled native replay harness',
    'test-native-world-capture.py': 'requires compiled native replay harness',
    'test-raw-edge-capture.py': 'requires explicit native entry/exit captures and harness',
}
# This assertion-based script has a reviewed, fixed five-check contract.
ASSERTION_SCRIPT = 'test-native-middle-height.py'


def script_prerequisites(name, which=shutil.which):
    if name != 'test-build-identity.py':
        return []
    missing = [name for name in ('git', 'node') if not which(name)]
    if not (which('powershell') or which('pwsh')):
        missing.append('PowerShell')
    return missing


def run_script(path, root, timeout, run=subprocess.run):
    command = [sys.executable, str(path)]
    row = {'test': path.name, 'status': 'failed', 'executedChecks': 0}
    missing = script_prerequisites(path.name)
    if missing:
        row.update(status='blocked', reason='Missing prerequisites: ' + ', '.join(missing))
        return row
    try:
        child = run(command, cwd=root, capture_output=True, text=True, timeout=timeout)
        row.update(exitCode=child.returncode, stdout=child.stdout, stderr=child.stderr)
        text = child.stdout + '\n' + child.stderr
        if child.returncode:
            row['reason'] = 'Test process failed'
            return row
        if path.name == ASSERTION_SCRIPT:
            count = 5 if re.search(r'^OFFLINE_EXECUTED_ASSERTIONS=5$', text, re.M) else 0
        else:
            matches = re.findall(r'^Ran (\d+) tests? in ', text, re.M)
            skipped = re.findall(r'skipped=(\d+)', text)
            count = int(matches[-1]) - (int(skipped[-1]) if skipped else 0) if matches else 0
            if not re.search(r'^OK(?: \([^\n]*\))?$', text, re.M):
                count = 0
        row['executedChecks'] = max(0, count)
        if count > 0:
            row['status'] = 'passed'
        else:
            row['reason'] = 'No positive executed coverage (empty, all-skipped, or unrecognized test result)'
    except subprocess.TimeoutExpired as exc:
        row.update(status='failed', reason='Test timed out: ' + str(exc))
    except OSError as exc:
        row.update(status='blocked', reason='Cannot launch test: ' + str(exc))
    return row


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--output', type=Path)
    parser.add_argument('--timeout', type=float, default=60)
    args = parser.parse_args()
    if args.timeout <= 0:
        parser.error('--timeout must be positive')
    root = args.root.resolve()
    results, not_run = [], []
    for path in sorted((root / 'scripts').glob('test-*.py')):
        if path.name in RESEARCH:
            not_run.append({'test': path.name, 'status': 'not-run', 'reason': RESEARCH[path.name]})
            continue
        row = run_script(path, root, args.timeout)
        results.append(row)
        print(path.name, row['status'], row.get('reason', ''), flush=True)
        if row['status'] != 'passed':
            print(row.get('stdout', '') + row.get('stderr', ''))
    status = 'failed' if not results or any(r['status'] == 'failed' for r in results) else (
        'blocked' if any(r['status'] == 'blocked' for r in results) else 'passed')
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps({'status': status, 'executed': results, 'notRunResearch': not_run}, indent=2) + '\n', encoding='utf-8')
    print(f'{sum(row["status"] == "passed" for row in results)}/{len(results)} test scripts passed')
    print(f'{len(not_run)} explicit research CLIs not run; see NativeReplay/README.md for separate qualification')
    return 0 if status == 'passed' else 1


if __name__ == '__main__':
    sys.exit(main())
