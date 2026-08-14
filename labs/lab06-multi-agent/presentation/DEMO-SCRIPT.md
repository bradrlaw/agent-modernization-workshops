# Demo Script — Lab 06: Multi-Agent Orchestration

**Contact:** Brad Lawrence · Brad.Lawrence@microsoft.com · Microsoft ISD
**Deck:** `presentation/Lab06-Multi-Agent-Orchestration.pptx` (14 slides, commands in speaker notes)
**Repo:** https://github.com/bradrlaw/agent-modernization-workshops → `labs/lab06-multi-agent`
**Status:** All 6 demos validate end-to-end against a Foundry **gpt-4o** deployment via the **.NET** path — LendingAgent fires `GetLoanRates` + `CalculateLoanPayment`, Compliance appends disclosures, and Agent→Skills (off vs on) visibly changes Compliance output.

> **Demo the .NET path** — it is the most reliable to run live. The Python path is source-identical but requires **Python 3.10–3.12** (the `agent-framework` wheels don't yet publish for 3.13+); if your Python is newer, show the code under `src/` instead of running it.

---

## ⏱️ PRE-FLIGHT — run ONCE, ~2 min before you start

All paths below are relative to the **repo root**.

```powershell
# 1. Confirm you're signed in to the right subscription
az account show --query name -o tsv

# 2. Warm the tokens — SILENCES the scary red DefaultAzureCredential dump on first run
az account get-access-token --resource https://ai.azure.com --query expiresOn -o tsv
az account get-access-token --resource https://cognitiveservices.azure.com --query expiresOn -o tsv

# 3. Move into the .NET project
cd labs/lab06-multi-agent/src-dotnet/BankingConcierge

# 4. Point at YOUR Foundry project + model (or copy the folder's .env.example to .env)
$env:FOUNDRY_PROJECT_ENDPOINT = "https://<your-resource>.services.ai.azure.com/api/projects/<your-project>"
$env:FOUNDRY_MODEL = "gpt-4o"

# 5. Windows only: net8 app runs on a newer installed runtime
$env:DOTNET_ROLL_FORWARD = "LatestMajor"

# 6. Which customer the session runs as
$env:DEMO_CUSTOMER_ID = "CUST-1001"

# 7. Build once (~5s) so demos start instantly
dotnet build -v q --nologo
```

> Tokens last ~75–90 min. If your session runs long, re-run step 2. After the first build, use `--no-build` on every run.

---

## 🎬 DEMO INVENTORY — all 5 patterns, one team

| # | Pattern | Command (from `BankingConcierge/`) | Mode | What to show |
|---|---|---|---|---|
| 1 | **Handoff** ⭐ | `dotnet run --no-build -- --pattern handoff` | 💬 interactive | Concierge triages → hands the whole convo to one specialist |
| 2 | Sequential | `dotnet run --no-build -- --pattern sequential` | ▶ auto | Accounts → Lending → Compliance **pipeline** |
| 3 | Concurrent | `dotnet run --no-build -- --pattern concurrent` | ▶ auto | 3 specialists **fan out in parallel**, fan back in |
| 4 | Group Chat | `dotnet run --no-build -- --pattern groupchat` | ▶ auto | Round-robin **dispute round-table** (max 6 turns) |
| 5 | **Magentic** | `dotnet run --no-build -- --pattern magentic` | ▶ auto | Manager **plans, delegates, re-plans** (showstopper) |
| 6 | **Skills (off→on)** | `--pattern sequential --skills off` then `--skills on` | ▶ auto | Same team, **SKILL.md** rules loaded at runtime — compliance behavior changes, no code change |

**Recommended flow:** open with **Handoff** (interactive — it visibly *routes*), then **Sequential + Concurrent** back-to-back (same agents, order vs. parallel), then **Group Chat**, and close on **Magentic** (the wow). Switch customer with `--customer CUST-1002`. Then run **🧩 Demo 6 — Agent → Skills (off→on)** (below) to prove the Agent→Skills axis live.

> 🖈 Deck slides **10 / 11 / 12** carry the **▶ DEMO** bands (handoff; sequential+concurrent; groupchat+magentic). Exact commands are also in each slide's speaker notes.

**The one message to repeat:** *the five specialist agents never change — only the builder (the orchestration wrapper) changes per pattern.*

---

## ⭐ DEMO 1 — Handoff — the default / triage pattern (interactive, best opener)

```powershell
dotnet run --no-build -- --pattern handoff
```

Then type these at the `You:` prompt:

| Say this | What it proves |
|---|---|
| `What's my available balance?` | Concierge calls `handoff_to_…` → **Accounts** takes over → `GetAccountBalance` |
| `What auto loan rates do you have?` | Hands off **again** → **Lending** → `GetLoanRates` |
| `quit` | Exit |

**Point at:** the `[ConciergeAgent]` line, then the `↳ calling handoff_to_…` transfer, then a **different** `[…Agent]` header taking over. That agent transfer *is* the pattern.

> Prefer to script it hands-free? Pipe the inputs:
> ```powershell
> @("What's my available balance?","What auto loan rates do you have?","quit") | dotnet run --no-build -- --pattern handoff
> ```

---

## ▶ DEMO 2 — Sequential (ordered pipeline)

```powershell
dotnet run --no-build -- --pattern sequential
```

Scripted task: *"I'd like a $25,000 auto loan for 60 months… with disclosures."*

**Point at (example output):**
- `[AccountsAgent]` gathers context, then hands down the pipeline
- `[LendingAgent]` → `↳ calling GetLoanRates({"productType":"auto"})` and `↳ calling CalculateLoanPayment(…)` → **$471.67/mo**, total interest **$3,299.98**
- `[ComplianceAgent]` appends the required disclosures

**Say:** "Each agent's output feeds the next — deterministic, testable, ideal for regulated workflows."

---

## ▶ DEMO 3 — Concurrent (parallel fan-out)

```powershell
dotnet run --no-build -- --pattern concurrent
```

Scripted task: *"Give me a financial health check: balances, loan options, anything notable on my cards."*

**Point at:** interleaved `[AccountsAgent]` / `[LendingAgent]` / `[CardsAgent]` headers — they run **at the same time**, then results aggregate. Tools you'll see fire: `ListAccounts`, `GetRecentTransactions`, `GetCustomerProfile`, `GetLoanRates`.

**Say:** "Same three specialists as sequential — but the builder fans them out for latency instead of chaining them. Zero change to the agents."

---

## ▶ DEMO 4 — Group Chat (managed round-table)

```powershell
dotnet run --no-build -- --pattern groupchat
```

Scripted task: *"I'm disputing a $180 charge on my debit card that I don't recognize…"*

**Point at:** a round-robin **manager** giving each specialist a turn — `[CardsAgent]` → `[AccountsAgent]` → `[ComplianceAgent]` — bounded by `MaximumIterationCount = 6` so it can't loop forever.

**Say:** "A manager decides who speaks next. Great for collaboration/debate where the answer needs several viewpoints."

---

## ▶ DEMO 5 — Magentic (adaptive planner — the showstopper)

```powershell
dotnet run --no-build -- --pattern magentic
```

Scripted task: *"I want to buy a $30,000 car. Figure out affordability, loan options, and next steps."*

**Point at:**
- `[Magentic plan]` — the manager writes a **task ledger** up front
- `[Magentic progress] next: … — …` lines — it delegates to **Accounts**, then **Lending** (often computing several down-payment scenarios via `CalculateLoanPayment`)
- If it stalls, watch for `[Magentic re-planned]` — it adapts and continues (bounded by max **10 rounds / 3 stalls / 2 resets**)

**Say:** "Give it an open-ended goal and it *plans, delegates, tracks progress, and re-plans*. Powerful — but non-deterministic, so reserve the top rung for genuinely open-ended work."

> ⏳ Magentic runs longest (~1–2 min, multiple rounds). Narrate the plan/progress lines as they scroll. Each run differs — that's expected.

---

## 🧩 DEMO 6 — Agent → Skills, LIVE (off vs on · ~3 min · deck slides 6–7)

> **Runnable.** The specialists load the versioned `SKILL.md` files at runtime and compose the mapped rules into their instructions. Show the same task **without** then **with** skills — the compliance behavior visibly changes, driven entirely by the files. **No code change between runs.**

**1) Show the skill files (10 sec):**
```powershell
code labs/lab06-multi-agent/skills
# or inline: Get-Content labs/lab06-multi-agent/skills/compliance-guidelines/SKILL.md
```

