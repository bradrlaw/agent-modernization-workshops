using System.Text.Json;
using BankingAssistant.Shared.Models;

namespace BankingAssistant.Shared.Data;

public static class MockDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string DataDirectory { get; } = ResolveDataDirectory();

    public static IReadOnlyList<Customer> Customers { get; } = LoadCustomers();

    public static IReadOnlyList<Account> Accounts { get; } = LoadJson<Account>("accounts.json");

    public static IReadOnlyList<Transaction> Transactions { get; } = LoadJson<Transaction>("transactions.json");

    public static IReadOnlyList<FaqEntry> FaqEntries { get; } = LoadFaqEntries();

    public static JsonSerializerOptions SerializerOptions { get; } = new(JsonOptions)
    {
        WriteIndented = true,
    };

    public static JsonSerializerOptions CompactSerializerOptions { get; } = new(JsonOptions);

    private static IReadOnlyList<Customer> LoadCustomers()
    {
        var rawCustomers = LoadJson<CustomerFileModel>("customers.json");
        return rawCustomers
            .Select(customer => new Customer(
                customer.CustomerId,
                customer.FirstName,
                customer.LastName,
                customer.Email,
                customer.Phone,
                FormatAddress(customer.Address),
                customer.MemberSince))
            .ToList()
            .AsReadOnly();
    }

    private static IReadOnlyList<T> LoadJson<T>(params string[] pathParts)
    {
        var filePath = Path.Combine(new[] { DataDirectory }.Concat(pathParts.Skip(1)).ToArray());
        if (pathParts.Length == 1)
        {
            filePath = Path.Combine(DataDirectory, pathParts[0]);
        }

        var json = File.ReadAllText(filePath);
        return (JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [])
            .AsReadOnly();
    }

    private static IReadOnlyList<FaqEntry> LoadFaqEntries()
    {
        var faqPath = Path.Combine(DataDirectory, "banking-faq.txt");
        if (!File.Exists(faqPath))
        {
            return Array.Empty<FaqEntry>();
        }

        var content = File.ReadAllText(faqPath).Trim();
        var lines = content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var entries = new List<FaqEntry>();
        for (var i = 0; i < lines.Count - 1; i += 2)
        {
            entries.Add(new FaqEntry(lines[i], lines[i + 1]));
        }

        return entries.AsReadOnly();
    }

    private static string ResolveDataDirectory()
    {
        foreach (var startPath in new[]
                 {
                     AppContext.BaseDirectory,
                     Path.GetDirectoryName(typeof(MockDataStore).Assembly.Location)
                 }.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var current = new DirectoryInfo(startPath!);
            while (current is not null)
            {
                var candidate = Path.Combine(current.FullName, "data");
                if (Directory.Exists(candidate)
                    && File.Exists(Path.Combine(candidate, "customers.json"))
                    && File.Exists(Path.Combine(candidate, "accounts.json"))
                    && File.Exists(Path.Combine(candidate, "transactions.json")))
                {
                    return candidate;
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Unable to locate the data directory for the banking assistant.");
    }

    private static string FormatAddress(AddressFileModel address)
        => $"{address.Street}, {address.City}, {address.State} {address.Zip}";

    private sealed record CustomerFileModel(
        string CustomerId,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        AddressFileModel Address,
        string MemberSince);

    private sealed record AddressFileModel(string Street, string City, string State, string Zip);
}
