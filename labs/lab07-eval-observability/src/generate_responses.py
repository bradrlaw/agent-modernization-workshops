"""
generate_responses.py — produce agent responses for the evaluation dataset.

Runs every case in ``data/eval-dataset.jsonl`` through the **Lab 03 Virtual
Banking Assistant** (reused directly, so we evaluate the real agent — not a
copy) and writes the answers to ``data/responses.jsonl``. That output file is
the input to ``evaluate_agent.py``.

Prerequisites:
  * Azure sign-in: ``az login``
  * ``PROJECT_ENDPOINT`` and ``MODEL_DEPLOYMENT_NAME`` in ``.env`` (see
    ``.env.example``)

Usage:
    cd labs/lab07-eval-observability/src
    python generate_responses.py                 # all cases
    python generate_responses.py --limit 3       # first 3 cases (smoke test)
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from dotenv import load_dotenv

# Reuse the Lab 03 agent so Lab 07 evaluates the exact system under test.
LAB07_SRC = Path(__file__).resolve().parent
LAB03_SRC = LAB07_SRC.parent.parent / "lab03-foundry-agent" / "src"
sys.path.insert(0, str(LAB03_SRC))

from agent import build_system_message, chat_completion, get_project_client  # noqa: E402

import os  # noqa: E402

DATA_DIR = LAB07_SRC.parent / "data"
DATASET_PATH = DATA_DIR / "eval-dataset.jsonl"
RESPONSES_PATH = DATA_DIR / "responses.jsonl"


def load_dataset(path: Path) -> list[dict]:
    with open(path, "r", encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate agent responses for the eval dataset.")
    parser.add_argument("--dataset", type=Path, default=DATASET_PATH)
    parser.add_argument("--output", type=Path, default=RESPONSES_PATH)
    parser.add_argument("--limit", type=int, default=0, help="Only run the first N cases (0 = all).")
    args = parser.parse_args()

    load_dotenv()
    model = os.environ.get("MODEL_DEPLOYMENT_NAME", "gpt-4o")

    cases = load_dataset(args.dataset)
    if args.limit:
        cases = cases[: args.limit]

    print(f"Connecting to Azure AI Foundry ({len(cases)} cases)...")
    project_client = get_project_client()
    openai_client = project_client.get_openai_client()
    print("Connected.\n")

    written = 0
    with open(args.output, "w", encoding="utf-8") as out:
        for case in cases:
            customer_id = case.get("customer_id", "CUST-1001")
            messages = [build_system_message(customer_id), {"role": "user", "content": case["query"]}]
            print(f"[{case['id']}] {case['query']}")
            try:
                response = chat_completion(openai_client, model, messages)
            except Exception as exc:  # keep the batch going; record the failure
                response = f"(ERROR: {exc})"
                print(f"  ! {exc}")

            record = {
                "id": case["id"],
                "category": case.get("category"),
                "customer_id": customer_id,
                "query": case["query"],
                "context": case.get("context", ""),
                "ground_truth": case.get("ground_truth", ""),
                "response": response,
            }
            out.write(json.dumps(record) + "\n")
            written += 1

    print(f"\nWrote {written} responses to {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