**2) Run WITHOUT skills** — startup prints `• Skills: OFF`:
```powershell
dotnet run --no-build -- --pattern sequential --skills off
```

**3) Run WITH skills (default)** — startup prints `✓ Skills: ON — loaded 3 from ./skills (…)`:
```powershell
dotnet run --no-build -- --pattern sequential --skills on
```

**Point at the `[ComplianceAgent]` output — with skills ON it enforces the exact SKILL.md rules:**
- "No full account numbers" — the **last-4-only** PII rule
- "estimates … **not a commitment to lend**" — the disclosure rule
- "rates **subject to change** … **credit approval**"
- "No **tax, legal, or investment** advice" — the prohibited-statements rule

With `--skills off` those lines disappear. *(Typical delta: "subject to change" appears several times with skills ON and not at all with skills OFF; the "not a commitment to lend" and tax/legal disclaimers appear only with skills ON.)*

**The money line:** *"I didn't touch agent code between these two runs — I only toggled whether the shared `compliance-guidelines` Skill is loaded. Edit that one file and every mapped agent updates. No redeploy."*

| Skill | Mapped to | The point |
|---|---|---|
| `compliance-guidelines` | **every** agent | one source of truth — last-4, disclosures, no tax/legal advice |
| `brand-voice` | customer-facing agents | marketing updates tone — no dev cycle |
| `escalation-policy` | Concierge, Cards | ops updates escalation rules — no code change |

