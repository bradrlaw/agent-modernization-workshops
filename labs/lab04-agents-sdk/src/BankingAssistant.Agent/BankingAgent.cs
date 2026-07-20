using System.Text.RegularExpressions;
using BankingAssistant.Agent.Cards;
using BankingAssistant.Agent.Data;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;

namespace BankingAssistant.Agent;

public class BankingAgent : AgentApplication
{
    private readonly ILogger<BankingAgent> _logger;

    public BankingAgent(AgentApplicationOptions options, ILogger<BankingAgent> logger)
        : base(options)
    {
        _logger = logger;

        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);
        OnActivity(ActivityTypes.Message, OnMessageAsync, rank: RouteRank.Last);
    }

    private async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (var member in turnContext.Activity.MembersAdded ?? [])
        {
            if (member.Id == turnContext.Activity.Recipient.Id)
            {
                continue;
            }

            await turnContext.SendActivityAsync(
                MessageFactory.Text(
                    "Welcome to the Banking Assistant workshop agent. Try 'Show accounts for CUST-1001', 'What is the checking balance for Alex Morgan?', or 'Show recent transactions for CUST-1002'."),
                cancellationToken);
        }
    }

    private async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        var message = turnContext.Activity.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(message))
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Please send a banking question such as 'Show accounts for CUST-1001'."),
                cancellationToken);
            return;
        }

        _logger.LogInformation("Received message: {Message}", message);

        if (ContainsAny(message, "hello", "hi", "help", "menu"))
        {
            await SendHelpAsync(turnContext, cancellationToken);
            return;
        }

        var customer = ResolveCustomer(message);
        if (customer is null)
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text(
                    "Please include one of the workshop demo customers: CUST-1001 (Alex Morgan), CUST-1002 (Jordan Rivera), or CUST-1003 (Taylor Chen)."),
                cancellationToken);
            return;
        }

        var requestedAccount = ResolveAccount(message, customer.CustomerId);

        if (ContainsAny(message, "transactions", "activity", "recent purchases", "history"))
        {
            var transactions = GetTransactions(customer.CustomerId, requestedAccount?.AccountId);
            if (transactions.Count == 0)
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text($"No transactions were found for {customer.FullName}."),
                    cancellationToken);
                return;
            }

            var targetAccount = requestedAccount ?? MockBankingData.GetAccounts(customer.CustomerId).First();
            var card = CreateTransactionsCard(customer, targetAccount, transactions);
            await turnContext.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);
            return;
        }

        if (ContainsAny(message, "balance", "balances"))
        {
            var accounts = GetBalance(customer.CustomerId, requestedAccount?.AccountId);
            if (accounts.Count == 0)
            {
                await turnContext.SendActivityAsync(
                    MessageFactory.Text($"I could not find a matching account for {customer.FullName}."),
                    cancellationToken);
                return;
            }

            var card = AccountCard.CreateAccountsCard(
                customer,
                accounts,
                "Current balances",
                requestedAccount is null
                    ? "Showing all workshop demo balances for this customer."
                    : $"Showing the requested balance for {requestedAccount.Type.ToLowerInvariant()} account.");

            await turnContext.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);
            return;
        }

        if (ContainsAny(message, "accounts", "account list", "what do i have"))
        {
            var accounts = GetAccounts(customer.CustomerId);
            var card = AccountCard.CreateAccountsCard(
                customer,
                accounts,
                "Account summary",
                "These are the local demo accounts available for this workshop customer.");

            await turnContext.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);
            return;
        }

        await turnContext.SendActivityAsync(
            MessageFactory.Text(
                "I don't understand that request yet. Ask for accounts, balances, or transactions for CUST-1001, CUST-1002, or CUST-1003."),
            cancellationToken);
    }

    private static async Task SendHelpAsync(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        const string helpMessage = "I can help with three demo banking actions: show accounts, show balances, and show recent transactions. Example prompts: 'Show accounts for CUST-1001', 'What is the checking balance for Jordan Rivera?', 'Show recent transactions for Taylor Chen'.";
        await turnContext.SendActivityAsync(MessageFactory.Text(helpMessage), cancellationToken);
    }

    private static MockCustomer? ResolveCustomer(string message)
    {
        var customerIdMatch = Regex.Match(message, "CUST-\\d{4}", RegexOptions.IgnoreCase);
        if (customerIdMatch.Success)
        {
            return MockBankingData.GetCustomer(customerIdMatch.Value.ToUpperInvariant());
        }

        return MockBankingData.Customers.FirstOrDefault(customer =>
            message.Contains(customer.FullName, StringComparison.OrdinalIgnoreCase) ||
            message.Contains(customer.FirstName, StringComparison.OrdinalIgnoreCase));
    }

    private static MockAccount? ResolveAccount(string message, string customerId)
    {
        var accountIdMatch = Regex.Match(message, "ACCT-\\d{4}", RegexOptions.IgnoreCase);
        if (accountIdMatch.Success)
        {
            return MockBankingData.GetAccounts(customerId)
                .FirstOrDefault(account => account.AccountId.Equals(accountIdMatch.Value, StringComparison.OrdinalIgnoreCase));
        }

        var accounts = MockBankingData.GetAccounts(customerId);

        if (message.Contains("checking", StringComparison.OrdinalIgnoreCase))
        {
            return accounts.FirstOrDefault(account => account.Type.Equals("Checking", StringComparison.OrdinalIgnoreCase));
        }

        if (message.Contains("savings", StringComparison.OrdinalIgnoreCase))
        {
            return accounts.FirstOrDefault(account => account.Type.Equals("Savings", StringComparison.OrdinalIgnoreCase));
        }

        if (message.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(message, "\\bcd\\b", RegexOptions.IgnoreCase))
        {
            return accounts.FirstOrDefault(account => account.Type.Equals("Certificate", StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private static IReadOnlyList<MockAccount> GetBalance(string customerId, string? accountId = null)
    {
        var accounts = MockBankingData.GetAccounts(customerId);
        return string.IsNullOrWhiteSpace(accountId)
            ? accounts
            : accounts.Where(account => account.AccountId.Equals(accountId, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static IReadOnlyList<MockAccount> GetAccounts(string customerId) => MockBankingData.GetAccounts(customerId);

    private static IReadOnlyList<MockTransaction> GetTransactions(string customerId, string? accountId = null, int limit = 5)
    {
        return MockBankingData.GetTransactions(customerId, accountId, limit);
    }

    private static Attachment CreateTransactionsCard(MockCustomer customer, MockAccount account, IReadOnlyList<MockTransaction> transactions)
    {
        var body = new List<object>
        {
            new
            {
                type = "TextBlock",
                text = "Recent transactions",
                weight = "Bolder",
                size = "Medium"
            },
            new
            {
                type = "TextBlock",
                text = $"{customer.FullName} ({customer.CustomerId})",
                isSubtle = true,
                wrap = true
            },
            new
            {
                type = "TextBlock",
                text = $"{account.Type} - {account.Nickname} (**** {account.Last4})",
                wrap = true,
                spacing = "Small"
            }
        };

        body.AddRange(transactions.Select(transaction => new
        {
            type = "Container",
            separator = true,
            items = new object[]
            {
                new
                {
                    type = "TextBlock",
                    text = transaction.Description,
                    weight = "Bolder",
                    wrap = true
                },
                new
                {
                    type = "FactSet",
                    facts = new object[]
                    {
                        new { title = "Date", value = transaction.Date.ToString("yyyy-MM-dd") },
                        new { title = "Amount", value = transaction.Amount.ToString("C") },
                        new { title = "Category", value = transaction.Category },
                        new { title = "Running balance", value = transaction.RunningBalance.ToString("C") }
                    }
                }
            }
        }));

        return new Attachment
        {
            ContentType = AccountCard.AdaptiveCardContentType,
            Content = new Dictionary<string, object?>
            {
                ["type"] = "AdaptiveCard",
                ["version"] = "1.5",
                ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
                ["body"] = body
            }
        };
    }

    private static bool ContainsAny(string message, params string[] values) =>
        values.Any(value => message.Contains(value, StringComparison.OrdinalIgnoreCase));
}
