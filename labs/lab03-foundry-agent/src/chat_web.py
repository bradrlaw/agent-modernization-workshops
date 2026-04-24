"""
Flask web chat interface for the Virtual Banking Assistant.

Provides a simple HTTP API and HTML frontend for chatting with the agent.

Usage:
    cd src/
    python chat_web.py

Then open http://localhost:5000 in your browser.
"""

import os
from dotenv import load_dotenv
from flask import Flask, request, jsonify, send_from_directory

from agent import get_project_client, build_system_message, chat_completion
from tools import CUSTOMERS

load_dotenv()

app = Flask(__name__, static_folder="../web")

# In-memory session state (single-user demo)
_state = {
    "openai_client": None,
    "model": None,
    "messages": None,
    "customer_id": None,
}


def _ensure_initialized(customer_id: str):
    """Lazily initialize the OpenAI client and conversation."""
    if _state["openai_client"] and _state["customer_id"] == customer_id:
        return

    model = os.environ.get("MODEL_DEPLOYMENT_NAME", "gpt-4o")
    project_client = get_project_client()
    openai_client = project_client.get_openai_client()

    _state["openai_client"] = openai_client
    _state["model"] = model
    _state["messages"] = [build_system_message(customer_id)]
    _state["customer_id"] = customer_id


def _reset():
    """Reset session state."""
    _state.update({
        "openai_client": None,
        "model": None,
        "messages": None,
        "customer_id": None,
    })


@app.route("/")
def index():
    return send_from_directory(app.static_folder, "index.html")


@app.route("/api/customers", methods=["GET"])
def api_customers():
    return jsonify([
        {"customerId": c["customerId"], "name": f"{c['firstName']} {c['lastName']}"}
        for c in CUSTOMERS
    ])


@app.route("/api/chat", methods=["POST"])
def api_chat():
    data = request.json
    customer_id = data.get("customerId")
    message = data.get("message", "").strip()

    if not customer_id or not message:
        return jsonify({"error": "customerId and message are required"}), 400

    _ensure_initialized(customer_id)

    _state["messages"].append({"role": "user", "content": message})

    try:
        response = chat_completion(
            _state["openai_client"],
            _state["model"],
            _state["messages"],
        )
        return jsonify({"response": response})
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/api/reset", methods=["POST"])
def api_reset():
    _reset()
    return jsonify({"status": "session reset"})


if __name__ == "__main__":
    print("Starting Virtual Banking Assistant Web UI...")
    print("Open http://localhost:5000 in your browser\n")
    app.run(host="0.0.0.0", port=5000, debug=False)
