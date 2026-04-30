using System.Text.Json;
using BankingAssistant.Shared.Data;

namespace BankingAssistant.Shared.Tools;

public static class BankingTools
{
    private static readonly JsonSerializerOptions SerializerOptions = MockDataStore.SerializerOptions;
    private static readonly JsonSerializerOptions CompactSerializerOptions = MockDataStore.CompactSerializerOptions;

    public static string GetAccountBalance(string customerId, string? accountId = null)
    {
        var accounts = MockDataStore.Accounts.Where(account => account.CustomerId == customerId).ToList();
        if (accounts.Count == 0)
        {
            return SerializeCompact(new { error = $"No accounts found for customer {customerId}" });
        }

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            accounts = accounts.Where(account => account.AccountId == accountId).ToList();
            if (accounts.Count == 0)
            {
                return SerializeCompact(new { error = $"Account {accountId} not found for customer {customerId}" });
            }
        }

        var results = accounts.Select(account => new
        {
            account.AccountId,
            account.Type,
            account.Nickname,
            account.Last4,
            account.CurrentBalance,
            account.AvailableBalance,
            account.Status,
        });

        return SerializeIndented(new { balances = results });
    }

    public static string GetRecentTransactions(string customerId, string? accountId = null, int limit = 5)
    {
        var customerAccounts = MockDataStore.Accounts
            .Where(account => account.CustomerId == customerId)
            .Select(account => account.AccountId)
            .ToList();

        if (customerAccounts.Count == 0)
        {
            return SerializeCompact(new { error = $"No accounts found for customer {customerId}" });
        }

        List<string> targetAccounts;
        if (!string.IsNullOrWhiteSpace(accountId))
        {
            if (!customerAccounts.Contains(accountId))
            {
                return SerializeCompact(new { error = $"Account {accountId} not found for customer {customerId}" });
            }

            targetAccounts = [accountId];
        }
        else
        {
            targetAccounts = customerAccounts;
        }

        var transactions = MockDataStore.Transactions
            .Where(transaction => targetAccounts.Contains(transaction.AccountId))
            .OrderByDescending(transaction => transaction.Date)
            .Take(limit)
            .Select(transaction => new
            {
                transaction.TransactionId,
                transaction.AccountId,
                transaction.Date,
                transaction.Description,
                transaction.Amount,
                transaction.Type,
                transaction.Category,
                transaction.RunningBalance,
            });

        return SerializeIndented(new { transactions });
    }

    public static string ListAccounts(string customerId)
    {
        var accounts = MockDataStore.Accounts.Where(account => account.CustomerId == customerId).ToList();
        if (accounts.Count == 0)
        {
            return SerializeCompact(new { error = $"No accounts found for customer {customerId}" });
        }

        var total = accounts.Sum(account => account.CurrentBalance);
        var summary = accounts.Select(account => new
        {
            account.AccountId,
            account.Type,
            account.Nickname,
            account.Last4,
            account.CurrentBalance,
            account.Status,
        });

        return SerializeIndented(new { accounts = summary, totalBalance = total });
    }

    public static string GetCustomerProfile(string customerId)
    {
        var customer = MockDataStore.Customers.FirstOrDefault(item => item.CustomerId == customerId);
        if (customer is null)
        {
            return SerializeCompact(new { error = $"Customer {customerId} not found" });
        }

        return SerializeIndented(new
        {
            customer.CustomerId,
            name = $"{customer.FirstName} {customer.LastName}",
            customer.Email,
            customer.Phone,
            customer.Address,
            customer.MemberSince,
        });
    }

    public static string GetLoanRates(string? productType = null)
    {
        var rates = new[]
        {
            new { product = "auto", term = "36 months", apr = 4.49m, asOf = "2026-04-23" },
            new { product = "auto", term = "60 months", apr = 4.99m, asOf = "2026-04-23" },
            new { product = "home", term = "15 years", apr = 5.75m, asOf = "2026-04-23" },
            new { product = "home", term = "30 years", apr = 6.25m, asOf = "2026-04-23" },
            new { product = "personal", term = "12 months", apr = 7.99m, asOf = "2026-04-23" },
            new { product = "personal", term = "36 months", apr = 8.49m, asOf = "2026-04-23" },
        }.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(productType))
        {
            rates = rates.Where(rate => string.Equals(rate.product, productType, StringComparison.OrdinalIgnoreCase));
            if (!rates.Any())
            {
                return SerializeCompact(new { error = $"Unknown product type: {productType}. Use: auto, home, personal" });
            }
        }

        return SerializeIndented(new { rates });
    }

    public static string CalculateLoanPayment(double principal, double annualRate, int termMonths)
    {
        if (principal <= 0 || annualRate <= 0 || termMonths <= 0)
        {
            return SerializeCompact(new { error = "All values must be positive numbers" });
        }

        var monthlyRate = annualRate / 100d / 12d;
        var monthlyPayment = principal
            * (monthlyRate * Math.Pow(1 + monthlyRate, termMonths))
            / (Math.Pow(1 + monthlyRate, termMonths) - 1);
        var totalCost = monthlyPayment * termMonths;
        var totalInterest = totalCost - principal;

        return SerializeIndented(new
        {
            monthlyPayment = Math.Round(monthlyPayment, 2),
            totalInterest = Math.Round(totalInterest, 2),
            totalCost = Math.Round(totalCost, 2),
            principal,
            annualRate,
            termMonths,
        });
    }

    public static string SearchFaq(string query)
    {
        var queryLower = query.ToLowerInvariant();
        var matches = new List<object>();

        foreach (var entry in MockDataStore.FaqEntries)
        {
            var hasMatch = queryLower
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 2)
                .Any(word => entry.Question.Contains(word, StringComparison.OrdinalIgnoreCase)
                    || entry.Answer.Contains(word, StringComparison.OrdinalIgnoreCase));

            if (hasMatch)
            {
                matches.Add(new { entry.Question, entry.Answer });
            }
        }

        if (matches.Count == 0)
        {
            return SerializeCompact(new { message = "No FAQ entries found matching your query. Please contact support at 1-800-555-0199." });
        }

        return SerializeIndented(new { faqResults = matches });
    }

    private static string SerializeIndented(object value) => JsonSerializer.Serialize(value, SerializerOptions);

    private static string SerializeCompact(object value) => JsonSerializer.Serialize(value, CompactSerializerOptions);
}
