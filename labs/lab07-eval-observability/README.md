# Lab 07 – Testing, Evaluation, and Observability

## Overview

Implement quality gates, evaluation pipelines, and enterprise monitoring for agents
built in previous labs. Ensure agents are production-ready with measurable quality.

## Learning Objectives

- Define quality metrics for conversational agents
- Build evaluation pipelines using Foundry evaluators or custom scripts
- Write deterministic, domain-specific custom evaluators and run them as an
  offline CI quality gate (Python **and** .NET)
- Instrument an agent with OpenTelemetry GenAI tracing to Azure Monitor
- Enable Copilot Studio analytics
- Configure Application Insights dashboards for agent observability

## Prerequisites

- Agents from Labs 02–06 deployed and accessible
- Application Insights / Log Analytics workspace enabled
- Access to conversation logs (non-PII or redacted)

> ⚠️ See [environment checklist](../../docs/environment-checklist.md) section A4.

## Lab Steps

### Step 1: Define Quality Metrics

Establish metrics for your agents:

| Metric | Description | Target |
|---|---|---|
| **Groundedness** | Are responses grounded in provided knowledge? | > 90% |
| **Relevance** | Do responses answer the user's question? | > 85% |
| **Coherence** | Are responses well-structured and clear? | > 90% |
| **Fluency** | Is the language natural and grammatical? | > 95% |
| **Safety** | Are responses free of harmful content? | 100% |
| **Completion rate** | Do users achieve their goal? | > 80% |

### Step 2: Build an Evaluation Dataset

1. Collect or create test conversations (10–20 examples minimum)
2. Include:
   - User messages (inputs)
   - Expected responses or acceptable response criteria
   - Context / knowledge sources used
3. Save as a structured dataset (JSON or CSV)

### Step 3: Run Evaluations

#### Option A: Azure AI Foundry Evaluators

1. Use built-in evaluators (groundedness, relevance, coherence)
2. Configure the evaluation pipeline
3. Run against your test dataset
4. Review evaluation scores

#### Option B: Custom Evaluation Pipeline

1. Create a script that sends test inputs to your agent
2. Compare agent responses against expected outcomes
3. Score using LLM-as-judge or rule-based criteria
4. Generate a summary report

### Step 4: Copilot Studio Analytics

1. Open Copilot Studio → **Analytics**
2. Review:
   - Session completion rates
   - Topic triggering accuracy
   - Escalation rates
   - User satisfaction scores
3. Identify topics that need improvement

### Step 5: Application Insights Observability

1. Open Application Insights for your deployed agents
2. Explore:
   - Request traces and latency
   - Tool-calling success/failure rates
   - Token usage and model performance
   - Error rates and exceptions
3. Create a dashboard with key agent health metrics
4. Set up alerts for critical failures

## Hands-On: Build and Run the Evaluation Harness

Steps 1–5 above are the conceptual workflow. This section is the runnable
implementation that **evaluates the Lab 03 banking agent** in both Python and
.NET. Everything except response generation, the online (LLM-judge) evaluators,
tracing export, and red teaming runs fully offline — no Azure required — so you
can try the quality gate immediately.

### What you build

| Artifact | Python | .NET | Azure needed? |
|---|---|---|---|
| Evaluation dataset (15 banking cases) | `data/eval-dataset.jsonl` | ← same file | No |
| Custom rule-based evaluators | `src/custom_evaluators.py` | `BankingEval.Shared/Evaluators/*.cs` | No |
| Evaluator unit tests | `tests/test_custom_evaluators.py` | `BankingEval.Tests/CustomEvaluatorTests.cs` | No |
| Offline quality gate | `src/evaluate_agent.py` | `BankingEval.Console --task evaluate` | No |
| Generate responses from the agent | `src/generate_responses.py` | `BankingEval.Console --task generate` | Yes |
| Online (LLM-judge) evaluators | `src/evaluate_agent.py --online` | `BankingEval.Console --task evaluate --online` | Yes |
| OpenTelemetry tracing → Azure Monitor | `src/trace_agent.py` | `BankingEval.Console --task trace` | Yes |
| AI red teaming scan | `src/red_team_scan.py` | *(Python-only — see below)* | Yes |
| Workbook KQL queries | `queries/*.kql` | ← same files | Yes |
| CI quality-gate workflow | `ci/eval-gate.yml` (both jobs) | ← same file | No |

