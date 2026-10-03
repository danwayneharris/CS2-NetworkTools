"""Run hyphen-named offline Python tests (unittest discovery skips their names)."""
import argparse
import json
from pathlib import Path
import subprocess
import sys

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
parser.add_argument('--output', type=Path)
args = parser.parse_args()
root = args.root.resolve()
results = []
# These are explicit installed-game/capture differential CLIs, not no-argument
# unittest scripts. Their qualification is separate and never implied by this run.
research = {
    'test-finish-height-preparation.py': 'requires captured cohort and pinned native binaries',
    'test-native-pipeline.py': 'requires a captured native schedule trace and harness',
    'test-native-replay.py': 'requires compiled native replay harness',
    'test-native-world-capture.py': 'requires compiled native replay harness',
    'test-raw-edge-capture.py': 'requires explicit native entry/exit captures and harness',
}
not_run = []
for path in sorted((root / 'scripts').glob('test-*.py')):
    if path.name in research:
        not_run.append({'test': path.name, 'status': 'not-run', 'reason': research[path.name]})
        print(path.name, 'NOT RUN:', research[path.name])
        continue
    result = subprocess.run([sys.executable, str(path)], cwd=root, capture_output=True, text=True, timeout=60)
    results.append({'test': path.name, 'exitCode': result.returncode, 'stdout': result.stdout, 'stderr': result.stderr})
    print(path.name, 'PASS' if result.returncode == 0 else 'FAIL')
    if result.returncode:
        print(result.stdout + result.stderr)
if args.output:
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps({'executed': results, 'notRunResearch': not_run}, indent=2) + '\n', encoding='utf-8')
print(f'{sum(row["exitCode"] == 0 for row in results)}/{len(results)} test scripts passed')
print(f'{len(not_run)} explicit research CLIs not run; see NativeReplay/README.md for separate qualification')
if not results or any(row['exitCode'] for row in results):
    sys.exit(1)
