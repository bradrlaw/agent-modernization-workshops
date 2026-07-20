using BankingAssistant.Agent.Data;
using Microsoft.Agents.Core.Models;

namespace BankingAssistant.Agent.Cards;

public static class AccountCard
{
    public const string AdaptiveCardContentType = "application/vnd.microsoft.card.adaptive";

    public static Attachment CreateAccountsCard(
        MockCustomer customer,
        IReadOnlyCollection<MockAccount> accounts,
        string title,
        string subtitle)
    {
        var body = new List<object>
        {
            new
            {
                type = "TextBlock",
                text = title,
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
                text = subtitle,
                wrap = true,
                spacing = "Small"
            }
        };

        body.AddRange(accounts.Select(CreateAccountSection));

        return new Attachment
        {
            ContentType = AdaptiveCardContentType,
            Content = new Dictionary<string, object?>
            {
                ["type"] = "AdaptiveCard",
                ["version"] = "1.5",
                ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
                ["body"] = body
            }
        };
    }

    private static object CreateAccountSection(MockAccount account)
    {
        return new
        {
            type = "Container",
            separator = true,
            style = "emphasis",
            items = new object[]
            {
                new
                {
                    type = "TextBlock",
                    text = $"{account.Type} - {account.Nickname}",
                    weight = "Bolder",
                    wrap = true
                },
                new
                {
                    type = "FactSet",
                    facts = new object[]
                    {
                        new { title = "Account", value = $"**** {account.Last4}" },
                        new { title = "Account ID", value = account.AccountId },
                        new { title = "Current balance", value = account.CurrentBalance.ToString("C") },
                        new { title = "Available", value = account.AvailableBalance.ToString("C") },
                        new { title = "Status", value = account.Status },
                        new { title = "Opened", value = account.OpenedDate.ToString("yyyy-MM-dd") }
                    }
                }
            }
        };
    }
}
