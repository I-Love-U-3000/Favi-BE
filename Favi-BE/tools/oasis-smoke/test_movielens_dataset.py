import unittest
from movielens_dataset import convert
from benchmark_recommenders import evaluate


class MovieLensTests(unittest.TestCase):
    def test_timestamps_are_sorted_and_ties_share_tick_before_positive_filter(self):
        events, info = convert('2 2 3 30\n1 1 5 10\n2 1 4 10\n1 2 4 40\n', '1|one\n2|two\n', strict=False)
        positives = [e for e in events if e['kind']=='like']
        self.assertEqual([e['tick'] for e in positives], [0, 0, 3])
        self.assertEqual(info['raw_ratings'], 4)
        self.assertEqual(info['positive_ratings'], 3)
        self.assertTrue(all(e['user_id']==-1 for e in events if e['kind']=='post'))

    def test_duplicate_and_bad_references_rejected(self):
        for text in ('1 1 4 10\n1 1 5 20\n', '1 2 4 10\n', '1 1 6 10\n'):
            with self.assertRaises(ValueError):
                convert(text, '1|one\n', strict=False)

    def test_external_author_metrics_are_not_fabricated(self):
        events = [{'kind':'post','post_id':i,'user_id':-1,'tick':-1} for i in (1,2)] + [
            {'kind':'like','post_id':1,'user_id':1,'tick':0},
            {'kind':'like','post_id':2,'user_id':1,'tick':2}]
        protocol={'k':1,'hours_per_tick':1,'timing_repeats':1}
        result, rows = evaluate(events, 1, 3, 'popularity', protocol, measure=False, author_metrics=False)
        self.assertIsNone(result['all']['author_diversity'])
        self.assertIsNone(rows[0]['max_author_share'])
        self.assertEqual(result['all']['recall'], 1)


if __name__=='__main__':
    unittest.main()
