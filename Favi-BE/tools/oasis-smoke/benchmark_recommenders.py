"""Temporal frozen-window evaluation of Popularity and the existing warm-feed score."""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
import math
from pathlib import Path
import platform
import statistics
import sys
import time
import tracemalloc

from check_dataset import verify
from dataset_policy import fingerprint


def validate_protocol(config, ticks):
    if set(config) != {"schema_version", "k", "train_fraction", "validation_fraction", "hours_per_tick", "timing_repeats"} or type(config["schema_version"]) is not int or config["schema_version"] != 1:
        raise ValueError("Unknown benchmark configuration/version")
    for key, low, high in (("k", 1, 100), ("timing_repeats", 1, 20)):
        if type(config[key]) is not int or not low <= config[key] <= high:
            raise ValueError(f"Invalid {key}")
    for key in ("train_fraction", "validation_fraction", "hours_per_tick"):
        if type(config[key]) not in (int, float) or not math.isfinite(config[key]):
            raise ValueError(f"Invalid {key}")
    train, validation = config["train_fraction"], config["validation_fraction"]
    if not 0 < train < 1 or not 0 < validation < 1 or train + validation >= 1 or not 0 <= config["hours_per_tick"] <= 24:
        raise ValueError("Invalid split fractions/time scale")
    first, second = math.floor(ticks * train), math.floor(ticks * (train + validation))
    if not 0 < first < second < ticks:
        raise ValueError("Every temporal partition must contain at least one tick")
    return first, second


class Ranker:
    def __init__(self, events, cutoff, method, hours_per_tick=1.0, parameters=None):
        if method not in {"popularity", "favi_formula", "favi_formula_v2"}:
            raise ValueError("Unknown ranker")
        self.parameters = parameters
        if method == "favi_formula_v2":
            if not isinstance(parameters, dict) or set(parameters) != {"social_weight", "decay", "author_cap"}:
                raise ValueError("V2 requires explicit selected parameters")
            if any(type(parameters[key]) not in (int, float) or not math.isfinite(parameters[key]) or parameters[key] < 0 for key in ("social_weight", "decay")) or parameters["author_cap"] not in (None, 2):
                raise ValueError("Invalid V2 parameters")
        self.method, self.hours_per_tick, self.cutoff = method, hours_per_tick, cutoff
        past = [e for e in events if e["tick"] < cutoff]
        self.items = {e["post_id"]: e for e in past if e["kind"] == "post"}
        self.likes, self.comments = Counter(), Counter()
        self.seen, self.follows = defaultdict(set), defaultdict(set)
        self.author_interactions = Counter()
        for event in past:
            user = event["user_id"]
            if event["kind"] == "like":
                item = event["post_id"]
                self.likes[item] += 1
                self.seen[user].add(item)
                if method != "popularity" and (cutoff - event["tick"]) * hours_per_tick <= 30 * 24:
                    self.author_interactions[user, self.items[item]["user_id"]] += 1
            elif method != "popularity" and event["kind"] == "comment":
                self.comments[event["post_id"]] += 1
            elif method != "popularity" and event["kind"] == "follow":
                self.follows[user].add(event["target_user_id"])
        self.order = sorted(self.items, key=lambda item: (-self.likes[item], item))

    def candidates(self, user):
        return {i for i, p in self.items.items() if p["user_id"] != user and i not in self.seen.get(user, ())}

    def rank(self, user, k):
        seen = self.seen.get(user, ())
        eligible = [i for i in self.order if self.items[i]["user_id"] != user and i not in seen]
        if self.method == "popularity":
            return eligible[:k]
        scores = {}
        for item in eligible:
            post = self.items[item]
            author = post["user_id"]
            affinity = 1.0
            if author in self.follows.get(user, ()):
                affinity += 1.5
                if user in self.follows.get(author, ()):
                    affinity += .5
            affinity += min(2, .4 * self.author_interactions[user, author])
            quality = math.log10(1 + 1.1 * self.likes[item] + 2 * self.comments[item])
            # Adapter imports no PostTags. Latent simulator topic is not an observed tag.
            decay_rate = self.parameters["decay"] if self.method == "favi_formula_v2" else .05
            social_weight = self.parameters["social_weight"] if self.method == "favi_formula_v2" else 2.5
            decay = math.exp(-decay_rate * max(0, (self.cutoff - post["tick"]) * self.hours_per_tick))
            scores[item] = (social_weight * affinity + 1.5 * quality) * decay
        ordered = sorted(eligible, key=lambda item: (-scores[item], item))
        if self.method == "favi_formula_v2":
            cap = self.parameters["author_cap"]
            if cap is None:
                return ordered[:k]
            counts, selected = Counter(), []
            for item in ordered:
                author = self.items[item]["user_id"]
                if counts[author] < cap:
                    selected.append(item)
                    counts[author] += 1
                if len(selected) == k:
                    break
            return selected
        authors = Counter()
        adjusted = {}
        for item in ordered:
            author = self.items[item]["user_id"]
            count = authors[author]
            adjusted[item] = scores[item] * (1 if count == 0 else .65 if count == 1 else .4)
            authors[author] += 1
        return sorted(ordered, key=lambda item: (-adjusted[item], item))[:k]


