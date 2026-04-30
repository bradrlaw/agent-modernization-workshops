using System.Text.Json;
using BankingAssistant.Clu;
using BankingAssistant.Shared.Configuration;
using BankingAssistant.Shared.Data;
using BankingAssistant.Shared.Tools;

const double ConfidenceThreshold = 0.65;

try
{
    AppSupport.LoadDotEnv();
    var endpoint = Environment.GetEnvironmentVariable("CLU_ENDPOINT");
    var apiKey = Environment.GetEnvironmentVariable("CLU_API_KEY");
    var projectName = Environment.GetEnvironmentVariable("CLU_PROJECT_NAME") ?? "banking-assistant";
    var deploymentName = Environment.GetEnvironmentVariable("CLU_DEPLOYMENT_NAME") ?? "production";

    if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "CLU_ENDPOINT and CLU_API_KEY environment variables are required. Create an Azure AI Language resource and deploy a CLU project.");
    }

    var customerId = SelectCustomer();
    using var httpClient = new HttpClient();
    var cluClient = new CluClient(httpClient, endpoint, apiKey, projectName, deploymentName);

    Console.WriteLine("\nConnecting to CLU endpoint...");
    await cluClient.GetPredictionAsync("hello");
    Console.WriteLine($"✓ Connected to CLU (project: {projectName})");
    Console.WriteLine("\nType your message (or 'quit' to exit):");
    Console.WriteLine("Note: CLU mode handles one request per turn (no multi-turn context).");
    Console.WriteLine(new string('-', 50));

    while (true)
    {
        Console.Write("\nYou: ");
        var userInput = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userInput))
        {
            continue;
        }

        if (userInput.Equals("quit", StringComparison.OrdinalIgnoreCase)
            || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase)
            || userInput.Equals("q", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        var cluResult = await cluClient.GetPredictionAsync(userInput);
        Console.WriteLine($"  [CLU] Intent: {cluResult.Intent} ({cluResult.Confidence:P0})");
        if (cluResult.Entities.Count > 0)
        {
            Console.WriteLine($"  [CLU] Entities: {JsonSerializer.Serialize(cluResult.Entities)}");
        }

        if (cluResult.Confidence < ConfidenceThreshold)
        {
            Console.WriteLine("\nAssistant: I'm not sure I understood that. Could you rephrase?\n  I can help with: balances, transactions, accounts, profile,\n  loan rates, loan calculations, and general banking questions.");
            continue;
        }

        if (string.Equals(cluResult.Intent, "None", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("\nAssistant: I'm a banking assistant. I can help you with:\n  • Account balances and transactions\n  • Account summaries and profile info\n  • Loan rates and payment calculations\n  • General banking questions (hours, policies, etc.)");
            continue;
        }

        cluResult.Entities["_utterance"] = userInput;
        var routing = cluClient.ResolveToolArgs(cluResult, customerId);
        if (routing is null)
        {
            Console.WriteLine("\nAssistant: I couldn't determine how to help with that. Please try again.");
            continue;
        }

        if (routing.Args.TryGetValue("_account_type", out var accountType) && accountType is string)
        {
            var resolved = ResolveAccountId(customerId, accountType: accountType as string);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                routing.Args["accountId"] = resolved;
            }
        }

        if (routing.Args.TryGetValue("_account_ref", out var accountRef) && accountRef is string)
        {
            var resolved = ResolveAccountId(customerId, accountRef: accountRef as string);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                routing.Args["accountId"] = resolved;
            }
        }

        routing.Args.Remove("_account_type");
        routing.Args.Remove("_account_ref");

        if (routing.ToolName == "calculate_loan_payment")
        {
            var missing = new List<string>();
            if (routing.Args.GetValueOrDefault("principal") is not double)
            {
                missing.Add("loan amount (e.g., $25,000)");
            }
            if (routing.Args.GetValueOrDefault("annualRate") is not double)
            {
                missing.Add("interest rate (e.g., 4.99%)");
            }
            if (routing.Args.GetValueOrDefault("termMonths") is not int)
            {
                missing.Add("loan term (e.g., 60 months)");
            }
            if (missing.Count > 0)
            {
                Console.WriteLine($"\nAssistant: I need a bit more information to calculate your payment.\n  Please include: {string.Join(", ", missing)}");
                continue;
            }
        }

        Console.WriteLine($"  [Route] → {routing.ToolName}({JsonSerializer.Serialize(routing.Args.Where(kvp => kvp.Value is not null).ToDictionary())})");
        var result = routing.ToolName switch
        {
            "get_account_balance" => BankingTools.GetAccountBalance(customerId, routing.Args.GetValueOrDefault("accountId") as string),
            "get_recent_transactions" => BankingTools.GetRecentTransactions(customerId, routing.Args.GetValueOrDefault("accountId") as string, routing.Args.GetValueOrDefault("limit") as int? ?? 5),
            "list_accounts" => BankingTools.ListAccounts(customerId),
            "get_customer_profile" => BankingTools.GetCustomerProfile(customerId),
            "get_loan_rates" => BankingTools.GetLoanRates(routing.Args.GetValueOrDefault("productType") as string),
            "calculate_loan_payment" => BankingTools.CalculateLoanPayment((double)routing.Args["principal"]!, (double)routing.Args["annualRate"]!, (int)routing.Args["termMonths"]!),
            "search_faq" => BankingTools.SearchFaq(routing.Args.GetValueOrDefault("query") as string ?? string.Empty),
            _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {routing.ToolName}" }),
        };

        Console.WriteLine($"\nAssistant: {FormatResponse(routing.ToolName, result)}");
    }

    Console.WriteLine("\nGoodbye!");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.ExitCode = 1;
}

