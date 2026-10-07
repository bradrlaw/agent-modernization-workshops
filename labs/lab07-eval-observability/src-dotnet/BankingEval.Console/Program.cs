using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using BankingAssistant.Shared.Agent;
using BankingAssistant.Shared.Configuration;
using BankingEval.Shared.Data;
using BankingEval.Shared.Evaluators;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// ─────────────────────────────────────────────────────────────────────────────
// BankingEval.Console — the .NET counterpart to the Lab 07 Python scripts.
//
//   dotnet run -- --task generate                 # produce responses.jsonl
//   dotnet run -- --task evaluate                  # offline rule-based gate
//   dotnet run -- --task evaluate --online         # + LLM quality evaluators
//   dotnet run -- --task trace --limit 5           # OpenTelemetry -> Azure Monitor
// ─────────────────────────────────────────────────────────────────────────────

const string ActivitySourceName = "BankingEval.Trace";

var options = Options.Parse(args);

return options.Task switch
{
    "generate" => await GenerateAsync(options),
    "evaluate" => await EvaluateAsync(options),
    "trace" => await TraceAsync(options),
    _ => Usage(),
};

static int Usage()
{
    Console.WriteLine("Usage: dotnet run -- --task <generate|evaluate|trace> [--data <path>] [--online] [--limit N] [--customer CUST-1001]");
    return 1;
}

// Run every dataset case through the Lab 03 banking agent and save the answers.
static async Task<int> GenerateAsync(Options o)
{
    var settings = AppSupport.LoadAzureOpenAiSettings();
    var chatClient = AppSupport.CreateChatClient(settings);

    var datasetPath = o.DataPath ?? EvalDataset.FindDataFile("eval-dataset.jsonl");
    var outputPath = Path.Combine(Path.GetDirectoryName(datasetPath)!, "responses.jsonl");

    var cases = EvalDataset.Load(datasetPath);
    if (o.Limit > 0)
    {
        cases = cases.Take(o.Limit).ToList();
    }

    Console.WriteLine($"Generating {cases.Count} responses...");
    foreach (var evalCase in cases)
    {
        var agent = new BankingAgent(chatClient, evalCase.CustomerId);
        var history = new List<OpenAI.Chat.ChatMessage>();
        try
        {
            evalCase.Response = await agent.ChatAsync(history, evalCase.Query);
        }
        catch (Exception ex)
        {
            evalCase.Response = $"(ERROR: {ex.Message})";
            Console.WriteLine($"  ! {evalCase.Id}: {ex.Message}");
        }

        Console.WriteLine($"[{evalCase.Id}] done");
    }

    EvalDataset.Save(outputPath, cases);
    Console.WriteLine($"Wrote {cases.Count} responses to {outputPath}");
    return 0;
}

// Score responses and enforce a quality gate (non-zero exit on failure).
static async Task<int> EvaluateAsync(Options o)
{
    AppSupport.LoadDotEnv();

    var dataPath = o.DataPath
        ?? Path.Combine(Path.GetDirectoryName(EvalDataset.FindDataFile("eval-dataset.jsonl"))!, "responses.jsonl");

    if (!File.Exists(dataPath))
    {
        Console.WriteLine($"No responses at {dataPath}. Run '--task generate' first.");
        return 2;
    }

    var rows = EvalDataset.Load(dataPath);
    IEvaluator[] customEvaluators =
    {
        new PiiLeakageEvaluator(),
        new CurrencyFormatEvaluator(),
        new RequiredDisclosureEvaluator(),
    };

    ChatConfiguration? chatConfiguration = null;
    if (o.Online)
    {
        var settings = AppSupport.LoadAzureOpenAiSettings();
        chatConfiguration = new ChatConfiguration(AppSupport.CreateChatClient(settings).AsIChatClient());
    }

    var failures = new List<string>();
    Console.WriteLine($"{"case",-12}{"PII",6}{"Currency",10}{"Disclosure",12}");
    Console.WriteLine(new string('-', 40));

    foreach (var row in rows)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, row.Query) };
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, row.Response ?? string.Empty));

        var passed = new Dictionary<string, bool>();
        foreach (var evaluator in customEvaluators)
        {
            var result = await evaluator.EvaluateAsync(messages, response);
            foreach (var metricName in evaluator.EvaluationMetricNames)
            {
                var metric = result.Get<BooleanMetric>(metricName);
                var ok = metric.Value ?? false;
                passed[metricName] = ok;
                if (!ok)
                {
                    failures.Add($"{row.Id}: {metricName}");
                }
            }
        }

        Console.WriteLine(
            $"{row.Id,-12}{Flag(passed, PiiLeakageEvaluator.MetricName),6}" +
            $"{Flag(passed, CurrencyFormatEvaluator.MetricName),10}" +
            $"{Flag(passed, RequiredDisclosureEvaluator.MetricName),12}");

        if (chatConfiguration is not null)
        {
            await RunQualityEvaluatorsAsync(row, messages, response, chatConfiguration);
        }
    }

    Console.WriteLine(new string('-', 40));
    if (failures.Count > 0)
    {
        Console.WriteLine($"\nGATE FAILED - {failures.Count} custom-metric violation(s):");
        foreach (var failure in failures)
        {
            Console.WriteLine($"  - {failure}");
        }

        return 1;
    }

    Console.WriteLine($"\nGATE PASSED - {rows.Count} cases, all custom metrics clean.");
    return 0;
}

