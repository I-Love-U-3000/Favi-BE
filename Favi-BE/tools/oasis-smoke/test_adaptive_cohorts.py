import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from adaptive_onboarding import TOPICS
from analyze_adaptive_cohorts import analyze


class CohortTests(unittest.TestCase):
    def test_cutoff_targets_and_short_topic_pool(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            config = {'users': 100, 'ticks': 10, 'topics': TOPICS}
            rows = [{'user_id': i, 'arrival_tick': -1, 'preference_tick': -1,
                     'topics': ['food'] if i == 0 else []} for i in range(100)]
            events = [dict(kind='post', user_id=99, post_id=1, topic=1, tick=-1),
                      dict(kind='post', user_id=99, post_id=2, topic=1, tick=6),
                      dict(kind='like', user_id=0, post_id=1, tick=6),
                      dict(kind='like', user_id=0, post_id=2, tick=7)]
            for name, value in (('config.resolved.json', config), ('onboarding.json', rows)):
                (root / name).write_text(json.dumps(value))
            (root / 'events.jsonl').write_text('\n'.join(json.dumps(e) for e in events))
            with patch('analyze_adaptive_cohorts.verify', return_value={'verified': True}):
                result = analyze(root)
                cohort = result['phases']['validation']['cohorts']['C3']
                self.assertEqual(cohort['with_target'], 1)
                self.assertEqual(cohort['targets'], 1)
                self.assertEqual(cohort['excluded_targets'], 1)
                self.assertEqual(result['phases']['test']['cohorts']['T2']['without_target'], 1)
                self.assertFalse(result['gate_passed'])
                rows[0]['arrival_tick'] = 7
                rows[0]['preference_tick'] = 7
                (root / 'onboarding.json').write_text(json.dumps(rows))
                with self.assertRaisesRegex(ValueError, 'before arrival'):
                    analyze(root)
