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
for path in sorted((root / 'scripts').glob('test-*.py')):
    result = subprocess.run([sys.executable, str(path)], cwd=root, capture_output=True, text=True, timeout=60)
    results.append({'test': path.name, 'exitCode': result.returncode, 'stdout': result.stdout, 'stderr': result.stderr})
    print(path.name, 'PASS' if result.returncode == 0 else 'FAIL')
    if result.returncode:
        print(result.stdout + result.stderr)
if args.output:
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(results, indent=2) + '\n', encoding='utf-8')
print(f'{sum(row["exitCode"] == 0 for row in results)}/{len(results)} test scripts passed')
if not results or any(row['exitCode'] for row in results):
    sys.exit(1)
