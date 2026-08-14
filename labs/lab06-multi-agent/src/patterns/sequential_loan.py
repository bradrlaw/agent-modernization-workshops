"""
Sequential pattern — loan application pipeline.

Fixed order: Accounts (verify eligibility) -> Lending (build quote) ->
Compliance (add disclosures). The shared conversation flows through each agent.

Run:
    cd src/
    python patterns/sequential_loan.py

Source: https://learn.microsoft.com/en-us/agent-framework/workflows/orchestrations/sequential
Verified: github.com/microsoft/agent-framework .../orchestrations/sequential_agents.py
"""

import asyncio
import os
import sys
from typing import cast

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from agent_framework import AgentResponse  # noqa: E402
from agent_framework.orchestrations import SequentialBuilder  # noqa: E402

from banking_agents import get_client, build_team  # noqa: E402


async def main() -> None:
    client = get_client()
    team = build_team(client)

    workflow = SequentialBuilder(
        participants=[team["accounts"], team["lending"], team["compliance"]],
        output_from="all",
    ).build()

    task = (
        "CUST-1001 wants a $25,000 auto loan for 60 months. Verify eligibility, quote it, "
        "and add the required disclosures."
    )
    result = await workflow.run(task)
    for output in result.get_outputs():
        response = cast(AgentResponse, output)
        for msg in response.messages:
            print(f"\n--- {msg.author_name or msg.role} ---\n{msg.text}")


if __name__ == "__main__":
    asyncio.run(main())
