using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BankingAssistant.Clu;

public sealed record CluResult(string Intent, double Confidence, Dictionary<string, string> Entities);
public sealed record ToolRoutingResult(string ToolName, Dictionary<string, object?> Args);

public sealed class CluClient(HttpClient httpClient, string endpoint, string apiKey, string projectName, string deploymentName)
{
    private static readonly Dictionary<string, string> AccountTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["checking"] = "Checking",
        ["savings"] = "Savings",
        ["certificate"] = "Certificate",
        ["cd"] = "Certificate",
    };

    private static readonly Dictionary<string, string> ProductTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["auto"] = "auto",
        ["car"] = "auto",
        ["vehicle"] = "auto",
        ["home"] = "home",
        ["mortgage"] = "home",
        ["house"] = "home",
        ["personal"] = "personal",
    };

    public async Task<CluResult> GetPredictionAsync(string utterance, CancellationToken cancellationToken = default)
    {
        var url = $"{endpoint.TrimEnd('/')}/language/:analyze-conversations?api-version=2022-10-01-preview";
        var body = new
        {
            kind = "Conversation",
            analysisInput = new
            {
                conversationItem = new
                {
                    id = "1",
                    text = utterance,
                    modality = "text",
                    participantId = "user",
                },
            },
            parameters = new
            {
                projectName,
                deploymentName,
                stringIndexType = "TextElement_V8",
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var prediction = document.RootElement.GetProperty("result").GetProperty("prediction");
        var topIntent = prediction.GetProperty("topIntent").GetString() ?? "None";

        var confidence = 0d;
        foreach (var intent in prediction.GetProperty("intents").EnumerateArray())
        {
            if (string.Equals(intent.GetProperty("category").GetString(), topIntent, StringComparison.Ordinal))
            {
                confidence = intent.GetProperty("confidenceScore").GetDouble();
                break;
            }
        }

        var entities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entity in prediction.GetProperty("entities").EnumerateArray())
        {
            var category = entity.GetProperty("category").GetString();
            var text = entity.GetProperty("text").GetString();
            if (!string.IsNullOrWhiteSpace(category) && text is not null)
            {
                entities[category] = text;
            }
        }

        return new CluResult(topIntent, confidence, entities);
    }

    public ToolRoutingResult? ResolveToolArgs(CluResult cluResult, string customerId)
    {
        var entities = cluResult.Entities;

        return cluResult.Intent switch
        {
            "GetAccountBalance" => new ToolRoutingResult("get_account_balance", new Dictionary<string, object?>
            {
                ["customerId"] = customerId,
                ["_account_type"] = entities.TryGetValue("AccountType", out var accountType) ? AccountTypeMap.GetValueOrDefault(accountType, accountType) : null,
                ["_account_ref"] = entities.TryGetValue("AccountReference", out var accountReference) ? accountReference : null,
            }),
            "GetRecentTransactions" => new ToolRoutingResult("get_recent_transactions", new Dictionary<string, object?>
            {
                ["customerId"] = customerId,
                ["limit"] = entities.TryGetValue("TransactionLimit", out var limitText) && int.TryParse(limitText, out var limit) ? limit : null,
                ["_account_type"] = entities.TryGetValue("AccountType", out var transactionType) ? AccountTypeMap.GetValueOrDefault(transactionType, transactionType) : null,
            }),
            "ListAccounts" => new ToolRoutingResult("list_accounts", new Dictionary<string, object?> { ["customerId"] = customerId }),
            "GetCustomerProfile" => new ToolRoutingResult("get_customer_profile", new Dictionary<string, object?> { ["customerId"] = customerId }),
            "GetLoanRates" => new ToolRoutingResult("get_loan_rates", new Dictionary<string, object?>
            {
                ["productType"] = entities.TryGetValue("ProductType", out var productType) && ProductTypeMap.TryGetValue(productType, out var mappedProduct)
                    ? mappedProduct
                    : null,
            }),
            "CalculateLoanPayment" => new ToolRoutingResult("calculate_loan_payment", new Dictionary<string, object?>
            {
                ["principal"] = entities.TryGetValue("LoanAmount", out var loanAmount) ? ParseCurrency(loanAmount) : null,
                ["annualRate"] = entities.TryGetValue("InterestRate", out var interestRate) ? ParseRate(interestRate) : null,
                ["termMonths"] = entities.TryGetValue("LoanTerm", out var loanTerm) ? ParseTerm(loanTerm) : null,
            }),
            "SearchFAQ" => new ToolRoutingResult("search_faq", new Dictionary<string, object?>
            {
                ["query"] = entities.TryGetValue("_utterance", out var utterance) ? utterance : string.Empty,
            }),
            _ => null,
        };
    }

    private static double? ParseCurrency(string text)
    {
        var cleaned = Regex.Replace(text, "[,$]", string.Empty);
        return double.TryParse(cleaned, out var value) ? value : null;
    }

    private static double? ParseRate(string text)
    {
        var cleaned = text.Replace("%", string.Empty).Trim();
        return double.TryParse(cleaned, out var value) ? value : null;
    }

    private static int? ParseTerm(string text)
    {
        var monthsMatch = Regex.Match(text, "(\\d+)\\s*month", RegexOptions.IgnoreCase);
        if (monthsMatch.Success)
        {
            return int.Parse(monthsMatch.Groups[1].Value);
        }

        var yearsMatch = Regex.Match(text, "(\\d+)\\s*year", RegexOptions.IgnoreCase);
        if (yearsMatch.Success)
        {
            return int.Parse(yearsMatch.Groups[1].Value) * 12;
        }

        var numberMatch = Regex.Match(text, "(\\d+)");
        if (!numberMatch.Success)
        {
            return null;
        }

        var value = int.Parse(numberMatch.Groups[1].Value);
        return value <= 30 ? value * 12 : value;
    }
}
