"""One combined readiness report; no hidden missing scenario or cohort."""
import argparse
import hashlib
import json
from pathlib import Path
from analyze_adaptive_cohorts import analyze


def summarize(main, sparse, output):
    if output.exists():
        raise ValueError('Preserve previous report; choose new output directory')
    reports = {name: analyze(root) for name, root in (('main', main), ('sparse-topic', sparse))}
    for name, report in reports.items():
        if report['scenario'] != name:
            raise ValueError('Scenario mismatch')
    configs = [json.loads((root / 'config.resolved.json').read_text(encoding='utf-8-sig')) for root in (main, sparse)]
    if configs[0] != configs[1]:
        raise ValueError('Pilot scenarios require identical generation config')
    output.mkdir(parents=True)
    result = {'schema_version': 1, 'dataset_gate_passed': all(r['gate_passed'] for r in reports.values()),
              'seed': configs[0]['seed'], 'users': configs[0]['users'], 'scenarios': reports,
              'scope': 'Exploratory data readiness only; formula parity, confirmatory freeze and model results remain separate gates',
              'analyzer_sha256': hashlib.sha256(Path(__file__).with_name('analyze_adaptive_cohorts.py').read_bytes()).hexdigest()}
    (output / 'report.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    lines = ['# Pilot lifecycle-v2: data readiness', '', f"Combined data gate: {'PASS' if result['dataset_gate_passed'] else 'FAIL'}", '',
             '| Scenario | Cohort | Validation target users | Test target users | Status |', '|---|---|---:|---:|---|']
    for name, report in reports.items():
        for cohort in report['required_cohorts']:
            v = report['phases']['validation']['cohorts'][cohort]['with_target']
            t = report['phases']['test']['cohorts'][cohort]['with_target']
            lines.append(f"| {name} | {cohort} | {v} | {t} | {'PASS' if min(v,t)>=30 else 'FAIL'} |")
    lines += ['', result['scope'], '', 'Counts describe a deliberately constructed synthetic population. They do not establish relevance gains or statistical power.']
    (output / 'review.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('\n'.join(lines))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--main', type=Path, required=True)
    parser.add_argument('--sparse', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    summarize(args.main, args.sparse, args.output)
