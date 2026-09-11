using System.Text;
using System.Text.Json;
using BankingConcierge;
using BankingConcierge.Memory;
using BankingConcierge.Skills;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
using Microsoft.Extensions.AI;

// -------------------------------------------------------------------------------------
// Lab 06 — Multi-Agent Orchestration (.NET path)
//
// One banking specialist team, five orchestration patterns. Pick a pattern with
// `--pattern` and watch the same agents compose differently. This is the .NET twin of
// the Python scripts under src/ (orchestrate_handoff.py + patterns/*.py).
//
//   dotnet run -- --pattern handoff        (interactive triage -> specialist, default)
//   dotnet run -- --pattern sequential     (accounts -> lending -> compliance pipeline)
//   dotnet run -- --pattern concurrent     (fan-out health check, fan-in summary)
//   dotnet run -- --pattern groupchat      (round-robin dispute round-table)
//   dotnet run -- --pattern magentic       (adaptive planner: plans, delegates, re-plans)
//   dotnet run -- --pattern handoff --customer CUST-1002
//   dotnet run -- --pattern sequential --skills off  (Agent->Skills OFF: base instructions only)
//   dotnet run -- --pattern sequential --memory local (Agent->Memory: recall across sessions)
//   dotnet run -- --pattern sequential --task "Custom request"  (override the scripted task)
//
// Agent -> Skills: by default the specialists load the versioned SKILL.md files under
// ../../skills/ at runtime (compliance-guidelines, brand-voice, escalation-policy). Pass
// `--skills off` to see the same team WITHOUT the shared rules, then edit a SKILL.md and
// re-run to change behavior with no code change.
//
// Agent -> Memory: pass `--memory local` to give the team cross-session memory. Before each run
// the recalled summary/facts are injected into the specialists' instructions; after each run the
// new turns are compacted and saved under .memory/. To SEE memory change behavior (not just the
// banner), seed one run with a detailed task, then send a follow-up that DEPENDS on it via `--task`:
//   dotnet run -- --pattern sequential --memory local --task "I'd like a $25,000 auto loan for 60 months. Prepare an offer with disclosures."
//   dotnet run -- --pattern sequential --memory local --task "Finalize the loan we discussed and restate its amount, term, APR, and monthly payment."   (recalls the specifics)
//   dotnet run -- --pattern sequential --memory off   --task "Finalize the loan we discussed and restate its amount, term, APR, and monthly payment."   (no memory: has to ask which loan)
// `--memory off` (the default) forgets. See README Part E for the Cosmos DB and Foundry-managed options.
//
// Verified against github.com/microsoft/agent-framework
// (dotnet/samples/03-workflows) as of Aug 2026.
// -------------------------------------------------------------------------------------

Console.OutputEncoding = Encoding.UTF8;

// Load .env FIRST so file-based settings are visible during arg parsing. DEMO_CUSTOMER_ID and
// MEMORY_MODE are read in ParseArgs, so the file must load before it. Precedence ends up:
// CLI flag  >  shell environment variable  >  .env file  >  built-in default.
AgentTeam.LoadDotEnv();

var (pattern, customerId, useSkills, memoryMode, customTask) = ParseArgs(args);

