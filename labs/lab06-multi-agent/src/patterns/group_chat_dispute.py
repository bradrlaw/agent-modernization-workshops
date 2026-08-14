"""
Group Chat pattern — dispute resolution round-table.

Specialists take turns (here, a round-robin selector) collaborating toward a
resolution; a termination condition stops the conversation.

Run:
    cd src/
    python patterns/group_chat_dispute.py

Source: https://learn.microsoft.com/en-us/agent-framework/workflows/orchestrations/group-chat
Verified: github.com/microsoft/agent-framework .../orchestrations/group_chat_simple_selector.py
"""

import asyncio
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from agent_framework import AgentResponseUpdate  # noqa: E402
from agent_framework.orchestrations import GroupChatBuilder, GroupChatState  # noqa: E402

from banking_agents import get_client, build_team  # noqa: E402


def round_robin(state: GroupChatState) -> str:
    """Pick the next speaker based on the current round index."""
    names = list(state.participants.keys())
    return names[state.current_round % len(names)]


async def main() -> None:
    client = get_client()
    team = build_team(client)

    participants = [team["cards"], team["accounts"], team["compliance"]]
    workflow = GroupChatBuilder(
        participants=participants,
        selection_func=round_robin,
        termination_condition=lambda conversation: len(conversation) >= 6,
        intermediate_output_from=participants,
    ).build()

    task = (
        "CUST-1003 disputes a $180 charge on their debit card ending 3378. Validate the account, "
        "assess the dispute, and agree on a resolution."
    )
    last_response_id = None
    async for event in workflow.run(task, stream=True):
        if event.type in ("intermediate", "output") and isinstance(event.data, AgentResponseUpdate):
            if event.data.response_id != last_response_id:
                print(f"\n\n{event.data.author_name}:", end=" ", flush=True)
                last_response_id = event.data.response_id
            print(event.data.text, end="", flush=True)
    print()


if __name__ == "__main__":
    asyncio.run(main())
