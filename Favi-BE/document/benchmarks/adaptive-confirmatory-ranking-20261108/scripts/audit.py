"""Independent arithmetic and population QA, without rerunning a model."""
import hashlib, json, math, statistics
from pathlib import Path
OUT=Path(__file__).resolve().parent
summary=json.loads((OUT/'summary.json').read_text())
checks=[]
for scenario in ('main','sparse'):
    deltas=[]
    for seed in (20261111,20261112,20261113):
        folder=OUT/f'adaptive-confirmatory-{scenario}-{seed}'
        report=json.loads((folder/'report.json').read_text())
        choices=json.loads((folder/'selection.json').read_text())['choices']
        trials=json.loads((folder/'tuning.json').read_text())
        assert all(v in trials for v in choices.values())
        common=None
        for result in report['results']:
            suffix='' if result['optimizer_seed'] is None else '-'+str(result['optimizer_seed'])
            path=folder/f"test-{result['label']}{suffix}-users.jsonl"
            rows=[json.loads(line) for line in path.read_text().splitlines()]
            population=[(r['user_id'],r['candidate_sha256'],r['relevant']) for r in rows]
            if common is None: common=population
            assert population==common
            ndcg=[]; recall=[]
            for row in rows:
                relevant=set(row['relevant']); ranked=row['recommended']
                dcg=sum(1/math.log2(i+2) for i,item in enumerate(ranked) if item in relevant)
                ideal=sum(1/math.log2(i+2) for i in range(min(10,len(relevant))))
                n=dcg/ideal; r=len(set(ranked)&relevant)/len(relevant)
                assert abs(n-row['ndcg'])<1e-14 and abs(r-row['recall'])<1e-14
                ndcg.append(n);recall.append(r)
            assert len(rows)==result['all']['users']
            assert abs(statistics.mean(ndcg)-result['all']['ndcg'])<1e-14
            assert abs(statistics.mean(recall)-result['all']['recall'])<1e-14
            for cohort,metric in result['adaptive_cohorts'].items():
                selected=[row for row in rows if row['adaptive_cohort']==cohort]
                assert len(selected)==metric['users']
                if selected: assert abs(statistics.mean(r['ndcg'] for r in selected)-metric['ndcg'])<1e-14
        means={label:statistics.mean(r['all']['ndcg'] for r in report['results'] if r['label']==label) for label in ('B1','R')}
        delta=means['R']-means['B1']; deltas.append(delta)
        stored=next(d for d in summary['scenarios'][scenario]['datasets'] if d['seed']==seed)
        assert abs(delta-stored['delta_R_B1_ndcg'])<1e-14
        identical=choices['B1']==choices['R']
        if identical:
            for r in (x for x in report['results'] if x['label']=='B1'):
                suffix='' if r['optimizer_seed'] is None else '-'+str(r['optimizer_seed'])
                assert (folder/f'test-B1{suffix}-users.jsonl').read_bytes()==(folder/f'test-R{suffix}-users.jsonl').read_bytes()
        checks.append(dict(scenario=scenario,seed=seed,result_records=len(report['results']),target_users=len(common),delta=delta,identical_B1_R_prediction_files=identical,status='PASS'))
    assert abs(statistics.mean(deltas)-summary['scenarios'][scenario]['delta_R_B1_ndcg']['mean'])<1e-14
    assert abs(statistics.stdev(deltas)-summary['scenarios'][scenario]['delta_R_B1_ndcg']['std'])<1e-14
result=dict(status='PASS',scope='All per-user nDCG/Recall and cohort means, common users/candidates/targets, selected trial membership, direct hierarchical deltas/std, identical choices prediction bytes',checks=checks,summary_sha256=hashlib.sha256((OUT/'summary.json').read_bytes()).hexdigest())
(OUT/'audit.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
