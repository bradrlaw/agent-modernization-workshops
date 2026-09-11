# Memory Demo — Cheat Sheet (Lab 06 follow-up)

**Contact:** Brad Lawrence · Brad.Lawrence@microsoft.com · Microsoft ISD
**Deck:** `Lab06-Agent-Memory-Strategies.pptx` (17 slides — recap + memory deep-dive; commands in speaker notes)
**Repo:** https://github.com/bradrlaw/agent-modernization-workshops → `labs/lab06-multi-agent`
**Companion:** `presentation/DEMO-SCRIPT.md` (the five orchestration patterns) · `MEMORY-GUIDANCE.md` (design guidance)

> **What this proves, in one line:** flip one flag — `--memory local` — and the same banking team can
> **answer a follow-up about a loan from an earlier session** ("finalize the loan we discussed") — it
> walks in already knowing the amount, term, and rate. With `--memory off` it can't: it has to ask you
> to repeat the details. The recalled **facts** are deterministic (no-LLM compactor), so it's stable on stage.

> **Demo the .NET path** — it's the reliable one to run live. The Python path is source-parity but
> needs **Python 3.10–3.12** (`agent-framework` wheels don't publish for 3.13+). If your Python is
> newer, show the `src/` code instead of running it.

---

## ⏱️ PRE-FLIGHT — run ONCE, ~2 min before you start

All paths are relative to the **repo root**.

```powershell
# 1. ⚠️ CONFIRM THE RIGHT SUBSCRIPTION — this is the #1 gotcha.
#    If az drifted to another tenant (e.g. an M365 dev tenant from a prior login),
#    DefaultAzureCredential gets a token for the WRONG tenant and every model call fails
#    with "Error invoking handler for ...TurnToken" and EMPTY agent output.
az account show --query "{name:name, user:user.name}" -o json
#    Not the subscription that holds your Foundry resource? Switch it:
az account set --subscription "<your-foundry-subscription-name>"

# 2. Warm the tokens — silences the red DefaultAzureCredential dump on first run
az account get-access-token --resource https://ai.azure.com --query expiresOn -o tsv
az account get-access-token --resource https://cognitiveservices.azure.com --query expiresOn -o tsv

# 3. Move into the .NET project
cd labs/lab06-multi-agent/src-dotnet/BankingConcierge

# 4. Point at YOUR Foundry project + model (or copy .env.example to .env)
$env:FOUNDRY_PROJECT_ENDPOINT = "https://<your-resource>.services.ai.azure.com/api/projects/<your-project>"
$env:FOUNDRY_MODEL = "gpt-4o"

# 5. Windows only: net8 app runs on a newer installed runtime
$env:DOTNET_ROLL_FORWARD = "LatestMajor"

# 6. Which customer the session runs as (memory is scoped to this ID)
$env:DEMO_CUSTOMER_ID = "CUST-1001"

# 7. Build once (~5s) so demos start instantly
dotnet build -v q --nologo
```

> Tokens last ~75–90 min. If your session runs long, re-run step 2. After the first build, use `--no-build`.

> **Fresh-start tip:** memory persists in `./.memory/<customer>.json` (git-ignored). To demo a
> guaranteed "first session," either use a new `DEMO_CUSTOMER_ID` or delete the file first:
> `Remove-Item .memory -Recurse -Force`.

---

## 🎬 THE KEY DEMO — memory changes what the team *can answer* (≈4 min)

**Seed one session, then ask a follow-up that omits the details.** With memory the team recalls the
loan and finalizes it; with memory off the same team has to ask you to repeat everything. The banner
is secondary — **the agents' answers are the proof.**

```powershell
# RESET (optional) — guarantee a clean "first session" for the demo customer
Remove-Item .memory\CUST-2001.json -ErrorAction SilentlyContinue

# RUN 1 — SEED (memory local): first contact. The team prepares the offer and SAVES the facts.
dotnet run --no-build -- --pattern sequential --memory local --customer CUST-2001 --task 'I would like a $25,000 auto loan for 60 months. Please prepare an offer with the required disclosures.'

# The follow-up finalizes "the loan we discussed" WITHOUT restating any numbers:
$fu = 'Please finalize the loan we discussed earlier and restate its amount, term, and rate before you send the disclosures.'

# RUN 2 — FOLLOW-UP, MEMORY ON: the team recalls $25,000 / 60-month / 4.99% and answers.
dotnet run --no-build -- --pattern sequential --memory local --customer CUST-2001 --task $fu

# RUN 3 — SAME FOLLOW-UP, MEMORY OFF: no record — the team asks you to re-supply the details.
dotnet run --no-build -- --pattern sequential --memory off --customer CUST-2001 --task $fu
```

**What to point at (banner + the agents' actual behavior):**

| Run | Banner | What the agents *do* | Say |
|---|---|---|---|
| 1 (seed) | `no prior memory for CUST-2001 yet (first session)` → tail `saved N turn(s)` | Prepare the auto-loan offer, then quietly compact + save. | "First contact — it works the request and saves the facts." |
| 2 (memory **ON**) | `recalled 1 prior session(s) … facts: $25,000, 60-month term, auto loan` | *"Based on our previous discussions — Loan Amount: $25,000, Term: 60 months, APR: 4.99%."* | "New question, no numbers in it — the team already knows the loan and finalizes it." |
| 3 (memory **OFF**) | `Memory: OFF — no cross-session recall` | *"Could you confirm the details we previously discussed (loan amount, term, and rate)?"* | "Same question, memory off — the team has no idea which loan and has to ask. **That's** the difference." |

> **Why this shows behavior, not just a banner:** the follow-up prompt deliberately says *"the loan we
> discussed"* and never restates the numbers. Memory-on injects the recalled facts into the specialists'
> instructions, so they answer; memory-off has nothing, so they ask. If you instead re-send a fully
> detailed request, both runs look identical — the prompt already carries every number, so recall has
> nothing to add. **The omission is the point.**

> **Ask only for amount, term, and rate** — those are stored deterministically (the three facts plus the
> `4.99% APR` decision), so recall is stable on stage. **Don't** ask for the monthly payment: the model
> varies it run-to-run and the compactor doesn't distill it into a fact.

**Optional — show the store (10 sec):** prove it's just a file you own.

```powershell
Get-Content .memory\CUST-2001.json | ConvertFrom-Json |
  Select-Object SessionCount, ConversationSummary, ImportantFacts, Decisions, PendingActions
```

**The one message to repeat:** *"I didn't change a single agent or workflow. Memory rides the same
seam Skills already used — recall→inject before the run, record→compact→save after. It's a host
concern, not an agent feature."*

---

## 🎛️ THE `--memory` MODES — one flag, one interface

| Mode | Backend | Status | Use it to show |
|---|---|---|---|
| `off` *(default)* | none | ✅ verified | Clean, stateless runs — every other pattern demo |
| `local` | JSON file under `.memory/` | ✅ **verified live** | The headline demo — BYO store, no Azure, offline |
| `cosmos` | Azure Cosmos DB (BYO managed store) | ✅ **verified live** | Durable, cross-session recall — same code, partitioned by `userId` |
| `foundry` | Foundry-managed Memory (preview) | ✅ **verified live** | Platform owns extraction + vector recall — dedicated single-agent managed demo |

> **Honesty note for the room:** all four modes are built and **verified live** today. `off`/`local`/`cosmos`
> are **BYO** — the app owns memory behind one `IMemoryStore` (recall→inject→compact→save), and `local`↔`cosmos`
> is a true **store swap** (Cosmos is **keyless** — Entra RBAC, no keys — on a **serverless** account you stand
> up in ~2 min with `infra/provision-cosmos.ps1`). `foundry` is the **managed** tier: it does **not** use
> `IMemoryStore` — it attaches a `FoundryMemoryProvider` to a **single agent + session** and the platform
> extracts/embeds/recalls automatically, so `--memory foundry` runs a dedicated single-agent demo (a different
> execution model), verified live end-to-end. The startup banner always prints the **backend it truly used** —
> it never claims a store it isn't using.

---

## 🌌 COSMOS MODE — the same demo, now durable & cross-session

`local` and `cosmos` run the **identical** seed→follow-up demo; only the store behind it changes.
Cosmos proves the memory survives across **processes and machines** (not just a file on your box) —
the follow-up run reads back what a *different* process wrote.

**One-time provision** (serverless, keyless — ~2 min; safe to reuse for every demo):

```powershell
# From labs/lab06-multi-agent. Creates a serverless account + db + container and grants YOUR
# signed-in identity the Cosmos DB Built-in Data Contributor (DATA-PLANE) role.
./infra/provision-cosmos.ps1 -ResourceGroup rg-agent-memory-lab06 -Location westus2
# It prints the three env values to set. The data-plane role takes ~1–2 min to propagate.
```

**Pre-flight (Cosmos):** set the endpoint and warm the Cosmos token (in addition to the Foundry token):

```powershell
$env:COSMOS_ENDPOINT  = "https://<your-account>.documents.azure.com:443/"
$env:COSMOS_DATABASE  = "agentmemory"
$env:COSMOS_CONTAINER = "conversations"
az account get-access-token --resource https://cosmos.azure.com --query expiresOn -o tsv   # warm
```

**Run the same seed→follow-up, but with `--memory cosmos`:**

```powershell
$fu = 'Please finalize the loan we discussed earlier and restate its amount, term, and rate before you send the disclosures.'

# RUN 1 — SEED: first contact on a fresh key. Prepares the offer and SAVES to Cosmos.
dotnet run --no-build -- --pattern sequential --memory cosmos --customer CUST-2001 --task 'I would like a $25,000 auto loan for 60 months. Please prepare an offer with the required disclosures.'

# RUN 2 — FOLLOW-UP: a DIFFERENT process reads Cosmos; the team recalls $25,000 / 60-mo / 4.99%.
dotnet run --no-build -- --pattern sequential --memory cosmos --customer CUST-2001 --task $fu
```

**What to point at:** the banner now reads **`Azure Cosmos DB — agentmemory/conversations at https://…`**
(not "local files"), and RUN 2 recalls the loan even though RUN 1 ran in a **separate process** — that's
durable, cross-session memory, same app code, one flag.

---

## 🧠 FOUNDRY MODE — platform-managed memory (a different execution model)

`--memory foundry` is **not** a store swap. Where `local`/`cosmos` inject a recall preamble into the
five-agent workflow, Foundry-managed Memory attaches a `FoundryMemoryProvider` to a **single agent +
session**; the platform extracts, embeds, and recalls automatically, scoped by `customerId`. So this
mode runs a dedicated **single-agent** concierge demo (seed turn → async extraction → fresh-session
recall probe) — faithful to how managed memory really works.

**One-time provision** (idempotent — grants the account **and** project managed identities the model role):

```powershell
# From labs/lab06-multi-agent. The Foundry memory service calls your deployments AS these identities.
./infra/provision-foundry-memory.ps1 -Account <your-ai-resource> -ResourceGroup <your-rg> -Project <your-project>
```

**Pre-flight (Foundry memory):** an **embedding** deployment + Foundry **Memory (preview)** on the project.

```powershell
$env:AZURE_AI_EMBEDDING_DEPLOYMENT_NAME = "text-embedding-3-small"   # your embedding deployment
$env:AZURE_AI_MEMORY_STORE_ID           = "lab06-agent-memory"       # any name; created on first run
az account get-access-token --resource https://ai.azure.com --query expiresOn -o tsv               # warm
```

**Run — seed on a fresh customer, then recall in a new process:**

```powershell
# RUN 1 — SEED: fresh scope. Creates/attaches the managed store, answers, then the service extracts.
dotnet run --no-build -- --memory foundry --customer CUST-3001 --task 'I would like a $25,000 auto loan for 60 months. Please prepare an offer with the required disclosures.'

# RUN 2 — RECALL: a DIFFERENT process, same --customer. Turn 1 already knows $25,000 / 60-mo / APR / payment.
dotnet run --no-build -- --memory foundry --customer CUST-3001 --task 'Finalize the loan we discussed and restate its amount, term, APR, and monthly payment.'
```

**What to point at:** the banner reads **`Foundry-managed (preview) — store 'lab06-agent-memory' … scope: CUST-3001`**;
there is **no** recall preamble and **no** local `.memory/` file — the platform owns storage — yet a
**separate-process** turn recalls the loan specifics. Use a **new** `--customer` for a clean first session.

> **⏱️ Timing — PRE-SEED before you present.** Extraction + vector indexing is **server-side and async**; even
> though RUN 1 waits (`WhenUpdatesCompletedAsync`), the in-session **recall probe on a brand-new scope can still say
> "I don't have any saved information yet"** — indexing hasn't caught up. This is expected on a *cold* scope, not a
> failure. **RUN 2** (separate process, same `--customer`) recalls reliably on turn 1. So for a live demo, **run RUN 1
> during setup a few minutes early**, then present **RUN 2** — recall is immediate and rich. (Local/Cosmos have no such
> lag — their recall is synchronous.)

---

## 🖥️ PORTAL TOUR — where managed memory lives

Open **https://ai.azure.com** → your **Foundry project** (the one in `FOUNDRY_PROJECT_ENDPOINT`).

| Show | Where | Talking point |
|---|---|---|
| The project | Overview | "Same project that backs Labs 03–06 — one endpoint." |
| Model deployment | **Models + endpoints** | your **gpt-4o** deployment — the model the agents call |
| Embedding model | **Models + endpoints** | Foundry-managed Memory needs an **embedding** deploy (e.g. `text-embedding-3-small`) — the vector index behind recall |
| Memory stores *(preview)* | **Agents → Memory** | where `--memory foundry` items land (store `lab06-agent-memory`) — the platform-owned equivalent of our `.memory/` file |
| Connections | **Management → Connections** | optional BYO connections; note the lab's `cosmos` mode connects **keyless** via `COSMOS_ENDPOINT` + Entra RBAC (no Connection needed) |

> For the **local** demo you don't touch the portal at all — that's the point of the BYO tier: it
> runs entirely on your box. The portal tour is for the **managed** (`foundry`) tier.

---

## 🛟 TROUBLESHOOTING

| Symptom | Cause → Fix |
|---|---|
| `Error invoking handler for …TurnToken` + **empty agent output** | **Wrong tenant/subscription.** `az` drifted (e.g. to an M365 dev tenant). → `az account set --subscription "<foundry sub>"`, re-warm tokens (pre-flight 1–2), re-run. **This is the most common failure.** |
| Red `DefaultAzureCredential` dump on first run | Cold token → re-run **pre-flight step 2**, relaunch |
| Banner says "first session" when you expected recall | Different `DEMO_CUSTOMER_ID`, or `.memory/` was cleared → re-run once to seed, then again to recall |
| `--memory cosmos` falls back to "local files" | `COSMOS_ENDPOINT` isn't set in the process → set `COSMOS_*` (pre-flight) and re-run; the banner is being honest about the store it used |
| Cosmos **403 / Forbidden** on first save | Data-plane role not propagated yet (~1–2 min after `provision-cosmos.ps1`), **or** wrong identity — the **Built-in Data Contributor** role must be on the same principal `az` is signed in as. Wait, re-warm, re-run |
| `--memory foundry` **401** "Authentication to the Azure OpenAI resource failed" while *extracting memories* | The Foundry memory service can't call your deployment. Grant the **account AND project** system-assigned identities **Cognitive Services OpenAI User** (`infra/provision-foundry-memory.ps1`); wait ~2–5 min for data-plane propagation, re-run |
| `--memory foundry` store-creation error / "memory" not found | Foundry **Memory (preview)** not enabled on the project, or no **embedding** deployment — enable the preview and deploy an embedding model (e.g. `text-embedding-3-small`) |
| `--memory foundry` RUN 1 recall probe says "no saved information" on a fresh scope | **Expected** — server-side indexing lag on a *cold* scope, not a failure. **RUN 2** (same `--customer`, new process) recalls on turn 1. **Pre-seed** the scope a few minutes before presenting, then demo RUN 2 |
| `You must install .NET` / runtime not found | `$env:DOTNET_ROLL_FORWARD = "LatestMajor"` (net8 app on newer runtime) |
| Auth / 401 mid-session | Token expired (~90 min) → re-run pre-flight step 2 |
| `--memory` seemingly ignored | Keep the `--` separator: `dotnet run -- --memory local` |

---

## 🗂️ QUICK REFERENCE

**Flag:** `--memory <off|local|cosmos|foundry>` · alias `-m` · env `MEMORY_MODE` · **default `off`**.
**Scope keys:** `customerId` = *whose* memory (persists) · `threadId` = *which* conversation (per-run GUID).
**Store (local):** `src-dotnet/BankingConcierge/.memory/<customerId>.json` — git-ignored.
**Store (cosmos):** account from `infra/provision-cosmos.ps1` · db `agentmemory` · container `conversations` · partition `/userId` · keyless (Entra RBAC).
**Store (foundry):** platform-managed store `AZURE_AI_MEMORY_STORE_ID` on your Foundry project · scope `customerId` · needs an embedding deploy + the account/project MI role (`provision-foundry-memory.ps1`).
**Compaction shape:** `{ ConversationSummary, ImportantFacts, Decisions, PendingActions, RecentTurns, SessionCount }`.
**Recall injection:** preamble built from that record → appended to **every** specialist's instructions (the Skills seam).
**Customers:** `CUST-1001` Alex Morgan · `CUST-1002` Jordan Rivera · `CUST-1003` Taylor Chen · memory demo uses `CUST-2001` (a fresh key so run 1 is always "first session").
**Data is synthetic** — safe for public/recording. Loan rates as-of 2026-04-23 (auto 60 mo 4.99%).

**What changed to add memory (the "diff" slide):** 5 new files under `Memory/` (~330 lines, zero Azure
deps for `local`) + 3 modified (`Program.cs`, `AgentTeam.cs`, `.gitignore`). No agent or workflow rewrites.
