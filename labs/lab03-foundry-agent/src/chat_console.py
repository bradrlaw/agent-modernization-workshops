"""
Interactive console chat client for the Virtual Banking Assistant.

Usage:
    cd src/
    python chat_console.py

Requires:
    - PROJECT_ENDPOINT env var (from AI Foundry project)
    - MODEL_DEPLOYMENT_NAME env var (e.g. "gpt-4o-1")
    - Azure CLI login (az login)
"""

import os
from dotenv import load_dotenv

from agent import get_project_client, build_system_message, chat_completion, TOOL_DEFINITIONS
from tools import CUSTOMERS

load_dotenv()


def select_customer() -> str:
    """Prompts the user to select a demo customer for this session."""
    print("\n╔══════════════════════════════════════════╗")
    print("║   Virtual Banking Assistant — Pro-Code   ║")
    print("╚══════════════════════════════════════════╝\n")
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


def run_chat():
    """Main chat loop using OpenAI function-calling via AI Foundry."""
    model = os.environ.get("MODEL_DEPLOYMENT_NAME", "gpt-4o")

    # Select demo customer
    customer_id = select_customer()

    # Initialize client
    print("\nConnecting to Azure AI Foundry...")
    project_client = get_project_client()
    openai_client = project_client.get_openai_client()
    print("✓ Connected")

    # Start conversation with system message
    messages = [build_system_message(customer_id)]

    print("\nType your message (or 'quit' to exit):\n")
    print("-" * 50)

    try:
        while True:
            user_input = input("\nYou: ").strip()
            if not user_input:
                continue
            if user_input.lower() in ("quit", "exit", "q"):
                break

            messages.append({"role": "user", "content": user_input})

            response = chat_completion(openai_client, model, messages)
            print(f"\nAssistant: {response}")

    except KeyboardInterrupt:
        print("\n\nInterrupted.")

    print("\nGoodbye!")


if __name__ == "__main__":
    run_chat()
