"""Verified MovieLens 100K conversion, separate from native OASIS datasets."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import zipfile

ARCHIVE_SHA256 = '50d2a982c66986937beb9ffb3aa76efe955bf3d5c6b761f4e3a7cd717c6a3229'
SOURCE = 'https://files.grouplens.org/datasets/movielens/ml-100k.zip'
RECIPE = {'schema_version': 1, 'dataset_kind': 'movielens100k', 'ticks': 100000,
          'positive_rating_threshold': 4, 'catalog': 'all u.item IDs at tick -1; no known author',
          'time': 'first raw ordinal in each timestamp block; fixed cutoffs 60000/80000'}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def convert(rating_text, item_text, strict=True):
    items = [int(line.split('|')[0]) for line in item_text.splitlines() if line]
    require(items and len(items) == len(set(items)) and min(items) > 0, 'Invalid or duplicate catalog IDs')
    ratings = []
    for line in rating_text.splitlines():
        values = line.split()
        require(len(values) == 4, 'Rating must have four fields')
        user, item, rating, timestamp = map(int, values)
        require(user > 0 and item in items and 1 <= rating <= 5 and timestamp > 0, 'Invalid rating/reference')
        ratings.append((timestamp, user, item, rating))
    require(ratings and len({(u, i) for _, u, i, _ in ratings}) == len(ratings), 'Duplicate user/item rating')
    users = Counter(u for _, u, _, _ in ratings)
    if strict:
        require(len(ratings) == 100000 and set(users) == set(range(1, 944)) and
                set(items) == set(range(1, 1683)) and min(users.values()) >= 20, 'Unexpected MovieLens 100K shape')
    ratings.sort()
    events = [{'kind': 'post', 'post_id': i, 'user_id': -1, 'tick': -1} for i in sorted(items)]
    raw_windows, positive_windows = Counter(), Counter()
    previous, block_start = None, 0
    for index, (timestamp, user, item, rating) in enumerate(ratings):
        if timestamp != previous:
            block_start, previous = index, timestamp
        phase = 'train' if block_start < 60000 else 'validation' if block_start < 80000 else 'test'
        raw_windows[phase] += 1
        if rating >= 4:
            events.append({'kind': 'like', 'post_id': item, 'user_id': user, 'tick': block_start})
            positive_windows[phase] += 1
    info = {'raw_ratings': len(ratings), 'users': len(users), 'items': len(items),
            'positive_ratings': sum(positive_windows.values()), 'raw_windows': dict(raw_windows),
            'positive_windows': dict(positive_windows), 'min_timestamp': ratings[0][0], 'max_timestamp': ratings[-1][0]}
    return events, info


def source_content(archive):
    require(archive.stat().st_size <= 50000000 and sha(archive) == ARCHIVE_SHA256, 'Archive checksum differs from declared official input')
    with zipfile.ZipFile(archive) as source:
        require(len(source.namelist()) == len(set(source.namelist())), 'Duplicate archive member')
        require(sum(i.file_size for i in source.infolist()) < 100000000 and source.testzip() is None, 'Corrupt/oversized archive')
        return (source.read('ml-100k/u.data').decode('ascii'), source.read('ml-100k/u.item').decode('latin-1'),
                source.read('ml-100k/README'))


def event_bytes(events):
    return ''.join(json.dumps(e, sort_keys=True) + '\n' for e in events).encode('utf-8')


def prepare(archive, output):
    require(not output.exists(), 'Choose a fresh dataset output')
    raw, items, license_text = source_content(archive)
    events, info = convert(raw, items)
    output.mkdir(parents=True)
    (output / 'source.zip').write_bytes(archive.read_bytes())
    (output / 'README.source.txt').write_bytes(license_text)
    (output / 'events.jsonl').write_bytes(event_bytes(events))
    (output / 'config.resolved.json').write_text(json.dumps(RECIPE, indent=2)+'\n', encoding='utf-8')
    (output / 'provenance.json').write_text(json.dumps({'source_url': SOURCE, 'archive_sha256': ARCHIVE_SHA256,
         'counts': info, 'scope': 'Local offline research; do not redistribute or use commercially without required permission'}, indent=2)+'\n', encoding='utf-8')
    (output / Path(__file__).name).write_bytes(Path(__file__).read_bytes())
    manifest = {p.name: sha(p) for p in output.iterdir()}
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(verify(output)))


def verify(root):
    root = root.resolve()
    manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8'))
    require(set(manifest) == {'source.zip', 'README.source.txt', 'events.jsonl', 'config.resolved.json', 'provenance.json', 'movielens_dataset.py'}, 'Unexpected bundle members')
    for name, expected in manifest.items():
        require(sha(root / name) == expected, 'Dataset member fingerprint mismatch: '+name)
    require(json.loads((root/'config.resolved.json').read_text()) == RECIPE, 'MovieLens recipe changed')
    raw, items, license_text = source_content(root/'source.zip')
    events, info = convert(raw, items)
    require((root/'events.jsonl').read_bytes() == event_bytes(events), 'Event conversion differs from raw archive')
    require((root/'README.source.txt').read_bytes() == license_text, 'Source license text changed')
    provenance = json.loads((root/'provenance.json').read_text())
    require(provenance['source_url'] == SOURCE and provenance['archive_sha256'] == ARCHIVE_SHA256 and provenance['counts'] == info,
            'Provenance differs from source')
    return {'dataset_kind': 'movielens100k', 'events_sha256': sha(root/'events.jsonl'),
            'archive_sha256': ARCHIVE_SHA256, 'counts': info, 'verified_files': len(manifest)}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    prepare(args.archive, args.output)
