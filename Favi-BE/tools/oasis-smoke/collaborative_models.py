"""Likes-only EASE, exact bipartite PPR, and CPU LightGCN/BPR."""
from collections import Counter, defaultdict
import math
import numpy as np
from scipy import sparse, linalg


class InteractionData:
    def __init__(self, events, cutoff):
        self.cutoff = cutoff
        past = [e for e in events if e["tick"] < cutoff]
        self.items = {e["post_id"]: e for e in past if e["kind"] == "post"}
        self.item_ids = np.array(sorted(self.items), dtype=np.int64)
        self.item_index = {int(item): i for i, item in enumerate(self.item_ids)}
        pairs = [(e["user_id"], e["post_id"]) for e in past if e["kind"] == "like"]
        if len(set(pairs)) != len(pairs) or not pairs:
            raise ValueError("Require nonempty unique training positives")
        self.user_ids = np.array(sorted({u for u, _ in pairs}), dtype=np.int64)
        self.user_index = {int(user): i for i, user in enumerate(self.user_ids)}
        self.seen, self.likes = defaultdict(set), Counter()
        for user, item in pairs:
            if item not in self.items:
                raise ValueError("Training positive references an unavailable item")
            self.seen[user].add(item)
            self.likes[item] += 1
        self.rows = np.array([self.user_index[u] for u, _ in pairs], dtype=np.int64)
        self.cols = np.array([self.item_index[i] for _, i in pairs], dtype=np.int64)
        self.x = sparse.csr_matrix((np.ones(len(pairs)), (self.rows, self.cols)), shape=(len(self.user_ids), len(self.item_ids)))
        self.author_ids = np.array([self.items[int(i)]["user_id"] for i in self.item_ids], dtype=np.int64)


class ScoreModel:
    def __init__(self, data, method, *, weights=None, user_embeddings=None, item_embeddings=None, normalization=1.0):
        self.data, self.method = data, method
        self.cutoff, self.items, self.seen, self.likes = data.cutoff, data.items, data.seen, data.likes
        self.weights = weights
        self.user_embeddings, self.item_embeddings = user_embeddings, item_embeddings
        self.normalization = normalization

    def candidates(self, user):
        return {int(i) for i in self.data.item_ids if self.items[int(i)]["user_id"] != user and int(i) not in self.seen.get(user, ())}

    def scores(self, user):
        index = self.data.user_index.get(user)
        if index is None:
            return np.zeros(len(self.data.item_ids))
        if self.weights is not None:
            start, stop = self.data.x.indptr[index:index + 2]
            history = self.data.x.indices[start:stop]
            scale = len(history) if self.method == "ppr" else 1
            return self.weights[history].sum(axis=0) * self.normalization / scale
        return self.item_embeddings @ self.user_embeddings[index]

    def rank(self, user, k):
        scores = self.scores(user)
        if not np.isfinite(scores).all():
            raise ValueError("Nonfinite recommendation scores")
        allowed = self.data.author_ids != user
        for item in self.seen.get(user, ()):
            allowed[self.data.item_index[item]] = False
        indices = np.flatnonzero(allowed)
        order = np.lexsort((self.data.item_ids[indices], -scores[indices]))
        return [int(i) for i in self.data.item_ids[indices[order[:k]]]]

    def state_bytes(self):
        arrays = [a for a in (self.weights, self.user_embeddings, self.item_embeddings) if a is not None]
        return int(sum(a.nbytes for a in arrays) + self.data.x.data.nbytes + self.data.x.indices.nbytes + self.data.x.indptr.nbytes)


def fit_ease(data, regularization):
    if not math.isfinite(regularization) or regularization <= 0:
        raise ValueError("EASE regularization must be positive")
    gram = (data.x.T @ data.x).toarray()
    gram.flat[::len(gram) + 1] += regularization
    precision = linalg.solve(gram, np.eye(len(gram)), assume_a="pos")
    weights = -precision / np.diag(precision)[None, :]
    np.fill_diagonal(weights, 0)
    return ScoreModel(data, "ease", weights=weights)


