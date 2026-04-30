# CLU Alternate Path — Deterministic Intent Routing (No LLM)

## Overview

This alternate path replaces GPT-4o function calling with **Azure AI Language CLU
(Conversational Language Understanding)** for intent detection and entity extraction.
The agent becomes fully deterministic — no LLM is involved at any point.

| Aspect | LLM Path (Default) | CLU Path (This Guide) |
|---|---|---|
| Intent detection | GPT-4o decides via function calling | CLU classifies intent |
| Entity extraction | GPT-4o parses from context | CLU extracts labeled entities |
| Response generation | GPT-4o formats natural language | Template-based (Python) |
| Multi-turn context | ✅ Full conversation history | ❌ Single-turn only |
| Multi-tool per turn | ✅ Model calls multiple tools | ❌ One intent per turn |
| Azure resource | AI Foundry (GPT-4o) | AI Language (CLU) |
| Cost | ~$0.0025/1K tokens | ~$0.50/1K text records |
| Deterministic | ❌ (probabilistic) | ✅ (same input → same output) |

### When to Use CLU Path

- Organization restricts LLM/GPT usage
- Regulatory requirement for deterministic, auditable behavior
- Need guaranteed response times (CLU: ~50ms vs LLM: 1-5s)
- Simple single-action interactions (not conversational)

---

## Prerequisites

| Requirement | Details |
|---|---|
| Azure subscription | Same as main lab |
| Azure AI Foundry project | Same project from the LLM path (or new) |
| Azure AI Language resource | Connected to Foundry project, or standalone (Standard S tier) |
| CLU project | Trained and deployed (import file provided) |
| Python 3.10+ | Same as main lab |

### Additional Python dependency

The `requests` package is already included in `src/requirements.txt`. If you
completed the main lab's Step 2 (venv + `pip install -r src/requirements.txt`),
you're all set. If not, from the `lab03-foundry-agent/` directory with your
venv activated:

```bash
pip install requests
```

> Note: The CLU client uses the REST API directly (no SDK dependency) to keep
> things simple and avoid version conflicts.

---

## Setup Steps

### 1. Add a Language Resource to Your Foundry Project

CLU is part of Azure AI Language, which can be connected directly to your
AI Foundry project as a "connected resource." This keeps everything under
one project rather than managing separate services.

> 💡 **Migration note:** Microsoft is migrating Language Studio into Foundry
> (completing March 2027). You can already create and manage CLU projects
> through Foundry or Language Studio — both use the same underlying endpoint.

#### Option A: Connect via AI Foundry (Recommended)

