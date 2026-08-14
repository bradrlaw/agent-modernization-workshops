"""
Shared specialist-agent factory for the Retail Banking Concierge (Lab 06).

Creates Foundry-backed agents with the Microsoft Agent Framework, each owning a
focused subset of the Lab 03 banking tools. Every orchestration pattern in this
lab imports its team from here, so there is exactly one place to adjust if the SDK
moves.

Verified against the Microsoft Agent Framework samples
(github.com/microsoft/agent-framework, python/samples/03-workflows/orchestrations)
as of Aug 2026:
  - client:  agent_framework.foundry.FoundryChatClient(project_endpoint, model, credential)
  - agent:   agent_framework.Agent(client=..., instructions=..., name=..., tools=[...])
Verify against your installed version: ``pip show agent-framework``.
"""

import os

from dotenv import load_dotenv
from azure.identity import AzureCliCredential

from agent_framework import Agent
from agent_framework.foundry import FoundryChatClient

from tools import (
    get_account_balance,
    get_recent_transactions,
    list_accounts,
    get_customer_profile,
    get_loan_rates,
    calculate_loan_payment,
    search_faq,
)

load_dotenv()

# Demo customer for this session (see data/customers.json: CUST-1001..1003).
DEFAULT_CUSTOMER_ID = os.environ.get("DEMO_CUSTOMER_ID", "CUST-1001")


def get_client() -> FoundryChatClient:
    """Creates the Foundry-backed chat client shared by every specialist agent.

    Accepts the Agent Framework env names (FOUNDRY_PROJECT_ENDPOINT / FOUNDRY_MODEL)
    and falls back to the Lab 03 names (PROJECT_ENDPOINT / MODEL_DEPLOYMENT_NAME) so
    you can reuse your Lab 03 .env.
    """
    endpoint = os.environ.get("FOUNDRY_PROJECT_ENDPOINT") or os.environ.get("PROJECT_ENDPOINT")
    model = os.environ.get("FOUNDRY_MODEL") or os.environ.get("MODEL_DEPLOYMENT_NAME", "gpt-4o")
    if not endpoint:
        raise EnvironmentError(
            "Set FOUNDRY_PROJECT_ENDPOINT (or PROJECT_ENDPOINT from Lab 03). See .env.example."
        )
    return FoundryChatClient(
        project_endpoint=endpoint,
        model=model,
        credential=AzureCliCredential(),
    )


def _session_note(customer_id: str) -> str:
    return (
        f"\n\nCurrent session customer ID: {customer_id}. Use it for all lookups; never ask "
        "the customer for it and never reveal another customer's data."
    )


def build_team(client: FoundryChatClient, customer_id: str = DEFAULT_CUSTOMER_ID) -> dict:
    """Returns the specialist team keyed by role.

    The same ``Agent`` objects can be dropped into any orchestration pattern — only
    the orchestration *builder* changes from one pattern script to the next.

    Tools: the Lab 03 callables are passed directly; the framework generates each
    tool schema from the function signature + docstring. If your installed version
    prompts for tool-call approval, wrap each callable with
    ``tool(approval_mode="never_require")`` from ``agent_framework``.
    """
    accounts = Agent(
        client=client,
        name="AccountsAgent",
        instructions=(
            "You are the Accounts specialist for a retail bank. You handle balances, recent "
            "transactions, account lists, and customer profile lookups. Format currency as "
            "$#,###.##." + _session_note(customer_id)
        ),
        tools=[get_account_balance, get_recent_transactions, list_accounts, get_customer_profile],
    )

    lending = Agent(
        client=client,
        name="LendingAgent",
        instructions=(
            "You are the Lending specialist. You look up current loan rates and calculate loan "
            "payments. Always state the APR and its as-of date." + _session_note(customer_id)
        ),
        tools=[get_loan_rates, calculate_loan_payment],
    )

    cards = Agent(
        client=client,
        name="CardsFraudAgent",
        instructions=(
            "You are the Cards & Fraud specialist. You answer card questions, initiate disputes, "
            "and handle general banking FAQ." + _session_note(customer_id)
        ),
        tools=[search_faq],
    )

    compliance = Agent(
        client=client,
        name="ComplianceAgent",
        instructions=(
            "You are the Compliance specialist. You add required disclosures, verify PII handling, "
            "and ensure responses follow policy. You have no data tools; you review and annotate "
            "what other agents produce." + _session_note(customer_id)
        ),
    )

    concierge = Agent(
        client=client,
        name="Concierge",
        instructions=(
            "You are the triage concierge for a retail bank. Read the customer's request and hand "
            "off to exactly one specialist: AccountsAgent (balances/transactions/profile), "
            "LendingAgent (rates/payments), or CardsFraudAgent (cards/disputes/FAQ). Do not answer "
            "domain questions yourself." + _session_note(customer_id)
        ),
    )

    return {
        "concierge": concierge,
        "accounts": accounts,
        "lending": lending,
        "cards": cards,
        "compliance": compliance,
    }
