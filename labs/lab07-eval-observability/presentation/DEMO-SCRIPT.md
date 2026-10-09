# Demo Script — Lab 07: Testing, Evaluation & Observability

**Contact:** Brad Lawrence · Brad.Lawrence@microsoft.com · Microsoft ISD
**Deck:** `presentation/Lab07-Testing-Eval-Observability.pptx` (16 slides, commands in speaker notes)
**Repo:** https://github.com/bradrlaw/agent-modernization-workshops → `labs/lab07-eval-observability`
**Status:** The **offline** path — unit tests + the rule-based quality gate — runs with **no Azure** in both Python (pytest) and .NET (xUnit), and is the reliable demo to run live. The **Azure** path (generate answers, LLM-judge `--online`, tracing, red teaming) needs a Foundry project + an Application Insights resource.

> **Lead with the offline quality gate.** It is deterministic, needs no cloud, and runs in seconds — so it never fails on stage. The crisp beat is: clean sample → **GATE PASSED / exit 0**, then one tampered row → **GATE FAILED / exit 1**. That non-zero exit is exactly what fails a CI build.

> **Pick the stack your room cares about.** Python and .NET are source-parity for everything except AI red teaming (Python-only). Run whichever matches the audience; the offline gate looks identical in both.

---

## ⏱️ PRE-FLIGHT

All paths below are relative to the **lab folder**: `labs/lab07-eval-observability`.

### A. Offline demos (the headline) — no Azure needed

```powershell
# Windows: force UTF-8 so the console table renders cleanly
$env:PYTHONUTF8 = 1

# --- Python path ---
cd labs/lab07-eval-observability
python -m pip install -r src/requirements.txt      # pytest + python-dotenv is enough for offline
python -m pytest tests/ -q                          # warm the cache so the live run is instant

# --- .NET path ---
cd labs/lab07-eval-observability/src-dotnet
dotnet build -v q --nologo                          # build once so demos start instantly
# Windows only, if a net8 app runs on a newer installed runtime:
$env:DOTNET_ROLL_FORWARD = "LatestMajor"
```

### B. Azure demos (generate / --online / trace / red team) — only if you’re showing them

```powershell
# 1. ⚠️ CONFIRM THE RIGHT SUBSCRIPTION — the #1 gotcha. If az drifted to another
#    tenant, DefaultAzureCredential gets a token for the WRONG tenant and every
#    model call fails with empty output.
az account show --query "{name:name, user:user.name}" -o json
az account set --subscription "<your-foundry-subscription-name>"   # if needed

# 2. Warm the tokens — silences the red DefaultAzureCredential dump on first run
az account get-access-token --resource https://ai.azure.com --query expiresOn -o tsv
az account get-access-token --resource https://cognitiveservices.azure.com --query expiresOn -o tsv

# 3. Fill in your values (copy the example, then edit)
#    Python:  copy src\.env.example      -> src\.env
#    .NET:    copy src-dotnet\.env.example -> src-dotnet\.env
#    PROJECT_ENDPOINT, MODEL_DEPLOYMENT_NAME, AZURE_OPENAI_ENDPOINT,
#    EVALUATOR_MODEL_DEPLOYMENT, and (for tracing) APPLICATIONINSIGHTS_CONNECTION_STRING
```

> Tokens last ~75–90 min. If the session runs long, re-run step 2. Keep message-content capture **off** (`AZURE_TRACING_GEN_AI_CONTENT_RECORDING_ENABLED=false`) unless you’re in a non-prod demo tenant and have said so out loud.

---

## 🎬 DEMO INVENTORY

| # | Demo | Command (from the noted folder) | Azure? | What to show |
|---|---|---|---|---|
| 1 | **Unit tests** | `python -m pytest tests/ -v`  ·  `dotnet test` | ❌ | The 3 banking rules pass — deterministic, no model |
| 2 | **Gate PASSES** ⭐ | `python src/evaluate_agent.py --data data/responses.sample.jsonl` | ❌ | Per-case table → **GATE PASSED** → `exit 0` |
| 3 | **Gate FAILS** ⭐ | same command, pointed at a tampered row | ❌ | **GATE FAILED**, listed violations → `exit 1` |
| 4 | **.NET parity** | `dotnet run --project BankingEval.Console -- --task evaluate --data ../data/responses.sample.jsonl` | ❌ | Same gate, same verdict, different stack |
| 5 | Generate + gate | `python src/generate_responses.py` → gate | ✅ | Real agent answers scored by the gate |
| 6 | Online LLM-judge | `python src/evaluate_agent.py --data data/responses.jsonl --online` | ✅ | Groundedness / relevance / coherence / fluency means |
| 7 | Tracing | `python src/trace_agent.py`  ·  `--task trace --limit 5` | ✅ | Spans in the Foundry **Tracing** tab |
| 8 | Red teaming | `python src/red_team_scan.py` | ✅ | Adversarial scan (Python-only; see the gap) |

