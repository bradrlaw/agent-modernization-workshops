# Lab 02 – Copilot Studio: Build a Virtual Banking Assistant

## Overview

Build a **Virtual Banking Assistant** using Microsoft Copilot Studio — a low-code agent
that helps customers check account balances, review recent transactions, list accounts,
and look up profile information.

This lab uses Copilot Studio's **generative orchestration** model: instead of manually
wiring topic flows and trigger phrases, you define **agent instructions** and register
**actions with rich descriptions**. The LLM orchestrator decides which action to call,
what clarifying questions to ask, and how to present the results — all based on the
conversation context.

> 📖 **Scenario details:** See [`scenario.md`](scenario.md) for the complete use case
> definition, data model, conversation flows, and success criteria.

---

## Learning Objectives

By the end of this lab you will be able to:

- Create a Copilot Studio agent driven by **generative orchestration**
- Write effective **agent instructions** that shape behavior without rigid flows
- Register **plugin actions** with descriptions the LLM uses for routing
- Add **knowledge sources** for generative grounding (FAQ, documents)
- Import mock data into Dataverse tables
- Use **adaptive cards** to display structured financial data
- Publish and test the agent in Microsoft Teams

### Classic vs Generative Orchestration

This lab deliberately uses the **generative (LLM-first)** approach:

| Classic NLP Approach | Generative Orchestration (This Lab) |
|---|---|
| Explicit trigger phrases per topic | Agent instructions + action descriptions drive routing |
| Manual question nodes for disambiguation | LLM asks clarifying questions naturally |
| Hardcoded branching / waterfall flows | Orchestrator decides next steps from context |
| Topics define rigid conversation paths | Topics used only as guardrails (welcome, fallback) |
| Developer anticipates every user path | LLM handles the long tail of user expressions |

---

## Prerequisites

| Requirement | Details |
|---|---|
| Power Platform environment | Non-production environment provisioned |
| Copilot Studio | Enabled in tenant |
| Dataverse | Enabled in the environment |
| Maker role | Assigned to your account |
| Power Automate | Access to create cloud flows |
| Microsoft Teams | For testing the published agent |

> ⚠️ See [environment checklist](../../docs/environment-checklist.md) section A1 if
> your environment is not yet provisioned.

---

## Lab Contents

```
lab02-copilot-studio/
├── README.md                  # This file — full walkthrough
├── scenario.md                # Use case definition, personas, data model
├── sample-data/
│   ├── customers.json         # 3 demo customer profiles
│   ├── accounts.json          # 7 accounts across customers
│   └── transactions.json      # 20 sample transactions
├── topics/
│   └── topic-design-guide.md  # Minimal topic setup (welcome + fallback only)
├── flows/
│   └── flow-design-guide.md   # Power Automate flow designs and Dataverse setup
└── adaptive-cards/
    ├── account-balance-card.json    # Balance display card
    ├── transaction-list-card.json   # Transaction history card
    ├── account-list-card.json       # All accounts summary card
    └── customer-profile-card.json   # Profile information card
```

---

## Lab Steps

### Step 1: Set Up Mock Data in Dataverse

Before building the agent, create Dataverse tables and import mock data so the
Power Automate actions have something to query.

#### 1.1 Create Dataverse Tables

