"""
Interactive console chat client using CLU (no LLM) for intent detection.

This is the deterministic alternative to chat_console.py — uses Azure AI Language
CLU for intent classification and entity extraction instead of GPT-4o function calling.
Responses are template-based (no LLM generation).

Usage:
    cd src/
    python chat_console_clu.py

Requires:
    - CLU_ENDPOINT env var (Azure AI Language resource endpoint)
    - CLU_API_KEY env var (Azure AI Language resource key)
    - CLU_PROJECT_NAME env var (default: banking-assistant)
    - CLU_DEPLOYMENT_NAME env var (default: production)
"""

import json
import os
from dotenv import load_dotenv

from clu_client import get_clu_prediction, resolve_tool_args
from tools import (
    CUSTOMERS,
    ACCOUNTS,
    get_account_balance,
    get_recent_transactions,
    list_accounts,
    get_customer_profile,
    get_loan_rates,
    calculate_loan_payment,
    search_faq,
)

load_dotenv()

# Confidence threshold — below this, ask the user to rephrase
CONFIDENCE_THRESHOLD = 0.65

# Tool dispatch (same functions as LLM path)
TOOL_DISPATCH = {
    "get_account_balance": get_account_balance,
    "get_recent_transactions": get_recent_transactions,
    "list_accounts": list_accounts,
    "get_customer_profile": get_customer_profile,
    "get_loan_rates": get_loan_rates,
    "calculate_loan_payment": calculate_loan_payment,
    "search_faq": search_faq,
}


def select_customer() -> str:
    """Prompts the user to select a demo customer for this session."""
    print("\n╔══════════════════════════════════════════════╗")
    print("║   Virtual Banking Assistant — CLU Mode       ║")
    print("║   (No LLM — Deterministic Intent Routing)    ║")
    print("╚══════════════════════════════════════════════╝\n")
    print("Select a demo customer for this session:\n")

    for i, cust in enumerate(CUSTOMERS, 1):
        print(f"  {i}. {cust['firstName']} {cust['lastName']} ({cust['customerId']})")

    print()
    while True:
        choice = input("Enter choice (1-3): ").strip()
        if choice in ("1", "2", "3"):
            selected = CUSTOMERS[int(choice) - 1]
            print(f"\n✓ Logged in as {selected['firstName']} {selected['lastName']}")
            return selected["customerId"]
        print("  Invalid choice. Enter 1, 2, or 3.")


def resolve_account_id(customer_id: str, account_type: str = None, account_ref: str = None) -> str:
    """Resolves an account type or reference to an actual account_id."""
    customer_accounts = [a for a in ACCOUNTS if a["customerId"] == customer_id]

    if account_ref:
        # Match by last4 digits
        for acct in customer_accounts:
            if acct["last4"] == account_ref:
                return acct["accountId"]

    if account_type:
        # Match by account type
        for acct in customer_accounts:
            if acct["type"].lower() == account_type.lower():
                return acct["accountId"]

    return None  # Return all accounts


def format_response(tool_name: str, result_json: str) -> str:
    """Formats tool output as a human-readable response (template-based, no LLM)."""
    data = json.loads(result_json)

    if "error" in data:
        return f"⚠️  {data['error']}"

    if tool_name == "get_account_balance":
        lines = ["Here are your account balances:\n"]
        for acct in data.get("balances", []):
            lines.append(
                f"  • {acct['nickname']} ({acct['type']}, ending {acct['last4']})\n"
                f"    Current: ${acct['currentBalance']:,.2f} | "
                f"Available: ${acct['availableBalance']:,.2f}"
            )
        return "\n".join(lines)

    elif tool_name == "get_recent_transactions":
        lines = ["Recent transactions:\n"]
        for txn in data.get("transactions", []):
            sign = "-" if txn["type"] in ("debit", "withdrawal", "payment") else "+"
            lines.append(
                f"  {txn['date']}  {sign}${abs(txn['amount']):,.2f}  "
                f"{txn['description']}"
            )
        return "\n".join(lines)

    elif tool_name == "list_accounts":
        lines = [f"You have {len(data['accounts'])} accounts (total: ${data['totalBalance']:,.2f}):\n"]
        for acct in data["accounts"]:
            lines.append(
                f"  • {acct['nickname']} ({acct['type']}, ending {acct['last4']}) — "
                f"${acct['currentBalance']:,.2f}"
            )
        return "\n".join(lines)

    elif tool_name == "get_customer_profile":
        return (
            f"Profile information:\n"
            f"  Name: {data['name']}\n"
            f"  Email: {data['email']}\n"
            f"  Phone: {data['phone']}\n"
            f"  Address: {data['address']}\n"
            f"  Member since: {data['memberSince']}"
        )

    elif tool_name == "get_loan_rates":
        lines = ["Current loan rates:\n"]
        for rate in data.get("rates", []):
            lines.append(f"  • {rate['product'].title()} ({rate['term']}): {rate['apr']}% APR")
        return "\n".join(lines)

    elif tool_name == "calculate_loan_payment":
        return (
            f"Loan payment estimate:\n"
            f"  Principal: ${data['principal']:,.2f}\n"
            f"  Rate: {data['annualRate']}% APR for {data['termMonths']} months\n"
            f"  Monthly Payment: ${data['monthlyPayment']:,.2f}\n"
            f"  Total Interest: ${data['totalInterest']:,.2f}\n"
            f"  Total Cost: ${data['totalCost']:,.2f}"
        )

    elif tool_name == "search_faq":
        if "faq_results" in data:
            lines = []
            for faq in data["faq_results"]:
                lines.append(f"  Q: {faq['question']}\n  A: {faq['answer']}\n")
            return "\n".join(lines)
        return data.get("message", "No information found.")

    return json.dumps(data, indent=2)


