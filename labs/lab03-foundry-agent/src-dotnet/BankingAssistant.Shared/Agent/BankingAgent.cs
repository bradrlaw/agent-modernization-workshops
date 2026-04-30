using System.Text;
using OpenAI.Chat;

namespace BankingAssistant.Shared.Agent;

public class BankingAgent
{
    private readonly ChatClient _chatClient;
    private readonly string _systemPrompt;

    public BankingAgent(ChatClient chatClient, string customerId)
    {
        _chatClient = chatClient;
        _systemPrompt = $$"""
You are a Virtual Banking Assistant for a retail financial institution.
You help authenticated customers check account balances, review recent transactions,
list their accounts, look up their profile information, check current loan rates,
and calculate loan payments.

Always be professional, accurate, and security-conscious. Format currency values
with dollar signs and two decimal places. When displaying multiple items (accounts,
transactions), present them in a clear, organized format.

Never share information about other customers. If you cannot fulfill a request,
offer to connect the customer with a human agent.

The current customer's ID will be provided in each conversation. Use it for all
data lookups. Do not ask the user for their customer ID — it is already known.

Current session customer ID: {{customerId}}. Use this customer ID for all account, transaction, and profile lookups.
""";
    }

    public async Task<string> ChatAsync(List<ChatMessage> messages, string userMessage)
    {
        if (messages.Count == 0 || messages[0] is not SystemChatMessage)
        {
            messages.Insert(0, new SystemChatMessage(_systemPrompt));
        }

        messages.Add(new UserChatMessage(userMessage));

        var options = new ChatCompletionOptions();
        foreach (var tool in ToolDefinitions.AllTools)
        {
            options.Tools.Add(tool);
        }

        while (true)
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var completion = (await _chatClient.CompleteChatAsync(messages, options, cancellationTokenSource.Token)).Value;

            if (completion.FinishReason == ChatFinishReason.Stop)
            {
                messages.Add(new AssistantChatMessage(completion));
                return GetTextContent(completion);
            }

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(completion));

                var toolCalls = completion.ToolCalls.ToList();
                var toolTasks = toolCalls
                    .Select(toolCall => Task.Run(() => ToolDispatcher.Dispatch(toolCall.FunctionName, toolCall.FunctionArguments)))
                    .ToArray();

                var results = await Task.WhenAll(toolTasks);
                for (var i = 0; i < toolCalls.Count; i++)
                {
                    messages.Add(new ToolChatMessage(toolCalls[i].Id, results[i]));
                }

                continue;
            }

            var fallback = GetTextContent(completion);
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                messages.Add(new AssistantChatMessage(fallback));
                return fallback;
            }

            return "(No response)";
        }
    }

    private static string GetTextContent(ChatCompletion completion)
    {
        var builder = new StringBuilder();
        foreach (var part in completion.Content)
        {
            if (!string.IsNullOrEmpty(part.Text))
            {
                builder.Append(part.Text);
            }
        }

        return builder.ToString();
    }
}
