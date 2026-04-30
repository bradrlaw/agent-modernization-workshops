using System.Text.Json;
using OpenAI.Chat;

namespace BankingAssistant.Shared.Agent;

public static class ToolDefinitions
{
    public static IReadOnlyList<ChatTool> AllTools { get; } =
    [
        ChatTool.CreateFunctionTool(
            functionName: "get_account_balance",
            functionDescription: "Retrieves the current and available balance for a customer's account(s).",
            functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "customer_id": { "type": "string", "description": "Customer identifier, e.g. CUST-1001" },
                "account_id": { "type": "string", "description": "Optional account identifier, e.g. ACCT-4521" }
              },
              "required": ["customer_id"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            functionName: "get_recent_transactions",
            functionDescription: "Retrieves recent transactions for a customer's account(s).",
            functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "customer_id": { "type": "string", "description": "Customer identifier" },
                "account_id": { "type": "string", "description": "Optional account identifier" },
                "limit": { "type": "integer", "description": "Max transactions to return (default 5)" }
              },
              "required": ["customer_id"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            functionName: "list_accounts",
            functionDescription: "Lists all accounts belonging to a customer with summary information.",
            functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "customer_id": { "type": "string", "description": "Customer identifier" }
              },
              "required": ["customer_id"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            functionName: "get_customer_profile",
            functionDescription: "Retrieves profile information (name, email, phone, address) for a customer.",
            functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "customer_id": { "type": "string", "description": "Customer identifier" }
              },
              "required": ["customer_id"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            functionName: "get_loan_rates",
            functionDescription: "Retrieves current loan interest rates. Can filter by product type.",
            functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "product_type": { "type": "string", "description": "Loan product filter: auto, home, or personal" }
              },
              "required": []
            }
            """)),
        ChatTool.CreateFunctionTool(
            functionName: "calculate_loan_payment",
            functionDescription: "Calculates monthly payment, total interest, and total cost for a loan.",
            functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "principal": { "type": "number", "description": "Loan amount in dollars" },
                "annual_rate": { "type": "number", "description": "Annual interest rate as a percentage (e.g. 5.25)" },
                "term_months": { "type": "integer", "description": "Loan term in months" }
              },
              "required": ["principal", "annual_rate", "term_months"]
            }
            """)),
        ChatTool.CreateFunctionTool(
            functionName: "search_faq",
            functionDescription: "Searches the banking FAQ knowledge base for answers to common questions about hours, policies, limits, cards, direct deposit, and disputes.",
            functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "query": { "type": "string", "description": "The user's question or search keywords" }
              },
              "required": ["query"]
            }
            """))
    ];
}
