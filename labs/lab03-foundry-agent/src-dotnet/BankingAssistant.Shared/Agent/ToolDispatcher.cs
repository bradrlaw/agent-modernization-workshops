using System.Text.Json;
using BankingAssistant.Shared.Tools;

namespace BankingAssistant.Shared.Agent;

public static class ToolDispatcher
{
    public static string Dispatch(string functionName, BinaryData arguments)
    {
        try
        {
            using var document = JsonDocument.Parse(arguments);
            var root = document.RootElement;

            return functionName switch
            {
                "get_account_balance" => BankingTools.GetAccountBalance(
                    GetRequiredString(root, "customer_id"),
                    GetOptionalString(root, "account_id")),
                "get_recent_transactions" => BankingTools.GetRecentTransactions(
                    GetRequiredString(root, "customer_id"),
                    GetOptionalString(root, "account_id"),
                    GetOptionalInt(root, "limit") ?? 5),
                "list_accounts" => BankingTools.ListAccounts(GetRequiredString(root, "customer_id")),
                "get_customer_profile" => BankingTools.GetCustomerProfile(GetRequiredString(root, "customer_id")),
                "get_loan_rates" => BankingTools.GetLoanRates(GetOptionalString(root, "product_type")),
                "calculate_loan_payment" => BankingTools.CalculateLoanPayment(
                    GetRequiredDouble(root, "principal"),
                    GetRequiredDouble(root, "annual_rate"),
                    GetRequiredInt(root, "term_months")),
                "search_faq" => BankingTools.SearchFaq(GetRequiredString(root, "query")),
                _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {functionName}" })
            };
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            return JsonSerializer.Serialize(new { error = $"Invalid arguments for tool {functionName}: {ex.Message}" });
        }
    }

    private static string GetRequiredString(JsonElement root, string name)
        => GetOptionalString(root, name) ?? throw new InvalidOperationException($"Missing required argument '{name}'.");

    private static string? GetOptionalString(JsonElement root, string name)
        => root.TryGetProperty(name, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString()
            : null;

    private static int GetRequiredInt(JsonElement root, string name)
        => GetOptionalInt(root, name) ?? throw new InvalidOperationException($"Missing required argument '{name}'.");

    private static int? GetOptionalInt(JsonElement root, string name)
        => root.TryGetProperty(name, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetInt32()
            : null;

    private static double GetRequiredDouble(JsonElement root, string name)
        => GetOptionalDouble(root, name) ?? throw new InvalidOperationException($"Missing required argument '{name}'.");

    private static double? GetOptionalDouble(JsonElement root, string name)
        => root.TryGetProperty(name, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetDouble()
            : null;
}
