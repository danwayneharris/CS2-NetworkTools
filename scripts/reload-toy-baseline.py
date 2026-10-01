"""Explicit, checkpointed graceful restart to a checksummed toy baseline.

Does not deploy, force-kill, change playsets, or assume launch means city readiness.
The caller must supply the live toy citySession it has independently identified.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import zipfile

spec = importlib.util.spec_from_file_location('regression', Path(__file__).with_name('live-regression.py'))
regression = importlib.util.module_from_spec(spec)
spec.loader.exec_module(regression)


def verified_package(root, name, checksum=None):
    paths = list(Path(root).rglob(name))
    if len(paths) != 1:
        raise ValueError('Missing or ambiguous save package')
    path = paths[0]
    if checksum and hashlib.sha256(path.read_bytes()).hexdigest().upper() != checksum.upper():
        raise ValueError('Baseline checksum changed')
    with zipfile.ZipFile(path) as archive:
        if archive.testzip() is not None or not any(n.endswith('.SaveGameMetadata.cid') for n in archive.namelist()):
            raise ValueError('Invalid save package')
    return path.resolve()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fixture', required=True)
    parser.add_argument('--save-root', required=True)
    parser.add_argument('--expected-city-session', required=True)
    parser.add_argument('--output', required=True)
    parser.add_argument('--bridge', default='../cities2-agent-bridge-ndc')
    parser.add_argument('--close-only', action='store_true', help='Checkpoint and close for deployment; do not launch')
    args = parser.parse_args()
    fixture = json.loads(Path(args.fixture).read_bytes())
    baseline = verified_package(args.save_root, fixture['baseline'], fixture['baselineSha256'])
    runner = regression.Runner(args.bridge, args.output)
    city = runner.call('get_city_state')
    if runner.city_session != args.expected_city_session or city['population'] != 0 or city['selectedSpeed'] != 0 or not city['controlEnabled']:
        raise ValueError('Requires the identified, paused, control-enabled toy city')
    operation = runner.call('save_checkpoint', {'label': 'regression-before-reload'})
    saved = runner.poll('get_operation', lambda s: s['status'] in ('complete','failed','interrupted'), {'id':operation['id']})
    if saved['status'] != 'complete':
        raise RuntimeError('Checkpoint did not complete; leaving game running')
    checkpoint = verified_package(args.save_root, saved['saveName'] + '.cok')
    runner.call('get_city_state')  # Recheck city session after saving.
    env = os.environ.copy()
    env['NT_EXPECTED_CITY_SESSION'] = args.expected_city_session
    # Fixed PowerShell code; values are passed as environment data, not shell text.
    close = r"""
$ErrorActionPreference='Stop'
$mailbox=Join-Path $env:LOCALAPPDATA 'CitiesIIAgentBridge'
if(Test-Path -LiteralPath (Join-Path $mailbox 'STOP')){throw 'STOP is present'}
$session=$null
for($attempt=0;$attempt -lt 10;$attempt++) {
    try {
        $stream=[IO.FileStream]::new((Join-Path $mailbox 'session.json'),[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        $reader=[IO.StreamReader]::new($stream)
        try {$session=$reader.ReadToEnd() | ConvertFrom-Json} finally {$reader.Dispose()}
        break
    } catch {if($attempt -eq 9){throw}; Start-Sleep -Milliseconds 50}
}
if(([DateTime]::UtcNow-[DateTime]::Parse($session.heartbeatUtc).ToUniversalTime()).TotalSeconds -gt 10){throw 'Stale heartbeat'}
if($session.citySession -ne $env:NT_EXPECTED_CITY_SESSION){throw 'City changed before shutdown'}
$games=@(Get-Process Cities2 -ErrorAction SilentlyContinue)
if($games.Count -ne 1 -or $games[0].Id -ne $session.pid){throw 'Game process mismatch'}
$game=$games[0]
if(!$game.CloseMainWindow()){throw 'Graceful close refused; no force kill attempted'}
if(!$game.WaitForExit(45000)){throw 'Graceful close timed out; no force kill attempted'}
"""
    closed = subprocess.run(['powershell.exe','-NoProfile','-Command',close], env=env,
                            capture_output=True,text=True,timeout=55)
    (runner.output/'shutdown.txt').write_text(closed.stdout+closed.stderr)
    if closed.returncode:
        raise RuntimeError('Shutdown stopped; inspect shutdown.txt, do not force-kill')
    if args.close_only:
        report={'baseline':baseline.name,'checkpoint':checkpoint.name,'status':'Checkpoint verified; game closed gracefully.'}
        (runner.output/'lifecycle.json').write_text(json.dumps(report,indent=2))
        print(json.dumps(report,indent=2))
        return
    launched = subprocess.run(['powershell.exe','-NoProfile','-File',
        str(runner.bridge/'launch-v2-experiment.ps1'),'-SavePath',str(baseline),'-Launch'],
        capture_output=True,text=True,timeout=30)
    (runner.output/'launch.txt').write_text(launched.stdout+launched.stderr)
    if launched.returncode:
        raise RuntimeError('Launch failed; inspect launch.txt before any retry')
    report={'baseline':baseline.name,'checkpoint':checkpoint.name,
            'status':'Launch requested; independently verify loaded city, pause, playset and fixture geometry.'}
    (runner.output/'lifecycle.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2))


if __name__ == '__main__':
    main()
