"""
Flagship demo — Handoff orchestration (Concierge <-> specialists, mesh topology).

The concierge triages each request and transfers control to the right specialist;
specialists can hand control back. Handoff is inherently interactive, so this demo
uses *scripted* customer turns to stay reproducible — replace ``scripted_turns``
with ``input(...)`` for a live console.

Run:
    cd src/
    python orchestrate_handoff.py

Env: FOUNDRY_PROJECT_ENDPOINT (or PROJECT_ENDPOINT) + FOUNDRY_MODEL (or
MODEL_DEPLOYMENT_NAME); then ``az login``.

Verified against github.com/microsoft/agent-framework
  python/samples/03-workflows/orchestrations/handoff_simple.py (Aug 2026).
"""

import asyncio

from agent_framework import AgentResponse
from agent_framework.orchestrations import HandoffAgentUserRequest, HandoffBuilder

from banking_agents import get_client, build_team


def _show(events) -> list:
    """Print any agent messages and return the pending user-input requests."""
    pending = []
    for event in events:
        if event.type == "handoff_sent":
            print(f"\n[handoff: {event.data.source} -> {event.data.target}]")
        elif event.type == "output" and isinstance(event.data, AgentResponse):
            for message in event.data.messages:
                if message.text:
                    print(f"- {message.author_name or message.role}: {message.text}")
        elif event.type == "request_info" and isinstance(event.data, HandoffAgentUserRequest):
            pending.append(event)
    return pending


async def main() -> None:
    client = get_client()
    team = build_team(client)

    workflow = (
        HandoffBuilder(
            name="banking_concierge_handoff",
            participants=[team["concierge"], team["accounts"], team["lending"], team["cards"]],
        )
        .with_start_agent(team["concierge"])
        .build()
    )

    # Watch the concierge route each turn to a different specialist.
    scripted_turns = [
        "What's my available balance on ACCT-4521?",             # -> Accounts
        "And what are your current 60-month auto loan rates?",   # -> Lending
        "One more: how do I dispute a charge on my debit card?", # -> Cards & Fraud
    ]

    opener = "Hi, I have a few account questions."
    print(f"- customer: {opener}")
    pending = _show([event async for event in workflow.run(opener, stream=True)])

    while pending:
        if not scripted_turns:
            responses = {req.request_id: HandoffAgentUserRequest.terminate() for req in pending}
            _show(await workflow.run(responses=responses))
            break
        turn = scripted_turns.pop(0)
        print(f"\n- customer: {turn}")
        responses = {req.request_id: HandoffAgentUserRequest.create_response(turn) for req in pending}
        pending = _show(await workflow.run(responses=responses))


if __name__ == "__main__":
    asyncio.run(main())
