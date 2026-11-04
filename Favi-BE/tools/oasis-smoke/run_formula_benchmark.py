"""Tune Formula V2 on validation only, then evaluate frozen baselines and V2."""
import argparse
import hashlib
import itertools
import json
from pathlib import Path
import platform
import random
import statistics
import sys

from benchmark_recommenders import evaluate, validate_protocol
from check_dataset import verify
from dataset_policy import fingerprint


def save(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def grid():
    return [{"social_weight": w, "decay": decay, "author_cap": cap}
            for w, decay, cap in itertools.product((.5, 1.5, 2.5), (0, .005), (None, 2))]


def select_trial(trials):
    # Stable first-trial tie break, independent of test results.
    return max(trials, key=lambda t: (t["validation"]["all"]["ndcg"], t["validation"]["all"]["recall"], -t["trial"]))


def paired_interval(left, right, metric, repetitions=2000, seed=20261103):
    if [r["user_id"] for r in left] != [r["user_id"] for r in right]:
        raise ValueError("Paired comparison requires identical users")
    differences = [b[metric] - a[metric] for a, b in zip(left, right)]
    rng = random.Random(seed)
    samples = sorted(statistics.mean(rng.choices(differences, k=len(differences))) for _ in range(repetitions))
    return {"mean_delta": statistics.mean(differences), "user_bootstrap_95_percentile_interval": [samples[int(.025 * repetitions)], samples[int(.975 * repetitions) - 1]],
            "repetitions": repetitions, "seed": seed, "scope": "Users within this single synthetic dataset, not uncertainty across dataset seeds"}


def run(dataset, output, config_path):
    dataset, output = dataset.resolve(), output.resolve()
    if output.is_relative_to(dataset):
        raise ValueError("Output must stay outside the immutable dataset")
    if output.exists():
        raise ValueError("Refuse to overwrite a benchmark")
    integrity = verify(dataset)
    protocol = json.loads(config_path.read_text(encoding="utf-8-sig"))
    data_config = json.loads((dataset / "config.resolved.json").read_text(encoding="utf-8"))
    first, second = validate_protocol(protocol, data_config["ticks"])
    events = [json.loads(line) for line in (dataset / "events.jsonl").read_text(encoding="utf-8").splitlines()]
    tuning_events = [event for event in events if event["tick"] < second]
    output.mkdir(parents=True, exist_ok=False)
    save(output / "protocol.json", {**protocol, "selection_metric": "validation macro nDCG@K", "grid": grid(),
                                    "tuning_max_tick_exclusive": second, "tuning_events_sha256": fingerprint(tuning_events)})
    trials, population_hash = [], None
    for index, parameters in enumerate(grid()):
        result, _ = evaluate(tuning_events, first, second, "favi_formula_v2", protocol, parameters, measure=False)
        if population_hash is not None and result["evaluation_population_sha256"] != population_hash:
            raise ValueError("Trial population changed")
        population_hash = result["evaluation_population_sha256"]
        trials.append({"trial": index, "parameters": parameters, "validation": result})
        save(output / "tuning.json", trials)
        print(json.dumps({"trial": index + 1, "total": len(grid()), "parameters": parameters,
                          "validation_ndcg": result["all"]["ndcg"], "validation_recall": result["all"]["recall"]}), flush=True)
    selected = select_trial(trials)
    save(output / "selection.json", selected)  # Persist before any test evaluation.
    frozen_selection_hash = hashlib.sha256((output / "selection.json").read_bytes()).hexdigest()
    results, test_rows = [], {}
    for phase, start, end in (("validation", first, second), ("test", second, data_config["ticks"])):
        population_hash = None
        for method in ("popularity", "favi_formula", "favi_formula_v2"):
            parameters = selected["parameters"] if method == "favi_formula_v2" else None
            result, rows = evaluate(events, start, end, method, protocol, parameters)
            if population_hash is not None and result["evaluation_population_sha256"] != population_hash:
                raise ValueError("Different candidates/targets between methods")
            population_hash = result["evaluation_population_sha256"]
            result.update(phase=phase, parameters=parameters)
            results.append(result)
            (output / f"{phase}-{method}-users.jsonl").write_text("".join(json.dumps(row) + "\n" for row in rows), encoding="utf-8")
            if phase == "test":
                test_rows[method] = rows
            print(json.dumps({"phase": phase, "method": method, "metrics": result["all"]}), flush=True)
    if hashlib.sha256((output / "selection.json").read_bytes()).hexdigest() != frozen_selection_hash:
        raise ValueError("Selection changed during test evaluation")
    intervals = {baseline: {metric: paired_interval(test_rows[baseline], test_rows["favi_formula_v2"], metric)
                           for metric in ("ndcg", "recall")}
                 for baseline in ("popularity", "favi_formula")}
    report = {"schema_version": 1, "dataset": str(dataset), "events_sha256": integrity["events_sha256"], "protocol": protocol,
              "selected_parameters": selected["parameters"], "selection_sha256": frozen_selection_hash, "results": results,
              "paired_test_differences_v2_minus_baseline": intervals, "python": sys.version, "platform": platform.platform(),
              "limitations": ["Offline ports, not production API or SQL candidate retrieval", "One synthetic dataset and assumed1hour/tick; no real-user or multi-dataset claim", "V1/V2 use extra comment/follow signals compared with likes-only Popularity", "Tags, latent preferences and exposures are not ranking features", "Python tracked fit allocation is not total process RAM", "Test labels never passed to selector; validation tuning can still overfit"]}
    save(output / "report.json", report)
    source = Path(__file__).resolve().parents[2] / "Favi-BE.API/Application/ContentDiscovery/ContentDiscoveryQueryReaderAdapter.cs"
    save(output / "source.json", {"formula_source_sha256": hashlib.sha256(source.read_bytes()).hexdigest()})
    for name in ("run_formula_benchmark.py", "benchmark_recommenders.py", "check_dataset.py", "dataset_policy.py"):
        (output / name).write_bytes(Path(__file__).with_name(name).read_bytes())
    lines = ["# Popularity, Formula V1 and validation-tuned Formula V2", "", "Selected V2: `" + json.dumps(selected["parameters"]) + "`", "",
             f"| Phase | Method | Users | Recall@{protocol['k']} | nDCG@{protocol['k']} | Coverage | Author diversity | p95 ms |", "|---|---|---:|---:|---:|---:|---:|---:|"]
    for result in results:
        m = result["all"]
        lines.append(f"| {result['phase']} | {result['method']} | {m['users']} | {m['recall']:.6f} | {m['ndcg']:.6f} | {m['catalog_coverage']:.4f} | {m['author_diversity']:.4f} | {result['rank_latency_ms']['p95']:.3f} |")
    lines += ["", "## Paired differences (V2 minus baseline)", "", "```json", json.dumps(intervals, indent=2), "```", "", *["- " + note for note in report["limitations"]]]
    (output / "review.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    save(output / "manifest.json", {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in output.iterdir()})


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--config", type=Path, default=Path(__file__).with_name("configs") / "recommenders" / "popularity.json")
    args = parser.parse_args()
    run(args.dataset, args.output, args.config)
