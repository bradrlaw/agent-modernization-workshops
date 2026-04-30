"""
CLU (Conversational Language Understanding) client for the Virtual Banking Assistant.

Calls the Azure AI Language CLU endpoint for intent detection and entity extraction,
providing a deterministic (no LLM) routing alternative.

Requires:
    - CLU_ENDPOINT: Azure AI Language resource endpoint
    - CLU_API_KEY: Azure AI Language resource key
    - CLU_PROJECT_NAME: CLU project name (default: banking-assistant)
    - CLU_DEPLOYMENT_NAME: CLU deployment name (default: production)
"""

import os
import re
import requests
from dataclasses import dataclass, field
from typing import Optional


@dataclass
class CLUResult:
    """Structured result from a CLU prediction."""
    intent: str
    confidence: float
    entities: dict = field(default_factory=dict)


# Maps natural language account types to account type filters
ACCOUNT_TYPE_MAP = {
    "checking": "Checking",
    "savings": "Savings",
    "certificate": "Certificate",
    "cd": "Certificate",
}

# Maps natural language product types to tool parameter values
PRODUCT_TYPE_MAP = {
    "auto": "auto",
    "car": "auto",
    "vehicle": "auto",
    "home": "home",
    "mortgage": "home",
    "house": "home",
    "personal": "personal",
}


def get_clu_prediction(utterance: str) -> CLUResult:
    """
    Sends an utterance to the CLU endpoint and returns structured intent + entities.

    Returns:
        CLUResult with intent name, confidence score, and extracted entities.
    """
    endpoint = os.environ.get("CLU_ENDPOINT")
    api_key = os.environ.get("CLU_API_KEY")
    project_name = os.environ.get("CLU_PROJECT_NAME", "banking-assistant")
    deployment_name = os.environ.get("CLU_DEPLOYMENT_NAME", "production")

    if not endpoint or not api_key:
        raise EnvironmentError(
            "CLU_ENDPOINT and CLU_API_KEY environment variables are required. "
            "Create an Azure AI Language resource and deploy a CLU project."
        )

    url = (
        f"{endpoint.rstrip('/')}/language/:analyze-conversations"
        f"?api-version=2022-10-01-preview"
    )

    body = {
        "kind": "Conversation",
        "analysisInput": {
            "conversationItem": {
                "id": "1",
                "text": utterance,
                "modality": "text",
                "participantId": "user",
            }
        },
        "parameters": {
            "projectName": project_name,
            "deploymentName": deployment_name,
            "stringIndexType": "TextElement_V8",
        },
    }

    headers = {
        "Ocp-Apim-Subscription-Key": api_key,
        "Content-Type": "application/json",
    }

    response = requests.post(url, json=body, headers=headers)
    response.raise_for_status()
    result = response.json()

    # Parse the prediction
    prediction = result["result"]["prediction"]
    top_intent = prediction["topIntent"]
    confidence = 0.0

    for intent in prediction.get("intents", []):
        if intent["category"] == top_intent:
            confidence = intent["confidenceScore"]
            break

    # Extract entities into a dictionary
    entities = {}
    for entity in prediction.get("entities", []):
        category = entity["category"]
        text = entity["text"]
        entities[category] = text

    return CLUResult(intent=top_intent, confidence=confidence, entities=entities)


def resolve_tool_args(clu_result: CLUResult, customer_id: str) -> dict:
    """
    Converts CLU intent + entities into tool function arguments.

    The customer_id always comes from the session (never from user utterance)
    for security.

    Returns:
        Dictionary with 'tool_name' and 'args' keys, or None if intent is unclear.
    """
    intent = clu_result.intent
    entities = clu_result.entities

    if intent == "GetAccountBalance":
        args = {"customer_id": customer_id}
        # Try to resolve account type to a specific account
        if "AccountType" in entities:
            args["_account_type"] = ACCOUNT_TYPE_MAP.get(
                entities["AccountType"].lower(), entities["AccountType"]
            )
        if "AccountReference" in entities:
            args["_account_ref"] = entities["AccountReference"]
        return {"tool_name": "get_account_balance", "args": args}

    elif intent == "GetRecentTransactions":
        args = {"customer_id": customer_id}
        if "TransactionLimit" in entities:
            try:
                args["limit"] = int(entities["TransactionLimit"])
            except ValueError:
                pass
        if "AccountType" in entities:
            args["_account_type"] = ACCOUNT_TYPE_MAP.get(
                entities["AccountType"].lower(), entities["AccountType"]
            )
        return {"tool_name": "get_recent_transactions", "args": args}

    elif intent == "ListAccounts":
        return {"tool_name": "list_accounts", "args": {"customer_id": customer_id}}

    elif intent == "GetCustomerProfile":
        return {"tool_name": "get_customer_profile", "args": {"customer_id": customer_id}}

    elif intent == "GetLoanRates":
        args = {}
        if "ProductType" in entities:
            mapped = PRODUCT_TYPE_MAP.get(entities["ProductType"].lower())
            if mapped:
                args["product_type"] = mapped
        return {"tool_name": "get_loan_rates", "args": args}

    elif intent == "CalculateLoanPayment":
        args = {}
        if "LoanAmount" in entities:
            args["principal"] = _parse_currency(entities["LoanAmount"])
        if "InterestRate" in entities:
            args["annual_rate"] = _parse_rate(entities["InterestRate"])
        if "LoanTerm" in entities:
            args["term_months"] = _parse_term(entities["LoanTerm"])
        return {"tool_name": "calculate_loan_payment", "args": args}

    elif intent == "SearchFAQ":
        # Pass the full user utterance as the search query
        return {"tool_name": "search_faq", "args": {"query": clu_result.entities.get("_utterance", "")}}

    return None


def _parse_currency(text: str) -> Optional[float]:
    """Extracts a numeric value from currency text like '$25,000' or '30000'."""
    cleaned = re.sub(r"[,$]", "", text)
    try:
        return float(cleaned)
    except ValueError:
        return None


def _parse_rate(text: str) -> Optional[float]:
    """Extracts a rate from text like '4.99%' or '5.25'."""
    cleaned = text.replace("%", "").strip()
    try:
        return float(cleaned)
    except ValueError:
        return None


def _parse_term(text: str) -> Optional[int]:
    """Extracts months from text like '60 months', '5 years', '30 years'."""
    # Try months first
    months_match = re.search(r"(\d+)\s*month", text, re.IGNORECASE)
    if months_match:
        return int(months_match.group(1))

    # Try years
    years_match = re.search(r"(\d+)\s*year", text, re.IGNORECASE)
    if years_match:
        return int(years_match.group(1)) * 12

    # Try bare number (assume months)
    num_match = re.search(r"(\d+)", text)
    if num_match:
        val = int(num_match.group(1))
        return val * 12 if val <= 30 else val  # Heuristic: <=30 likely years

    return None