**Say slide 7 accurately:** *"These compliance rules are defined as a **Skill**, loaded at runtime and composed into the agents' instructions. The `--skills off`/`on` toggle proves the fleet-wide, no-redeploy value prop. In production these live centrally in Foundry behind an **MCP Toolbox** (preview) — README Part B1."*

**Python note:** same toggle via env — `USE_SKILLS=off python patterns/sequential_loan.py` (requires Python 3.10–3.12).

---

## 🖥️ PORTAL TOUR — the Foundry project behind the demos

Open **https://ai.azure.com** → your **Foundry project** (the one in `FOUNDRY_PROJECT_ENDPOINT`).

| Show | Where | Talking point |
|---|---|---|
| The project | Overview | "One Foundry project can back Labs 03 → 06 — same endpoint, same model." |
| Model deployment | **Models + endpoints** | your **gpt-4o** deployment — the model every agent in the demo calls |
| Project endpoint | Overview / Settings | this is `FOUNDRY_PROJECT_ENDPOINT` from the pre-flight |
| Connections | **Management → Connections** | where knowledge/tools/data connections attach |

> The orchestration agents are created **programmatically at runtime** by the Agent Framework (`AgentTeam.Build` → `.AsAIAgent(...)`), so the demo team is defined in code, not the portal's authoring canvas — that's the whole point of the **pro-code** path.

---

## 🧭 CURRENCY — dates to mention (deck slide 13)

| Retiring | Date |
|---|---|
| Azure OpenAI Assistants API | **Aug 26, 2026** |
| Foundry Multi-Agent Workflows (visual/YAML designer) | **Dec 1, 2026** |
| Agent Service *classic* incl. Connected Agents | **Mar 31, 2027** |
| Prompt Flow | **Apr 20, 2027** |

**Go-forward:** the **Microsoft Agent Framework** (what this lab uses) — successor to Semantic Kernel + AutoGen. Full table + source links are in the lab README's deprecation section.

---

## 🛟 TROUBLESHOOTING

| Symptom | Fix |
|---|---|
| Red `DefaultAzureCredential` dump on first run | Re-run **PRE-FLIGHT step 2** (warm both tokens), relaunch |
| `You must install .NET` / runtime not found | `$env:DOTNET_ROLL_FORWARD = "LatestMajor"` (net8 app, newer runtime) |
| Auth / 401 mid-session | Token expired (~90 min) — re-run PRE-FLIGHT step 2 |
| Garbled box/emoji chars in console | Console encoding — app sets UTF-8; ensure Windows Terminal, not legacy conhost |
| `--pattern` seemingly ignored | Keep the `--` separator: `dotnet run -- --pattern <name>` |
| Want a different persona | `dotnet run --no-build -- --pattern <name> --customer CUST-1002` |

---

## 🗂️ QUICK REFERENCE — the team & data

**Specialists:** Concierge (triage) · Accounts · Lending · Cards & Fraud · Compliance.
**Customers:** `CUST-1001` Alex Morgan (~$21,342 total) · `CUST-1002` Jordan Rivera · `CUST-1003` Taylor Chen.
**Tools:** `GetAccountBalance`, `GetRecentTransactions`, `ListAccounts`, `GetCustomerProfile`, `GetLoanRates`, `CalculateLoanPayment`, `SearchFaq`. Loan rates as-of 2026-04-23 (auto 60 mo **4.99%**).
**Data is synthetic** — no customer data, safe for public/recording.
