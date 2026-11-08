"""Summarize frozen runs; optimizer repeats stay nested within datasets."""
import hashlib, json, statistics
from pathlib import Path
ROOT=Path(__file__).resolve().parent
COHORTS=('C0','C1','C2','C3','T1','T2','W1','W2')
METRICS=('ndcg','recall','precision','hit_rate','catalog_coverage','author_diversity','max_author_share')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def save(p,v): p.write_text(json.dumps(v,indent=2,allow_nan=False)+'\n',encoding='utf-8')
def stats(values):
    return dict(mean=statistics.mean(values),std=statistics.stdev(values) if len(values)>1 else 0,min=min(values),max=max(values),n=len(values))
def main():
    batch=json.loads((ROOT/'batch.json').read_text())
    if batch['status']!='PASS' or len(batch['datasets'])!=6: raise ValueError('Require six successful datasets')
    summary=dict(protocol_id='adaptive-v1-20261108',batch_sha256=sha(ROOT/'batch.json'),scenarios={},limitations=['Synthetic recovery, no causal or real-user benefit evidence','Three dataset seeds descriptive, no significance or power claim','Validation uses optimizer seed20261103 only','Full process cost and HTTP not measured by ranking report'])
    for scenario in ('main','sparse'):
        datasets=[]
        for seed in (20261111,20261112,20261113):
            folder=ROOT/f'adaptive-confirmatory-{scenario}-{seed}'
            manifest=json.loads((folder/'manifest.json').read_text())
            assert all(sha(folder/n)==h for n,h in manifest.items())
            report=json.loads((folder/'report.json').read_text())
            selection=json.loads((folder/'selection.json').read_text())
            assert sha(folder/'selection.json')==report['selection_sha256']
            assert selection['test_not_used']
            results=report['results']
            assert len({r['evaluation_population_sha256'] for r in results})==1
            groups={label:[r for r in results if r['label']==label] for label in sorted({r['label'] for r in results})}
            methods={}
            for label,rows in groups.items():
                expected=[20261103,20261104,20261105] if label in selection['choices'] and selection['choices'][label]['family']=='lightgcn' else [None]
                assert [r['optimizer_seed'] for r in rows]==expected,(label,expected)
                assert len({r['all']['users'] for r in rows})==1
                m=dict(optimizer_seeds=expected,users=rows[0]['all']['users'],all={},cohorts={},optimizer_ndcg=[r['all']['ndcg'] for r in rows],diagnostics={})
                for metric in METRICS:
                    values=[r['all'][metric] for r in rows]
                    m['all'][metric]=stats(values)
                for c in COHORTS:
                    cohort=rows[0]['adaptive_cohorts'][c]
                    assert len({r['adaptive_cohorts'][c]['users'] for r in rows})==1
                    m['cohorts'][c]=dict(users=cohort['users'],metrics={metric:stats([r['adaptive_cohorts'][c][metric] for r in rows]) for metric in METRICS if cohort.get(metric) is not None})
                for key in ('empty_users','short_users','mean_item_age_hours','cold_target_pool_misses'):
                    m['diagnostics'][key]=stats([r['feed_diagnostics'][key] for r in rows])
                m['refit_seconds']=[r.get('refit_seconds') for r in rows]
                m['model_state_bytes']=[r.get('model_state_bytes') for r in rows]
                methods[label]=m
            delta=methods['R']['all']['ndcg']['mean']-methods['B1']['all']['ndcg']['mean']
            cohort_delta={c:methods['R']['cohorts'][c]['metrics']['ndcg']['mean']-methods['B1']['cohorts'][c]['metrics']['ndcg']['mean'] for c in COHORTS if methods['R']['cohorts'][c]['users']}
            datasets.append(dict(seed=seed,selection_sha256=report['selection_sha256'],choices=selection['choices'],population_sha256=results[0]['evaluation_population_sha256'],gate=report['gate'],methods=methods,delta_R_B1_ndcg=delta,cohort_delta_R_B1_ndcg=cohort_delta))
        labels=sorted(datasets[0]['methods'])
        aggregates={label:{metric:stats([d['methods'][label]['all'][metric]['mean'] for d in datasets]) for metric in METRICS} for label in labels}
        deltas=[d['delta_R_B1_ndcg'] for d in datasets]
        cohort_deltas={c:stats([d['cohort_delta_R_B1_ndcg'][c] for d in datasets if c in d['cohort_delta_R_B1_ndcg']]) for c in COHORTS if all(d['methods']['R']['cohorts'][c]['users']>=30 for d in datasets)}
        summary['scenarios'][scenario]=dict(datasets=datasets,aggregate=aggregates,delta_R_B1_ndcg=stats(deltas),win_tie_loss=dict(win=sum(v>1e-12 for v in deltas),tie=sum(abs(v)<=1e-12 for v in deltas),loss=sum(v< -1e-12 for v in deltas)),cohort_delta_R_B1_ndcg=cohort_deltas)
    main=summary['scenarios']['main']
    primary_cohorts=('C0','C1','C2','T1','T2','W1','W2')
    checks=dict(mean_gain_at_least_001=main['delta_R_B1_ndcg']['mean']>=.001,nonnegative_at_least_two_seeds=sum(d['delta_R_B1_ndcg']>=0 for d in main['datasets'])>=2,no_main_cohort_mean_loss_below_minus_002=all(main['cohort_delta_R_B1_ndcg'][c]['mean']>=-.002 for c in primary_cohorts))
    summary['main_practical_gate']=dict(checks=checks,passed=all(checks.values()),interpretation='Engineering thresholds; not statistical significance or satisfaction')
    sparse=summary['scenarios']['sparse']
    summary['sparse_C3_loss_budget']=dict(mean_delta=sparse['cohort_delta_R_B1_ndcg']['C3']['mean'],passed=sparse['cohort_delta_R_B1_ndcg']['C3']['mean']>=-.002)
    save(ROOT/'summary.json',summary)
    print(json.dumps(dict(main_practical_gate=summary['main_practical_gate'],main_delta=main['delta_R_B1_ndcg'],sparse_delta=sparse['delta_R_B1_ndcg'],sparse_C3=summary['sparse_C3_loss_budget']),indent=2))
if __name__=='__main__': main()
