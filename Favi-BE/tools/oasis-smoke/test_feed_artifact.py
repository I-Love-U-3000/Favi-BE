from pathlib import Path
import tempfile
import unittest
import numpy as np
from build_feed_artifact import mapping, load_model
from collaborative_models import InteractionData, fit_ppr
from run_expanded_benchmark import write_model


class FeedArtifactTests(unittest.TestCase):
    def test_guid_mapping_rejects_collisions(self):
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'users.csv'
            path.write_text('source,target\n1,00000000-0000-0000-0000-000000000001\n2,00000000-0000-0000-0000-000000000001\n')
            with self.assertRaises(ValueError): mapping(path,'source','target')

    def test_saved_ppr_reconstructs_absolute_scores_and_checks_mapping(self):
        events=[{'kind':'post','post_id':i,'user_id':10+i,'tick':-1} for i in [1,2,3]]+[
            {'kind':'like','user_id':u,'post_id':i,'tick':0} for u,i in [(1,1),(1,2),(2,2),(2,3)]]
        data=InteractionData(events,1)
        model=fit_ppr(data,0.85)
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'ppr.npz'
            write_model(path,model)
            loaded=load_model(data,path,'ppr',0.85)
            np.testing.assert_allclose(loaded.scores(1),model.scores(1))
            changed=InteractionData(events+[{'kind':'like','user_id':3,'post_id':1,'tick':0}],1)
            with self.assertRaises(ValueError): load_model(changed,path,'ppr',0.85)


if __name__=='__main__': unittest.main()
