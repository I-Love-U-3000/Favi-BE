"""Independent per-user arithmetic audit and nested optimizer/dataset summary."""
import hashlib, json, math, statistics
from collections import defaultdict, Counter
from pathlib import Path
OUT=Path(__file__).resolve().parent
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def save(p,v): p.write_text(json.dumps(v,indent=2,allow_nan=False)+'\n',encoding='utf-8')
def stats(xs):
    return dict(mean=statistics.mean(xs),std=statistics.stdev(xs) if len(xs)>1 else 0,min=min(xs),max=max(xs),n=len(xs))
def close(a,b): assert abs(a-b)<1e-13,(a,b)
def main():
    batch=read(OUT/'batch.json'); assert batch['status']=='PASS' and len(batch['datasets'])==6
    summary=dict(batch_sha256=sha(OUT/'batch.json'),scenarios={}); checks=[]
    for scenario in ('main','sparse'):
        datasets=[]
        for seed in (20261111,20261112,20261113):
            folder=OUT/f'adaptive-confirmatory-{scenario}-{seed}'
            for n,h in read(folder/'manifest.json').items(): assert sha(folder/n)==h,n
            report=read(folder/'report.json'); common=None; grouped=defaultdict(list); audited_modes=set()
            original=OUT.parent/'adaptive-confirmatory-ranking-20261108'/folder.name
            selection=read(original/'selection.json')
            dataset=OUT.parents[1]/'datasets'/folder.name
            primary_topics={r['user_id']:r['topics'] for r in read(dataset/'onboarding.json')}
            for result in report['results']:
                mode,label,opt=result['mode'],result['label'],result['optimizer_seed']
                suffix='' if opt is None else '-'+str(opt)
                rows=[json.loads(line) for line in (folder/f'{mode}-{label}{suffix}-users.jsonl').read_text().splitlines()]
                population=[(r['user_id'],r['candidate_sha256'],r['relevant']) for r in rows]
                if common is None: common=population
                assert population==common
                source_label='R' if label=='R-no-routing' else label
                original_rows=[json.loads(line) for line in (original/f'test-{source_label}{suffix}-users.jsonl').read_text().splitlines()]
                assert population==[(r['user_id'],r['candidate_sha256'],r['relevant']) for r in original_rows]
                assert all(r['primary_cohort']==o['adaptive_cohort'] for r,o in zip(rows,original_rows))
                if mode not in audited_modes:
                    topics=primary_topics if mode=='primary' else {u:[] for u in primary_topics} if mode=='no-topics' else {r['user_id']:r['topics'] for r in read(dataset/f'onboarding.{mode}.json')}
                    slots=sum(len(topics[r['user_id']]) for r in rows)
                    overlap=sum(len(set(primary_topics[r['user_id']]) & set(topics[r['user_id']])) for r in rows)
                    expected=dict(paired_users=len(rows),changed_topic_users=sum(primary_topics[r['user_id']]!=topics[r['user_id']] for r in rows),declared_slots=slots,overlap_with_primary=overlap,overlap_rate=overlap/slots if slots else None,cohort_transitions=dict(Counter(r['primary_cohort']+'->'+r['adaptive_cohort'] for r in rows)))
                    assert expected==report['metadata'][mode],mode
                    audited_modes.add(mode)
                if mode=='primary' and label!='R-no-routing':
                    assert [r['recommended'] for r in rows]==[r['recommended'] for r in original_rows]
                for row in rows:
                    relevant=set(row['relevant']); ranked=row['recommended']
                    assert len(set(ranked))==len(ranked)<=10
                    dcg=sum(1/math.log2(i+2) for i,item in enumerate(ranked) if item in relevant)
                    ideal=sum(1/math.log2(i+2) for i in range(min(10,len(relevant))))
                    close(dcg/ideal,row['ndcg']); close(len(set(ranked)&relevant)/len(relevant),row['recall'])
                assert len(rows)==result['all']['users']
                for metric in ('ndcg','recall'): close(statistics.mean(r[metric] for r in rows),result['all'][metric])
                for field,key in (('primary_cohorts','primary_cohort'),('adaptive_cohorts','adaptive_cohort')):
                    for c,metrics in result[field].items():
                        selected=[r for r in rows if r[key]==c]; assert len(selected)==metrics['users']
                        for metric in ('ndcg','recall'):
                            if selected: close(statistics.mean(r[metric] for r in selected),metrics[metric])
                if 'choice' in result:
                    assert result['choice']==selection['choices'][source_label]
                    assert result['threshold']==(1 if label=='R-no-routing' else result['choice']['threshold'])
                grouped[(mode,label)].append(result)
            modes={}
            for (mode,label),records in grouped.items():
                choice=selection['choices'].get('R' if label=='R-no-routing' else label)
                expected=[20261103,20261104,20261105] if choice and choice['family']=='lightgcn' else [None]
                assert [r['optimizer_seed'] for r in records]==expected
                methods=modes.setdefault(mode,{})
                m=dict(optimizer_seeds=expected,users=records[0]['all']['users'],all={},primary_cohorts={},adaptive_cohorts={})
                for metric in ('ndcg','recall','catalog_coverage','author_diversity','max_author_share'):
                    m['all'][metric]=stats([r['all'][metric] for r in records])
                for field in ('primary_cohorts','adaptive_cohorts'):
                    for c,metrics in records[0][field].items():
                        assert len({r[field][c]['users'] for r in records})==1
                        m[field][c]=dict(users=metrics['users'],**{metric:stats([r[field][c][metric] for r in records]) for metric in ('ndcg','recall') if metrics.get(metric) is not None})
                methods[label]=m
            deltas={}
            for mode,methods in modes.items():
                deltas[mode]={'R-B1':methods['R']['all']['ndcg']['mean']-methods['B1']['all']['ndcg']['mean']}
                if mode=='primary':
                    deltas[mode].update({'R-R-no-routing':methods['R']['all']['ndcg']['mean']-methods['R-no-routing']['all']['ndcg']['mean'], 'R-no-routing-B1':methods['R-no-routing']['all']['ndcg']['mean']-methods['B1']['all']['ndcg']['mean']})
                for label in ('B1','R'):
                    deltas[mode][label+'-primary']=methods[label]['all']['ndcg']['mean']-modes['primary'][label]['all']['ndcg']['mean']
            datasets.append(dict(seed=seed,modes=modes,deltas=deltas,metadata=report['metadata'],population_sha256=report['results'][0]['evaluation_population_sha256']))
            checks.append(dict(scenario=scenario,seed=seed,records=len(report['results']),users=len(common),status='PASS'))
        aggregates={}; deltas={}; cohorts={}
        for mode,methods in datasets[0]['modes'].items():
            aggregates[mode]={label:{metric:stats([d['modes'][mode][label]['all'][metric]['mean'] for d in datasets]) for metric in methods[label]['all']} for label in methods}
            deltas[mode]={key:stats([d['deltas'][mode][key] for d in datasets]) for key in datasets[0]['deltas'][mode]}
            cohorts[mode]={}
            for field in ('primary_cohorts','adaptive_cohorts'):
                cohorts[mode][field]={}
                for c in methods['R'][field]:
                    eligible=[d for d in datasets if d['modes'][mode]['R'][field][c]['users']>=30]
                    if eligible:
                        cohorts[mode][field][c]=dict(dataset_count=len(eligible),delta_R_B1=stats([d['modes'][mode]['R'][field][c]['ndcg']['mean']-d['modes'][mode]['B1'][field][c]['ndcg']['mean'] for d in eligible]),R_minus_primary=stats([d['modes'][mode]['R'][field][c]['ndcg']['mean']-d['modes']['primary']['R'][field][c]['ndcg']['mean'] for d in eligible]) if field=='primary_cohorts' else None)
        summary['scenarios'][scenario]=dict(datasets=datasets,aggregate=aggregates,deltas=deltas,cohorts=cohorts)
    save(OUT/'summary.json',summary)
    save(OUT/'audit.json',dict(status='PASS',checks=checks,summary_sha256=sha(OUT/'summary.json'),scope='Every per-user nDCG/Recall; common original population/candidates/targets; exact primary ranked IDs; both cohort partitions; frozen choices and optimizer hierarchy'))
    print(json.dumps(checks,indent=2))
if __name__=='__main__': main()
