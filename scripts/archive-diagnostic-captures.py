"""Archive committed capture files at a Git revision, retaining per-file hashes.

Run from the repository root. No files are removed or Git refs changed.
"""
import argparse
import hashlib
import json
import shutil
import subprocess
import zipfile
from pathlib import Path


def verify(path):
    with zipfile.ZipFile(path) as archive:
        manifest = json.loads(archive.read('manifest.json'))
        expected = {'manifest.json'} | {row['path'] for row in manifest['files']}
        if len(archive.namelist()) != len(expected) or set(archive.namelist()) != expected:
            raise ValueError('Archive member list mismatch')
        for row in manifest['files']:
            data = archive.read(row['path'])
            if len(data) != row['bytes'] or hashlib.sha256(data).hexdigest() != row['sha256']:
                raise ValueError('Archive content mismatch: ' + row['path'])
    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--git', default=shutil.which('git'))
    parser.add_argument('--base', default='origin/main')
    parser.add_argument('--ref', default='HEAD')
    parser.add_argument('--output', type=Path)
    parser.add_argument('--all-tracked', action='store_true', help='Include all captures at the source revision, not just additions')
    parser.add_argument('--verify', type=Path)
    args = parser.parse_args()
    if args.verify:
        manifest = verify(args.verify)
        print(f"Verified {len(manifest['files'])} files in {args.verify}")
        return
    if not args.git or not args.output:
        parser.error('--git and --output are required for creation')
    def git(*values):
        return subprocess.check_output([args.git, *values])
    revision = git('rev-parse', args.ref).decode().strip()
    base = git('rev-parse', args.base).decode().strip()
    prefix = 'NetworkTools.docs/session-notes/captures/'
    names = (git('ls-tree', '-r', '--name-only', '-z', revision) if args.all_tracked else
             git('diff', '--diff-filter=A', '--name-only', '-z', f'{base}...{revision}')).decode().split('\0')
    names = sorted(name for name in names if name.startswith(prefix))
    if not names:
        raise ValueError('No newly added captures at selected revision')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    manifest = {'sourceCommit': revision, 'baseCommit': base, 'scope': 'all-tracked' if args.all_tracked else 'new-additions', 'files': []}
    with subprocess.Popen([args.git, 'cat-file', '--batch'], stdin=subprocess.PIPE, stdout=subprocess.PIPE) as batch, \
            zipfile.ZipFile(args.output, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name in names:
            batch.stdin.write(f'{revision}:{name}\n'.encode())
            batch.stdin.flush()
            header = batch.stdout.readline().split()
            if len(header) != 3 or header[1] != b'blob':
                raise ValueError('Not a Git blob: ' + name)
            size = int(header[2])
            data = batch.stdout.read(size)
            if len(data) != size or batch.stdout.read(1) != b'\n':
                raise ValueError('Incomplete Git blob: ' + name)
            archive.writestr(name, data)
            manifest['files'].append({'path': name, 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()})
        archive.writestr('manifest.json', json.dumps(manifest, indent=2) + '\n')
        batch.stdin.close()
    verify(args.output)
    digest = hashlib.sha256(args.output.read_bytes()).hexdigest()
    args.output.with_suffix('.zip.sha256').write_text(f'{digest}  {args.output.name}\n', encoding='utf-8')
    print(json.dumps({'files': len(names), 'zipBytes': args.output.stat().st_size, 'sha256': digest, 'sourceCommit': revision}))


if __name__ == '__main__':
    main()
