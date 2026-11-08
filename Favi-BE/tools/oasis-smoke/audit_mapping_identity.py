"""Read-only CSV/snapshot identity audit; not database or authenticity proof."""
import argparse
import csv
import hashlib
import json
from pathlib import Path
import uuid


def audit(root):
    root = Path(root)
    export = root / 'export'
    manifest = json.loads((export / 'manifest.json').read_text(encoding='utf-8-sig'))
    # Adapter snapshot_sha256 hashes its UTF-8 payload before write_text;
    # universal newline decoding matches that contract on Windows too.
    snapshot_bytes = (export / 'snapshot.json').read_text(encoding='utf-8').encode('utf-8')
    if hashlib.sha256(snapshot_bytes).hexdigest() != manifest['snapshot_sha256']:
        raise ValueError('Snapshot checksum mismatch')
    snapshot = json.loads(snapshot_bytes)
    namespace = uuid.uuid5(uuid.NAMESPACE_URL, 'favi:oasis:' + manifest['run_id'])
    counts = {}
    for kind, plural, source, target in (
        ('user', 'users', 'oasis_user_id', 'user_id'),
        ('post', 'posts', 'oasis_post_id', 'post_id')):
        expected = {}
        for row in snapshot[plural]:
            key = row['source_id']
            if type(key) is not int or key < 0 or key in expected:
                raise ValueError('Invalid snapshot source identity')
            value = str(uuid.UUID(row['id']))
            if value != str(uuid.uuid5(namespace, f'{kind}:{key}')):
                raise ValueError('Snapshot namespace identity mismatch')
            expected[key] = value
        with (export / (plural + '.csv')).open(encoding='utf-8-sig', newline='') as stream:
            rows = list(csv.DictReader(stream))
        actual = {int(row[source]): str(uuid.UUID(row[target])) for row in rows}
        if len(actual) != len(rows) or actual != expected:
            raise ValueError('CSV/snapshot identity mismatch: ' + plural)
        if len(expected) != manifest['counts'][plural]:
            raise ValueError('Snapshot count mismatch')
        counts[plural] = len(expected)
    return dict(run_id=manifest['run_id'], counts=counts,
                snapshot_sha256=hashlib.sha256(snapshot_bytes).hexdigest(),
                scope='CSV/source-ID/GUID/snapshot namespace; no SQL execution or authenticity proof')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('datasets', type=Path, nargs='+')
    print(json.dumps([audit(root) for root in parser.parse_args().datasets], indent=2))
