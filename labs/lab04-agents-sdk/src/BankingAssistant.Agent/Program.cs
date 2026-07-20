using BankingAssistant.Agent;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IStorage, MemoryStorage>();

builder.AddAgents(agents =>
{
    agents.AddAgent<BankingAgent>("banking-assistant");
});
var app = builder.Build();

app.MapAgents();

app.Run();

internal sealed class AgentsRegistrationBuilder(WebApplicationBuilder builder)
{
    private readonly WebApplicationBuilder _builder = builder;

    public void AddAgent<TAgent>(string name) where TAgent : AgentApplication
    {
        _ = name;
        _builder.AddAgent<TAgent>();
    }
}

internal static class WorkshopAgentHostExtensions
{
    public static WebApplicationBuilder AddAgents(this WebApplicationBuilder builder, Action<AgentsRegistrationBuilder> configure)
    {
        builder.Services.AddHttpClient();
        builder.AddAgentApplicationOptions();

        configure(new AgentsRegistrationBuilder(builder));
        return builder;
    }

    public static WebApplication MapAgents(this WebApplication app)
    {
        app.MapControllers();
        app.MapGet("/", () => Results.Text("BankingAssistant.Agent is running."));
        return app;
    }
}

[ApiController]
[Microsoft.AspNetCore.Mvc.Route("api/messages")]
public sealed class MessagesController(IAgentHttpAdapter adapter, IAgent agent) : ControllerBase
{
    [HttpPost]
    public Task PostAsync(CancellationToken cancellationToken) =>
        adapter.ProcessAsync(Request, Response, agent, cancellationToken);
}
