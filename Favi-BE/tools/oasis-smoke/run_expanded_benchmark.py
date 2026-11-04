"""Validation-selected collaborative baselines on an immutable temporal dataset."""
import os
for variable in ("OPENBLAS_NUM_THREADS", "MKL_NUM_THREADS", "OMP_NUM_THREADS"):
    os.environ[variable] = "4"

import argparse
import hashlib
import itertools
import json
from pathlib import Path
import statistics
import time
import importlib.metadata
import numpy as np
from threadpoolctl import threadpool_limits, threadpool_info

from benchmark_recommenders import evaluate, validate_protocol
from collaborative_models import InteractionData, ScoreModel, fit_ease, fit_ppr, fit_lightgcn
from check_dataset import verify
from dataset_policy import fingerprint
from source_provenance import verify_dependency_lock


def save(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def validate_search(config):
    expected = {"schema_version", "ease_regularization", "ppr_alpha", "lightgcn_layers", "lightgcn_learning_rates", "lightgcn_dimensions", "lightgcn_regularization", "lightgcn_max_epochs", "lightgcn_check_every", "lightgcn_patience", "training_seed", "test_training_seeds"}
    if set(config) != expected or type(config["schema_version"]) is not int or config["schema_version"] != 1:
        raise ValueError("Unsupported search configuration")
    for name, low, high in (("lightgcn_dimensions", 4, 256), ("lightgcn_max_epochs", 1, 1000), ("lightgcn_check_every", 1, 50), ("lightgcn_patience", 1, 20), ("training_seed", 0, 2147483647)):
        if type(config[name]) is not int or not low <= config[name] <= high:
            raise ValueError("Invalid " + name)
    import math
    for name, low, high in (("ease_regularization", 0, 100000), ("ppr_alpha", 0, 1), ("lightgcn_learning_rates", 0, 1)):
        values = config[name]
        if not isinstance(values, list) or not 1 <= len(values) <= 8 or len(set(values)) != len(values) or any(type(v) not in (float, int) or not math.isfinite(v) or not low < v < high for v in values):
            raise ValueError("Invalid search values: " + name)
    layers = config["lightgcn_layers"]
    seeds = config["test_training_seeds"]
    if not isinstance(layers, list) or not layers or any(type(v) is not int or not 1 <= v <= 4 for v in layers) or len(set(layers)) != len(layers):
        raise ValueError("Invalid layers")
    if len(layers) * len(config["lightgcn_learning_rates"]) > 12:
        raise ValueError("Too many LightGCN configurations")
    if not isinstance(seeds, list) or not 1 <= len(seeds) <= 5 or len(set(seeds)) != len(seeds) or config["training_seed"] not in seeds or any(type(s) is not int or not 0 <= s <= 2147483647 for s in seeds):
        raise ValueError("Invalid training seeds")
    reg = config["lightgcn_regularization"]
    if type(reg) not in (float, int) or not math.isfinite(reg) or reg < 0:
        raise ValueError("Invalid regularization")


def quality_key(result):
    return result["all"]["ndcg"], result["all"]["recall"]


def write_model(path, model):
    values = {"user_ids": model.data.user_ids, "item_ids": model.data.item_ids}
    if model.weights is not None:
        values["weights"] = model.weights
    else:
        values.update(user_embeddings=model.user_embeddings, item_embeddings=model.item_embeddings)
    np.savez_compressed(path, **values)


def run(dataset, output, protocol_path, search_path, formula_run=None, dataset_kind="oasis"):
    dataset, output = dataset.resolve(), output.resolve()
    if output.exists() or output.is_relative_to(dataset):
        raise ValueError("Use a fresh output outside the dataset")
    if dataset_kind == "oasis":
        integrity = verify(dataset)
    elif dataset_kind == "movielens100k":
        from movielens_dataset import verify as verify_movielens
        if formula_run is not None:
            raise ValueError("MovieLens cannot attach social Formula references")
        integrity = verify_movielens(dataset)
    else:
        raise ValueError("Unsupported dataset kind")
    author_metrics = dataset_kind == "oasis"
    locked = verify_dependency_lock(Path(__file__).with_name("requirements-oasis.lock.txt"))
    protocol = json.loads(protocol_path.read_text(encoding="utf-8-sig"))
    search = json.loads(search_path.read_text(encoding="utf-8-sig"))
    validate_search(search)
    ticks = json.loads((dataset / "config.resolved.json").read_text())["ticks"]
    first, second = validate_protocol(protocol, ticks)
    events = [json.loads(line) for line in (dataset / "events.jsonl").read_text().splitlines()]
    references = []
    if formula_run is not None:
        manifest = json.loads((formula_run / "manifest.json").read_text())
        for name, expected in manifest.items():
            path = (formula_run / name).resolve()
            if not path.is_relative_to(formula_run.resolve()) or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
                raise ValueError("Invalid historical formula artifact")
        old = json.loads((formula_run / "report.json").read_text())
        if old["events_sha256"] != integrity["events_sha256"] or old["protocol"] != protocol:
            raise ValueError("Formula reference has a different dataset/protocol")
        references = [r for r in old["results"] if r["phase"] == "test" and r["method"] in {"favi_formula", "favi_formula_v2"}]
    output.mkdir(parents=True, exist_ok=False)
    save(output / "protocol.json", protocol)
    save(output / "search.json", search)
    tuning_events = [e for e in events if e["tick"] < second]
    prepared = time.perf_counter()
    train = InteractionData(tuning_events, first)
    preparation = {"validation_prefix_seconds": time.perf_counter() - prepared}
    if len(train.items) > 5000:
        raise ValueError("Dense EASE/exact PPR limited to5000items")
    selections, trials, validation_models = {}, [], {}
    expected_population = None

    def evaluate_model(model, start, end, method, measure=False):
        nonlocal expected_population
        result, rows = evaluate(events if measure else tuning_events, start, end, method, protocol, measure=measure, fitted_model=model, author_metrics=author_metrics)
        result["fit_peak_python_bytes"] = None
        result["model_state_bytes"] = model.state_bytes()
        return result, rows

    for method, values, fit in (("ease", search["ease_regularization"], fit_ease), ("ppr", search["ppr_alpha"], fit_ppr)):
        best = None
        for index, value in enumerate(values):
            begun = time.perf_counter()
            model = fit(train, value)
            fit_seconds = time.perf_counter() - begun
            result, _ = evaluate_model(model, first, second, method)
            if expected_population is not None and result["evaluation_population_sha256"] != expected_population:
                raise ValueError("Different validation population")
            expected_population = result["evaluation_population_sha256"]
            trial = {"method": method, "trial": index, "parameter": value, "fit_seconds": fit_seconds, "validation": result}
            trials.append(trial)
            save(output / "tuning.json", trials)
            print(json.dumps({"method": method, "trial": index, "value": value, "validation": result["all"]}), flush=True)
            if best is None or quality_key(result) > quality_key(best["validation"]):
                best = trial
                validation_models[method] = model
        selections[method] = best

    for index, (layers, rate) in enumerate(itertools.product(search["lightgcn_layers"], search["lightgcn_learning_rates"])):
        parameters = {"dimensions": search["lightgcn_dimensions"], "layers": layers, "learning_rate": rate,
                      "regularization": search["lightgcn_regularization"], "epochs": search["lightgcn_max_epochs"], "check_every": search["lightgcn_check_every"]}
        best, stale, history = None, 0, []
        begun = time.perf_counter()

        def checkpoint(epoch, model, record):
            nonlocal best, stale
            result, _ = evaluate_model(model, first, second, "lightgcn")
            if result["evaluation_population_sha256"] != expected_population:
                raise ValueError("Different LightGCN validation population")
            history.append({**record, "validation": result["all"]})
            if best is None or quality_key(result) > quality_key(best["validation"]):
                best = {"method": "lightgcn", "trial": index, "parameters": {**parameters, "epochs": epoch}, "validation": result}
                stale = 0
                write_model(output / f"lightgcn-trial-{index}-best.npz", model)
            else:
                stale += 1
            save(output / f"lightgcn-trial-{index}-history.json", history)
            print(json.dumps({"method": "lightgcn", "trial": index, "epoch": epoch, "loss": record["loss"], "validation_ndcg": result["all"]["ndcg"], "stale_checks": stale}), flush=True)
            return stale < search["lightgcn_patience"]

        fit_lightgcn(train, parameters, search["training_seed"], checkpoint=checkpoint)
        best["search_seconds_including_validation"] = time.perf_counter() - begun
        trials.append(best)
        save(output / "tuning.json", trials)
        if "lightgcn" not in selections or quality_key(best["validation"]) > quality_key(selections["lightgcn"]["validation"]):
            selections["lightgcn"] = best
    save(output / "selection.json", {"selections": selections, "tuning_events_sha256": fingerprint(tuning_events), "test_not_used_for_selection": True})
    selection_hash = hashlib.sha256((output / "selection.json").read_bytes()).hexdigest()

    results = []
    # Re-evaluate validation selected models with inference timing (no retuning).
    selected_gcn = selections["lightgcn"]
    with np.load(output / f"lightgcn-trial-{selected_gcn['trial']}-best.npz", allow_pickle=False) as stored:
        if not np.array_equal(stored["user_ids"], train.user_ids) or not np.array_equal(stored["item_ids"], train.item_ids):
            raise ValueError("Checkpoint source mappings changed")
        validation_models["lightgcn"] = ScoreModel(train, "lightgcn", user_embeddings=stored["user_embeddings"].copy(), item_embeddings=stored["item_embeddings"].copy())
    for method, model in validation_models.items():
        result, rows = evaluate_model(model, first, second, method, True)
        result.update(phase="validation")
        results.append(result)
        (output / f"validation-{method}-users.jsonl").write_text("".join(json.dumps(row) + "\n" for row in rows))

    prepared = time.perf_counter()
    test_data = InteractionData(events, second)
    if len(test_data.items) > 5000:
        raise ValueError("Dense EASE/exact PPR limited to5000items at test refit too")
    preparation["test_prefix_seconds"] = time.perf_counter() - prepared
    pop, pop_rows = evaluate(events, second, ticks, "popularity", protocol, author_metrics=author_metrics)
    pop.update(phase="test", track="likes-only")
    results.append(pop)
    population = pop["evaluation_population_sha256"]
    (output / "test-popularity-users.jsonl").write_text("".join(json.dumps(row) + "\n" for row in pop_rows))

    def finish_test(model, label, seconds, seed=None):
        result, rows = evaluate_model(model, second, ticks, model.method, True)
        if result["evaluation_population_sha256"] != population:
            raise ValueError("Different test targets/candidates")
        result.update(phase="test", label=label, track="likes-only", fit_seconds=seconds, seed=seed)
        results.append(result)
        write_model(output / (label + ".npz"), model)
        (output / ("test-" + label + "-users.jsonl")).write_text("".join(json.dumps(row) + "\n" for row in rows))
        print(json.dumps({"test": label, "metrics": result["all"]}), flush=True)

    for method, fit in (("ease", fit_ease), ("ppr", fit_ppr)):
        begun = time.perf_counter()
        model = fit(test_data, selections[method]["parameter"])
        seconds = time.perf_counter() - begun
        finish_test(model, method, seconds)
    for seed in search["test_training_seeds"]:
        begun = time.perf_counter()
        model, history = fit_lightgcn(test_data, selected_gcn["parameters"], seed)
        seconds = time.perf_counter() - begun
        save(output / f"lightgcn-test-{seed}-history.json", history)
        finish_test(model, "lightgcn-" + str(seed), seconds, seed)
    for reference in references:
        if reference["evaluation_population_sha256"] != population:
            raise ValueError("Historical formula used different candidates/targets")
    if hashlib.sha256((output / "selection.json").read_bytes()).hexdigest() != selection_hash:
        raise ValueError("Selection changed after test evaluation")
    gcn_results = [r for r in results if r["phase"] == "test" and r["method"] == "lightgcn"]
    summary = {metric: {"mean": statistics.mean(r["all"][metric] for r in gcn_results), "std": statistics.stdev(r["all"][metric] for r in gcn_results) if len(gcn_results) > 1 else 0}
               for metric in (("ndcg", "recall", "catalog_coverage", "author_diversity") if author_metrics else ("ndcg", "recall", "catalog_coverage"))}
    report = {"schema_version": 1, "dataset_kind": dataset_kind, "source_verification": integrity, "events_sha256": integrity["events_sha256"], "protocol": protocol, "search": search,
              "selections": selections, "selection_sha256": selection_hash, "results": results, "lightgcn_seed_summary": summary,
              "formula_references": references, "shared_preparation": preparation, "locked_dependencies": locked,
              "threadpools": threadpool_info(), "limitations": ["Single synthetic dataset; training seeds are not independent social datasets", "Likes-only algorithm track, formula references use extra signals and historical timings", "Full-batch LightGCN and bounded search, not a reproduction of paper benchmark scores", "Model state bytes are not process or training peak RAM", "Per-query latency computes scores and ranking; no cached all-user scores"]}
    save(output / "report.json", report)
    if not author_metrics:
        report["limitations"] = ["Independent explicit movie ratings converted to implicit positives >=4, not Favi social behavior", "Fixed full catalog; timestamp-block chronological split over raw rating ordinals", "No known movie author identity: author diversity/concentration are N/A", "One external dataset and three optimizer seeds, not independent data replicates", *report["limitations"][2:]]
        save(output / "report.json", report)
    for name in ("run_expanded_benchmark.py", "collaborative_models.py", "benchmark_recommenders.py", "check_dataset.py", "dataset_policy.py", "source_provenance.py", "requirements-oasis.lock.txt"):
        (output / name).write_bytes(Path(__file__).with_name(name).read_bytes())
    if not author_metrics:
        (output / "movielens_dataset.py").write_bytes(Path(__file__).with_name("movielens_dataset.py").read_bytes())
    lines = ["# Expanded collaborative benchmark", "", "| Track | Method | Recall@K | nDCG@K | Coverage | Authors/item | Rank p95 ms |", "|---|---|---:|---:|---:|---:|---:|"]
    for result in [r for r in results if r["phase"] == "test"] + references:
        m = result["all"]
        track = "extra-signals reference" if result in references else "likes-only"
        diversity = f"{m['author_diversity']:.4f}" if m['author_diversity'] is not None else "N/A"
        lines.append(f"| {track} | {result.get('label', result['method'])} | {m['recall']:.6f} | {m['ndcg']:.6f} | {m['catalog_coverage']:.4f} | {diversity} | {result['rank_latency_ms']['p95']:.4f} |")
    lines += ["", "## LightGCN training-seed mean/std", "", "```json", json.dumps(summary, indent=2), "```", "", *["- " + note for note in report["limitations"]]]
    (output / "review.md").write_text("\n".join(lines) + "\n")
    save(output / "manifest.json", {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in output.iterdir()})


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--protocol", type=Path, default=Path(__file__).with_name("configs") / "recommenders" / "popularity.json")
    parser.add_argument("--search-config", type=Path, default=Path(__file__).with_name("configs") / "recommenders" / "expanded.json")
    parser.add_argument("--formula-run", type=Path)
    parser.add_argument("--dataset-kind", choices=("oasis", "movielens100k"), default="oasis")
    args = parser.parse_args()
    with threadpool_limits(limits=4):
        run(args.dataset, args.output, args.protocol, args.search_config, args.formula_run, args.dataset_kind)
