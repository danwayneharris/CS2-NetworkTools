"""Archive committed captures or explicit working-tree artifacts, retaining per-file hashes.

Run from the repository root. No files are removed or Git refs changed.
"""
import argparse
import hashlib
import json
import os
import stat
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


CAPTURE_SUFFIXES = {'.json', '.log', '.txt', '.md'}


def _within(path, parent):
    return path == parent or parent in path.parents


def _reject_links(path):
    # Junctions/reparse points matter on Windows even when is_symlink() is false.
    for part in (path, *path.parents):
        if part.is_symlink() or (part.exists() and getattr(part.stat(), 'st_file_attributes', 0)
                                & getattr(stat, 'FILE_ATTRIBUTE_REPARSE_POINT', 0x400)):
            raise ValueError('Symlink/junction capture path is not supported: ' + str(part))


def collect_working_captures(repo, roots, output):
    repo = Path(repo).resolve(strict=True)
    artifacts = repo / 'artifacts'
    if not roots:
        raise ValueError('At least one capture root is required')
    selected = []
    for root in roots:
        candidate = Path(root)
        if not candidate.is_absolute():
            candidate = repo / candidate
        _reject_links(candidate.absolute())
        resolved = candidate.resolve(strict=True)
        if not _within(resolved, artifacts) or not resolved.is_dir():
            raise ValueError('Capture roots must be directories inside repository artifacts: ' + str(root))
        if any(_within(resolved, previous) or _within(previous, resolved) for previous in selected):
            raise ValueError('Duplicate or overlapping capture roots: ' + str(root))
        selected.append(resolved)
    output = Path(output).absolute()
    _reject_links(output)
    output = output.resolve()
    if any(_within(output, root) for root in selected):
        raise ValueError('Archive output must not be inside a capture root')
    if _within(output, repo) and not _within(output, artifacts):
        raise ValueError('Archive output must not be in repository source')
    files = []
    identities = set()
    for root in selected:
        for directory, dirs, names in os.walk(root, followlinks=False):
            for name in sorted(dirs + names):
                path = Path(directory) / name
                _reject_links(path)
                resolved = path.resolve(strict=True)
                if not _within(resolved, root):
                    raise ValueError('Capture path escaped root: ' + str(path))
                if path.is_dir():
                    continue
                if not path.is_file():
                    raise ValueError('Capture is not a regular file: ' + str(path))
                if path.suffix.lower() not in CAPTURE_SUFFIXES:
                    continue
                info = path.stat()
                identity = (info.st_dev, info.st_ino)
                if info.st_ino and identity in identities:
                    raise ValueError('Duplicate capture file identity: ' + str(path))
                identities.add(identity)
                files.append((path.relative_to(repo).as_posix(), path))
    if not files:
        raise ValueError('No supported raw evidence files in capture roots')
    return sorted(files), sorted(path.relative_to(repo).as_posix() for path in selected), output


def archive_working_captures(repo, roots, output, source_commit=None):
    files, root_names, output = collect_working_captures(repo, roots, output)
    manifest = {'sourceCommit': source_commit, 'baseCommit': None, 'scope': 'working-tree',
                'captureRoots': root_names, 'files': []}
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name, path in files:
            _reject_links(path)
            before = path.stat()
            data = path.read_bytes()
            after = path.stat()
            if (before.st_dev, before.st_ino, before.st_size, before.st_mtime_ns) != (
                    after.st_dev, after.st_ino, after.st_size, after.st_mtime_ns):
                raise ValueError('Capture changed while archiving: ' + name)
            archive.writestr(name, data)
            manifest['files'].append({'path': name, 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()})
        archive.writestr('manifest.json', json.dumps(manifest, indent=2) + '\n')
    verify(output)
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    output.with_suffix('.zip.sha256').write_text(f'{digest}  {output.name}\n', encoding='utf-8')
    return {'files': len(files), 'zipBytes': output.stat().st_size, 'sha256': digest,
            'sourceCommit': source_commit, 'scope': 'working-tree'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--git', default=shutil.which('git'))
    parser.add_argument('--base', default='origin/main')
    parser.add_argument('--ref', default='HEAD')
    parser.add_argument('--output', type=Path)
    parser.add_argument('--all-tracked', action='store_true', help='Include all captures at the source revision, not just additions')
    parser.add_argument('--capture-root', action='append', type=Path, default=[],
                        help='Repeatable working-tree directory under repository artifacts; archives only JSON/log/text/Markdown evidence')
    parser.add_argument('--verify', type=Path)
    args = parser.parse_args()
    if args.verify and args.capture_root:
        parser.error('--verify cannot be combined with --capture-root')
    if args.verify:
        manifest = verify(args.verify)
        print(f"Verified {len(manifest['files'])} files in {args.verify}")
        return
    if args.capture_root:
        if not args.output or args.all_tracked or args.base != 'origin/main' or args.ref != 'HEAD':
            parser.error('--capture-root requires --output and cannot be combined with Git revision selection')
        repo = Path(__file__).resolve().parent.parent
        revision = (subprocess.check_output([args.git, '-C', str(repo), 'rev-parse', 'HEAD']).decode().strip()
                    if args.git else None)
        print(json.dumps(archive_working_captures(repo, args.capture_root, args.output, revision)))
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
