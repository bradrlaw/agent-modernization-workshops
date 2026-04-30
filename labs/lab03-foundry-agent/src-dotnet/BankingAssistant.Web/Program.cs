using BankingAssistant.Shared.Agent;
using BankingAssistant.Shared.Configuration;
using BankingAssistant.Shared.Data;
using OpenAI.Chat;

AppSupport.LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);
var endpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT") ?? builder.Configuration["AzureOpenAI:Endpoint"];
var deploymentName = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME") ?? builder.Configuration["AzureOpenAI:DeploymentName"];

if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deploymentName))
{
    throw new InvalidOperationException("PROJECT_ENDPOINT and MODEL_DEPLOYMENT_NAME (or AzureOpenAI settings) must be configured.");
}

var chatClient = AppSupport.CreateChatClient(new AzureOpenAiSettings(endpoint, deploymentName));
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/customers", () => MockDataStore.Customers.Select(customer => new
{
    customerId = customer.CustomerId,
    name = $"{customer.FirstName} {customer.LastName}",
}));

app.MapPost("/api/chat", async (ChatRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.CustomerId) || string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "customerId and message are required" });
    }

    var history = new List<ChatMessage>();
    foreach (var item in request.History ?? [])
    {
        if (string.IsNullOrWhiteSpace(item.Content))
        {
            continue;
        }

        if (string.Equals(item.Role, "assistant", StringComparison.OrdinalIgnoreCase))
        {
            history.Add(new AssistantChatMessage(item.Content));
        }
        else if (string.Equals(item.Role, "user", StringComparison.OrdinalIgnoreCase))
        {
            history.Add(new UserChatMessage(item.Content));
        }
    }

    try
    {
        var agent = new BankingAgent(chatClient, request.CustomerId);
        var response = await agent.ChatAsync(history, request.Message);
        return Results.Ok(new { response });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }
});

app.MapFallbackToFile("index.html");
app.Run();

internal sealed record ChatRequest(string Message, string CustomerId, List<HistoryMessage>? History);
internal sealed record HistoryMessage(string Role, string Content);
