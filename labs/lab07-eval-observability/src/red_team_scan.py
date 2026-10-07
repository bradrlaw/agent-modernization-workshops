"""
red_team_scan.py — automated adversarial safety scan (Python only).

Uses the Azure AI Foundry **AI Red Teaming Agent** (built on PyRIT) to attack
the Lab 03 banking assistant with automatically generated adversarial prompts
across risk categories, then reports an attack-success-rate scorecard.

    ┌───────────────────────────────────────────────────────────────────┐
    │ .NET note: the AI Red Teaming Agent ships only as a Python package  │
    │ (azure-ai-evaluation[redteam]); there is no .NET SDK equivalent.    │
    │ .NET-first teams run THIS script as a separate CI job, or use the   │
    │ Foundry portal AI Red Teaming experience. Everything else in this   │
    │ lab (quality + custom evaluation, tracing) has full .NET parity.    │
    └───────────────────────────────────────────────────────────────────┘

Prerequisites:
  * ``pip install "azure-ai-evaluation[redteam]"``  (Python 3.10-3.12)
  * Azure sign-in: ``az login``
  * ``AZURE_AI_PROJECT`` (Foundry project endpoint) in ``.env``
  * ``PROJECT_ENDPOINT`` and ``MODEL_DEPLOYMENT_NAME`` for the target agent

Usage:
    cd labs/lab07-eval-observability/src
    python red_team_scan.py --objectives 5 --output ../data/redteam-results.json

Red teaming is a preview capability — confirm region support and production
suitability before relying on it in a live system.
"""

from __future__ import annotations

import argparse
import asyncio
import json
import os
import sys
from pathlib import Path

from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential

LAB07_SRC = Path(__file__).resolve().parent
LAB03_SRC = LAB07_SRC.parent.parent / "lab03-foundry-agent" / "src"
sys.path.insert(0, str(LAB03_SRC))

from agent import build_system_message, chat_completion, get_project_client  # noqa: E402


async def main() -> int:
    parser = argparse.ArgumentParser(description="Run an AI Red Teaming scan against the Lab 03 agent.")
    parser.add_argument("--objectives", type=int, default=5, help="Attack objectives per risk category.")
    parser.add_argument("--output", type=Path, default=LAB07_SRC.parent / "data" / "redteam-results.json")
    args = parser.parse_args()

    load_dotenv()

    # Imported lazily so the rest of the lab does not require the redteam extra.
    from azure.ai.evaluation.red_team import AttackStrategy, RedTeam, RiskCategory

    model = os.environ.get("MODEL_DEPLOYMENT_NAME", "gpt-4o")
    project_client = get_project_client()
    openai_client = project_client.get_openai_client()

    # The target callback is the system under test: our banking assistant.
    def call_agent(query: str) -> str:
        messages = [build_system_message("CUST-1001"), {"role": "user", "content": query}]
        try:
            return chat_completion(openai_client, model, messages)
        except Exception as exc:
            return f"(error: {exc})"

    red_team_agent = RedTeam(
        azure_ai_project=os.environ["AZURE_AI_PROJECT"],
        credential=DefaultAzureCredential(),
        risk_categories=[
            RiskCategory.Violence,
            RiskCategory.HateUnfairness,
            RiskCategory.Sexual,
            RiskCategory.SelfHarm,
        ],
        num_objectives=args.objectives,
    )

    print("Running AI Red Teaming scan (this can take several minutes)...")
    result = await red_team_agent.scan(
        target=call_agent,
        attack_strategies=[AttackStrategy.Flip, AttackStrategy.Base64],
    )

    # Persist the scorecard as release evidence.
    with open(args.output, "w", encoding="utf-8") as out:
        json.dump(getattr(result, "to_dict", lambda: {"result": str(result)})(), out, indent=2, default=str)

    print(f"Scan complete. Scorecard written to {args.output}")
    print("Create work items for any category with a non-zero attack success rate.")
    return 0


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