Open [Power Apps](https://make.powerapps.com) → select your environment → **Tables** → **New table**

Create three tables:

| Table Name | Primary Column | Key Columns |
|---|---|---|
| **Banking Customers** | Customer ID | First Name, Last Name, Email, Phone, Street, City, State, Zip, Member Since |
| **Banking Accounts** | Account ID | Customer ID (lookup), Account Type, Nickname, Last 4, Current Balance, Available Balance, Status |
| **Banking Transactions** | Transaction ID | Account ID (lookup), Date, Description, Amount, Type, Category, Running Balance |

> 📖 Full column definitions are in [`flows/flow-design-guide.md`](flows/flow-design-guide.md)

#### 1.2 Import Sample Data

Use the JSON files in [`sample-data/`](sample-data/) to populate your tables:

- **Option A:** Manually enter records via the Power Apps table editor
- **Option B:** Use **Power Apps** → **Tables** → **Import data** (Excel/CSV export of the JSON)
- **Option C:** Create a quick Power Automate flow that reads the JSON and inserts records

Verify your data:
- 3 customers
- 7 accounts (distributed across customers)
- 20 transactions (distributed across accounts)

> 💡 **Tip:** Start with Customer **CUST-1001 (Alex Morgan)** who has the most accounts
> and transactions for thorough testing.

---

### Step 2: Create the Agent

#### 2.1 Open Copilot Studio

1. Navigate to [https://copilotstudio.microsoft.com](https://copilotstudio.microsoft.com)
2. Select your **non-production environment** from the environment picker (top right)
3. Click **Create** → **New agent**

#### 2.2 Configure Agent Identity

| Setting | Value |
|---|---|
| **Name** | Virtual Banking Assistant |
| **Description** | A self-service agent that helps customers check balances, review transactions, list accounts, and view profile information. |

#### 2.3 Write Agent Instructions

The instructions are the **most important part** of a generative agent. They replace
the rigid topic/trigger architecture with natural language guidance that tells the LLM
how to behave, what it can and cannot do, and how to format responses.

Paste the following into the **Instructions** field:

```
You are a Virtual Banking Assistant for a retail financial institution.

## Your capabilities
You help authenticated customers with these self-service actions:
- Check the current and available balance of any of their accounts
- View recent transaction history for a specific account
- List all accounts they hold with their balances
- Look up their profile information (name, address, phone, email)

## How to handle requests
- When a customer asks about an account, first retrieve their account list using
  the List Accounts action to see what accounts they have.
- If they have multiple accounts and haven't specified which one, ask them to
  choose. Present the options clearly (nickname and last 4 digits).
- When showing balances, always include both current and available balance.
- When showing transactions, default to the 5 most recent unless the customer
  asks for more. Always include the summary totals.
- When showing profile information, remind the customer that updates must be
  done at a branch or by calling 1-800-555-0199.

## Formatting
- Always use the provided adaptive card templates to display account data,
  transactions, and profile information. Do not render financial data as
  plain text.
- Format all currency values with 2 decimal places.

## Security and boundaries
- Never reveal information about other customers.
- You cannot modify account data, transfer funds, or change profile information.
  If asked, explain what you can do and offer to connect them with a human agent.
- Do not speculate about account activity or provide financial advice.

## Tone
Be professional, concise, and helpful. Acknowledge the customer's request
before taking action.
```

> 💡 **Why this matters:** In generative orchestration, the instructions are your primary
> control mechanism. The LLM reads them on every turn to decide how to respond, which
> actions to call, and what follow-up questions to ask. Well-written instructions eliminate
> the need for most manual topic flows.

#### 2.4 Configure Generative AI Settings

1. Go to **Settings** → **Generative AI**
2. Set orchestration to **Generative** (not Classic)
3. Set the content moderation level to **Medium**
4. Enable **Generative answers** for knowledge-grounded fallback

---

### Step 3: Add Knowledge Sources

Knowledge sources let the agent answer general questions (like branch hours or policies)
that don't require calling an action.

#### 3.1 Create a Knowledge FAQ Document

Create a simple FAQ document (Word or text file) with content like:

> **What are your branch hours?**
> Our branches are open Monday–Friday 9:00 AM to 5:00 PM, and Saturday 9:00 AM to 1:00 PM.
>
> **How do I report a lost or stolen card?**
> Call our 24/7 support line at 1-800-555-0199 immediately.
>
> **What is the daily ATM withdrawal limit?**
> The standard daily ATM withdrawal limit is $500. Contact us to request a temporary increase.
>
> **How do I set up direct deposit?**
> Provide your employer with your routing number (555-000-123) and your account number.
>
> **How do I dispute a transaction?**
> Contact our support team within 60 days of the transaction date. You can call
> 1-800-555-0199 or visit any branch.

#### 3.2 Add to Copilot Studio

1. In your agent, go to **Knowledge** → **+ Add knowledge**
2. Upload the FAQ document (or add as a public website / SharePoint URL)
3. Wait for the indexing to complete

#### 3.3 Test Generative Answers

Open the **Test** panel and try questions from the FAQ:
- "What are your branch hours?"
- "How do I report a stolen card?"
- "What's the ATM limit?"

✅ The agent should answer accurately, citing the knowledge source.

---

### Step 4: Build Power Automate Flows (Backend Actions)

The agent needs actions to retrieve data from Dataverse. You'll create four
Power Automate cloud flows. The LLM will decide **when** to call each one based on
the action's name and description — no trigger phrases needed.

> 📖 Full flow designs with schemas: [`flows/flow-design-guide.md`](flows/flow-design-guide.md)

#### 4.1 Flow: List Accounts

1. Open [Power Automate](https://make.powerautomate.com) → **Cloud flows** → **New** → **Instant cloud flow**
2. Choose the trigger: **Run a flow from Copilot**
3. Add an input parameter: `CustomerId` (Text)
4. Add action: **List rows** (Dataverse: Banking Accounts)
   - Filter rows: `_customerid_value eq '{CustomerId}'`
5. Add action: **Select** — map to clean output schema
6. Add action: **Respond to Copilot** — return the account list

#### 4.2 Flow: Get Account Balance

1. Create a new flow with trigger: **Run a flow from Copilot**
2. Input: `AccountId` (Text)
3. Action: **Get a row by ID** (Dataverse: Banking Accounts)
4. Action: **Respond to Copilot** — return balance details

#### 4.3 Flow: Get Recent Transactions

1. Create a new flow with trigger: **Run a flow from Copilot**
2. Inputs: `AccountId` (Text), `Count` (Integer, default 5)
3. Action: **List rows** (Dataverse: Banking Transactions)
   - Filter: `_accountid_value eq '{AccountId}'`
   - Order by: `date desc`
   - Top count: `{Count}`
4. Action: **Select** — map to clean output
5. Action: **Compose** — calculate summary (total credits, debits, net change)
6. Action: **Respond to Copilot** — return transactions + summary

#### 4.4 Flow: Get Customer Profile

1. Create a new flow with trigger: **Run a flow from Copilot**
2. Input: `CustomerId` (Text)
3. Action: **Get a row by ID** (Dataverse: Banking Customers)
4. Action: **Respond to Copilot** — return profile data

#### 4.5 Test Each Flow

Before connecting to Copilot Studio, test each flow independently:
1. Click **Test** → **Manually**
2. Enter sample input: `CustomerId: CUST-1001` or `AccountId: ACCT-4521`
3. Verify the output matches the expected schema

---

### Step 5: Register Actions (LLM-Routed)

This is where the generative orchestration model differs most from classic Copilot Studio.
Instead of wiring actions into topic nodes, you register them as **plugin actions** with
**descriptions the LLM reads** to decide when to invoke them.

#### 5.1 Add Each Flow as an Action

1. In Copilot Studio, go to **Actions** → **+ Add an action**
2. Select **Power Automate flow**
3. Choose the flow
4. Map input/output parameters

#### 5.2 Write Action Descriptions (Critical)

The **description** is how the orchestrator knows when to call each action. Write them
as if you're explaining to a colleague when this action should be used:

| Action Name | Description |
|---|---|
| **List Accounts** | Use this action to retrieve all bank accounts belonging to the authenticated customer. Returns account IDs, types (Checking, Savings, Certificate), nicknames, last 4 digits, current balances, available balances, and status. Call this first when the customer asks about any account — you need the account list to know what accounts they have. |
| **Get Account Balance** | Use this action to get detailed balance information for a single specific account. Requires an AccountId. Use this after the customer has selected or identified a specific account from their account list. Returns current balance, available balance, account type, nickname, and status. |
| **Get Recent Transactions** | Use this action to retrieve recent transaction history for a specific account. Requires an AccountId and optionally a Count (defaults to 5). Returns a list of transactions with date, description, amount, type (Credit/Debit), category, and running balance, plus summary totals (total credits, total debits, net change). |
| **Get Customer Profile** | Use this action to look up the customer's personal information on file. Returns first name, last name, email, phone number, mailing address, and member-since date. This is read-only information — the customer cannot update their profile through this agent. |

> 💡 **Why descriptions matter so much:** In generative orchestration, the LLM reads every
> action description on each turn to decide which action (if any) to call. Vague descriptions
> like "Gets account data" lead to routing errors. Specific descriptions that explain
> **when** to use the action and **what it returns** give the LLM the context to make
> good decisions.

#### 5.3 Configure Adaptive Card Responses

For each action, configure the output to use the corresponding adaptive card template:

| Action | Adaptive Card |
|---|---|
| List Accounts | [`account-list-card.json`](adaptive-cards/account-list-card.json) |
| Get Account Balance | [`account-balance-card.json`](adaptive-cards/account-balance-card.json) |
| Get Recent Transactions | [`transaction-list-card.json`](adaptive-cards/transaction-list-card.json) |
| Get Customer Profile | [`customer-profile-card.json`](adaptive-cards/customer-profile-card.json) |

---

### Step 6: Configure Minimal Topics (Guardrails Only)

With generative orchestration, you need very few topics. The LLM handles the
conversation flow. Topics serve only as **guardrails** for specific edge cases.

> 📖 See [`topics/topic-design-guide.md`](topics/topic-design-guide.md) for details.

#### 6.1 System Topics (Review Defaults)

Review the built-in system topics and ensure they're configured:

| System Topic | Configuration |
|---|---|
| **Greeting** | Let the generative orchestrator handle it using your agent instructions. Alternatively, add a simple welcome message (see topic guide). |
| **Fallback** | Generative answers enabled as first response. If knowledge sources can't answer, offer human escalation. |
| **Escalation** | "Let me connect you with a human agent." + handoff if configured. |

#### 6.2 Optional: Welcome Topic

You may optionally create a single Welcome topic that displays a brief greeting when
the user first connects. This is useful for setting expectations:

> 👋 Welcome to the Virtual Banking Assistant! I can help you check balances, view
> transactions, list your accounts, or look up your profile. Just ask!

This is the **only custom topic** you should need. Everything else is handled by
the orchestrator + actions.

#### 6.3 What You Should NOT Create

In generative orchestration mode, **do not** create:
- ❌ Individual topics for "Check Balance", "View Transactions", etc.
- ❌ Trigger phrases per capability
- ❌ Question nodes with hardcoded disambiguation choices
- ❌ Manual branching / condition nodes for routing

The LLM handles all of this based on your instructions and action descriptions.

---

### Step 7: Test the Agent

This is where you'll see the power of generative orchestration — the agent handles
natural variations, multi-turn conversations, and edge cases without explicit topic flows.

#### 7.1 Basic Capability Tests

| User Message | Expected Behavior |
|---|---|
| "Hi" | Greets the user, explains capabilities |
| "What's my checking balance?" | Calls List Accounts → identifies checking → calls Get Balance → shows card |
| "Show me my recent transactions" | Calls List Accounts → asks which account → calls Get Transactions → shows card |
| "What accounts do I have?" | Calls List Accounts → shows account list card |
| "What's my address?" | Calls Get Customer Profile → shows profile card |

#### 7.2 Multi-Turn Conversation Tests

These test the LLM's ability to maintain context across turns:

| Turn | User Message | Expected Behavior |
|---|---|---|
| 1 | "Show my accounts" | Lists all accounts |
| 2 | "What's the balance on the savings one?" | Knows which savings account from context → shows balance |
| 3 | "And the transactions?" | Knows to show transactions for the same savings account |
| 4 | "What about my checking?" | Switches context to checking → asks "balance or transactions?" |

#### 7.3 Natural Language Variation Tests

The LLM should handle these **without** explicit trigger phrases:

| User Message | Should Still Work |
|---|---|
| "How much money do I have?" | Routes to List Accounts or Get Balance |
| "Did I get paid this week?" | Routes to Get Transactions, understands "paid" = credit |
| "Where do you think I live?" | Routes to Get Customer Profile, shows address |
| "Show me everything" | Lists accounts, or asks what specifically they'd like |
| "What did I spend at restaurants?" | Routes to transactions, filters may not apply but shows recent |

#### 7.4 Guardrail / Boundary Tests

| User Message | Expected Behavior |
|---|---|
| "Transfer $500 to savings" | Politely declines — explains it can only view, not modify |
| "Show me John's account" | Refuses — only shows authenticated user's data |
| "Change my email address" | Explains profile is read-only, directs to branch or 1-800-555-0199 |
| "Should I invest in crypto?" | Declines — no financial advice per instructions |
| "What are your branch hours?" | Answers from knowledge source (generative answers) |

#### 7.5 Troubleshoot Routing Issues

If the LLM calls the wrong action or doesn't call any:

1. **Check action descriptions** — Are they specific enough? Do they explain when to use the action?
2. **Check agent instructions** — Do they cover the scenario the user is asking about?
3. **Check orchestration mode** — Ensure it's set to **Generative**, not Classic
4. **Review the conversation trace** — In the test panel, expand the trace to see which
   action the orchestrator considered and why

---

### Step 8: Publish to Microsoft Teams

#### 8.1 Configure the Teams Channel

1. In Copilot Studio, go to **Channels**
2. Click **Microsoft Teams**
3. Review the configuration (name, icon, description)
4. Click **Turn on Teams**

#### 8.2 Publish

1. Go to **Publish** → click **Publish**
2. Wait for the publishing process to complete
3. Open the Teams link provided by Copilot Studio

#### 8.3 Test in Teams

1. Open Microsoft Teams
2. Find the agent in your chat list (or use the link from publish)
3. Run through all test scenarios from Step 7
4. Verify adaptive cards render correctly in the Teams client

> 💡 **Tip:** Adaptive cards may render slightly differently in Teams vs the Copilot Studio
> test panel. Always validate in Teams before considering the lab complete.

---

### Step 9: Export and Save

1. Go to **Solutions** in Power Apps
2. Find the solution containing your agent
3. **Export** as a managed or unmanaged solution (`.zip`)
4. Save the exported solution to this lab folder for reference

---

## Deliverables

- [ ] Dataverse tables created and populated with mock data (3 customers, 7 accounts, 20 transactions)
- [ ] Working Copilot Studio agent with generative orchestration enabled
- [ ] Agent instructions written and tested
- [ ] Knowledge source added with generative answers working
- [ ] 4 Power Automate flows created and tested (List Accounts, Get Balance, Get Transactions, Get Profile)
- [ ] Flows registered as plugin actions with LLM-optimized descriptions
- [ ] Adaptive cards displaying financial data correctly
- [ ] Multi-turn conversation working (context maintained across turns)
- [ ] Guardrails tested (refuses modifications, no cross-customer data, no financial advice)
- [ ] Agent published and tested in Microsoft Teams
- [ ] Solution exported (`.zip`)

---

## Common Issues and Troubleshooting

| Issue | Solution |
|---|---|
| LLM calls the wrong action | Improve action descriptions — be more specific about when to use each one |
| LLM doesn't call any action | Check that orchestration is set to Generative mode; verify actions are published |
| Agent ignores instructions | Instructions may be too long or contradictory; simplify and test incrementally |
| Power Automate flow not appearing | Ensure the flow uses the "Run a flow from Copilot" trigger and is in the same environment |
| Dataverse query returns no results | Check the OData filter syntax; verify data was imported to the correct environment |
| Adaptive card not rendering | Validate JSON at [adaptivecards.io/designer](https://adaptivecards.io/designer/) |
| Agent provides financial advice | Strengthen the security/boundaries section of your instructions |
| Multi-turn context is lost | This can happen with very long conversations; test with shorter flows |

---

## Key Concepts Recap

| Concept | What You Learned |
|---|---|
| **Generative orchestration** | The LLM decides which action to call based on instructions and descriptions |
| **Agent instructions** | Natural language guidance that replaces rigid topic flows |
| **Action descriptions** | Tell the LLM *when* to use an action and *what* it returns |
| **Knowledge grounding** | Generative answers sourced from uploaded documents |
| **Minimal topics** | Only welcome + fallback needed; the orchestrator handles the rest |
| **Adaptive cards** | Rich, structured UI for displaying data in chat |

### Why This Approach Matters

In classic Copilot Studio, you had to anticipate every way a user might phrase a request
and build explicit topic flows for each path. This is fragile — users inevitably say things
you didn't anticipate, and maintaining dozens of topics becomes a burden.

With generative orchestration:
- **Instructions** define the agent's identity, capabilities, and boundaries
- **Action descriptions** tell the LLM what tools are available
- The **LLM handles routing**, disambiguation, multi-turn context, and natural language variation
- You write **fewer topics**, maintain **less configuration**, and get **better coverage**

This is the direction Microsoft is investing in. Classic topics still have their place
(strict compliance flows, exact-match routing), but for most conversational scenarios,
generative orchestration is the recommended approach.

---

## Fallback

If Power Platform is unavailable, review the documentation and materials in this lab
folder, then proceed to [Lab 03](../lab03-foundry-agent/) where you'll build the same
Virtual Banking Assistant scenario using Azure AI Foundry (pro-code approach).

---

## What's Next

This same Virtual Banking Assistant scenario will be revisited in later labs:

| Lab | How the Scenario Evolves |
|---|---|
| **Lab 03** | Rebuild as a pro-code agent using Azure AI Foundry |
| **Lab 04** | Publish via Microsoft 365 Agents SDK to Teams |
| **Lab 05** | Connect this Copilot Studio agent to a Foundry backend (hybrid) |
| **Lab 06** | Add specialist agents (accounts, transactions, profile) with a router |

→ [Lab 03: Azure AI Foundry](../lab03-foundry-agent/) — Build the same scenario as a pro-code agent
