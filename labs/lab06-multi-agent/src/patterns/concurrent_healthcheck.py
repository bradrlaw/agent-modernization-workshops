"""
Concurrent pattern — financial health check (parallel fan-out/fan-in).

The default dispatcher fans the same prompt out to all specialists in parallel;
the default aggregator fans their answers back in.

Run:
    cd src/
    python patterns/concurrent_healthcheck.py

Source: https://learn.microsoft.com/en-us/agent-framework/workflows/orchestrations/concurrent
Verified: github.com/microsoft/agent-framework .../orchestrations/concurrent_agents.py
"""

import asyncio
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from agent_framework import AgentResponse  # noqa: E402
from agent_framework.orchestrations import ConcurrentBuilder  # noqa: E402

from banking_agents import get_client, build_team  # noqa: E402


async def main() -> None:
    client = get_client()
    team = build_team(client)

    workflow = ConcurrentBuilder(
        participants=[team["accounts"], team["lending"], team["cards"]],
    ).build()

    task = (
        "Give CUST-1001 a financial health check: summarize balances, relevant loan options, "
        "and any card/FAQ notes."
    )
    events = await workflow.run(task)
    for output in events.get_outputs():
        if isinstance(output, AgentResponse):
            for msg in output.messages:
                print(f"\n--- {msg.author_name or 'agent'} ---\n{msg.text}")


if __name__ == "__main__":
    asyncio.run(main())
