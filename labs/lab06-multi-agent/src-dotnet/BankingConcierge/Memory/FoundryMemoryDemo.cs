using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;

namespace BankingConcierge.Memory;

/// <summary>
/// Foundry-managed memory tier (<c>--memory foundry</c>). Unlike the BYO tiers
/// (<c>local</c>/<c>cosmos</c>) — which THIS app owns: it recalls a summary, injects it as an
/// instruction preamble into the five specialists, then compacts + saves after the run — Foundry-managed
/// memory is a PLATFORM-MANAGED <see cref="FoundryMemoryProvider"/> attached to a SINGLE agent + session.
/// The service extracts, embeds, stores, and re-injects memories automatically, keyed by a per-customer
/// scope. That is a fundamentally different execution model, so this runs a dedicated single-agent
/// concierge session rather than the multi-agent workflow — faithful to how managed memory actually
/// works, and honest about the difference (no recall preamble, no local <c>.memory/</c> file).
///
/// Verified against github.com/microsoft/agent-framework
/// (dotnet/samples/02-agents/AgentWithMemory/AgentWithMemory_Step04_MemoryUsingFoundry).
///
/// Preview requirements: a chat deployment AND an embedding deployment on the Foundry project, plus
/// Foundry "memory (preview)" enabled. If any is missing, store creation fails loud with guidance.
/// </summary>
public static class FoundryMemoryDemo
{
    public static async Task RunAsync(AIProjectClient client, string chatModel, string customerId, string task)
    {
        var storeName = Environment.GetEnvironmentVariable("AZURE_AI_MEMORY_STORE_ID") ?? "lab06-agent-memory";
        var embeddingModel = Environment.GetEnvironmentVariable("AZURE_AI_EMBEDDING_DEPLOYMENT_NAME")
            ?? "text-embedding-3-small";
        var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
            ?? Environment.GetEnvironmentVariable("PROJECT_ENDPOINT");

        // Scope keys memory to THIS customer, so recall persists across sessions/processes for the same
        // --customer. The stateInitializer hands the provider the scope for every new session.
        var memory = new FoundryMemoryProvider(
            client,
            storeName,
            stateInitializer: _ => new(new FoundryMemoryProviderScope(customerId)));

        // A single concierge agent with the memory provider attached as an AIContextProvider. This is the
        // options overload of AsAIAgent (not the string overload the workflow tiers use), so we can pass
        // AIContextProviders.
        var agent = client.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "BankingConcierge",
            Description = "A single banking concierge with Foundry-managed cross-session memory.",
            ChatOptions = new ChatOptions
            {
                ModelId = chatModel,
                Instructions =
                    "You are a retail-bank concierge. Help the customer with accounts, loans, and cards. "
                    + "When memory already tells you facts about this customer, use them and do NOT ask the "
                    + "customer to repeat themselves. Format currency as $#,###.##.",
            },
            AIContextProviders = [memory],
        });

        // Create (or reuse) the server-side memory store. Idempotent. Fails loud with clear guidance if
        // the feature isn't available on this project (preview off, or embedding deployment missing).
        Console.WriteLine($"• Memory: Foundry-managed (preview) — attaching store '{storeName}' "
            + $"(chat: {chatModel}, embeddings: {embeddingModel})...");
        try
        {
            await memory.EnsureMemoryStoreCreatedAsync(chatModel, embeddingModel,
                "Lab 06 banking concierge cross-session memory.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("✗ Foundry-managed memory could not be initialized. This tier needs:");
            Console.Error.WriteLine("   • a chat deployment (FOUNDRY_MODEL) AND an embedding deployment "
                + "(AZURE_AI_EMBEDDING_DEPLOYMENT_NAME),");
            Console.Error.WriteLine("   • Foundry 'memory (preview)' enabled on the project,");
            Console.Error.WriteLine("   • an identity with data-plane access (Azure AI User) to the project.");
            Console.Error.WriteLine($"   Underlying error: {ex.GetType().Name}: {ex.Message}");
            throw;
        }

        var host = HostOf(endpoint);
        Console.WriteLine($"✓ Memory: ON (Foundry-managed, preview) — store '{storeName}' on {host}; "
            + $"scope: {customerId}. The service injects prior memories and extracts new ones automatically.");
        Console.WriteLine("  (Managed tier: no recall preamble and no local .memory/ file — the service owns "
            + "storage. This runs ONE concierge agent, not the five-agent workflow.)");

        // Turn 1 — the customer's task in a fresh session. If the service already holds memories for this
        // scope (e.g., a prior run with the same --customer), it injects them before the model responds.
        var session = await agent.CreateSessionAsync();
        Console.WriteLine($"\nYou ({customerId}): {task}\n" + new string('-', 60));
        Console.Write("[BankingConcierge] ");
        await foreach (var update in agent.RunStreamingAsync(task, session))
        {
            Console.Write(update.Text);
        }

        Console.WriteLine();

        // Extraction is ASYNC — the service distills + embeds memories AFTER the turn. Wait for it to
        // finish so the recall probe reflects what was just learned (this matters for live-demo timing).
        Console.WriteLine("\n... waiting for the service to extract & persist memories (async)...");
        await memory.WhenUpdatesCompletedAsync();

        // Recall probe — a BRAND-NEW session (separate conversation), SAME customer scope. Any recall here
        // must come from managed memory, not from this conversation's own history.
        var recallSession = await agent.CreateSessionAsync();
        const string probe =
            "Without me repeating myself, what do you already know about my banking needs, "
            + "including the specific amounts and terms?";
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("Recall probe — NEW session, same customer scope (recall must come from memory):");
        Console.WriteLine($"You ({customerId}): {probe}\n" + new string('-', 60));
        Console.Write("[BankingConcierge] ");
        await foreach (var update in agent.RunStreamingAsync(probe, recallSession))
        {
            Console.Write(update.Text);
        }

        Console.WriteLine();
        await memory.WhenUpdatesCompletedAsync();

        Console.WriteLine("\n" + new string('-', 60));
        Console.WriteLine($"✓ Done. Memories persist server-side (store '{storeName}', scope '{customerId}').");
        Console.WriteLine("  Re-run with the SAME --customer (even in a new process) to see recall grow; use a "
            + "NEW --customer for a clean first session.");
    }

    private static string HostOf(string? endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var u) ? u.Host : (endpoint ?? "the Foundry project");
}
