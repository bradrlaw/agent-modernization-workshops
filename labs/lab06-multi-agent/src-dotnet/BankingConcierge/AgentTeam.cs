using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using BankingConcierge.Skills;
using BankingConcierge.Tools;

namespace BankingConcierge;

/// <summary>The five specialist agents, created once and reused by every pattern.</summary>
public sealed record BankingTeam(
    ChatClientAgent Concierge,
    ChatClientAgent Accounts,
    ChatClientAgent Lending,
    ChatClientAgent Cards,
    ChatClientAgent Compliance)
{
    public IReadOnlyList<AIAgent> Specialists => [Accounts, Lending, Cards];
}

/// <summary>
/// Shared Foundry client + specialist-agent factory for Lab 06 (the .NET twin of
/// <c>src/banking_agents.py</c>). Every orchestration pattern builds its workflow from
/// the same team, so only the <see cref="AgentWorkflowBuilder"/> call changes per pattern.
///
/// Verified against the Microsoft Agent Framework samples
/// (github.com/microsoft/agent-framework, dotnet/samples/03-workflows) as of Aug 2026:
///   - client: new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
///   - agent:  aiProjectClient.AsAIAgent(model, instructions, name:, description:, tools:)
/// </summary>
public static class AgentTeam
{
    public static (AIProjectClient Client, string Model) CreateProjectClient()
    {
        LoadDotEnv();

        var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
            ?? Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")
            ?? throw new InvalidOperationException(
                "Set FOUNDRY_PROJECT_ENDPOINT (or PROJECT_ENDPOINT from Lab 03). See .env.example.");

        var model = Environment.GetEnvironmentVariable("FOUNDRY_MODEL")
            ?? Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")
            ?? "gpt-4o";

        var client = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
        return (client, model);
    }

    public static BankingTeam Build(AIProjectClient client, string model, string customerId,
        SkillLibrary? skills = null)
    {
        // Inject the session customer so specialists never ask for an ID or cross customers.
        string sessionNote =
            $"\n\nCurrent session customer ID: {customerId}. Use it for all lookups; never ask "
            + "the customer for it and never reveal another customer's data.";

        // Agent -> Skills: append the versioned SKILL.md rules mapped to each agent (or nothing
        // when skills are disabled). Editing a SKILL.md and re-running changes behavior with no
        // code change — the whole point of a shared skill library.
        string Skilled(string agentName) => sessionNote + (skills?.ComposeFor(agentName) ?? string.Empty);

        var accounts = client.AsAIAgent(
            model: model,
            instructions: "You are the Accounts specialist for a retail bank. You handle balances, "
                + "recent transactions, account lists, and customer profile lookups. Format currency "
                + "as $#,###.##." + Skilled("AccountsAgent"),
            name: "AccountsAgent",
            description: "Balances, transactions, account lists, and profile lookups.",
            tools:
            [
                AIFunctionFactory.Create(BankingTools.GetAccountBalance),
                AIFunctionFactory.Create(BankingTools.GetRecentTransactions),
                AIFunctionFactory.Create(BankingTools.ListAccounts),
                AIFunctionFactory.Create(BankingTools.GetCustomerProfile),
            ]);

        var lending = client.AsAIAgent(
            model: model,
            instructions: "You are the Lending specialist. You look up current loan rates and "
                + "calculate loan payments. Always state the APR and its as-of date." + Skilled("LendingAgent"),
            name: "LendingAgent",
            description: "Loan rates and payment calculations.",
            tools:
            [
                AIFunctionFactory.Create(BankingTools.GetLoanRates),
                AIFunctionFactory.Create(BankingTools.CalculateLoanPayment),
            ]);

        var cards = client.AsAIAgent(
            model: model,
            instructions: "You are the Cards & Fraud specialist. You answer card questions, initiate "
                + "disputes, and handle general banking FAQ." + Skilled("CardsFraudAgent"),
            name: "CardsFraudAgent",
            description: "Card questions, disputes, and general banking FAQ.",
            tools: [AIFunctionFactory.Create(BankingTools.SearchFaq)]);

        var compliance = client.AsAIAgent(
            model: model,
            instructions: "You are the Compliance specialist. You add required disclosures, verify PII "
                + "handling, and ensure responses follow policy. You have no data tools; you review and "
                + "annotate what the other agents produce." + Skilled("ComplianceAgent"),
            name: "ComplianceAgent",
            description: "Adds required disclosures and checks policy compliance.");

        var concierge = client.AsAIAgent(
            model: model,
            instructions: "You are the triage concierge for a retail bank. Read the customer's request "
                + "and hand off to exactly one specialist: AccountsAgent (balances/transactions/profile), "
                + "LendingAgent (rates/payments), or CardsFraudAgent (cards/disputes/FAQ). Do not answer "
                + "domain questions yourself." + Skilled("Concierge"),
            name: "Concierge",
            description: "Routes requests to the right specialist agent.");

        return new BankingTeam(concierge, accounts, lending, cards, compliance);
    }

    private static void LoadDotEnv()
    {
        var envPath = FindFileInParents(".env") ?? FindFileInParents(Path.Combine("src", ".env"));
        if (envPath is null || !File.Exists(envPath))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? FindFileInParents(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
