"""
trace_agent.py — emit OpenTelemetry GenAI traces for the Lab 03 agent.

Instruments the Virtual Banking Assistant with the Azure Monitor OpenTelemetry
distro so every model call and tool call is exported to Application Insights and
surfaced in the Foundry **Tracing** tab (and Azure Monitor → Agents preview).
It also wraps each evaluation case in a custom span so the trace tells the full
"route → tool → model" story.

Prerequisites:
  * Azure sign-in: ``az login``
  * ``PROJECT_ENDPOINT`` and ``MODEL_DEPLOYMENT_NAME`` in ``.env``
  * An Application Insights resource linked to the Foundry project, or
    ``APPLICATIONINSIGHTS_CONNECTION_STRING`` set directly.

Usage:
    cd labs/lab07-eval-observability/src
    python trace_agent.py --limit 5

Privacy: message-content capture is OFF unless you opt in with
``AZURE_TRACING_GEN_AI_CONTENT_RECORDING_ENABLED=true`` (and the OpenTelemetry
equivalent ``OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=true``). Never
enable content capture against real customer data without approval.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path

from dotenv import load_dotenv
from azure.monitor.opentelemetry import configure_azure_monitor
from opentelemetry import trace
from opentelemetry.instrumentation.openai_v2 import OpenAIInstrumentor

LAB07_SRC = Path(__file__).resolve().parent
LAB03_SRC = LAB07_SRC.parent.parent / "lab03-foundry-agent" / "src"
sys.path.insert(0, str(LAB03_SRC))

from agent import build_system_message, chat_completion, get_project_client  # noqa: E402

DATASET_PATH = LAB07_SRC.parent / "data" / "eval-dataset.jsonl"

tracer = trace.get_tracer("banking.eval.trace")


def resolve_connection_string(project_client) -> str:
    conn = os.environ.get("APPLICATIONINSIGHTS_CONNECTION_STRING")
    if conn:
        return conn
    # Fall back to the Application Insights resource linked to the Foundry project.
    return project_client.telemetry.get_application_insights_connection_string()


def main() -> int:
    parser = argparse.ArgumentParser(description="Trace the Lab 03 agent with OpenTelemetry + Azure Monitor.")
    parser.add_argument("--dataset", type=Path, default=DATASET_PATH)
    parser.add_argument("--limit", type=int, default=5, help="Number of cases to trace.")
    args = parser.parse_args()

    load_dotenv()
    model = os.environ.get("MODEL_DEPLOYMENT_NAME", "gpt-4o")

    project_client = get_project_client()
    connection_string = resolve_connection_string(project_client)

    # Wire up Azure Monitor export and GenAI instrumentation for OpenAI calls.
    configure_azure_monitor(connection_string=connection_string)
    OpenAIInstrumentor().instrument()

    openai_client = project_client.get_openai_client()

    with open(args.dataset, "r", encoding="utf-8") as handle:
        cases = [json.loads(line) for line in handle if line.strip()][: args.limit]

    print(f"Tracing {len(cases)} cases -> Application Insights / Foundry Tracing tab\n")
    for case in cases:
        # A custom span per case gives the trace a business-level anchor that
        # the nested gen_ai.* model and tool spans hang beneath.
        with tracer.start_as_current_span("eval-case") as span:
            span.set_attribute("agent.case_id", case["id"])
            span.set_attribute("agent.category", case.get("category", ""))
            span.set_attribute("agent.customer_id", case.get("customer_id", ""))

            messages = [
                build_system_message(case.get("customer_id", "CUST-1001")),
                {"role": "user", "content": case["query"]},
            ]
            response = chat_completion(openai_client, model, messages)
            span.set_attribute("agent.response_chars", len(response))
            print(f"[{case['id']}] traced ({len(response)} chars)")

    print("\nDone. Open Foundry -> Tracing (traces may take 1-2 minutes to appear).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