1. Open your **AI Foundry project** at [ai.azure.com](https://ai.azure.com)
2. Go to **Management** → **Connected resources**
3. Click **+ New connection** → select **Azure AI Language**
4. Either create a new Language resource or connect an existing one:
   - **Region**: Same region as your Foundry project
   - **Pricing tier**: Standard S (Free F0 works for testing)
5. Once connected, the Language resource endpoint is available to your project

#### Option B: Create a Standalone Language Resource

If you prefer to keep it separate:

1. Go to the [Azure Portal](https://portal.azure.com)
2. Click **+ Create a resource** → search for "Language"
3. Select **Language** (Azure AI Language) → **Create**
4. Configure:
   - **Resource group**: Same as your AI Foundry resource (or create new)
   - **Region**: Same region as your other resources
   - **Name**: e.g., `banking-assistant-language`
   - **Pricing tier**: Standard S (Free F0 works for testing but has limits)
5. Click **Review + Create** → **Create**
6. After deployment, go to the resource and copy:
   - **Endpoint** (e.g., `https://banking-assistant-language.cognitiveservices.azure.com`)
   - **Key** (under Keys and Endpoint)

### 2. Create and Train the CLU Project

You can create the CLU project through either **AI Foundry** or **Language Studio**
(both manage the same underlying resource).

#### Option A: Import the Pre-Built Project (Recommended)

**Via AI Foundry:**
1. Open your AI Foundry project → go to **Language** → **Conversational Language Understanding**
2. Click **+ Create new project** → **Import**
3. Upload the file: `clu/banking-assistant-clu.json`

**Via Language Studio (alternative):**
1. Open [Language Studio](https://language.cognitive.azure.com)
2. Sign in and select your Language resource
3. Go to **Conversational Language Understanding**
4. Click **+ Create new project** → **Import**
5. Upload the file: `clu/banking-assistant-clu.json`

**After import (either path):**
6. This imports:
   - 7 intents (+ None) with sample utterances
   - 7 entity types with labeled examples
7. Click **Train** → name it `v1` → **Train**
8. After training, go to **Deploy** → create a deployment named `production`

#### Option B: Create Manually

If you prefer to build the CLU project from scratch:

1. Create a new **Conversation** project named `banking-assistant`
2. Add these intents:

| Intent | Description |
|---|---|
| GetAccountBalance | Check account balance |
| GetRecentTransactions | View transaction history |
| ListAccounts | List all accounts |
| GetCustomerProfile | View profile/contact info |
| GetLoanRates | Check loan interest rates |
| CalculateLoanPayment | Calculate loan payments |
| SearchFAQ | General banking questions |
| None | Greetings, off-topic |

3. Add these entities:

| Entity | Examples |
|---|---|
| AccountType | checking, savings, certificate, cd |
| AccountReference | 4521, 7834 (last 4 digits) |
| ProductType | auto, home, personal, mortgage, car |
| LoanAmount | $25,000, 30000 |
| InterestRate | 4.99%, 5.25 |
| LoanTerm | 60 months, 30 years |
| TransactionLimit | 5, 10 (number of transactions) |

4. Add 5-10 utterances per intent with entity labels
5. Train and deploy as above

### 3. Configure Environment Variables

Add to your `src/.env` file:

```ini
# CLU Configuration (for CLU alternate path)
CLU_ENDPOINT=https://your-language-resource.cognitiveservices.azure.com
CLU_API_KEY=your-language-resource-key
CLU_PROJECT_NAME=banking-assistant
CLU_DEPLOYMENT_NAME=production
```

---

## Running the CLU Console

> **Prerequisite:** Complete Step 2 from the main [README](../README.md) first —
> create and activate the Python virtual environment and install dependencies.
> The CLU path uses the same venv and `requirements.txt` as the LLM path.

```bash
cd src
python chat_console_clu.py
```

You'll see:

```
╔══════════════════════════════════════════════╗
║   Virtual Banking Assistant — CLU Mode       ║
║   (No LLM — Deterministic Intent Routing)    ║
╚══════════════════════════════════════════════╝

Select a demo customer for this session:

  1. Alex Morgan (CUST-1001)
  2. Jordan Rivera (CUST-1002)
  3. Taylor Chen (CUST-1003)
```

### What You'll See (CLU Diagnostics)

The CLU console shows routing diagnostics:

```
You: What is my checking account balance?
  [CLU] Intent: GetAccountBalance (94%)
  [CLU] Entities: {'AccountType': 'checking'}
  [Route] → get_account_balance({'customer_id': 'CUST-1001', 'account_id': 'ACCT-4521'})

Assistant: Here are your account balances:

  - Primary Checking (ending 4521)
    Current: $3,842.56 | Available: $3,742.56
```

---

## Limitations of CLU Path

- **Single-turn only** — no conversation memory across messages
- **One intent per message** — cannot handle compound requests
- **Template responses** — less natural than LLM-generated text
- **Requires training data** — new intents need utterance examples + retraining
- **No reasoning** — cannot handle ambiguous or novel requests

---

## Hybrid Mode (Optional Stretch)

For a middle ground, you can use CLU for intent detection but GPT-4o for
response formatting only. This minimizes LLM usage while keeping natural
responses:

```python
# In chat_console_clu.py, replace format_response() with:
result_data = func(**args)  # Get tool result

messages = [
    {'role': 'system', 'content': 'Format this banking data as a helpful response.'},
    {'role': 'user', 'content': f'User asked: {user_input}\nData: {result_data}'},
]
formatted = openai_client.chat.completions.create(model=model, messages=messages)
```  

This gives you:
- Deterministic routing (CLU)
- Natural responses (LLM)
- Minimal token usage (only formatting, not reasoning)

---

## Architecture Comparison

```  
LLM Path:  User -> GPT-4o (intent + entities + response) -> Tools -> GPT-4o (format)
CLU Path:  User -> CLU (intent + entities) -> Tools -> Templates (format)
Hybrid:    User -> CLU (intent + entities) -> Tools -> GPT-4o (format only)
```  

---

## File Reference

| File | Purpose |
|---|---|
| `clu/banking-assistant-clu.json` | CLU project import file (intents + entities + utterances) |
| `clu/README.md` | This guide |
| `src/clu_client.py` | CLU REST API client + entity resolution |
| `src/chat_console_clu.py` | Interactive console using CLU routing |
