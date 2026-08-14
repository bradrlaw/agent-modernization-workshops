"""
Magentic pattern — open-ended purchase planner (adaptive, planner-driven).

A Magentic manager builds a plan, delegates to specialists, tracks progress, and
re-plans when it stalls. Use for open-ended goals where the steps aren't known up
front. ``max_stall_count`` / ``max_reset_count`` bound the adaptation loop.

Run:
    cd src/
    python patterns/magentic_planner.py

Source: https://learn.microsoft.com/en-us/agent-framework/workflows/orchestrations/magentic
Verified: github.com/microsoft/agent-framework .../orchestrations/magentic.py
"""

import asyncio
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from agent_framework import AgentResponseUpdate  # noqa: E402
from agent_framework.orchestrations import MagenticBuilder  # noqa: E402

from banking_agents import get_client, build_team  # noqa: E402


async def main() -> None:
    client = get_client()
    team = build_team(client)

    specialists = [team["accounts"], team["lending"], team["cards"]]
    workflow = MagenticBuilder(
        participants=specialists,
        manager_agent=team["concierge"],       # plans, delegates, and adapts
        intermediate_output_from=specialists,
        max_round_count=10,
        max_stall_count=3,
        max_reset_count=2,
    ).build()

    task = (
        "CUST-1001 wants to buy a $30,000 car. Figure out affordability from their accounts, "
        "suitable loan options, and recommend next steps."
    )
    last_message_id = None
    async for event in workflow.run(task, stream=True):
        if event.type in ("intermediate", "output") and isinstance(event.data, AgentResponseUpdate):
            if event.data.message_id != last_message_id:
                print(f"\n\n- {event.executor_id}:", end=" ", flush=True)
                last_message_id = event.data.message_id
            print(event.data.text, end="", flush=True)
    print()


if __name__ == "__main__":
    asyncio.run(main())