def fit_ppr(data, alpha):
    if not 0 < alpha < 1:
        raise ValueError("PPR alpha must be between0 and1")
    user_degree = np.asarray(data.x.sum(axis=1)).ravel()
    item_degree = np.asarray(data.x.sum(axis=0)).ravel()
    ui = sparse.diags(1 / user_degree) @ data.x
    iu = sparse.diags(np.divide(1, item_degree, out=np.zeros_like(item_degree), where=item_degree > 0)) @ data.x.T
    # Schur complement of full user/item personalized PageRank:
    # item scores = a(1-a)*user_seed*inv(I-a²*P_IU*P_UI).
    transition = (iu @ ui).toarray()
    kernel = linalg.solve(np.eye(len(item_degree)) - alpha ** 2 * transition, np.eye(len(item_degree)))
    return ScoreModel(data, "ppr", weights=kernel, normalization=alpha * (1 - alpha))


def normalized_adjacency(data):
    nu, ni = data.x.shape
    adjacency = sparse.bmat([[None, data.x], [data.x.T, None]], format="coo")
    degree = np.asarray(adjacency.sum(axis=1)).ravel()
    inv = np.divide(1, np.sqrt(degree), out=np.zeros_like(degree), where=degree > 0)
    return (sparse.diags(inv) @ adjacency @ sparse.diags(inv)).tocoo()


def sample_negatives(data, rows, rng):
    if np.any(np.diff(data.x.indptr)[np.unique(rows)] == data.x.shape[1]):
        raise ValueError("No unobserved item available for negative sampling")
    negatives = rng.integers(0, data.x.shape[1], size=len(rows), dtype=np.int64)
    while True:
        invalid = np.asarray(data.x[rows, negatives]).ravel() != 0
        if not invalid.any():
            return negatives
        negatives[invalid] = rng.integers(0, data.x.shape[1], size=int(invalid.sum()))


def propagate_embeddings(embedding, adjacency, layers):
    import torch
    current, accumulated = embedding, embedding
    for _ in range(layers):
        current = torch.sparse.mm(adjacency, current)
        accumulated = accumulated + current
    return accumulated / (layers + 1)


def fit_lightgcn(data, parameters, seed, *, checkpoint=None):
    import torch
    from torch import nn
    torch.set_num_threads(4)
    torch.use_deterministic_algorithms(True)
    torch.manual_seed(seed)
    rng = np.random.default_rng(seed)
    dim, layers, epochs = parameters["dimensions"], parameters["layers"], parameters["epochs"]
    if dim < 1 or layers < 0 or epochs < 1 or parameters["learning_rate"] <= 0 or parameters["regularization"] < 0:
        raise ValueError("Invalid LightGCN parameters")
    nu, ni = data.x.shape
    matrix = normalized_adjacency(data)
    adjacency = torch.sparse_coo_tensor(torch.tensor(np.vstack((matrix.row, matrix.col)), dtype=torch.long),
        torch.tensor(matrix.data, dtype=torch.float32), (nu + ni, nu + ni), check_invariants=True).coalesce()
    embedding = nn.Embedding(nu + ni, dim)
    nn.init.normal_(embedding.weight, std=.1)
    optimizer = torch.optim.Adam(embedding.parameters(), lr=parameters["learning_rate"])
    rows = torch.tensor(data.rows, dtype=torch.long)
    positives = torch.tensor(data.cols + nu, dtype=torch.long)
    history = []

    def propagate():
        return propagate_embeddings(embedding.weight, adjacency, layers)

    def snapshot():
        with torch.no_grad():
            vectors = propagate().numpy().copy()
        return ScoreModel(data, "lightgcn", user_embeddings=vectors[:nu], item_embeddings=vectors[nu:])

    for epoch in range(1, epochs + 1):
        negatives = torch.tensor(sample_negatives(data, data.rows, rng) + nu, dtype=torch.long)
        optimizer.zero_grad(set_to_none=True)
        vectors = propagate()
        users, positive, negative = vectors[rows], vectors[positives], vectors[negatives]
        loss = torch.nn.functional.softplus((users * (negative - positive)).sum(dim=1)).mean()
        penalty = (embedding(rows).square().sum() + embedding(positives).square().sum() + embedding(negatives).square().sum()) / (2 * len(rows))
        loss = loss + parameters["regularization"] * penalty
        if not torch.isfinite(loss):
            raise ValueError("Nonfinite LightGCN training loss")
        loss.backward()
        optimizer.step()
        if epoch == 1 or epoch % parameters.get("check_every", 10) == 0 or epoch == epochs:
            record = {"epoch": epoch, "loss": float(loss.detach())}
            history.append(record)
            if checkpoint is not None and checkpoint(epoch, snapshot(), record) is False:
                break
    return snapshot(), history