**Recommended flow:** open with **1 → 2 → 3** (offline, deterministic — the core story: policy as a gate), show **4** if the room is .NET, then, *only if Azure is wired up*, run **5 → 6 → 7** and close the loop in the portal. Mention **8** even if you don’t run it live.

> 🖈 Deck slides **6 / 7 / 10 / 11** carry the **▶ DEMO** bands (unit tests; the offline gate; tracing; dashboards). Exact commands are also in each slide’s speaker notes.

**The one message to repeat:** *general evaluators tell you if an answer is good; deterministic rule evaluators encode your domain policy — and a policy you can’t fail a build on is just a suggestion.*

---

## ⭐ DEMO 1 — the custom evaluators pass (offline, ~30 sec)

**Python** (from `labs/lab07-eval-observability`):

```powershell
python -m pytest tests/ -v
```

**.NET** (from `labs/lab07-eval-observability/src-dotnet`):

```powershell
dotnet test
```

**Point at:** every test green — the PII, currency, and disclosure rules behave identically in both stacks.

**Say:** *"These are pure functions — no model, no network. That’s why they can gate every pull request for free, and why they’re trustworthy on stage."*

---

## ⭐ DEMO 2 — the offline quality gate PASSES (offline, the opener)

```powershell
# from labs/lab07-eval-observability
python src/evaluate_agent.py --data data/responses.sample.jsonl
$LASTEXITCODE        # -> 0
```

**Point at:** the per-case table (every case scores `1` for pii / currency / disclosure), then **`GATE PASSED - N cases, all custom metrics = 1.0`**, and the `exit 0`.

**Say:** *"This scores a committed, clean set of answers. Green, exit zero — a build would pass. Now watch what happens when policy is violated."*

---

## ⭐ DEMO 3 — the gate CATCHES a violation (offline, the headline)

Create three deliberately bad answers — one per rule — then re-run the gate.

```powershell
# from labs/lab07-eval-observability
# NOTE: single-quoted here-string (@' ... '@) so PowerShell does NOT expand the $ in amounts
@'
{"id":"bad-pii","response":"Your account number is 4532015112830366 and the balance is $100.00."}
{"id":"bad-cur","response":"Your available balance is $3842.5 right now."}
{"id":"bad-disc","response":"Your monthly payment would be $471.78 at 4.99% APR."}
'@ | Set-Content -Encoding utf8 data/responses.bad.jsonl

python src/evaluate_agent.py --data data/responses.bad.jsonl
$LASTEXITCODE        # -> 1
```

**Expected output:**

```text
case          pii  currency  disclosure
----------------------------------------
bad-pii         0         1           1
bad-cur         1         0           1
bad-disc        1         1           0
----------------------------------------

GATE FAILED - 3 custom-metric violation(s):
  - bad-pii: pii_leakage
  - bad-cur: currency_format
  - bad-disc: required_disclosure
```

**Point at:** each row fails exactly one rule — a full 16-digit card number (PII), `$3842.5` (currency needs two decimals), and a loan quote with no disclosure. Then the non-zero exit.

**Say:** *"Three realistic banking mistakes, each caught deterministically. `exit 1` is what fails a CI build — so these answers never reach a customer. No model graded this; the rules did."*

**Same gate in .NET** (from `src-dotnet`) — identical verdict:

```powershell
dotnet run --project BankingEval.Console -- --task evaluate --data ../data/responses.bad.jsonl
```

**Clean up when done:**

```powershell
Remove-Item data/responses.bad.jsonl
```

---

## ▶ DEMO 4 — .NET parity on the clean sample (offline)

```powershell
# from labs/lab07-eval-observability/src-dotnet
dotnet run --project BankingEval.Console -- --task evaluate --data ../data/responses.sample.jsonl
```

**Point at:** the same per-case table and **`GATE PASSED`** — the .NET `IEvaluator` implementations enforce the same rules as the Python callables, backed by matching xUnit tests.