def run_chat():
    """Main chat loop using CLU for intent detection (no LLM)."""
    customer_id = select_customer()

    print("\nConnecting to CLU endpoint...")
    # Verify CLU connection with a test call
    try:
        test_result = get_clu_prediction("hello")
        print(f"✓ Connected to CLU (project: {os.getenv('CLU_PROJECT_NAME', 'banking-assistant')})")
    except Exception as e:
        print(f"✗ Failed to connect to CLU: {e}")
        return

    print("\nType your message (or 'quit' to exit):")
    print("Note: CLU mode handles one request per turn (no multi-turn context).")
    print("-" * 50)

    try:
        while True:
            user_input = input("\nYou: ").strip()
            if not user_input:
                continue
            if user_input.lower() in ("quit", "exit", "q"):
                break

            # Get CLU prediction
            clu_result = get_clu_prediction(user_input)
            print(f"  [CLU] Intent: {clu_result.intent} ({clu_result.confidence:.0%})")

            if clu_result.entities:
                print(f"  [CLU] Entities: {clu_result.entities}")

            # Check confidence threshold
            if clu_result.confidence < CONFIDENCE_THRESHOLD:
                print(
                    "\nAssistant: I'm not sure I understood that. Could you rephrase?\n"
                    "  I can help with: balances, transactions, accounts, profile,\n"
                    "  loan rates, loan calculations, and general banking questions."
                )
                continue

            # Handle None intent
            if clu_result.intent == "None":
                print(
                    "\nAssistant: I'm a banking assistant. I can help you with:\n"
                    "  • Account balances and transactions\n"
                    "  • Account summaries and profile info\n"
                    "  • Loan rates and payment calculations\n"
                    "  • General banking questions (hours, policies, etc.)"
                )
                continue

            # Resolve tool and arguments
            # Inject full utterance for FAQ searches
            clu_result.entities["_utterance"] = user_input
            routing = resolve_tool_args(clu_result, customer_id)

            if not routing:
                print("\nAssistant: I couldn't determine how to help with that. Please try again.")
                continue

            tool_name = routing["tool_name"]
            args = routing["args"]

            # Resolve account references to actual account_ids
            if "_account_type" in args:
                account_id = resolve_account_id(customer_id, account_type=args.pop("_account_type"))
                if account_id:
                    args["account_id"] = account_id
            if "_account_ref" in args:
                account_id = resolve_account_id(customer_id, account_ref=args.pop("_account_ref"))
                if account_id:
                    args["account_id"] = account_id

            # Check for missing required args (loan calculator)
            if tool_name == "calculate_loan_payment":
                missing = []
                if not args.get("principal"):
                    missing.append("loan amount (e.g., $25,000)")
                if not args.get("annual_rate"):
                    missing.append("interest rate (e.g., 4.99%)")
                if not args.get("term_months"):
                    missing.append("loan term (e.g., 60 months)")
                if missing:
                    print(
                        f"\nAssistant: I need a bit more information to calculate your payment.\n"
                        f"  Please include: {', '.join(missing)}"
                    )
                    continue

            # Execute tool
            print(f"  [Route] → {tool_name}({args})")
            func = TOOL_DISPATCH[tool_name]
            result = func(**args)

            # Format and display response
            response = format_response(tool_name, result)
            print(f"\nAssistant: {response}")

    except KeyboardInterrupt:
        print("\n\nInterrupted.")

    print("\nGoodbye!")


if __name__ == "__main__":
    run_chat()
