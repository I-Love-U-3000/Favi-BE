"""Pilot baseline evaluation under the new observed-topic/arrival contract."""
import argparse
import hashlib
import json
from pathlib import Path
from adaptive_feed import AdaptiveFeed
from analyze_adaptive_cohorts import analyze, COHORTS
from benchmark_recommenders import evaluate, summarize


def run(dataset, output):
    if output.exists() or output.resolve().is_relative_to(dataset.resolve()):
        raise ValueError('Use fresh output outside dataset')
    gate = analyze(dataset)
    if not gate['gate_passed']:
        raise ValueError('Scenario data gate failed')
    output.mkdir(parents=True)
    config = json.loads((dataset / 'config.resolved.json').read_text(encoding='utf-8-sig'))
    rows = json.loads((dataset / 'onboarding.json').read_text())
    events = [json.loads(line) for line in (dataset / 'events.jsonl').read_text().splitlines()]
    protocol = dict(k=10, hours_per_tick=1.0, timing_repeats=1)
    results = []
    for phase, start, end in (('validation', config['ticks'] * 3 // 5, config['ticks'] * 4 // 5),
                              ('test', config['ticks'] * 4 // 5, config['ticks'])):
        population = None
        for method in ('popularity', 'topic-only', 'formula'):
            feed = AdaptiveFeed(events, start, rows, method=method)
            result, users = evaluate(events, start, end, method, protocol, fitted_model=feed, measure=False)
            if population is not None and result['evaluation_population_sha256'] != population:
                raise ValueError('Baseline candidates/targets differ')
            population = result['evaluation_population_sha256']
            for user in users:
                user['adaptive_cohort'] = feed.cohort(user['user_id'])
            result['adaptive_cohorts'] = {c: summarize([r for r in users if r['adaptive_cohort'] == c], len(feed.items)) for c in COHORTS}
            for c in gate['required_cohorts']:
                if result['adaptive_cohorts'][c]['users'] != gate['phases'][phase]['cohorts'][c]['with_target']:
                    raise ValueError('Gate/evaluator cohort denominator differs')
            result['phase'] = phase
            results.append(result)
            (output / f'{phase}-{method}-users.jsonl').write_text(''.join(json.dumps(r) + '\n' for r in users))
            print(json.dumps(dict(phase=phase, method=method, users=result['all']['users'], ndcg=result['all']['ndcg'])), flush=True)
    report = dict(schema_version=1, exploratory=True, gate=gate, protocol=protocol, results=results,
                  limitations=['Synthetic observed-topic metadata, not imported production tags', 'Cold bounded graph-empty reader-shaped pool under shared offline eligibility; not SQL/HTTP replay', 'Formula uses comments/follows/tag count; Popularity uses likes', 'Pilot only, no tuning or superiority claim'])
    (output / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
    source_names = ('run_adaptive_baselines.py', 'adaptive_feed.py', 'favi_rankers.py', 'adaptive_onboarding.py', 'analyze_adaptive_cohorts.py', 'benchmark_recommenders.py')
    for name in source_names:
        (output / name).write_bytes(Path(__file__).with_name(name).read_bytes())
    (output / 'manifest.json').write_text(json.dumps({p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in output.iterdir()}, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dataset', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    run(args.dataset, args.output)