def metrics(recommendations, relevant, k):
    if not relevant or len(set(recommendations)) != len(recommendations) or len(recommendations) > k:
        raise ValueError("Invalid metric inputs")
    hits = sum(item in relevant for item in recommendations)
    dcg = sum(1 / math.log2(rank + 2) for rank, item in enumerate(recommendations) if item in relevant)
    ideal = sum(1 / math.log2(rank + 2) for rank in range(min(k, len(relevant))))
    return {"recall": hits / len(relevant), "ndcg": dcg / ideal, "precision": hits / k, "hit_rate": float(hits > 0)}


def summarize(rows, catalog_size):
    if not rows:
        return {"users": 0, "recall": None, "ndcg": None, "precision": None, "hit_rate": None, "catalog_coverage": 0}
    return {"users": len(rows), **{m: statistics.mean(r[m] for r in rows) if rows[0][m] is not None else None for m in ("recall", "ndcg", "precision", "hit_rate", "author_diversity", "max_author_share")},
            "catalog_coverage": len({i for r in rows for i in r["recommended"]}) / catalog_size}


def percentile(values, quantile):
    ordered = sorted(values)
    return ordered[max(0, math.ceil(len(ordered) * quantile) - 1)]


def evaluate(events, start, end, method, config, parameters=None, measure=True, fitted_model=None, author_metrics=True):
    tracemalloc.start()
    started = time.perf_counter_ns()
    try:
        model = fitted_model if fitted_model is not None else Ranker(events, start, method, config["hours_per_tick"], parameters)
        if model.cutoff != start:
            raise ValueError("Fitted model cutoff differs from evaluation cutoff")
        fit_ms = (time.perf_counter_ns() - started) / 1e6
        _, fit_peak = tracemalloc.get_traced_memory()
    finally:
        tracemalloc.stop()
    if fitted_model is not None:
        fit_ms, fit_peak = None, None  # External training is timed by its caller.
    truth = defaultdict(set)
    for event in events:
        if event["kind"] == "like" and start <= event["tick"] < end:
            truth[event["user_id"]].add(event["post_id"])
    rows, excluded, future_positives = [], 0, 0
    for user, raw in sorted(truth.items()):
        candidates = model.candidates(user)
        relevant = raw & candidates
        future_positives += sum(i not in model.items for i in raw)
        if not relevant:
            excluded += 1
            continue
        recommendations = model.rank(user, config["k"])
        if not set(recommendations).issubset(candidates):
            raise ValueError("Ranker returned an unavailable/seen/self item")
        authors = Counter(model.items[item]["user_id"] for item in recommendations)
        rows.append({"user_id": user, "cohort": "warm" if model.seen.get(user) else "cold",
                     "candidate_count": len(candidates), "candidate_sha256": fingerprint(sorted(candidates)), "relevant": sorted(relevant), "recommended": recommendations,
                     "random_expected_recall": min(config["k"], len(candidates)) / len(candidates),
                     "author_diversity": (len(authors) / len(recommendations) if recommendations else 0) if author_metrics else None,
                     "max_author_share": (max(authors.values(), default=0) / len(recommendations) if recommendations else 0) if author_metrics else None,
                     **metrics(recommendations, relevant, config["k"])})
    if not rows:
        raise ValueError("No evaluable users; choose a larger dataset or different protocol")
    # Warm each user once above; time ranking only, without auditing, JSON, I/O or tracemalloc.
    latencies = []
    for _ in range(config["timing_repeats"] if measure else 0):
        for row in rows:
            begun = time.perf_counter_ns()
            model.rank(row["user_id"], config["k"])
            latencies.append((time.perf_counter_ns() - begun) / 1e6)
    result = {"method": method, "fit_before_tick": start, "label_window": [start, end],
              "catalog_items": len(model.items), "fit_like_edges": sum(model.likes.values()),
              "label_users": len(truth), "excluded_users_no_eligible_target": excluded,
              "future_item_positive_edges_excluded": future_positives,
              "all": summarize(rows, len(model.items)),
              "warm": summarize([r for r in rows if r["cohort"] == "warm"], len(model.items)),
              "cold": summarize([r for r in rows if r["cohort"] == "cold"], len(model.items)),
              "uniform_random_expected_recall": statistics.mean(r["random_expected_recall"] for r in rows),
              "fit_ms_with_tracemalloc": fit_ms, "fit_peak_python_bytes": fit_peak,
              "rank_latency_ms": {"p50": statistics.median(latencies) if latencies else None, "p95": percentile(latencies, .95) if latencies else None, "queries": len(latencies)},
              "evaluation_population_sha256": fingerprint([{k: r[k] for k in ("user_id", "candidate_sha256", "relevant")} for r in rows])}
    return result, rows


