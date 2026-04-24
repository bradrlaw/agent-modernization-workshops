"""
Function tool implementations for the Virtual Banking Assistant.

Each function is designed to be registered as a FunctionTool with the
Azure AI Agent Service. Functions load data from JSON files and return
structured results the agent can present to the user.
"""

import json
import os
from pathlib import Path
from typing import Optional

# Load data from JSON files at module level
_DATA_DIR = Path(__file__).parent.parent / "data"


def _load_json(filename: str) -> list:
    with open(_DATA_DIR / filename, "r") as f:
        return json.load(f)


CUSTOMERS = _load_json("customers.json")
ACCOUNTS = _load_json("accounts.json")
TRANSACTIONS = _load_json("transactions.json")


# ---------------------------------------------------------------------------
# Tool 1: Get Account Balance
# ---------------------------------------------------------------------------
def get_account_balance(customer_id: str, account_id: Optional[str] = None) -> str:
    """
    Retrieves the current and available balance for a customer's account.

    :param customer_id: The customer identifier (e.g. "CUST-1001"). Required.
    :param account_id: Optional account identifier (e.g. "ACCT-4521"). If not
        provided, returns balances for all accounts belonging to the customer.
    :return: JSON string with account balance information.
    """
    accounts = [a for a in ACCOUNTS if a["customerId"] == customer_id]
    if not accounts:
        return json.dumps({"error": f"No accounts found for customer {customer_id}"})

    if account_id:
        accounts = [a for a in accounts if a["accountId"] == account_id]
        if not accounts:
            return json.dumps({"error": f"Account {account_id} not found for customer {customer_id}"})

    results = []
    for acct in accounts:
        results.append({
            "accountId": acct["accountId"],
            "type": acct["type"],
            "nickname": acct["nickname"],
            "last4": acct["last4"],
            "currentBalance": acct["currentBalance"],
            "availableBalance": acct["availableBalance"],
            "status": acct["status"],
        })

    return json.dumps({"balances": results}, indent=2)


# ---------------------------------------------------------------------------
# Tool 2: Get Recent Transactions
# ---------------------------------------------------------------------------
def get_recent_transactions(
    customer_id: str,
    account_id: Optional[str] = None,
    limit: int = 5,
) -> str:
    """
    Retrieves recent transactions for a customer's account.

    :param customer_id: The customer identifier (e.g. "CUST-1001"). Required.
    :param account_id: Optional account identifier. If not provided, returns
        transactions across all customer accounts.
    :param limit: Maximum number of transactions to return. Defaults to 5.
    :return: JSON string with transaction list.
    """
    customer_accounts = [a["accountId"] for a in ACCOUNTS if a["customerId"] == customer_id]
    if not customer_accounts:
        return json.dumps({"error": f"No accounts found for customer {customer_id}"})

    if account_id:
        if account_id not in customer_accounts:
            return json.dumps({"error": f"Account {account_id} not found for customer {customer_id}"})
        target_accounts = [account_id]
    else:
        target_accounts = customer_accounts

    txns = [t for t in TRANSACTIONS if t["accountId"] in target_accounts]
    txns.sort(key=lambda t: t["date"], reverse=True)
    txns = txns[:limit]

    return json.dumps({"transactions": txns}, indent=2)


# ---------------------------------------------------------------------------
# Tool 3: List Accounts
# ---------------------------------------------------------------------------
def list_accounts(customer_id: str) -> str:
    """
    Lists all accounts belonging to a customer with summary information.

    :param customer_id: The customer identifier (e.g. "CUST-1001"). Required.
    :return: JSON string with account list and total balance.
    """
    accounts = [a for a in ACCOUNTS if a["customerId"] == customer_id]
    if not accounts:
        return json.dumps({"error": f"No accounts found for customer {customer_id}"})

    total = sum(a["currentBalance"] for a in accounts)
    summary = []
    for acct in accounts:
        summary.append({
            "accountId": acct["accountId"],
            "type": acct["type"],
            "nickname": acct["nickname"],
            "last4": acct["last4"],
            "currentBalance": acct["currentBalance"],
            "status": acct["status"],
        })

    return json.dumps({"accounts": summary, "totalBalance": total}, indent=2)


# ---------------------------------------------------------------------------
# Tool 4: Get Customer Profile
# ---------------------------------------------------------------------------
def get_customer_profile(customer_id: str) -> str:
    """
    Retrieves the profile information for a customer.

    :param customer_id: The customer identifier (e.g. "CUST-1001"). Required.
    :return: JSON string with customer profile data.
    """
    customer = next((c for c in CUSTOMERS if c["customerId"] == customer_id), None)
    if not customer:
        return json.dumps({"error": f"Customer {customer_id} not found"})

    return json.dumps({
        "customerId": customer["customerId"],
        "name": f"{customer['firstName']} {customer['lastName']}",
        "email": customer["email"],
        "phone": customer["phone"],
        "address": customer["address"],
        "memberSince": customer["memberSince"],
    }, indent=2)


# ---------------------------------------------------------------------------
# Tool 5: Get Loan Rates
# ---------------------------------------------------------------------------
def get_loan_rates(product_type: Optional[str] = None) -> str:
    """
    Retrieves current loan interest rates. In production this would call an
    external API; here it returns simulated rate data.

    :param product_type: Optional loan product filter. Values: "auto",
        "home", "personal". If not provided, returns all rates.
    :return: JSON string with loan rate information.
    """
    rates = [
        {"product": "auto", "term": "36 months", "apr": 4.49, "asOf": "2026-04-23"},
        {"product": "auto", "term": "60 months", "apr": 4.99, "asOf": "2026-04-23"},
        {"product": "home", "term": "15 years", "apr": 5.75, "asOf": "2026-04-23"},
        {"product": "home", "term": "30 years", "apr": 6.25, "asOf": "2026-04-23"},
        {"product": "personal", "term": "12 months", "apr": 7.99, "asOf": "2026-04-23"},
        {"product": "personal", "term": "36 months", "apr": 8.49, "asOf": "2026-04-23"},
    ]

    if product_type:
        rates = [r for r in rates if r["product"] == product_type.lower()]
        if not rates:
            return json.dumps({"error": f"Unknown product type: {product_type}. Use: auto, home, personal"})

    return json.dumps({"rates": rates}, indent=2)


# ---------------------------------------------------------------------------
# Tool 6: Calculate Loan Payment
# ---------------------------------------------------------------------------
def calculate_loan_payment(
    principal: float,
    annual_rate: float,
    term_months: int,
) -> str:
    """
    Calculates the monthly payment, total interest, and total cost for a loan.

    :param principal: Loan amount in dollars (e.g. 25000).
    :param annual_rate: Annual interest rate as a percentage (e.g. 5.25 for 5.25%).
    :param term_months: Loan term in months (e.g. 60 for a 5-year loan).
    :return: JSON string with payment breakdown.
    """
    if principal <= 0 or annual_rate <= 0 or term_months <= 0:
        return json.dumps({"error": "All values must be positive numbers"})

    monthly_rate = annual_rate / 100 / 12
    monthly_payment = (
        principal
        * (monthly_rate * (1 + monthly_rate) ** term_months)
        / ((1 + monthly_rate) ** term_months - 1)
    )
    total_cost = monthly_payment * term_months
    total_interest = total_cost - principal

    return json.dumps({
        "monthlyPayment": round(monthly_payment, 2),
        "totalInterest": round(total_interest, 2),
        "totalCost": round(total_cost, 2),
        "principal": principal,
        "annualRate": annual_rate,
        "termMonths": term_months,
    }, indent=2)