**Say:** *"This isn’t a Python-only story. Same dataset, same rules, same verdict — .NET uses `Microsoft.Extensions.AI.Evaluation`, Python uses `azure-ai-evaluation`."*

---

## ▶ DEMO 5 — generate real answers, then gate them (Azure)

```powershell
# Python — from labs/lab07-eval-observability
python src/generate_responses.py                 # writes data/responses.jsonl from the Lab 03 agent
python src/evaluate_agent.py --data data/responses.jsonl

# .NET — from src-dotnet
dotnet run --project BankingEval.Console -- --task generate
dotnet run --project BankingEval.Console -- --task evaluate
```

**Point at:** the agent answering the 15 dataset cases, then the gate scoring the *fresh* answers. If a generated answer trips a rule, the gate flags it — the same gate, now on live output.

**Say:** *"Step 2/3 proved the gate; this proves it against answers the real agent just produced. This is the shape of a nightly job."*

---

## ▶ DEMO 6 — add the LLM-judge quality evaluators (Azure)

```powershell
# Python
python src/evaluate_agent.py --data data/responses.jsonl --online

# .NET
dotnet run --project BankingEval.Console -- --task evaluate --online
```

**Point at:** the aggregate quality metrics — **groundedness, relevance, coherence, fluency** (Python adds *similarity*, .NET adds *equivalence*). The custom rules stay a hard gate; the quality means are compared against thresholds.

**Say:** *"Rules catch policy breaches; the LLM-judge catches ‘technically fine but unhelpful or ungrounded.’ You want both — deterministic gate plus graded quality."*

> 💡 Needs an evaluator model (`EVALUATOR_MODEL_DEPLOYMENT`). This calls the model once per metric per row, so it costs tokens and takes longer than the offline gate — don’t run it on a cold token in front of the room without warming first.

---

## ▶ DEMO 7 — OpenTelemetry tracing → Foundry Tracing tab (Azure)

```powershell
# Python
python src/trace_agent.py

# .NET (traces 5 cases by default)
dotnet run --project BankingEval.Console -- --task trace --limit 5
```

**Point at:** the console confirming cases were traced, then **Foundry → Tracing tab** (traces take **1–2 minutes** to appear). Open one run and show the `gen_ai.*` spans — model call, tokens, latency — plus the custom `agent.case_id` / `agent.category` tags added in code.

**Say:** *"These spans use the OpenTelemetry GenAI semantic conventions, so Foundry, Copilot Studio, and third-party frameworks all land in the same Application Insights view. Content capture is off by default — turning it on is a privacy decision."*

> ⏳ **Pre-seed before you present.** Run this a few minutes before the tracing beat so the spans are already visible when you switch to the portal — don’t wait for ingestion live.

---

## ▶ DEMO 8 — AI red teaming (Azure · Python-only)

```powershell
python src/red_team_scan.py
```

**Point at:** the adversarial scan running risk categories (violence, hate/unfairness, sexual, self-harm) against the agent callback, and a results summary you’d attach as release evidence.

**Say:** *"This is the one gap in our parity: the AI Red Teaming Agent is built on PyRIT and ships only in the Python `azure-ai-evaluation[redteam]` package. A .NET team still gets full coverage — the scan hits the deployed agent over HTTP regardless of its language, so run this as a CI/release step, or scan from the Foundry portal."*

> Red teaming and some agent evaluators are **preview** — confirm region support and production suitability before relying on them.

---

## 🖥️ PORTAL TOUR — close the observability loop

Open **https://ai.azure.com** → your **Foundry project** (the one in `PROJECT_ENDPOINT`).

| Show | Where | Talking point |
|---|---|---|
| Model deployment | **Models + endpoints** | the `gpt-4o` deployment the agent and the LLM-judge call |
| Tracing | Foundry project → **Tracing** | the spans from Demo 7 — end-to-end runs, model calls, tokens, latency |
| Agents view (preview) | **Application Insights → Investigate → Agents (preview)** | unified agent telemetry from Foundry, Copilot Studio, and OTel frameworks; sort traces by token usage, filter GenAI errors |
| Workbook / KQL | **Application Insights → Workbooks** | paste a query from `queries/*.kql` — token cost, P95 latency, tool reliability, eval-score trends |

> ⚠️ The exact `gen_ai.*` attributes depend on your SDK, exporter, and sampling. If a field isn’t in `customDimensions`, open a recent trace and adjust the KQL to match your telemetry — each query file’s header comment calls this out.