static string SelectCustomer()
{
    Console.WriteLine("\n╔══════════════════════════════════════════════╗");
    Console.WriteLine("║   Virtual Banking Assistant — CLU Mode       ║");
    Console.WriteLine("║   (No LLM — Deterministic Intent Routing)    ║");
    Console.WriteLine("╚══════════════════════════════════════════════╝\n");
    Console.WriteLine("Select a demo customer for this session:\n");

    for (var i = 0; i < MockDataStore.Customers.Count; i++)
    {
        var customer = MockDataStore.Customers[i];
        Console.WriteLine($"  {i + 1}. {customer.FirstName} {customer.LastName} ({customer.CustomerId})");
    }

    Console.WriteLine();
    while (true)
    {
        Console.Write("Enter choice (1-3): ");
        var choice = Console.ReadLine()?.Trim();
        if (int.TryParse(choice, out var index) && index >= 1 && index <= MockDataStore.Customers.Count)
        {
            var selected = MockDataStore.Customers[index - 1];
            Console.WriteLine($"\n✓ Logged in as {selected.FirstName} {selected.LastName}");
            return selected.CustomerId;
        }

        Console.WriteLine("  Invalid choice. Enter 1, 2, or 3.");
    }
}

static string? ResolveAccountId(string customerId, string? accountType = null, string? accountRef = null)
{
    var customerAccounts = MockDataStore.Accounts.Where(account => account.CustomerId == customerId).ToList();

    if (!string.IsNullOrWhiteSpace(accountRef))
    {
        foreach (var account in customerAccounts)
        {
            if (account.Last4 == accountRef)
            {
                return account.AccountId;
            }
        }
    }

    if (!string.IsNullOrWhiteSpace(accountType))
    {
        foreach (var account in customerAccounts)
        {
            if (string.Equals(account.Type, accountType, StringComparison.OrdinalIgnoreCase))
            {
                return account.AccountId;
            }
        }
    }

    return null;
}