### Folder layout

```text
lab07-eval-observability/
├── data/
│   ├── eval-dataset.jsonl        # 15 labeled banking cases (shared by both stacks)
│   └── responses.sample.jsonl    # committed clean sample the offline gate scores
├── src/                          # Python path
│   ├── custom_evaluators.py
│   ├── generate_responses.py
│   ├── evaluate_agent.py
│   ├── trace_agent.py
│   ├── red_team_scan.py
│   └── requirements.txt
├── tests/
│   └── test_custom_evaluators.py
├── src-dotnet/                   # .NET path
│   ├── BankingEval.slnx
│   ├── BankingEval.Shared/       # dataset model + custom IEvaluators
│   ├── BankingEval.Console/      # generate | evaluate | trace
│   └── BankingEval.Tests/        # xUnit tests for the evaluators
├── queries/                      # Application Insights / Log Analytics KQL
└── ci/
    └── eval-gate.yml             # reference GitHub Actions quality gate
```

### Custom evaluators: encoding banking policy

General-purpose evaluators tell you whether an answer is *good*; they do not
know your *domain policy*. Three deterministic evaluators encode rules that must
hold on every banking response (pass = 1.0 / fail = 0.0):

- **PII leakage** — fails if a full card/account number (13–19 digits) or an SSN
  appears. Accounts may only be referenced by their last four digits.
- **Currency format** — every dollar amount must be `$` + optional thousands
  separators + exactly two decimals (`$3,842.56`, not `$3842.5`).
- **Required disclosure** — any loan quote (payment or APR) must carry an
  "estimate / subject to change" disclosure.

Because they are pure rules (no model call) they are fast, free, deterministic,
and ideal for unit tests and an offline CI gate. The Python and .NET
implementations enforce identical logic and ship with matching unit tests.

### Run it — Python

```bash
cd labs/lab07-eval-observability
cp src/.env.example src/.env          # then fill in your values
python -m pip install -r src/requirements.txt

# 1. Unit-test the custom evaluators (offline)
python -m pytest tests/ -v

# 2. Offline quality gate vs the committed sample (offline; non-zero exit on failure)
python src/evaluate_agent.py --data data/responses.sample.jsonl

# 3. Generate fresh answers from the Lab 03 agent (needs Azure sign-in), then gate them
python src/generate_responses.py                      # writes data/responses.jsonl
python src/evaluate_agent.py --data data/responses.jsonl

# 4. Add the LLM-judge quality evaluators (needs an evaluator model)
python src/evaluate_agent.py --data data/responses.jsonl --online

# 5. Emit OpenTelemetry traces to Azure Monitor / Foundry
python src/trace_agent.py

# 6. Adversarial scan (preview)
python src/red_team_scan.py
```

Python 3.10–3.12 is recommended for the Azure evaluation SDK. The offline gate
and unit tests only need `pytest` and `python-dotenv`.

### Run it — .NET

```bash
cd labs/lab07-eval-observability/src-dotnet
cp .env.example .env                  # then fill in your values

# 1. Unit-test the custom evaluators (offline)
dotnet test

# 2. Offline quality gate vs the committed sample (offline; non-zero exit on failure)
dotnet run --project BankingEval.Console -- --task evaluate --data ../data/responses.sample.jsonl

# 3. Generate fresh answers from the Lab 03 agent (needs Azure sign-in), then gate them
dotnet run --project BankingEval.Console -- --task generate          # writes data/responses.jsonl
dotnet run --project BankingEval.Console -- --task evaluate

# 4. Add the LLM-judge quality evaluators (needs an evaluator model)
dotnet run --project BankingEval.Console -- --task evaluate --online

# 5. Emit OpenTelemetry traces to Azure Monitor / Foundry
dotnet run --project BankingEval.Console -- --task trace --limit 5
```

