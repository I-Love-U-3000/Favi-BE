"""Export real saved EASE/PPR ranks through the verified Favi GUID mappings."""
import argparse
import csv
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import uuid
import numpy as np

from check_dataset import verify
from collaborative_models import InteractionData, ScoreModel
from summarize_multiseed_benchmark import verify_manifest


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def mapping(path, source_key, target_key):
    with path.open(encoding='utf-8-sig', newline='') as stream:
        rows = list(csv.DictReader(stream))
    result = {int(r[source_key]): str(uuid.UUID(r[target_key])) for r in rows}
    if len(result) != len(rows) or len(set(result.values())) != len(rows) or any(uuid.UUID(v).int==0 for v in result.values()):
        raise ValueError('Mapping IDs must be nonempty and one-to-one')
    return result


def load_model(data, path, method, alpha=None):
    with np.load(path, allow_pickle=False) as stored:
        if not np.array_equal(stored['user_ids'], data.user_ids) or not np.array_equal(stored['item_ids'], data.item_ids):
            raise ValueError('Saved model source-ID mappings differ from the declared prefix')
        weights = stored['weights'].copy()
    if weights.shape != (len(data.item_ids), len(data.item_ids)) or not np.isfinite(weights).all():
        raise ValueError('Invalid saved numeric model')
    if method == 'ppr' and (alpha is None or not 0 < alpha < 1):
        raise ValueError('PPR requires its frozen alpha')
    return ScoreModel(data, method, weights=weights, normalization=alpha*(1-alpha) if method=='ppr' else 1)


def build(dataset, benchmark, output, max_users=1000):
    dataset, benchmark, output = dataset.resolve(), benchmark.resolve(), output.resolve()
    if output.exists() or output.is_relative_to(dataset) or output.is_relative_to(benchmark) or not 1 <= max_users <= 1000:
        raise ValueError('Use a fresh artifact directory outside inputs and 1..1000 users')
    integrity = verify(dataset)
    verify_manifest(benchmark)
    report = json.loads((benchmark/'report.json').read_text())
    if report['events_sha256'] != integrity['events_sha256'] or report.get('dataset_kind', 'oasis') != 'oasis':
        raise ValueError('Serving requires a matching native Favi dataset/model')
    if sha(benchmark/'selection.json') != report['selection_sha256']:
        raise ValueError('Selected parameters changed')
    test = [r for r in report['results'] if r['phase']=='test' and r['method'] in ('ease','ppr')]
    if len(test)!=2 or len({r['fit_before_tick'] for r in test})!=1:
        raise ValueError('Require both test-refitted EASE and PPR models at one cutoff')
    cutoff = test[0]['fit_before_tick']
    events = [json.loads(line) for line in (dataset/'events.jsonl').read_text().splitlines()]
    data = InteractionData(events, cutoff)
    users = mapping(dataset/'export/users.csv', 'oasis_user_id', 'user_id')
    posts = mapping(dataset/'export/posts.csv', 'oasis_post_id', 'post_id')
    alpha = report['selections']['ppr']['parameter']
    models = {m:load_model(data,benchmark/(m+'.npz'),m,alpha) for m in ('ease','ppr')}
    rows = []
    for user in data.user_ids[:max_users]:
        user=int(user)
        row={'userId':users[user]}
        for method,model in models.items():
            scores=model.scores(user)
            row[method]=[{'postId':posts[item], 'score':float(scores[data.item_index[item]])} for item in model.rank(user,300)]
        rows.append(row)
    mapping_hash=hashlib.sha256((dataset/'export/users.csv').read_bytes()+b'\0'+(dataset/'export/posts.csv').read_bytes()).hexdigest()
    model_hashes={m:sha(benchmark/(m+'.npz')) for m in models}
    version=hashlib.sha256(json.dumps([integrity['events_sha256'],report['selection_sha256'],mapping_hash,model_hashes],sort_keys=True).encode()).hexdigest()
    artifact={'schemaVersion':1,'version':version,'createdAtUtc':datetime.now(timezone.utc).isoformat(),
              'trainingCutoffTick':cutoff,'datasetEventsSha256':integrity['events_sha256'],
              'selectionSha256':report['selection_sha256'],'sourceMappingSha256':mapping_hash,'users':rows}
    output.mkdir(parents=True)
    (output/'artifact.json').write_text(json.dumps(artifact,allow_nan=False,separators=(',',':'))+'\n',encoding='utf-8')
    if (output/'artifact.json').stat().st_size > 16*1024*1024:
        raise ValueError('Artifact exceeds serving bound; reduce max-users')
    provenance={'dataset':str(dataset),'benchmark':str(benchmark),'events_sha256':integrity['events_sha256'],
                'model_sha256':model_hashes,'selection_sha256':report['selection_sha256'],
                'source_mapping_sha256':mapping_hash,'artifact_sha256':sha(output/'artifact.json'),
                'users':len(rows),'training_cutoff_tick':cutoff,'parameters':{m:report['selections'][m]['parameter'] for m in models}}
    (output/'provenance.json').write_text(json.dumps(provenance,indent=2)+'\n',encoding='utf-8')
    (output/Path(__file__).name).write_bytes(Path(__file__).read_bytes())
    (output/'manifest.json').write_text(json.dumps({p.name:sha(p) for p in output.iterdir()},indent=2)+'\n',encoding='utf-8')
    print(json.dumps(provenance))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dataset',type=Path,required=True)
    parser.add_argument('--benchmark',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--max-users',type=int,default=1000)
    args=parser.parse_args()
    build(args.dataset,args.benchmark,args.output,args.max_users)
