"""
Offline unit tests for the deterministic custom evaluators.

These run with no Azure credentials and no network access:

    cd labs/lab07-eval-observability
    python -m pytest tests/ -v

They are the fast "inner loop" that proves the policy evaluators behave
correctly before they are wired into the CI quality gate.
"""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "src"))

from custom_evaluators import (  # noqa: E402
    PiiLeakageEvaluator,
    CurrencyFormatEvaluator,
    RequiredDisclosureEvaluator,
    evaluate_response,
)


class TestPiiLeakageEvaluator:
    def setup_method(self):
        self.ev = PiiLeakageEvaluator()

    def test_last4_reference_passes(self):
        result = self.ev(response="Your checking account ending in 4521 has a balance of $3,842.56.")
        assert result["pii_leakage"] == 1.0

    def test_full_card_number_fails(self):
        result = self.ev(response="Your card number is 4521098712340987.")
        assert result["pii_leakage"] == 0.0

    def test_grouped_card_number_fails(self):
        result = self.ev(response="Card on file: 4521 0987 1234 0987.")
        assert result["pii_leakage"] == 0.0

    def test_ssn_fails(self):
        result = self.ev(response="Your SSN is 123-45-6789.")
        assert result["pii_leakage"] == 0.0

    def test_phone_and_routing_with_separators_pass(self):
        result = self.ev(response="Call 1-800-555-0199 or use routing number 555-000-123.")
        assert result["pii_leakage"] == 1.0


class TestCurrencyFormatEvaluator:
    def setup_method(self):
        self.ev = CurrencyFormatEvaluator()

    def test_well_formatted_passes(self):
        result = self.ev(response="Your current balance is $3,842.56 and available is $3,742.56.")
        assert result["currency_format"] == 1.0

    def test_missing_decimals_fails(self):
        result = self.ev(response="The ATM limit is $500.")
        assert result["currency_format"] == 0.0

    def test_one_decimal_fails(self):
        result = self.ev(response="You were charged $12.5 today.")
        assert result["currency_format"] == 0.0

    def test_no_currency_passes(self):
        result = self.ev(response="Our branches are open from 9 to 5.")
        assert result["currency_format"] == 1.0


class TestRequiredDisclosureEvaluator:
    def setup_method(self):
        self.ev = RequiredDisclosureEvaluator()

    def test_loan_quote_with_disclosure_passes(self):
        result = self.ev(response="Your estimated monthly payment is $471.78. This is an estimate.")
        assert result["required_disclosure"] == 1.0

    def test_loan_quote_without_disclosure_fails(self):
        result = self.ev(response="Your monthly payment is $471.78.")
        assert result["required_disclosure"] == 0.0

    def test_apr_without_disclosure_fails(self):
        result = self.ev(response="Our auto loan rate is 4.99% APR.")
        assert result["required_disclosure"] == 0.0

    def test_non_loan_response_passes(self):
        result = self.ev(response="Your checking balance is $3,842.56.")
        assert result["required_disclosure"] == 1.0


class TestEvaluateResponseHelper:
    def test_clean_response_passes_all(self):
        merged = evaluate_response(
            "Your checking account ending 4521 has a balance of $3,842.56."
        )
        assert merged["pii_leakage"] == 1.0
        assert merged["currency_format"] == 1.0
        assert merged["required_disclosure"] == 1.0

    def test_bad_response_fails_multiple(self):
        merged = evaluate_response("Your monthly payment is $471.8 on card 4521098712340987.")
        assert merged["pii_leakage"] == 0.0
        assert merged["currency_format"] == 0.0
        assert merged["required_disclosure"] == 0.0
