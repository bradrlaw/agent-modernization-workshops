using BankingAssistant.Shared.Agent;
using BankingAssistant.Shared.Configuration;
using BankingAssistant.Shared.Data;
using OpenAI.Chat;

try
{
    var settings = AppSupport.LoadAzureOpenAiSettings();
    var chatClient = AppSupport.CreateChatClient(settings);
    var customerId = SelectCustomer();
    var agent = new BankingAgent(chatClient, customerId);
    var messages = new List<ChatMessage>();

    Console.WriteLine("\nConnecting to Azure AI Foundry...");
    Console.WriteLine("✓ Connected");
    Console.WriteLine("\nType your message (or 'quit' to exit):\n");
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

        var response = await agent.ChatAsync(messages, userInput);
        Console.WriteLine($"\nAssistant: {response}");
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
    Console.WriteLine("\n╔══════════════════════════════════════════╗");
    Console.WriteLine("║   Virtual Banking Assistant — Pro-Code   ║");
    Console.WriteLine("╚══════════════════════════════════════════╝\n");
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