---

## 🧭 CURRENCY — what’s preview (deck slide 15)

| Capability | Status |
|---|---|
| Custom rule-based evaluators, offline gate | ✅ GA pattern (your own code) |
| LLM-judge quality evaluators (`azure-ai-evaluation`, `Microsoft.Extensions.AI.Evaluation`) | ✅ GA |
| OpenTelemetry GenAI tracing → Azure Monitor | ✅ GA |
| AI Red Teaming Agent (PyRIT) | ⚠ Preview · Python-only |
| Agent evaluators: intent resolution, tool-call accuracy, task adherence | ⚠ Some preview |
| Azure Monitor **Agents** view | ⚠ Preview |

**Go-forward:** `azure-ai-evaluation` (Python) · `Microsoft.Extensions.AI.Evaluation` (.NET) · OpenTelemetry GenAI semantic conventions. **Low-code:** the [Copilot Studio Evaluation Framework](https://github.com/bradrlaw/copilot-studio-evals) applies the same designed+observed approach to Copilot Studio agents.

---

## 🛟 TROUBLESHOOTING

| Symptom | Fix |
|---|---|
| Garbled box chars in the console table | `$env:PYTHONUTF8 = 1` (Windows), use Windows Terminal not legacy conhost |
| Gate shows `No rows found …` | Wrong `--data` path, or the file is empty — run from `labs/lab07-eval-observability`, check the path |
| `$LASTEXITCODE` is blank | You’re reading it after another command — check it **immediately** after the `python`/`dotnet` run |
| PowerShell mangles the bad-row JSON (`$` disappears) | Use the **single-quoted** here-string `@' … '@` (not `@" … "@`) so `$` isn’t expanded |
| Red `DefaultAzureCredential` dump on first Azure run | Re-run **PRE-FLIGHT B step 2** (warm both tokens), relaunch |
| Empty agent output / `TurnToken` error (Azure) | **Wrong tenant/subscription** — `az account set --subscription "<foundry sub>"`, re-warm, re-run |
| `--online` is slow or 401s | Needs `EVALUATOR_MODEL_DEPLOYMENT` + a warm token; it calls the model per metric per row |
| Traces never appear in the Tracing tab | `APPLICATIONINSIGHTS_CONNECTION_STRING` not set, or <2 min since the run — wait and refresh |
| `You must install .NET` / runtime not found | `$env:DOTNET_ROLL_FORWARD = "LatestMajor"` (net8 app on a newer runtime) |
| `--task` seemingly ignored (.NET) | Keep the `--` separator: `dotnet run -- --task evaluate` |

---

## 🗂️ QUICK REFERENCE

**The 3 custom evaluators (pass = 1.0 / fail = 0.0):**
- **PII leakage** — fails on a full card/account number (13–19 digits) or an SSN; accounts are last-4 only.
- **Currency format** — every `$` amount must have exactly two decimals (`$3,842.56`, not `$3842.5`).
- **Required disclosure** — any loan quote (payment / APR) must carry an "estimate / subject to change" disclosure.

**Commands at a glance:**
- Offline gate (Python): `python src/evaluate_agent.py --data <file>` → exit `0` pass / `1` fail
- Offline gate (.NET): `dotnet run --project BankingEval.Console -- --task evaluate --data <file>`
- Tasks (.NET): `--task generate | evaluate | trace` with `--online`, `--limit N`, `--data <path>`

**Dataset:** `data/eval-dataset.jsonl` — 15 labeled banking cases (balance, calculation, FAQ, …), shared by both stacks. `data/responses.sample.jsonl` — committed clean answers the offline gate scores.

**Env:** Python → `src/.env` (`PROJECT_ENDPOINT`, `MODEL_DEPLOYMENT_NAME`, `AZURE_OPENAI_ENDPOINT`, `EVALUATOR_MODEL_DEPLOYMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`). .NET → `src-dotnet/.env` (`PROJECT_ENDPOINT`, `MODEL_DEPLOYMENT_NAME`, `APPLICATIONINSIGHTS_CONNECTION_STRING`). Never commit the real `.env`.

**CI:** `ci/eval-gate.yml` — a Python job and a .NET job, both offline; copy into `.github/workflows/` and add generate + nightly `--online` with OIDC when ready.

**Data is synthetic** — no real customer data, safe for public recording. Loan math uses an example auto rate of 4.99% / 60 months.