try
{
    var (client, model) = AgentTeam.CreateProjectClient();
    Console.WriteLine($"Connecting to Azure AI Foundry (model: {model})...");

    SkillLibrary? skills = useSkills ? SkillLibrary.Load() : null;

    // Agent -> Memory (BYO tier): recall what we already know about this customer, then inject it
    // into every specialist's instructions. `off` (default) skips this entirely.
    var memory = memoryMode == "off" ? null : new MemoryManager(MemoryManager.CreateStore(memoryMode));
    var threadId = Guid.NewGuid().ToString("N");
    Recall? recall = memory is null ? null : await memory.RecallAsync(customerId);

    var team = AgentTeam.Build(client, model, customerId, skills,
        recall is { HasMemory: true } ? recall.Preamble : null);

    if (skills is { Count: > 0 })
    {
        Console.WriteLine($"✓ Skills: ON — loaded {skills.Count} from ./skills "
            + $"({string.Join(", ", skills.All.Select(s => s.Name))}).");
    }
    else if (useSkills)
    {
        Console.WriteLine("• Skills: ON but none found under ./skills — using base instructions.");
    }
    else
    {
        Console.WriteLine("• Skills: OFF — base instructions only (pass --skills on to enable).");
    }

    PrintMemoryStatus(memory, recall, customerId);

    Console.WriteLine($"✓ Team ready. Session customer: {customerId}. Pattern: {pattern}.\n");

    List<ChatMessage> finalMessages = [];

    switch (pattern)
    {
        case "sequential":
            // Fixed pipeline: each agent's output feeds the next.
            finalMessages = await RunOnceAsync(
                AgentWorkflowBuilder.BuildSequential([team.Accounts, team.Lending, team.Compliance]),
                customTask ?? "I'd like a $25,000 auto loan for 60 months. Please prepare an offer with the "
                    + "required disclosures.");
            break;

        case "concurrent":
            // Fan out to specialists in parallel, then aggregate.
            finalMessages = await RunOnceAsync(
                AgentWorkflowBuilder.BuildConcurrent([team.Accounts, team.Lending, team.Cards]),
                customTask ?? "Give me a financial health check: my balances, loan options I might qualify for, "
                    + "and anything notable on my cards.");
            break;

        case "groupchat":
            // A manager picks who speaks next as specialists collaborate to a resolution.
            finalMessages = await RunOnceAsync(
                AgentWorkflowBuilder
                    .CreateGroupChatBuilderWith(agents =>
                        new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 6 })
                    .AddParticipants([team.Cards, team.Accounts, team.Compliance])
                    .WithName("DisputeRoundTable")
                    .WithDescription("Cards, Accounts, and Compliance resolve a disputed charge.")
                    .Build(),
                customTask ?? "I'm disputing a $180 charge on my debit card that I don't recognize. Please "
                    + "investigate and resolve it.");
            break;

        case "magentic":
            // Open-ended goal: the manager builds a plan, delegates to specialists, tracks a
            // progress ledger, and re-plans when it stalls. Bounded by max rounds/stalls/resets.
            finalMessages = await RunOnceAsync(
                new MagenticWorkflowBuilder(team.Concierge)
                    .AddParticipants([team.Accounts, team.Lending, team.Cards])
                    .WithName("PurchasePlanner")
                    .WithDescription("Plans an affordability analysis across the banking specialists.")
                    .RequirePlanSignoff(false)
                    .WithMaxRounds(10)
                    .WithMaxStalls(3)
                    .WithMaxResets(2)
                    .Build(),
                customTask ?? "I want to buy a $30,000 car. Figure out affordability from my accounts, "
                    + "suitable loan options, and recommend next steps.");
            break;

        case "handoff":
        default:
            // Triage concierge hands the whole conversation to exactly one specialist,
            // and specialists can hand back for a follow-up on another topic.
            var handoff = AgentWorkflowBuilder
                .CreateHandoffBuilderWith(team.Concierge)
                .WithHandoffs(team.Concierge, team.Specialists)
                .WithHandoffs(team.Specialists, team.Concierge)
                .Build();
            finalMessages = await RunInteractiveAsync(handoff);
            break;
    }

    if (memory is not null)
    {
        var turns = ToMemoryTurns(finalMessages);
        var saved = await memory.RecordAsync(customerId, threadId, turns);
        if (saved > 0)
        {
            Console.WriteLine($"\n✓ Memory: saved {saved} turn(s) for {customerId} to {memory.Backend}.");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"\nError: {ex.Message}");
    Environment.ExitCode = 1;
}

// Runs a workflow to completion for a single scripted task and streams the turns.
static async Task<List<ChatMessage>> RunOnceAsync(Workflow workflow, string task)
{
    Console.WriteLine($"Task: {task}\n" + new string('-', 60));
    List<ChatMessage> input = [new ChatMessage(ChatRole.User, task)];
    var produced = await StreamWorkflowAsync(workflow, input);
    Console.WriteLine("\n" + new string('-', 60) + "\nDone.");

    // Record the user's task plus what the workflow produced (skip any echoed user turns).
    List<ChatMessage> transcript = [.. input];
    transcript.AddRange(produced.Where(m => m.Role != ChatRole.User));
    return transcript;
}