def run(dataset, output, protocol, methods=("popularity", "favi_formula")):
    if not methods or len(set(methods)) != len(methods) or any(m not in {"popularity", "favi_formula"} for m in methods):
        raise ValueError("Choose unique supported methods")
    dataset, output = dataset.resolve(), output.resolve()
    if output.is_relative_to(dataset):
        raise ValueError("Output must stay outside the immutable dataset")
    if output.exists():
        raise ValueError("Refuse to overwrite an existing benchmark")
    integrity = verify(dataset)
    data_config = json.loads((dataset / "config.resolved.json").read_text(encoding="utf-8"))
    first, second = validate_protocol(protocol, data_config["ticks"])
    events = [json.loads(line) for line in (dataset / "events.jsonl").read_text(encoding="utf-8").splitlines()]
    output.mkdir(parents=True, exist_ok=False)
    results = []
    for phase, start, end in (("validation", first, second), ("test", second, data_config["ticks"])):
        population_hashes = set()
        for method in methods:
            result, rows = evaluate(events, start, end, method, protocol)
            result["phase"] = phase
            results.append(result)
            population_hashes.add(result["evaluation_population_sha256"])
            (output / f"{phase}-{method}-users.jsonl").write_text("".join(json.dumps(r) + "\n" for r in rows), encoding="utf-8")
        if len(population_hashes) != 1:
            raise ValueError("Methods were evaluated on different targets/candidates")
    source = Path(__file__).resolve().parents[2] / "Favi-BE.API/Application/ContentDiscovery/ContentDiscoveryQueryReaderAdapter.cs"
    report = {"schema_version": 1, "protocol": protocol, "methods": list(methods), "dataset": str(dataset), "dataset_events_sha256": integrity["events_sha256"],
              "formula_source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
              "python": sys.version, "platform": platform.platform(), "results": results,
              "limitations": ["Synthetic observed-positive recovery, not human relevance or causal online uplift", "Frozen-window full-catalog ranking; not production candidate retrieval/privacy/DB/API latency", "Formula offline port, stable source-ID ties, tags=0 as imported; not a production replay", "Cold cohort formula is counterfactual: production routes these users to cold-start ranking", "Tick-to-hour scale is an explicit experiment assumption", "Fit allocation is tracemalloc Python peak, not process RSS or loaded dataset memory; ranking latency excludes I/O", "Single dataset seed; no statistical superiority claim"]}
    (output / "report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    (output / "protocol.json").write_text(json.dumps(protocol, indent=2), encoding="utf-8")
    for name in ("benchmark_recommenders.py", "check_dataset.py", "dataset_policy.py"):
        (output / name).write_bytes(Path(__file__).with_name(name).read_bytes())
    lines = ["# Offline benchmark: " + ", ".join(methods), "", f"Dataset events SHA256: {integrity['events_sha256']}", "", "| Phase | Method | Users | Recall@K | nDCG@K | Coverage | Rank p95 ms |", "|---|---|---:|---:|---:|---:|---:|"]
    for r in results:
        m = r["all"]
        lines.append(f"| {r['phase']} | {r['method']} | {m['users']} | {m['recall']:.6f} | {m['ndcg']:.6f} | {m['catalog_coverage']:.4f} | {r['rank_latency_ms']['p95']:.4f} |")
    lines += ["", "## Warm cohort (intended users of the existing formula)", "", "| Phase | Method | Users | Recall@K | nDCG@K |", "|---|---|---:|---:|---:|"]
    for r in results:
        m = r["warm"]
        if m["users"]:
            lines.append(f"| {r['phase']} | {r['method']} | {m['users']} | {m['recall']:.6f} | {m['ndcg']:.6f} |")
    lines += ["", "K=" + str(protocol["k"]), "", *["- " + note for note in report["limitations"]]]
    (output / "review.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    manifest = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in output.iterdir()}
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print("\n".join(lines[:len(results) + 6]))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--config", type=Path, default=Path(__file__).with_name("configs") / "recommenders" / "popularity.json")
    parser.add_argument("--methods", nargs="+", choices=("popularity", "favi_formula"), default=["popularity", "favi_formula"])
    args = parser.parse_args()
    run(args.dataset, args.output, json.loads(args.config.read_text(encoding="utf-8-sig")), args.methods)
