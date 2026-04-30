"""
Unit tests for the Virtual Banking Assistant function tools.

Run with: python -m pytest tests/ -v
"""

import json
import sys
from pathlib import Path

# Add src/ to path so we can import tools
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from tools import (
    get_account_balance,
    get_recent_transactions,
    list_accounts,
    get_customer_profile,
    get_loan_rates,
    calculate_loan_payment,
    search_faq,
)


class TestGetAccountBalance:
    def test_returns_all_accounts_for_customer(self):
        result = json.loads(get_account_balance("CUST-1001"))
        assert "balances" in result
        assert len(result["balances"]) == 3  # Checking, Savings, Certificate

    def test_returns_specific_account(self):
        result = json.loads(get_account_balance("CUST-1001", "ACCT-4521"))
        assert len(result["balances"]) == 1
        assert result["balances"][0]["accountId"] == "ACCT-4521"
        assert result["balances"][0]["currentBalance"] == 3842.56

    def test_unknown_customer_returns_error(self):
        result = json.loads(get_account_balance("CUST-9999"))
        assert "error" in result

    def test_unknown_account_returns_error(self):
        result = json.loads(get_account_balance("CUST-1001", "ACCT-0000"))
        assert "error" in result


class TestGetRecentTransactions:
    def test_returns_transactions_for_customer(self):
        result = json.loads(get_recent_transactions("CUST-1001"))
        assert "transactions" in result
        assert len(result["transactions"]) <= 5

    def test_returns_transactions_for_specific_account(self):
        result = json.loads(get_recent_transactions("CUST-1001", "ACCT-4521", limit=3))
        assert len(result["transactions"]) == 3
        for txn in result["transactions"]:
            assert txn["accountId"] == "ACCT-4521"

    def test_transactions_sorted_by_date_descending(self):
        result = json.loads(get_recent_transactions("CUST-1001", "ACCT-4521", limit=10))
        dates = [t["date"] for t in result["transactions"]]
        assert dates == sorted(dates, reverse=True)

    def test_unknown_customer_returns_error(self):
        result = json.loads(get_recent_transactions("CUST-9999"))
        assert "error" in result


class TestListAccounts:
    def test_lists_all_customer_accounts(self):
        result = json.loads(list_accounts("CUST-1001"))
        assert "accounts" in result
        assert len(result["accounts"]) == 3
        assert result["totalBalance"] > 0

    def test_total_balance_is_sum(self):
        result = json.loads(list_accounts("CUST-1001"))
        expected = sum(a["currentBalance"] for a in result["accounts"])
        assert result["totalBalance"] == expected

    def test_unknown_customer_returns_error(self):
        result = json.loads(list_accounts("CUST-9999"))
        assert "error" in result


class TestGetCustomerProfile:
    def test_returns_profile(self):
        result = json.loads(get_customer_profile("CUST-1001"))
        assert result["name"] == "Alex Morgan"
        assert result["email"] == "alex.morgan@example.com"
        assert "address" in result

    def test_unknown_customer_returns_error(self):
        result = json.loads(get_customer_profile("CUST-9999"))
        assert "error" in result


class TestGetLoanRates:
    def test_returns_all_rates(self):
        result = json.loads(get_loan_rates())
        assert "rates" in result
        assert len(result["rates"]) == 6

    def test_filter_by_product(self):
        result = json.loads(get_loan_rates("auto"))
        assert all(r["product"] == "auto" for r in result["rates"])
        assert len(result["rates"]) == 2

    def test_unknown_product_returns_error(self):
        result = json.loads(get_loan_rates("boat"))
        assert "error" in result


class TestCalculateLoanPayment:
    def test_basic_calculation(self):
        result = json.loads(calculate_loan_payment(25000, 5.0, 60))
        assert result["monthlyPayment"] == 471.78
        assert result["principal"] == 25000
        assert result["totalCost"] > 25000

    def test_total_interest_plus_principal_equals_total_cost(self):
        result = json.loads(calculate_loan_payment(10000, 6.0, 36))
        assert result["totalCost"] == round(result["principal"] + result["totalInterest"], 2)

    def test_invalid_values_return_error(self):
        result = json.loads(calculate_loan_payment(0, 5.0, 60))
        assert "error" in result
        result = json.loads(calculate_loan_payment(10000, -1, 60))
        assert "error" in result


class TestSearchFaq:
    def test_finds_branch_hours(self):
        result = json.loads(search_faq("branch hours"))
        assert "faq_results" in result
        assert any("branch" in r["question"].lower() for r in result["faq_results"])

    def test_finds_lost_card(self):
        result = json.loads(search_faq("lost stolen card"))
        assert "faq_results" in result
        assert any("lost" in r["question"].lower() for r in result["faq_results"])

    def test_finds_atm_limit(self):
        result = json.loads(search_faq("ATM withdrawal limit"))
        assert "faq_results" in result
        assert any("ATM" in r["question"] for r in result["faq_results"])

    def test_no_match_returns_message(self):
        result = json.loads(search_faq("cryptocurrency"))
        assert "message" in result