The Console reuses the Lab 03 `BankingAssistant.Shared` project (via a project
reference) as the system under test, and the
`Microsoft.Extensions.AI.Evaluation` libraries for scoring.

### Offline CI quality gate

`ci/eval-gate.yml` is a ready-to-copy GitHub Actions workflow with a Python job
and a .NET job. Both run with **no Azure credentials**: they unit-test the
evaluators and score `data/responses.sample.jsonl`, failing the build on any
violation. Copy it into `.github/workflows/` and, when you are ready, add a
generate step with OIDC auth plus a nightly `--online` run for LLM-judge metrics.

### Dashboards

`queries/*.kql` holds starter Application Insights / Log Analytics queries for a
workbook: GenAI token usage, estimated cost by model, latency percentiles,
failed requests and throttling, tool-call reliability, and evaluation-score
trends. See [Operational Dashboards and Alerts](#operational-dashboards-and-alerts)
below for the tile list, and the caveat about the `dependencies` vs `traces`
table in each query's header comment.

### .NET and Python parity

Evaluation and tracing have **full parity** across both stacks — custom
evaluators, the offline gate, LLM-judge quality evaluators, and OpenTelemetry
export all have first-class .NET and Python implementations here.

| Capability | Python | .NET | Notes |
|---|---|---|---|
| Custom rule-based evaluators | ✅ | ✅ | `IEvaluator` in .NET; callable classes in Python |
| Offline quality gate (CI) | ✅ | ✅ | Both exit non-zero on failure |
| LLM-judge quality evaluators | ✅ `azure-ai-evaluation` | ✅ `Microsoft.Extensions.AI.Evaluation.Quality` | Groundedness, relevance, coherence, fluency; Python adds similarity, .NET adds equivalence |
| Response generation from the agent | ✅ | ✅ | Reuses the Lab 03 agent in both stacks |
| OpenTelemetry GenAI tracing → Azure Monitor | ✅ | ✅ | `configure_azure_monitor` / `UseOpenTelemetry` + Azure Monitor exporter |
| **AI red teaming (PyRIT)** | ✅ | ❌ | **Python-only** — see mitigation below |

**The one gap: AI Red Teaming.** The AI Red Teaming Agent is built on PyRIT and
ships only in the Python `azure-ai-evaluation[redteam]` package; there is no .NET
SDK today. For a .NET-first team, mitigate by either (a) running the Python
`red_team_scan.py` as a CI/release step — it targets your deployed agent over
HTTP regardless of the agent's language — or (b) running scans from the Azure AI
Foundry portal. Everything else in this lab is native .NET.

## Foundry Observability Best Practices

Agent observability combines **tracing**, **evaluation**, and **monitoring**.
Traditional APM shows latency, exceptions, and dependencies, but agents also
introduce non-deterministic responses, multi-step tool calls, token cost,
prompt/context changes, and quality drift. Treat observability as a feedback
loop: trace what happened, evaluate whether it was good, and monitor trends.
See the [Microsoft Foundry observability overview](https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/observability)
for the lifecycle model.

```mermaid
flowchart LR
    A[Agent or Copilot] --> B[OpenTelemetry spans\nGenAI semantic conventions]
    B --> C[Azure Monitor\nApplication Insights]
    C --> D[Foundry portal\nTracing tab]
    C --> E[Azure Monitor\nAgents preview]
    D --> F[Evaluation runs\nquality and safety]
    E --> F
    F --> G[Dashboards, alerts,\nCI/CD quality gates]
```

### Three Pillars for Agents

| Pillar | What to capture | Why it matters |
|---|---|---|
| **Tracing** | Agent runs, model calls, tool invocations, retrieval steps, span attributes | Explains why an answer changed, which tool failed, and where latency or cost was introduced |
| **Evaluation** | Groundedness, relevance, coherence, fluency, similarity, safety, task and tool quality | Measures response quality and safety, not just uptime |
| **Monitoring** | Latency, tokens, cost, errors, throttling, quality scores, traffic volume | Detects production regressions, drift, and operational incidents |

### Tracing with OpenTelemetry GenAI Semantic Conventions

Microsoft Foundry tracing stores traces in Application Insights and surfaces
them in the Foundry portal **Tracing** tab. The trace data uses OpenTelemetry
Generative AI semantic conventions (`gen_ai.*` attributes) for model calls,
tool invocations, and agent steps. Azure Monitor's Agents experience also uses
these semantics for cross-framework views. See the Foundry
[trace application guide](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/trace-application),
Azure Monitor [Agents view](https://learn.microsoft.com/en-us/azure/azure-monitor/app/agents-view),
and [OpenTelemetry GenAI semantic conventions](https://opentelemetry.io/docs/specs/semconv/gen-ai/).

1. Link an Application Insights resource to the Foundry project.
2. Enable telemetry for the agent/application where the SDK supports it
   (for example, `enable_telemetry=True` or the equivalent project setting).
3. Configure Azure Monitor OpenTelemetry export using the project's
   Application Insights connection string.
4. Run the agent, then open Foundry → **Tracing** to inspect the end-to-end run.

```bash
pip install azure-ai-projects azure-monitor-opentelemetry \
    opentelemetry-instrumentation-openai-v2
```

```python
import os
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from azure.monitor.opentelemetry import configure_azure_monitor
from opentelemetry.instrumentation.openai_v2 import OpenAIInstrumentor

project_client = AIProjectClient(
    credential=DefaultAzureCredential(),
    endpoint=os.environ["AZURE_AI_PROJECT"],
)

connection_string = (
    project_client.telemetry.get_application_insights_connection_string()
)

configure_azure_monitor(connection_string=connection_string)
OpenAIInstrumentor().instrument()

# Calls made through the project client now emit OpenTelemetry spans.
client = project_client.get_openai_client()
response = client.chat.completions.create(
    model=os.environ["MODEL_DEPLOYMENT_NAME"],
    messages=[{"role": "user", "content": "Summarize the account policy."}],
)
```

> ⚠️ Content capture is a privacy decision. Keep message content capture off by
> default for production unless approved. For development, the OpenTelemetry
> instrumentation supports `OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT`
> and Foundry `trace-content` settings. Use environment-specific configuration
> and redact or avoid PII in prompts, retrieved context, traces, and logs.

Add your own spans around orchestration logic so the trace tells the full story:

```python
from opentelemetry import trace

tracer = trace.get_tracer(__name__)

@tracer.start_as_current_span("route-customer-request")
def route_customer_request(intent: str, tool_name: str):
    span = trace.get_current_span()
    span.set_attribute("agent.intent", intent)
    span.set_attribute("agent.tool.selected", tool_name)
    # Call retrieval, tools, and model here.
```

### Continuous and Online Evaluation

Do not limit evaluation to the offline dataset from Step 2. Use the Azure AI
Evaluation SDK (`azure-ai-evaluation`) to run quality and safety evaluators
against both curated test cases and sampled production traffic. Foundry supports
built-in evaluators for groundedness, relevance, coherence, fluency,
similarity, and risk/safety checks such as hate/unfairness, violence, sexual,
self-harm, protected material, and indirect attacks. Agent evaluation also adds
agent-specific checks such as intent resolution, tool call accuracy, and task
adherence; some agent evaluation capabilities are preview. See the
[Evaluation SDK guide](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk)
and [agent evaluation guide](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/agent-evaluate-sdk).

```bash
pip install azure-ai-evaluation
```

```python
import os

from azure.ai.evaluation import (
    CoherenceEvaluator,
    FluencyEvaluator,
    GroundednessEvaluator,
    RelevanceEvaluator,
    SimilarityEvaluator,
)

model_config = {
    "azure_endpoint": os.environ["AZURE_OPENAI_ENDPOINT"],
    "azure_deployment": os.environ["EVALUATOR_MODEL_DEPLOYMENT"],
    "api_version": os.environ.get("AZURE_OPENAI_API_VERSION", "2024-12-01-preview"),
}

evaluators = {
    "groundedness": GroundednessEvaluator(model_config),
    "relevance": RelevanceEvaluator(model_config),
    "coherence": CoherenceEvaluator(model_config),
    "fluency": FluencyEvaluator(model_config),
    "similarity": SimilarityEvaluator(model_config),
}

row = {
    "query": "What documents are needed to open an account?",
    "response": agent_response,
    "context": retrieved_context,
    "ground_truth": expected_answer,
}

scores = {name: evaluator(**row) for name, evaluator in evaluators.items()}
```

Recommended workshop pattern:

1. **Pull request gate**: run a small deterministic dataset in CI/CD and fail
   the build if key scores drop below the targets from Step 1.
2. **Nightly regression**: run the full evaluation dataset plus recent failure
   examples and compare with the previous baseline.
3. **Online sampling**: sample a low percentage of redacted production traffic,
   evaluate asynchronously, and send scores back to Application Insights as
   custom metrics or span attributes.
4. **Human review queue**: route low-scoring or safety-flagged conversations to
   a review process before adding them to the regression set.

### Unified Agent Observability in Azure Monitor

Application Insights includes an **Agents (Preview)** experience that provides
a unified view of agent telemetry from Microsoft Foundry, Copilot Studio, and
third-party frameworks that emit OpenTelemetry GenAI semantics. Use it to:

- Inspect end-to-end agent runs, model calls, tool calls, and failures.
- Sort traces by token usage to find expensive interactions.
- Filter traces with GenAI errors to debug failed or low-quality runs.
- Correlate trace IDs with evaluation scores and safety findings.
- Open Foundry or Azure Monitor from the same investigation path.

When possible, write the evaluation run ID, evaluator names, and scores back to
the active trace or to a custom event keyed by `operation_Id`. This keeps
"what happened" and "was it good" together during incident review.

### Operational Dashboards and Alerts

Create an Application Insights workbook or dashboard with at least these tiles:

| Signal | Example threshold |
|---|---|
| P50/P95 latency by model, agent, and tool | Alert when P95 is 2x baseline for 15 minutes |
| Token usage and estimated cost | Alert on daily spend spikes or unusual model mix |
| Tool-call success/failure rate | Alert on critical tool failure rate > 5% |
| Error and exception rate | Alert on new exception types or error rate > 2% |
| Throttling / 429s | Alert on sustained 429s or retry exhaustion |
| Quality and safety scores | Alert when groundedness, relevance, or safety drops below target |

Example KQL starter queries for a workbook:

```kusto
// GenAI traces with token counts and duration
traces
| where timestamp > ago(24h)
| extend model = tostring(customDimensions["gen_ai.request.model"])
| extend inputTokens = todouble(customDimensions["gen_ai.usage.input_tokens"])
| extend outputTokens = todouble(customDimensions["gen_ai.usage.output_tokens"])
| summarize totalTokens=sum(inputTokens + outputTokens),
            traceCount=count()
    by model, bin(timestamp, 1h)
| order by timestamp desc
```

```kusto
// Failed requests, exceptions, and throttling
requests
| where timestamp > ago(24h)
| summarize requests=count(), failures=countif(success == false),
            p95Duration=percentile(duration, 95)
    by cloud_RoleName, resultCode, bin(timestamp, 15m)
| where failures > 0 or resultCode == "429"
```

> ⚠️ Schema details vary by SDK, exporter, and sampling configuration. If a
> `gen_ai.*` attribute is not in `customDimensions`, inspect a recent trace in
> Application Insights and adjust the workbook query to match your telemetry.

### AI Red Teaming as an Observability Signal

Add adversarial testing to the same quality loop. The Azure AI Foundry AI Red
Teaming Agent (preview) is built on PyRIT and can run automated scans against a
model endpoint or application callback for risks such as violence,
hate/unfairness, sexual content, self-harm, protected material, code
vulnerability, and ungrounded attributes. Use it before production releases and
on a schedule for high-risk changes. See the
[AI Red Teaming Agent guide](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/run-scans-ai-red-teaming-agent).

```bash
pip install "azure-ai-evaluation[redteam]"
```

```python
import os

from azure.ai.evaluation.red_team import RedTeam, RiskCategory
from azure.identity import DefaultAzureCredential

red_team_agent = RedTeam(
    azure_ai_project=os.environ["AZURE_AI_PROJECT"],
    credential=DefaultAzureCredential(),
    risk_categories=[
        RiskCategory.Violence,
        RiskCategory.HateUnfairness,
        RiskCategory.Sexual,
        RiskCategory.SelfHarm,
    ],
    num_objectives=5,
)

async def target_callback(query: str) -> str:
    return call_agent(query)

red_team_result = await red_team_agent.scan(target=target_callback)
```

Capture scan results as release evidence and create work items for failures.
Because red teaming and some agent evaluation features are preview, confirm
region support and production suitability before using them in a live system.

### Data Governance Checklist

- Use mock or redacted data in workshop traces and evaluation datasets.
- Disable prompt/response content capture in production unless there is an
  approved data handling requirement.
- Redact account numbers, names, addresses, access tokens, and other PII before
  logs or traces leave the application boundary.
- Separate development and production telemetry settings with environment
  variables, not code changes.
- Restrict Application Insights and Log Analytics access with least privilege.
- Document sampling rates, retention settings, and who can view trace content.

## Deliverables

- [ ] Quality metrics defined with targets
- [ ] Evaluation dataset created (10+ test cases)
- [ ] Evaluation pipeline run with results documented
- [ ] Copilot Studio analytics reviewed (if applicable)
- [ ] Application Insights dashboard created
- [ ] Foundry tracing enabled and verified in the Tracing tab
- [ ] Continuous evaluation plan documented, including CI/CD quality gates
- [ ] Agent observability workbook or dashboard includes latency, cost, errors,
  tool calls, and quality scores
- [ ] Data governance settings documented for trace content capture and PII redaction
- [ ] AI red teaming scan or plan documented for safety regression testing
- [ ] Key findings and improvement recommendations documented

## Next Steps

→ [Lab 08: Managing Knowledge, Tools & Skills](../lab08-knowledge-and-tools/) — Foundry IQ
knowledge bases, MCP tools, and skills

## References

- [Trace AI applications in Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/trace-application)
- [Observability in Generative AI - Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/observability)
- [Local evaluation with the Azure AI Evaluation SDK](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/evaluate-sdk)
- [Agent evaluation with the Microsoft Foundry SDK](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/agent-evaluate-sdk)
- [.NET: The Microsoft.Extensions.AI.Evaluation libraries](https://learn.microsoft.com/en-us/dotnet/ai/conceptual/evaluation-libraries)
- [.NET: Tutorial — evaluate the quality of a model's response](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/evaluate-ai-response)
- [Monitor AI agents with Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/agents-view)
- [Run AI Red Teaming Agent locally](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/run-scans-ai-red-teaming-agent)
- [OpenTelemetry GenAI semantic conventions](https://opentelemetry.io/docs/specs/semconv/gen-ai/)
