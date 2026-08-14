using System.Text;
using System.Text.Json;
using BankingConcierge;
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
//
// Agent -> Skills: by default the specialists load the versioned SKILL.md files under
// ../../skills/ at runtime (compliance-guidelines, brand-voice, escalation-policy). Pass
// `--skills off` to see the same team WITHOUT the shared rules, then edit a SKILL.md and
// re-run to change behavior with no code change.
//
// Verified against github.com/microsoft/agent-framework
// (dotnet/samples/03-workflows) as of Aug 2026.
// -------------------------------------------------------------------------------------

Console.OutputEncoding = Encoding.UTF8;

var (pattern, customerId, useSkills) = ParseArgs(args);

try
{
    var (client, model) = AgentTeam.CreateProjectClient();
    Console.WriteLine($"Connecting to Azure AI Foundry (model: {model})...");

    SkillLibrary? skills = useSkills ? SkillLibrary.Load() : null;
    var team = AgentTeam.Build(client, model, customerId, skills);

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

    Console.WriteLine($"✓ Team ready. Session customer: {customerId}. Pattern: {pattern}.\n");

    switch (pattern)
    {
        case "sequential":
            // Fixed pipeline: each agent's output feeds the next.
            await RunOnceAsync(
                AgentWorkflowBuilder.BuildSequential([team.Accounts, team.Lending, team.Compliance]),
                "I'd like a $25,000 auto loan for 60 months. Please prepare an offer with the "
                    + "required disclosures.");
            break;

        case "concurrent":
            // Fan out to specialists in parallel, then aggregate.
            await RunOnceAsync(
                AgentWorkflowBuilder.BuildConcurrent([team.Accounts, team.Lending, team.Cards]),
                "Give me a financial health check: my balances, loan options I might qualify for, "
                    + "and anything notable on my cards.");
            break;

        case "groupchat":
            // A manager picks who speaks next as specialists collaborate to a resolution.
            await RunOnceAsync(
                AgentWorkflowBuilder
                    .CreateGroupChatBuilderWith(agents =>
                        new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 6 })
                    .AddParticipants([team.Cards, team.Accounts, team.Compliance])
                    .WithName("DisputeRoundTable")
                    .WithDescription("Cards, Accounts, and Compliance resolve a disputed charge.")
                    .Build(),
                "I'm disputing a $180 charge on my debit card that I don't recognize. Please "
                    + "investigate and resolve it.");
            break;

        case "magentic":
            // Open-ended goal: the manager builds a plan, delegates to specialists, tracks a
            // progress ledger, and re-plans when it stalls. Bounded by max rounds/stalls/resets.
            await RunOnceAsync(
                new MagenticWorkflowBuilder(team.Concierge)
                    .AddParticipants([team.Accounts, team.Lending, team.Cards])
                    .WithName("PurchasePlanner")
                    .WithDescription("Plans an affordability analysis across the banking specialists.")
                    .RequirePlanSignoff(false)
                    .WithMaxRounds(10)
                    .WithMaxStalls(3)
                    .WithMaxResets(2)
                    .Build(),
                "I want to buy a $30,000 car. Figure out affordability from my accounts, "
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
            await RunInteractiveAsync(handoff);
            break;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"\nError: {ex.Message}");
    Environment.ExitCode = 1;
}

// Runs a workflow to completion for a single scripted task and streams the turns.
static async Task RunOnceAsync(Workflow workflow, string task)
{
    Console.WriteLine($"Task: {task}\n" + new string('-', 60));
    await StreamWorkflowAsync(workflow, [new ChatMessage(ChatRole.User, task)]);
    Console.WriteLine("\n" + new string('-', 60) + "\nDone.");
}

// Drives the interactive handoff loop: each user turn re-runs the workflow, carrying
// the growing message history so control can move between specialists.
static async Task RunInteractiveAsync(Workflow workflow)
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
            Console.Error.WriteLine(workflowError.Exception?.Message ?? "Unknown workflow error.");
            Console.ResetColor();
        }
    }

    return [];
}

static (string Pattern, string CustomerId, bool UseSkills) ParseArgs(string[] args)
{
    var pattern = "handoff";
    var customerId = Environment.GetEnvironmentVariable("DEMO_CUSTOMER_ID") ?? "CUST-1001";
    var useSkills = true;

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
        }
    }

    return (pattern, customerId, useSkills);
}