static string FormatResponse(string toolName, string resultJson)
{
    using var document = JsonDocument.Parse(resultJson);
    var root = document.RootElement;

    if (root.TryGetProperty("error", out var errorElement))
    {
        return $"⚠️  {errorElement.GetString()}";
    }

    if (toolName == "get_account_balance")
    {
        var lines = new List<string> { "Here are your account balances:\n" };
        foreach (var account in root.GetProperty("balances").EnumerateArray())
        {
            lines.Add(
                $"  • {account.GetProperty("nickname").GetString()} ({account.GetProperty("type").GetString()}, ending {account.GetProperty("last4").GetString()})\n" +
                $"    Current: ${account.GetProperty("currentBalance").GetDecimal():N2} | Available: ${account.GetProperty("availableBalance").GetDecimal():N2}");
        }
        return string.Join("\n", lines);
    }

    if (toolName == "get_recent_transactions")
    {
        var lines = new List<string> { "Recent transactions:\n" };
        foreach (var transaction in root.GetProperty("transactions").EnumerateArray())
        {
            var type = transaction.GetProperty("type").GetString();
            var sign = type is "debit" or "withdrawal" or "payment" ? "-" : "+";
            lines.Add($"  {transaction.GetProperty("date").GetString()}  {sign}${Math.Abs(transaction.GetProperty("amount").GetDecimal()):N2}  {transaction.GetProperty("description").GetString()}");
        }
        return string.Join("\n", lines);
    }

    if (toolName == "list_accounts")
    {
        var accounts = root.GetProperty("accounts").EnumerateArray().ToList();
        var lines = new List<string> { $"You have {accounts.Count} accounts (total: ${root.GetProperty("totalBalance").GetDecimal():N2}):\n" };
        lines.AddRange(accounts.Select(account =>
            $"  • {account.GetProperty("nickname").GetString()} ({account.GetProperty("type").GetString()}, ending {account.GetProperty("last4").GetString()}) — ${account.GetProperty("currentBalance").GetDecimal():N2}"));
        return string.Join("\n", lines);
    }

    if (toolName == "get_customer_profile")
    {
        return "Profile information:\n"
               + $"  Name: {root.GetProperty("name").GetString()}\n"
               + $"  Email: {root.GetProperty("email").GetString()}\n"
               + $"  Phone: {root.GetProperty("phone").GetString()}\n"
               + $"  Address: {root.GetProperty("address").GetString()}\n"
               + $"  Member since: {root.GetProperty("memberSince").GetString()}";
    }

    if (toolName == "get_loan_rates")
    {
        var lines = new List<string> { "Current loan rates:\n" };
        foreach (var rate in root.GetProperty("rates").EnumerateArray())
        {
            lines.Add($"  • {rate.GetProperty("product").GetString()!.Replace(rate.GetProperty("product").GetString()![0].ToString(), rate.GetProperty("product").GetString()![0].ToString().ToUpper())} ({rate.GetProperty("term").GetString()}): {rate.GetProperty("apr").GetDecimal()}% APR");
        }
        return string.Join("\n", lines);
    }

    if (toolName == "calculate_loan_payment")
    {
        return "Loan payment estimate:\n"
               + $"  Principal: ${root.GetProperty("principal").GetDouble():N2}\n"
               + $"  Rate: {root.GetProperty("annualRate").GetDouble()}% APR for {root.GetProperty("termMonths").GetInt32()} months\n"
               + $"  Monthly Payment: ${root.GetProperty("monthlyPayment").GetDouble():N2}\n"
               + $"  Total Interest: ${root.GetProperty("totalInterest").GetDouble():N2}\n"
               + $"  Total Cost: ${root.GetProperty("totalCost").GetDouble():N2}";
    }

    if (toolName == "search_faq")
    {
        if (root.TryGetProperty("faqResults", out var faqResults))
        {
            var lines = new List<string>();
            foreach (var faq in faqResults.EnumerateArray())
            {
                lines.Add($"  Q: {faq.GetProperty("question").GetString()}\n  A: {faq.GetProperty("answer").GetString()}\n");
            }
            return string.Join("\n", lines);
        }

        return root.TryGetProperty("message", out var message) ? message.GetString() ?? "No information found." : "No information found.";
    }

    return resultJson;
}
