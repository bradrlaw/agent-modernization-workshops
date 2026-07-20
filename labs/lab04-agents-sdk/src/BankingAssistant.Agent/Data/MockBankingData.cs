namespace BankingAssistant.Agent.Data;

public static class MockBankingData
{
    public static IReadOnlyList<MockCustomer> Customers { get; } =
    [
        new(
            "CUST-1001",
            "Alex",
            "Morgan",
            "alex.morgan@example.com",
            "(555) 123-4567",
            new MockAddress("742 Evergreen Terrace", "Springfield", "VA", "22150"),
            new DateOnly(2018, 3, 15)),
        new(
            "CUST-1002",
            "Jordan",
            "Rivera",
            "jordan.rivera@example.com",
            "(555) 234-5678",
            new MockAddress("1600 Wilson Boulevard", "Arlington", "VA", "22201"),
            new DateOnly(2020, 7, 22)),
        new(
            "CUST-1003",
            "Taylor",
            "Chen",
            "taylor.chen@example.com",
            "(555) 345-6789",
            new MockAddress("350 Granby Street", "Norfolk", "VA", "23510"),
            new DateOnly(2022, 1, 10))
    ];

    public static IReadOnlyList<MockAccount> Accounts { get; } =
    [
        new("ACCT-4521", "CUST-1001", "Checking", "Primary Checking", "4521", 3842.56m, 3742.56m, "Active", new DateOnly(2018, 3, 15)),
        new("ACCT-7834", "CUST-1001", "Savings", "Emergency Fund", "7834", 12500.00m, 12500.00m, "Active", new DateOnly(2018, 3, 15)),
        new("ACCT-2190", "CUST-1001", "Certificate", "12-Month CD", "2190", 5000.00m, 0.00m, "Active", new DateOnly(2025, 6, 1)),
        new("ACCT-6612", "CUST-1002", "Checking", "Daily Checking", "6612", 1523.89m, 1523.89m, "Active", new DateOnly(2020, 7, 22)),
        new("ACCT-9945", "CUST-1002", "Savings", "Vacation Fund", "9945", 4200.75m, 4200.75m, "Active", new DateOnly(2020, 8, 10)),
        new("ACCT-3378", "CUST-1003", "Checking", "Main Account", "3378", 8921.33m, 8821.33m, "Active", new DateOnly(2022, 1, 10)),
        new("ACCT-5501", "CUST-1003", "Savings", "House Down Payment", "5501", 35000.00m, 35000.00m, "Active", new DateOnly(2022, 2, 1))
    ];