// LLM-based quality evaluators (groundedness, relevance, coherence, fluency,
// equivalence). Scores are printed; wire thresholds into the gate as needed.
static async Task RunQualityEvaluatorsAsync(
    EvalCase row,
    IList<ChatMessage> messages,
    ChatResponse response,
    ChatConfiguration chatConfiguration)
{
    var evaluators = new (IEvaluator Evaluator, EvaluationContext[]? Context)[]
    {
        (new RelevanceEvaluator(), null),
        (new CoherenceEvaluator(), null),
        (new FluencyEvaluator(), null),
        (new GroundednessEvaluator(), new EvaluationContext[] { new GroundednessEvaluatorContext(row.Context) }),
        (new EquivalenceEvaluator(), new EvaluationContext[] { new EquivalenceEvaluatorContext(row.GroundTruth) }),
    };

    foreach (var (evaluator, context) in evaluators)
    {
        var result = await evaluator.EvaluateAsync(messages, response, chatConfiguration, context);
        foreach (var metricName in evaluator.EvaluationMetricNames)
        {
            var metric = result.Get<NumericMetric>(metricName);
            Console.WriteLine($"    {row.Id} {metricName}: {metric.Value}");
        }
    }
}

// Instrument the agent with OpenTelemetry GenAI spans and export to Azure Monitor.
static async Task<int> TraceAsync(Options o)
{
    var settings = AppSupport.LoadAzureOpenAiSettings();

    var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.WriteLine("Set APPLICATIONINSIGHTS_CONNECTION_STRING to export traces.");
        return 2;
    }

    var captureContent = string.Equals(
        Environment.GetEnvironmentVariable("AZURE_TRACING_GEN_AI_CONTENT_RECORDING_ENABLED"),
        "true",
        StringComparison.OrdinalIgnoreCase);

    using var tracerProvider = Sdk.CreateTracerProviderBuilder()
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("banking-eval"))
        .AddSource(ActivitySourceName)
        .AddSource("Experimental.Microsoft.Extensions.AI")
        .AddAzureMonitorTraceExporter(exporter => exporter.ConnectionString = connectionString)
        .Build();

    using var activitySource = new ActivitySource(ActivitySourceName);

    var innerClient = AppSupport.CreateChatClient(settings).AsIChatClient();
    IChatClient tracedClient = new ChatClientBuilder(innerClient)
        .UseOpenTelemetry(sourceName: ActivitySourceName, configure: c => c.EnableSensitiveData = captureContent)
        .Build();

    var datasetPath = o.DataPath ?? EvalDataset.FindDataFile("eval-dataset.jsonl");
    var limit = o.Limit > 0 ? o.Limit : 5;
    var cases = EvalDataset.Load(datasetPath).Take(limit).ToList();

    Console.WriteLine($"Tracing {cases.Count} cases -> Application Insights / Foundry Tracing tab\n");
    foreach (var evalCase in cases)
    {
        using var activity = activitySource.StartActivity("eval-case");
        activity?.SetTag("agent.case_id", evalCase.Id);
        activity?.SetTag("agent.category", evalCase.Category);
        activity?.SetTag("agent.customer_id", evalCase.CustomerId);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a Virtual Banking Assistant. Answer concisely."),
            new(ChatRole.User, evalCase.Query),
        };

        var response = await tracedClient.GetResponseAsync(messages);
        Console.WriteLine($"[{evalCase.Id}] traced ({response.Text.Length} chars)");
    }

    Console.WriteLine("\nDone. Open Foundry -> Tracing (traces may take 1-2 minutes to appear).");
    return 0;
}

static string Flag(IReadOnlyDictionary<string, bool> passed, string metricName)
    => passed.TryGetValue(metricName, out var ok) ? (ok ? "1" : "0") : "-";

internal sealed record Options(string Task, string? DataPath, bool Online, int Limit, string CustomerId)
{
    public static Options Parse(string[] args)
    {
        string task = "";
        string? dataPath = null;
        bool online = false;
        int limit = 0;
        string customer = "CUST-1001";

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--task" when i + 1 < args.Length:
                    task = args[++i];
                    break;
                case "--data" when i + 1 < args.Length:
                    dataPath = args[++i];
                    break;
                case "--online":
                    online = true;
                    break;
                case "--limit" when i + 1 < args.Length:
                    _ = int.TryParse(args[++i], out limit);
                    break;
                case "--customer" when i + 1 < args.Length:
                    customer = args[++i];
                    break;
            }
        }

        return new Options(task, dataPath, online, limit, customer);
    }
}
