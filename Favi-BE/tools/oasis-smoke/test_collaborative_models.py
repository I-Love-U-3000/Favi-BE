import unittest
import numpy as np
from collaborative_models import InteractionData, fit_ease, fit_ppr, normalized_adjacency, sample_negatives, propagate_embeddings, fit_lightgcn
from run_expanded_benchmark import validate_search
from pathlib import Path
import json
import tempfile
from benchmark_recommenders import evaluate
from run_expanded_benchmark import write_model
from collaborative_models import ScoreModel


def fixture():
    return [{"kind": "post", "post_id": i, "user_id": 20 + i, "tick": -1} for i in (1, 2, 3, 4)] + [
        {"kind": "like", "user_id": u, "post_id": i, "tick": 0} for u, i in ((0, 1), (0, 2), (1, 2), (1, 3))]


class CollaborativeTests(unittest.TestCase):
    def test_future_events_and_nonlike_signals_do_not_change_matrix(self):
        events = fixture()
        extra = [{"kind": "like", "user_id": 2, "post_id": 4, "tick": 2},
                 {"kind": "follow", "user_id": 0, "target_user_id": 1, "tick": -1}]
        a, b = InteractionData(events, 2), InteractionData(events + extra, 2)
        np.testing.assert_array_equal(a.x.toarray(), b.x.toarray())
        np.testing.assert_allclose(fit_ease(a, 10).scores(0), fit_ease(b, 10).scores(0))

    def test_ease_matches_closed_form_and_zero_diagonal(self):
        data = InteractionData(fixture(), 1)
        model = fit_ease(data, 2)
        x = data.x.toarray()
        precision = np.linalg.inv(x.T @ x + 2 * np.eye(x.shape[1]))
        expected = -precision / np.diag(precision)[None, :]
        np.fill_diagonal(expected, 0)
        np.testing.assert_allclose(model.weights, expected)
        np.testing.assert_allclose(model.scores(0), (x @ expected)[0])
        self.assertTrue(np.all(np.diag(model.weights) == 0))

    def test_ppr_matches_full_bipartite_networkx(self):
        import networkx as nx
        data = InteractionData(fixture(), 1)
        graph = nx.Graph()
        graph.add_nodes_from(("i", int(i)) for i in data.item_ids)
        for user in data.user_ids:
            graph.add_edges_from([(("u", int(user)), ("i", item)) for item in data.seen[int(user)]])
        model = fit_ppr(data, .85)
        for user in data.user_ids:
            rank = nx.pagerank(graph, alpha=.85, personalization={("u", int(user)): 1}, max_iter=10000, tol=1e-13)
            np.testing.assert_allclose(model.scores(int(user)), [rank["i", int(i)] for i in data.item_ids], atol=1e-11)

    def test_seen_self_filters_and_cold_ties(self):
        data = InteractionData(fixture(), 1)
        for model in (fit_ease(data, 10), fit_ppr(data, .85)):
            self.assertEqual(set(model.rank(0, 10)), {3, 4})
            self.assertEqual(model.rank(99, 2), [1, 2])
            self.assertNotIn(1, model.rank(21, 10))

    def test_duplicate_positive_rejected_and_negative_sampling_valid(self):
        with self.assertRaises(ValueError):
            InteractionData(fixture() + [fixture()[-1]], 1)
        data = InteractionData(fixture(), 1)
        rows = np.repeat(np.arange(2), 100)
        negatives = sample_negatives(data, rows, np.random.default_rng(42))
        self.assertFalse(np.asarray(data.x[rows, negatives]).any())

    def test_lightgcn_propagation_and_seed_reproducibility(self):
        import torch
        data = InteractionData(fixture(), 1)
        matrix = normalized_adjacency(data)
        np.testing.assert_allclose(matrix.toarray(), matrix.toarray().T)
        adjacency = torch.tensor(matrix.toarray(), dtype=torch.float32).to_sparse()
        embeddings = torch.arange(12, dtype=torch.float32).reshape(6, 2)
        dense = adjacency.to_dense()
        expected = (embeddings + dense @ embeddings + dense @ dense @ embeddings) / 3
        torch.testing.assert_close(propagate_embeddings(embeddings, adjacency, 2), expected)
        parameters = {"dimensions": 4, "layers": 1, "epochs": 2, "learning_rate": .01, "regularization": .0001}
        a, history = fit_lightgcn(data, parameters, 42)
        b, _ = fit_lightgcn(data, parameters, 42)
        np.testing.assert_array_equal(a.scores(0), b.scores(0))
        self.assertTrue(all(np.isfinite(row["loss"]) for row in history))

    def test_search_bounds(self):
        config = json.loads((Path(__file__).with_name("configs") / "recommenders/expanded.json").read_text())
        validate_search(config)
        with self.assertRaises(ValueError):
            validate_search({**config, "ppr_alpha": [1]})
        with self.assertRaises(ValueError):
            validate_search({**config, "lightgcn_max_epochs": 100000})

    def test_saved_matrix_weights_preserve_rank_and_mapping(self):
        data = InteractionData(fixture(), 1)
        model = fit_ease(data, 10)
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "model.npz"
            write_model(path, model)
            with np.load(path, allow_pickle=False) as state:
                np.testing.assert_array_equal(state["user_ids"], data.user_ids)
                np.testing.assert_array_equal(state["item_ids"], data.item_ids)
                loaded = ScoreModel(data, "ease", weights=state["weights"].copy())
            self.assertEqual(model.rank(0, 10), loaded.rank(0, 10))

    def test_fitted_model_cannot_cross_temporal_boundary(self):
        model = fit_ease(InteractionData(fixture(), 1), 10)
        with self.assertRaisesRegex(ValueError, "cutoff"):
            evaluate(fixture(), 2, 3, "ease", {"hours_per_tick": 1}, fitted_model=model)

    def test_lightgcn_checkpoint_can_stop_without_test_labels(self):
        data = InteractionData(fixture(), 1)
        visited = []
        def stop(epoch, model, record):
            visited.append(epoch)
            return False
        parameters = {"dimensions": 4, "layers": 1, "epochs": 20, "learning_rate": .01, "regularization": .0001}
        _, history = fit_lightgcn(data, parameters, 42, checkpoint=stop)
        self.assertEqual(visited, [1])
        self.assertEqual([r["epoch"] for r in history], [1])


if __name__ == "__main__":
    unittest.main()