    public static IReadOnlyList<MockTransaction> Transactions { get; } =
    [
        new("TXN-90001", "ACCT-4521", new DateTimeOffset(2026, 4, 8, 14, 23, 0, TimeSpan.Zero), "Direct Deposit - Employer", 2450.00m, "Credit", "Income", 3842.56m),
        new("TXN-90002", "ACCT-4521", new DateTimeOffset(2026, 4, 7, 9, 15, 0, TimeSpan.Zero), "Grocery Mart", -87.43m, "Debit", "Groceries", 1392.56m),
        new("TXN-90003", "ACCT-4521", new DateTimeOffset(2026, 4, 5, 11, 0, 0, TimeSpan.Zero), "Transfer from Savings", 500.00m, "Credit", "Transfer", 1479.99m),
        new("TXN-90004", "ACCT-4521", new DateTimeOffset(2026, 4, 3, 19, 22, 0, TimeSpan.Zero), "Streaming Subscription", -14.99m, "Debit", "Entertainment", 979.99m),
        new("TXN-91001", "ACCT-7834", new DateTimeOffset(2026, 4, 5, 11, 0, 0, TimeSpan.Zero), "Transfer to Checking", -500.00m, "Debit", "Transfer", 12500.00m),
        new("TXN-91002", "ACCT-7834", new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero), "Interest Payment", 8.75m, "Credit", "Interest", 13000.00m),
        new("TXN-92001", "ACCT-6612", new DateTimeOffset(2026, 4, 8, 10, 30, 0, TimeSpan.Zero), "Direct Deposit - Employer", 1875.00m, "Credit", "Income", 1523.89m),
        new("TXN-92002", "ACCT-6612", new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.Zero), "Rent Payment", -1400.00m, "Debit", "Housing", -344.36m),
        new("TXN-92003", "ACCT-6612", new DateTimeOffset(2026, 4, 3, 13, 45, 0, TimeSpan.Zero), "Online Shopping", -67.22m, "Debit", "Shopping", 1055.64m),
        new("TXN-92004", "ACCT-6612", new DateTimeOffset(2026, 4, 2, 8, 5, 0, TimeSpan.Zero), "Coffee Shop", -6.75m, "Debit", "Dining", 1122.86m),
        new("TXN-92101", "ACCT-9945", new DateTimeOffset(2026, 4, 4, 16, 40, 0, TimeSpan.Zero), "Automatic Savings Transfer", 200.00m, "Credit", "Transfer", 4200.75m),
        new("TXN-92102", "ACCT-9945", new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero), "Interest Payment", 3.15m, "Credit", "Interest", 4000.75m),
        new("TXN-93001", "ACCT-3378", new DateTimeOffset(2026, 4, 9, 8, 0, 0, TimeSpan.Zero), "Payroll Deposit", 3200.00m, "Credit", "Income", 8921.33m),
        new("TXN-93002", "ACCT-3378", new DateTimeOffset(2026, 4, 7, 14, 0, 0, TimeSpan.Zero), "Auto Insurance", -185.00m, "Debit", "Insurance", 5721.33m),
        new("TXN-93003", "ACCT-3378", new DateTimeOffset(2026, 4, 5, 10, 30, 0, TimeSpan.Zero), "Transfer to Savings", -1000.00m, "Debit", "Transfer", 5906.33m),
        new("TXN-93004", "ACCT-3378", new DateTimeOffset(2026, 4, 4, 18, 15, 0, TimeSpan.Zero), "Restaurant - Harbor Grill", -64.25m, "Debit", "Dining", 6906.33m),
        new("TXN-93101", "ACCT-5501", new DateTimeOffset(2026, 4, 5, 10, 30, 0, TimeSpan.Zero), "Transfer from Checking", 1000.00m, "Credit", "Transfer", 35000.00m),
        new("TXN-93102", "ACCT-5501", new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero), "Interest Payment", 21.55m, "Credit", "Interest", 34000.00m)
    ];

    public static MockCustomer? GetCustomer(string customerId) =>
        Customers.FirstOrDefault(customer => customer.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<MockAccount> GetAccounts(string customerId) =>
        Accounts.Where(account => account.CustomerId.Equals(customerId, StringComparison.OrdinalIgnoreCase)).ToList();

    public static IReadOnlyList<MockTransaction> GetTransactions(string customerId, string? accountId = null, int limit = 5)
    {
        var customerAccountIds = GetAccounts(customerId)
            .Select(account => account.AccountId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IEnumerable<MockTransaction> query = Transactions
            .Where(transaction => customerAccountIds.Contains(transaction.AccountId));

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query = query.Where(transaction => transaction.AccountId.Equals(accountId, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(transaction => transaction.Date)
            .Take(limit)
            .ToList();
    }
}

public sealed record MockCustomer(
    string CustomerId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    MockAddress Address,
    DateOnly MemberSince)
{
    public string FullName => $"{FirstName} {LastName}";
}

public sealed record MockAddress(string Street, string City, string State, string Zip);

public sealed record MockAccount(
    string AccountId,
    string CustomerId,
    string Type,
    string Nickname,
    string Last4,
    decimal CurrentBalance,
    decimal AvailableBalance,
    string Status,
    DateOnly OpenedDate);

public sealed record MockTransaction(
    string TransactionId,
    string AccountId,
    DateTimeOffset Date,
    string Description,
    decimal Amount,
    string Type,
    string Category,
    decimal RunningBalance);