// Drives the interactive handoff loop: each user turn re-runs the workflow, carrying
// the growing message history so control can move between specialists.
static async Task<List<ChatMessage>> RunInteractiveAsync(Workflow workflow)
{
    Console.WriteLine("Interactive handoff. Type a banking question (or 'quit' to exit).");
    Console.WriteLine("Try: \"What's my available balance?\" then \"What auto loan rates do you have?\"\n");

    List<ChatMessage> messages = [];
    while (true)
    {
        Console.Write("\nYou: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            continue;
        }

        if (input.Equals("quit", StringComparison.OrdinalIgnoreCase)
            || input.Equals("exit", StringComparison.OrdinalIgnoreCase)
            || input.Equals("q", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        messages.Add(new ChatMessage(ChatRole.User, input));
        messages.AddRange(await StreamWorkflowAsync(workflow, messages));
    }

    Console.WriteLine("\nGoodbye!");
    return messages;
}

// Shared streaming reader (mirrors the Agent Framework sample's RunWorkflowAsync):
// prints each agent's tokens as they arrive and returns the workflow's final messages.
static async Task<List<ChatMessage>> StreamWorkflowAsync(Workflow workflow, List<ChatMessage> messages)
{
    string? lastExecutorId = null;

    await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);
    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

    await foreach (WorkflowEvent evt in run.WatchStreamAsync())
    {
        if (evt is AgentResponseUpdateEvent update)
        {
            if (update.ExecutorId != lastExecutorId)
            {
                lastExecutorId = update.ExecutorId;
                Console.WriteLine();
                Console.WriteLine($"[{update.ExecutorId}]");
            }

            Console.Write(update.Update.Text);

            if (update.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is { } call)
            {
                Console.WriteLine();
                Console.WriteLine($"  ↳ calling {call.Name}({JsonSerializer.Serialize(call.Arguments)})");
            }
        }
        else if (evt is MagenticPlanCreatedEvent planCreated)
        {
            Console.WriteLine($"\n\n[Magentic plan]\n{planCreated.FullTaskLedger.Text}\n");
        }
        else if (evt is MagenticReplannedEvent replanned)
        {
            Console.WriteLine($"\n\n[Magentic re-planned]\n{replanned.FullTaskLedger.Text}\n");
        }
        else if (evt is MagenticProgressLedgerUpdatedEvent progress)
        {
            Console.WriteLine($"\n[Magentic progress] next: {progress.ProgressLedger.NextSpeaker}"
                + $" — {progress.ProgressLedger.InstructionOrQuestion}");
        }
        else if (evt is WorkflowOutputEvent output)
        {
            Console.WriteLine();
            return output.As<List<ChatMessage>>() ?? [];
        }
        else if (evt is WorkflowErrorEvent workflowError)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            var ex = workflowError.Exception;
            Console.Error.WriteLine(ex?.Message ?? "Unknown workflow error.");
            if (ex?.InnerException is { } inner)
            {
                Console.Error.WriteLine($"  ↳ {inner.GetType().Name}: {inner.Message}");
            }
            Console.ResetColor();
        }
    }

    return [];
}

// Prints the one-line memory status banner (mirrors the Skills line).
static void PrintMemoryStatus(MemoryManager? memory, Recall? recall, string customerId)
{
    if (memory is null)
    {
        Console.WriteLine("• Memory: OFF — no cross-session recall (pass --memory local to enable).");
        return;
    }

    if (recall is { HasMemory: true, Record: { } r })
    {
        var facts = r.ImportantFacts.Count > 0
            ? $"; facts: {string.Join(", ", r.ImportantFacts.Take(4))}"
            : string.Empty;
        Console.WriteLine($"✓ Memory: ON ({memory.Backend}) — recalled {r.SessionCount} prior "
            + $"session(s) for {customerId}{facts}.");
    }
    else
    {
        Console.WriteLine($"✓ Memory: ON ({memory.Backend}) — no prior memory for {customerId} yet "
            + "(first session; run again to see recall).");
    }
}

// Maps the workflow's chat messages to durable memory turns (dropping empty/tool-only messages).
static List<MemoryTurn> ToMemoryTurns(IEnumerable<ChatMessage> messages)
{
    var now = DateTimeOffset.UtcNow;
    return messages
        .Where(m => !string.IsNullOrWhiteSpace(m.Text))
        .Select(m => new MemoryTurn(
            m.Role == ChatRole.User ? "user" : m.Role == ChatRole.System ? "system" : "assistant",
            m.AuthorName,
            m.Text,
            now))
        .ToList();
}

static (string Pattern, string CustomerId, bool UseSkills, string MemoryMode, string? Task) ParseArgs(string[] args)
{
    var pattern = "handoff";
    var customerId = Environment.GetEnvironmentVariable("DEMO_CUSTOMER_ID") ?? "CUST-1001";
    var useSkills = true;
    var memoryMode = (Environment.GetEnvironmentVariable("MEMORY_MODE") ?? "off").Trim().ToLowerInvariant();
    string? customTask = null;

    for (var i = 0; i < args.Length - 1; i++)
    {
        switch (args[i])
        {
            case "--pattern" or "-p":
                pattern = args[i + 1].ToLowerInvariant();
                break;
            case "--customer" or "-c":
                customerId = args[i + 1];
                break;
            case "--skills" or "-s":
                var v = args[i + 1].ToLowerInvariant();
                useSkills = v is not ("off" or "false" or "no" or "0");
                break;
            case "--memory" or "-m":
                memoryMode = args[i + 1].Trim().ToLowerInvariant();
                break;
            case "--task" or "-t":
                customTask = args[i + 1];
                break;
        }
    }

    if (memoryMode is "false" or "no" or "0" or "none" or "disabled")
    {
        memoryMode = "off";
    }

    return (pattern, customerId, useSkills, memoryMode, customTask);
}
