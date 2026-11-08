import csv
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import uuid

from audit_mapping_identity import audit


class MappingIdentityTests(unittest.TestCase):
    def test_identity_and_negative_controls(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            export = root / 'export'
            export.mkdir()
            namespace = uuid.uuid5(uuid.NAMESPACE_URL, 'favi:oasis:fixture')
            snapshot = {plural: [dict(source_id=i, id=str(uuid.uuid5(namespace, f'{kind}:{i}')))
                                 for i in (0, 1)]
                        for kind, plural in (('user', 'users'), ('post', 'posts'))}

            def save():
                data = json.dumps(snapshot).encode()
                (export / 'snapshot.json').write_bytes(data)
                (export / 'manifest.json').write_text(json.dumps(dict(
                    run_id='fixture', snapshot_sha256=hashlib.sha256(data).hexdigest(),
                    counts=dict(users=2, posts=2))))

            def csvs():
                for kind, plural in (('user', 'users'), ('post', 'posts')):
                    with (export / (plural + '.csv')).open('w', newline='') as stream:
                        writer = csv.writer(stream)
                        writer.writerow(['oasis_' + kind + '_id', kind + '_id'])
                        writer.writerows((r['source_id'], r['id']) for r in snapshot[plural])

            save()
            csvs()
            self.assertEqual(audit(root)['counts'], dict(users=2, posts=2))
            path = export / 'snapshot.json'
            # Payload checksum includes the final newline; use a multiline payload
            # to exercise CRLF normalization without changing logical content.
            data = json.dumps(snapshot, indent=2) + '\n'
            manifest = json.loads((export / 'manifest.json').read_text())
            manifest['snapshot_sha256'] = hashlib.sha256(data.encode()).hexdigest()
            (export / 'manifest.json').write_text(json.dumps(manifest))
            path.write_bytes(data.replace('\n', '\r\n').encode())
            self.assertEqual(audit(root)['counts'], dict(users=2, posts=2))
            for plural in ('users', 'posts'):
                path = export / (plural + '.csv')
                original = path.read_text()
                lines = original.splitlines()
                ids = [line.split(',') for line in lines[1:]]
                # Bijective GUID permutation passes shape validation but changes identity.
                path.write_text(lines[0] + '\n' + '\n'.join(
                    ids[i][0] + ',' + ids[1-i][1] for i in range(2)) + '\n')
                with self.assertRaisesRegex(ValueError, 'CSV/snapshot'):
                    audit(root)
                path.write_text(original + lines[1] + '\n')
                with self.assertRaisesRegex(ValueError, 'CSV/snapshot'):
                    audit(root)
                path.write_text(original)
            (export / 'snapshot.json').write_bytes(b'{}')
            with self.assertRaisesRegex(ValueError, 'checksum'):
                audit(root)
            snapshot['users'][0]['id'] = str(uuid.uuid4())
            save()
            csvs()
            with self.assertRaisesRegex(ValueError, 'namespace'):
                audit(root)


if __name__ == '__main__':
    unittest.main()
