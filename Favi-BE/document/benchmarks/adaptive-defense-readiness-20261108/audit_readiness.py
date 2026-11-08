"""Restore archived evidence into a fresh tree and run stdlib arithmetic audits only."""
import argparse
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import subprocess
import sys
import zipfile


def sha(data):
    return hashlib.sha256(data).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def require(condition, message):
    if not condition:
        raise ValueError(message)


def members(archive):
    names = archive.namelist()
    require(len(names) == len(set(names)), 'Duplicate ZIP member')
    for name in names:
        p = PurePosixPath(name)
        require(not p.is_absolute() and '..' not in p.parts and '\\' not in name
                and ':' not in name, 'Unsafe ZIP member: ' + name)
    return names


def restore(archive, target):
    # Explicit validated writes; never extract over original evidence.
    for name in members(archive):
        path = target / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(archive.read(name))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True,
                        help='Fresh directory outside original evidence')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[3]
    bench = root / 'document/benchmarks'
    output = args.output.resolve()
    require(not output.exists(), 'Output must not exist')
    # Keep the writable tree separate from every source/archive directory.
    require(output.is_relative_to(root / '.tools/defense-readiness'),
            'Output must be a fresh child of solution .tools/defense-readiness')
    output.mkdir(parents=True)
    result = dict(status='RUNNING',scope='Archive restoration and arithmetic only; no model/API run',
                  archives=[], restored_audits=[])
    ranking = 'adaptive-confirmatory-ranking-20261108'
    replay = 'adaptive-replay-20261108'
    names = [ranking, replay, 'adaptive-serving-export-20261108', 'adaptive-http-20261108']
    names += [f'adaptive-confirmatory-{s}-{seed}' for s in ('main', 'sparse')
              for seed in (20261111, 20261112, 20261113)]
    for name in names:
        if name.startswith('adaptive-confirmatory-') and name != ranking:
            receipt = read(bench / 'adaptive-confirmatory-data-20261108' / (name+'-archive.json'))
        elif name == 'adaptive-http-20261108':
            receipt = read(bench / name / 'summary.json')['archive']
        else:
            receipt = read(bench / name / 'archive.json')
        path = root / '.tools/dataset-archives' / (name+'.zip')
        require(path.stat().st_size == receipt['bytes'], name+' size')
        digest = sha(path.read_bytes())
        require(digest == receipt['sha256'], name+' archive SHA256')
        with zipfile.ZipFile(path) as z:
            entries = members(z)
            require(len(entries) == receipt['members'], name+' member count')
            hashes = {n: sha(z.read(n)) for n in entries}  # CRC checked by ZipFile.read
            expected = receipt.get('member_sha256', receipt.get('files'))
            if expected is not None:
                require(hashes == expected, name+' member hashes')
            nested_count = None
            if name in (ranking, replay):
                prefix = '' if name == ranking else name+'/'
                inventory = json.loads(z.read(prefix+'source-inventory.json'))
                with zipfile.ZipFile(io.BytesIO(z.read(prefix+'frozen-source.zip'))) as source:
                    nested_count = len(members(source))
                    require({n: sha(source.read(n)) for n in source.namelist()} == inventory,
                            name+' frozen source inventory')
                restore(z, output / '.tools/benchmarks' / ranking if name == ranking
                        else output / '.tools/benchmarks')
            elif name.startswith('adaptive-confirmatory-'):
                # Only sidecar metadata is needed for replay arithmetic.
                for n in entries:
                    if PurePosixPath(n).name.startswith('onboarding') and n.endswith('.json'):
                        target = output / '.tools/datasets' / name / PurePosixPath(n).name
                        target.parent.mkdir(parents=True, exist_ok=True)
                        target.write_bytes(z.read(n))
            result['archives'].append(dict(name=name,sha256=digest,members=len(entries),
                                           frozen_source_members=nested_count))
    for name, scripts in ((ranking, ('summarize.py', 'audit.py')),
                          (replay, ('summarize_audit.py',))):
        folder = output / '.tools/benchmarks' / name
        original_summary = read(folder / 'summary.json')
        require(original_summary == read(bench / name / 'summary.json'),
                name+' archived versus tracked summary drift')
        for script in scripts:
            run = subprocess.run([sys.executable, '-B', str(folder / script)],
                                 cwd=output, capture_output=True, text=True, encoding='utf-8')
            (output / (name+'-'+script+'.log')).write_text(run.stdout+run.stderr, encoding='utf-8')
            require(run.returncode == 0, name+'/'+script+' failed; see retained log')
        require(read(folder / 'summary.json') == original_summary, name+' regenerated summary drift')
        audit = read(folder / 'audit.json')
        require(audit['status'] == 'PASS', name+' arithmetic audit')
        result['restored_audits'].append(dict(name=name, datasets=len(audit['checks']),
                                             summary_semantic_equality=True,
                                             tracked_summary_sha256=sha((bench / name / 'summary.json').read_bytes()),
                                             status='PASS'))
    result['status'] = 'PASS'
    result['script_sha256'] = sha(Path(__file__).read_bytes())
    (output / 'summary.json').write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
