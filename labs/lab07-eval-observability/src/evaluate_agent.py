"""
evaluate_agent.py — score agent responses and enforce a quality gate.

Two modes:

  * ``--offline`` (default when azure-ai-evaluation is unavailable): runs only
    the deterministic, rule-based custom evaluators from ``custom_evaluators.py``.
    No Azure credentials, model, or network required — this is the fast,
    free gate that runs on every pull request.

  * online (``--online``): additionally runs the LLM-based Azure AI Evaluation
    quality evaluators (groundedness, relevance, coherence, fluency, similarity)
    via ``azure.ai.evaluation.evaluate``. Requires an evaluator model.

In both modes the script prints a per-case table and a summary, then exits with
a non-zero status code if any gate fails — so it can be dropped straight into a
CI step.

Usage:
    cd labs/lab07-eval-observability/src
    python evaluate_agent.py --data ../data/responses.jsonl             # offline gate
    python evaluate_agent.py --data ../data/responses.jsonl --online    # + quality
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path

from dotenv import load_dotenv

LAB07_SRC = Path(__file__).resolve().parent
sys.path.insert(0, str(LAB07_SRC))

from custom_evaluators import (  # noqa: E402
    CurrencyFormatEvaluator,
    PiiLeakageEvaluator,
    RequiredDisclosureEvaluator,
)

# Custom (rule-based) metric names that must equal 1.0 for every case.
CUSTOM_METRICS = ("pii_leakage", "currency_format", "required_disclosure")

# Default minimum means for the LLM-based quality metrics (1-5 scale).
DEFAULT_QUALITY_THRESHOLDS = {
    "groundedness": 4.0,
    "relevance": 4.0,
    "coherence": 4.0,
    "fluency": 4.0,
    "similarity": 3.0,
}


def load_rows(path: Path) -> list[dict]:
    with open(path, "r", encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def run_offline(rows: list[dict]) -> int:
    """Rule-based gate: every custom metric must pass for every case."""
    evaluators = [PiiLeakageEvaluator(), CurrencyFormatEvaluator(), RequiredDisclosureEvaluator()]
    failures: list[str] = []

    print(f"{'case':<12}{'pii':>6}{'currency':>10}{'disclosure':>12}")
    print("-" * 40)
    for row in rows:
        response = row.get("response", "")
        scores: dict[str, float] = {}
        for evaluator in evaluators:
            scores.update({k: v for k, v in evaluator(response=response).items() if isinstance(v, (int, float))})

        pii = scores.get("pii_leakage", 0.0)
        cur = scores.get("currency_format", 0.0)
        dis = scores.get("required_disclosure", 0.0)
        print(f"{row.get('id', '?'):<12}{pii:>6.0f}{cur:>10.0f}{dis:>12.0f}")

        for metric in CUSTOM_METRICS:
            if scores.get(metric, 0.0) < 1.0:
                failures.append(f"{row.get('id', '?')}: {metric}")

    print("-" * 40)
    if failures:
        print(f"\nGATE FAILED - {len(failures)} custom-metric violation(s):")
        for failure in failures:
            print(f"  - {failure}")
        return 1

    print(f"\nGATE PASSED - {len(rows)} cases, all custom metrics = 1.0")
    return 0


def run_online(rows: list[dict], data_path: Path, thresholds: dict[str, float]) -> int:
    """Full gate: Azure AI Evaluation quality evaluators + custom evaluators."""
    from azure.ai.evaluation import (  # imported lazily so offline mode needs no SDK
        CoherenceEvaluator,
        FluencyEvaluator,
        GroundednessEvaluator,
        RelevanceEvaluator,
        SimilarityEvaluator,
        evaluate,
    )

    model_config = {
        "azure_endpoint": os.environ["AZURE_OPENAI_ENDPOINT"],
        "azure_deployment": os.environ["EVALUATOR_MODEL_DEPLOYMENT"],
        "api_version": os.environ.get("AZURE_OPENAI_API_VERSION", "2024-12-01-preview"),
    }

    evaluators = {
        "groundedness": GroundednessEvaluator(model_config),
        "relevance": RelevanceEvaluator(model_config),
        "coherence": CoherenceEvaluator(model_config),
        "fluency": FluencyEvaluator(model_config),
        "similarity": SimilarityEvaluator(model_config),
        "pii": PiiLeakageEvaluator(),
        "currency": CurrencyFormatEvaluator(),
        "disclosure": RequiredDisclosureEvaluator(),
    }

    # Map dataset columns to evaluator inputs.
    column_mapping = {
        "query": "${data.query}",
        "response": "${data.response}",
        "context": "${data.context}",
        "ground_truth": "${data.ground_truth}",
    }
    evaluator_config = {name: {"column_mapping": column_mapping} for name in evaluators}

    result = evaluate(
        data=str(data_path),
        evaluators=evaluators,
        evaluator_config=evaluator_config,
    )

    metrics = result.get("metrics", {})
    print("Aggregate metrics:")
    for name, value in sorted(metrics.items()):
        print(f"  {name}: {value}")

    failures: list[str] = []

    # Hard gate on custom metrics (per-row, from result rows).
    for row in result.get("rows", []):
        case_id = row.get("inputs.id") or row.get("inputs.query", "?")
        for metric in CUSTOM_METRICS:
            value = _find_metric(row, metric)
            if value is not None and value < 1.0:
                failures.append(f"{case_id}: {metric}={value}")

    # Threshold gate on quality metric means.
    for metric, minimum in thresholds.items():
        mean = _find_aggregate(metrics, metric)
        if mean is not None and mean < minimum:
            failures.append(f"{metric} mean {mean:.2f} < {minimum}")

    if failures:
        print(f"\nGATE FAILED - {len(failures)} issue(s):")
        for failure in failures:
            print(f"  - {failure}")
        return 1

    print("\nGATE PASSED - custom metrics clean and quality thresholds met.")
    return 0


def _find_metric(row: dict, metric: str):
    for key, value in row.items():
        if key.endswith(f".{metric}") and isinstance(value, (int, float)):
            return float(value)
    return None


def _find_aggregate(metrics: dict, metric: str):
    for key, value in metrics.items():
        if key.endswith(f".{metric}") and isinstance(value, (int, float)):
            return float(value)
    return None


def main() -> int:
    parser = argparse.ArgumentParser(description="Evaluate agent responses and enforce a quality gate.")
    parser.add_argument("--data", type=Path, default=LAB07_SRC.parent / "data" / "responses.jsonl")
    parser.add_argument("--online", action="store_true", help="Also run Azure AI Evaluation quality evaluators.")
    args = parser.parse_args()

    load_dotenv()
    rows = load_rows(args.data)
    if not rows:
        print(f"No rows found in {args.data}. Run generate_responses.py first.")
        return 2

    if args.online:
        return run_online(rows, args.data, DEFAULT_QUALITY_THRESHOLDS)
    return run_offline(rows)


if __name__ == "__main__":
    raise SystemExit(main())
