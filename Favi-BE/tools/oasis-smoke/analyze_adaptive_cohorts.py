"""Read-back audit and target-bearing cohorts for adaptive native datasets."""
import argparse
from collections import Counter, defaultdict
import json
from pathlib import Path
from adaptive_onboarding import TOPICS, plan, preferences, validate, visible_preferences, classify_cohort
from check_dataset import verify

COHORTS = ('C0', 'C1', 'C2', 'C3', 'T1', 'T2', 'W1', 'W2')


def analyze(root):
    integrity = verify(root)
    config = json.loads((root / 'config.resolved.json').read_text(encoding='utf-8-sig'))
    if tuple(config['topics']) != TOPICS:
        raise ValueError('Production topic catalog mismatch')
    rows = json.loads((root / 'onboarding.json').read_text())
    validate(rows, config['users'], config['ticks'])
    policy_path = root / 'adaptive-policy.json'
    scenario = 'legacy-all'
    preference_summary = {}
    if policy_path.exists():
        policy = json.loads(policy_path.read_text())
        expected = plan(config['users'], config['seed'], config['ticks'], policy['arrival_policy'])
        scenario = policy.get('scenario', 'legacy-all')
        if policy['arrival_policy'] == 'lifecycle-v2':
            from community_policy import population
            people = population(config['seed'], config['users'], len(TOPICS))
            informative = preferences(expected, people, 'informative')
            for mode in ('informative', 'noisy', 'independent'):
                stored = json.loads((root / f'onboarding.{mode}.json').read_text())
                if stored != preferences(expected, people, mode):
                    raise ValueError('Preference sidecar differs from declared policy')
                slots = sum(len(r['topics']) for r in stored)
                overlap = sum(len(set(r['topics']) & set(i['topics'])) for r, i in zip(stored, informative))
                preference_summary[mode] = {'declared_slots': slots, 'overlap_with_initial_top_interests': overlap,
                                            'overlap_rate': overlap / slots if slots else None,
                                            'scope': 'pre-scarcity alternative metadata; not actual sparse preferences' if scenario == 'sparse-topic' else 'ranking replay metadata; not behavioral effect'}
            expected = preferences(expected, people, 'informative')
        if scenario == 'sparse-topic':
            expected = [{**r, 'topics': ['music'] if r['topics'] else []} for r in expected]
        if rows != expected:
            raise ValueError('Onboarding differs from declared deterministic policy')
    users = {r['user_id']: r for r in rows}
    events = [json.loads(line) for line in (root / 'events.jsonl').read_text().splitlines()]
    if scenario == 'sparse-topic' and sum(e['kind'] == 'post' and e['topic'] == 9 for e in events) > 9:
        raise ValueError('Rare topic exceeds nine posts')
    for event in events:
        if event['tick'] < users[event['user_id']]['arrival_tick']:
            raise ValueError('Action before arrival')
        if event['kind'] == 'follow' and event['tick'] < users[event['target_user_id']]['arrival_tick']:
            raise ValueError('Target before arrival')
    required = ['C3'] if scenario == 'sparse-topic' else [c for c in COHORTS if c != 'C3'] if scenario == 'main' else list(COHORTS)
    report = {'schema_version': 1, 'dataset': str(root), 'integrity': integrity, 'scenario': scenario, 'required_cohorts': required, 'preference_summary': preference_summary,
              'minimum_target_users': 30, 'phases': {}}
    first, second = config['ticks'] * 3 // 5, config['ticks'] * 4 // 5
    for phase, cutoff, end in (('validation', first, second), ('test', second, config['ticks'])):
        items = {e['post_id']: e for e in events if e['kind'] == 'post' and e['tick'] < cutoff}
        history, future = defaultdict(set), defaultdict(set)
        for e in events:
            if e['kind'] == 'like':
                if e['tick'] < cutoff:
                    history[e['user_id']].add(e['post_id'])
                elif e['tick'] < end:
                    future[e['user_id']].add(e['post_id'])
        counts = {c: Counter(users=0, with_target=0, without_target=0, targets=0, excluded_targets=0,
                             excluded_future_item=0, excluded_own_item=0, excluded_prior_positive=0) for c in COHORTS}
        not_arrived = 0
        for row in rows:
            user = row['user_id']
            topics = visible_preferences(row, cutoff)
            if topics is None:
                not_arrived += 1
                continue
            eligible = {i for i, p in items.items() if p['user_id'] != user and i not in history[user]}
            n = len(history[user])
            matching = sum(config['topics'][items[i]['topic']] in topics for i in eligible)
            cohort = classify_cohort(n, topics, matching)
            targets = future[user] & eligible
            counts[cohort].update(users=1, with_target=bool(targets), without_target=not targets,
                                  targets=len(targets), excluded_targets=len(future[user] - eligible))
            for item in future[user] - eligible:
                reason = 'excluded_future_item' if item not in items else 'excluded_own_item' if items[item]['user_id'] == user else 'excluded_prior_positive'
                counts[cohort][reason] += 1
        report['phases'][phase] = {'cutoff': cutoff, 'end': end, 'not_arrived': not_arrived,
                                  'cohorts': counts, 'cohort_status': {c: ('PASS' if counts[c]['with_target'] >= 30 else 'FAIL') if c in required else 'N/A' for c in COHORTS},
                                  'gate_passed': all(counts[c]['with_target'] >= 30 for c in required)}
    report['gate_passed'] = all(p['gate_passed'] for p in report['phases'].values())
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dataset', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        parser.error('Output exists; preserve prior results')
    result = analyze(args.dataset)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result))
