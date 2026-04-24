"""
Agent creation and configuration for the Virtual Banking Assistant.

Uses the Azure AI Foundry SDK v2 (AIProjectClient) to obtain an OpenAI client,
then uses standard OpenAI function-calling to give the model access to banking tools.
"""

import json
import os
from pathlib import Path

from azure.identity import DefaultAzureCredential, AzureCliCredential
from azure.ai.projects import AIProjectClient

from tools import (
    get_account_balance,
    get_recent_transactions,
    list_accounts,
    get_customer_profile,
    get_loan_rates,
    calculate_loan_payment,
)

# Agent system instructions — matches Lab 02 persona
SYSTEM_INSTRUCTIONS = """You are a Virtual Banking Assistant for a retail financial institution.
You help authenticated customers check account balances, review recent transactions,
list their accounts, look up their profile information, check current loan rates,
and calculate loan payments.

Always be professional, accurate, and security-conscious. Format currency values
with dollar signs and two decimal places. When displaying multiple items (accounts,
transactions), present them in a clear, organized format.

Never share information about other customers. If you cannot fulfill a request,
offer to connect the customer with a human agent.

The current customer's ID will be provided in each conversation. Use it for all
data lookups. Do not ask the user for their customer ID — it is already known.
"""

# ---------------------------------------------------------------------------
# OpenAI function-calling tool definitions (JSON Schema)
# ---------------------------------------------------------------------------
TOOL_DEFINITIONS = [
    {
        "type": "function",
        "function": {
            "name": "get_account_balance",
            "description": "Retrieves the current and available balance for a customer's account(s).",
            "parameters": {
                "type": "object",
                "properties": {
                    "customer_id": {"type": "string", "description": "Customer identifier, e.g. CUST-1001"},
                    "account_id": {"type": "string", "description": "Optional account identifier, e.g. ACCT-4521"},
                },
                "required": ["customer_id"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_recent_transactions",
            "description": "Retrieves recent transactions for a customer's account(s).",
            "parameters": {
                "type": "object",
                "properties": {
                    "customer_id": {"type": "string", "description": "Customer identifier"},
                    "account_id": {"type": "string", "description": "Optional account identifier"},
                    "limit": {"type": "integer", "description": "Max transactions to return (default 5)"},
                },
                "required": ["customer_id"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "list_accounts",
            "description": "Lists all accounts belonging to a customer with summary information.",
            "parameters": {
                "type": "object",
                "properties": {
                    "customer_id": {"type": "string", "description": "Customer identifier"},
                },
                "required": ["customer_id"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_customer_profile",
            "description": "Retrieves profile information (name, email, phone, address) for a customer.",
            "parameters": {
                "type": "object",
                "properties": {
                    "customer_id": {"type": "string", "description": "Customer identifier"},
                },
                "required": ["customer_id"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_loan_rates",
            "description": "Retrieves current loan interest rates. Can filter by product type.",
            "parameters": {
                "type": "object",
                "properties": {
                    "product_type": {"type": "string", "description": "Loan product filter: auto, home, or personal"},
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "calculate_loan_payment",
            "description": "Calculates monthly payment, total interest, and total cost for a loan.",
            "parameters": {
                "type": "object",
                "properties": {
                    "principal": {"type": "number", "description": "Loan amount in dollars"},
                    "annual_rate": {"type": "number", "description": "Annual interest rate as a percentage (e.g. 5.25)"},
                    "term_months": {"type": "integer", "description": "Loan term in months"},
                },
                "required": ["principal", "annual_rate", "term_months"],
            },
        },
    },
]

# Map function names to callables
TOOL_DISPATCH = {
    "get_account_balance": get_account_balance,
    "get_recent_transactions": get_recent_transactions,
    "list_accounts": list_accounts,
    "get_customer_profile": get_customer_profile,
    "get_loan_rates": get_loan_rates,
    "calculate_loan_payment": calculate_loan_payment,
}


def get_project_client() -> AIProjectClient:
    """Creates an authenticated AIProjectClient from environment variables."""
    endpoint = os.environ.get("PROJECT_ENDPOINT")
    if not endpoint:
        raise EnvironmentError(
            "PROJECT_ENDPOINT environment variable is required. "
            "Find it in your AI Foundry project overview page. "
            "Format: https://<resource>.services.ai.azure.com/api/projects/<project>"
        )
    try:
        credential = DefaultAzureCredential()
        credential.get_token("https://cognitiveservices.azure.com/.default")
    except Exception:
        credential = AzureCliCredential()

    return AIProjectClient(
        endpoint=endpoint,
        credential=credential,
    )


def build_system_message(customer_id: str) -> dict:
    """Returns the system message with the customer ID injected."""
    content = SYSTEM_INSTRUCTIONS + (
        f"\n\nCurrent session customer ID: {customer_id}. "
        "Use this customer ID for all account, transaction, and profile lookups."
    )
    return {"role": "system", "content": content}


def execute_tool_call(name: str, arguments: dict) -> str:
    """Dispatches a tool call to the matching Python function."""
    func = TOOL_DISPATCH.get(name)
    if not func:
        return json.dumps({"error": f"Unknown tool: {name}"})
    return func(**arguments)


def chat_completion(openai_client, model: str, messages: list) -> str:
    """
    Sends messages to the model and handles the tool-call loop.

    If the model requests tool calls, executes them locally, appends results,
    and re-sends until the model produces a final text response.

    Returns the assistant's final text response.
    """
    while True:
        response = openai_client.chat.completions.create(
            model=model,
            messages=messages,
            tools=TOOL_DEFINITIONS,
            tool_choice="auto",
        )

        choice = response.choices[0]

        # If the model produced a text response, we're done
        if choice.finish_reason == "stop":
            assistant_msg = choice.message.content or ""
            messages.append({"role": "assistant", "content": assistant_msg})
            return assistant_msg

        # If the model wants to call tools, process each one
        if choice.finish_reason == "tool_calls":
            # Append the assistant message with tool calls
            messages.append(choice.message.model_dump())

            for tool_call in choice.message.tool_calls:
                fn_name = tool_call.function.name
                fn_args = json.loads(tool_call.function.arguments)
                print(f"  ⚙ Calling tool: {fn_name}({fn_args})")

                result = execute_tool_call(fn_name, fn_args)

                messages.append({
                    "role": "tool",
                    "tool_call_id": tool_call.id,
                    "content": result,
                })

            # Loop back to send tool results to the model
            continue

        # Unexpected finish reason — return whatever we got
        return choice.message.content or "(No response)"
