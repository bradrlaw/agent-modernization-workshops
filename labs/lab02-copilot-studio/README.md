# Lab 02 – Copilot Studio: Build a Virtual Banking Assistant

## Overview

Build a **Virtual Banking Assistant** using Microsoft Copilot Studio — a low-code agent
that helps customers check account balances, review recent transactions, list accounts,
and look up profile information. This lab introduces the full Copilot Studio development
lifecycle: agent creation, knowledge grounding, topic design, Power Automate actions,
adaptive card responses, and publishing to Teams.

> 📖 **Scenario details:** See [`scenario.md`](scenario.md) for the complete use case
> definition, data model, conversation flows, and success criteria.

---

## Learning Objectives

By the end of this lab you will be able to:

- Create and configure a Copilot Studio agent with a custom persona
- Add knowledge sources for generative grounding (FAQ, documents)
- Design and build multiple topics with trigger phrases and branching logic
- Create Power Automate cloud flows that serve as backend actions
- Import and query mock data in Dataverse tables
- Use adaptive cards to display structured financial data
- Publish and test the agent in Microsoft Teams

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
├── scenario.md                # Use case definition, personas, conversation flows
├── sample-data/
│   ├── customers.json         # 3 demo customer profiles
│   ├── accounts.json          # 7 accounts across customers
│   └── transactions.json      # 20 sample transactions
├── topics/
│   └── topic-design-guide.md  # Detailed topic flows and trigger phrases
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
| **Instructions** | See below |

**Agent Instructions** (paste into the instructions field):

> You are a Virtual Banking Assistant for a retail financial institution. You help
> authenticated customers check account balances, review recent transactions, list
> their accounts, and look up their profile information.
>
> Guidelines:
> - Always be professional, accurate, and security-conscious
> - Never share information about other customers
> - Format currency values with 2 decimal places
> - When displaying account data, always use the provided adaptive cards
> - If you cannot fulfill a request, offer to connect the customer with a human agent
> - Do not offer to modify profile information — direct users to a branch or support line

#### 2.3 Configure Agent Settings

1. Go to **Settings** → **Generative AI**
2. Set the moderation level to **Medium** (appropriate for financial data)
3. Enable **Generative answers** as a fallback for unrecognized intents

---

### Step 3: Add Knowledge Sources

Knowledge sources enable generative answers for questions not covered by specific topics.

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

✅ The agent should answer accurately using the knowledge source.

---

### Step 4: Build Power Automate Flows (Backend Actions)

The agent needs backend actions to retrieve data from Dataverse. You'll create four
Power Automate cloud flows.

> 📖 Full flow designs with schemas: [`flows/flow-design-guide.md`](flows/flow-design-guide.md)

#### 4.1 Flow: List Accounts

1. Open [Power Automate](https://make.powerautomate.com) → **Cloud flows** → **New** → **Instant cloud flow**
2. Choose the trigger: **Run a flow from Copilot** (or "When an action is performed")
3. Add an input parameter: `CustomerId` (Text)
4. Add action: **List rows** (Dataverse)
   - Table: Banking Accounts
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
4. Action: **Select** — map to clean output schema
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

### Step 5: Register Flows as Agent Actions

Connect each Power Automate flow to your Copilot Studio agent:

1. In Copilot Studio, go to **Actions** → **+ Add an action**
2. Select **Power Automate flow**
3. Choose the flow from the list
4. Map the input/output parameters
5. Set a clear **name** and **description** — the orchestrator uses the description
   to decide when to invoke the action:

| Action Name | Description |
|---|---|
| **List Accounts** | Retrieves all bank accounts and balances for the authenticated customer |
| **Get Account Balance** | Gets the detailed balance for a specific bank account by account ID |
| **Get Recent Transactions** | Returns recent transaction history for a specific account with summary totals |
| **Get Customer Profile** | Retrieves the customer's personal information including name, address, and contact details |

> 💡 **Tip:** The description field is critical. Copilot Studio's orchestrator uses it
> to route user requests to the correct action. Be specific and include key terms.

---

### Step 6: Create Topics

Build the conversation topics that tie trigger phrases to actions and adaptive card
responses.

> 📖 Full topic designs with trigger phrases and flow diagrams:
> [`topics/topic-design-guide.md`](topics/topic-design-guide.md)

#### 6.1 Topic: Welcome / Greeting

1. Go to **Topics** → **+ Add a topic** → **From blank**
2. Name: `Welcome`
3. Add trigger phrases: "Hi", "Hello", "Get started", "Help", "What can you do?"
4. Add a **Message** node with the welcome text:

   > 👋 Welcome to the Virtual Banking Assistant! I can help you with:
   > • **Check account balance** — View current and available balances
   > • **Recent transactions** — See your latest account activity
   > • **List accounts** — View all your accounts at a glance
   > • **Profile information** — Review your contact details on file
   >
   > What would you like to do?

#### 6.2 Topic: Check Account Balance

1. Create topic with trigger phrases: "balance", "how much money", "check my balance"
2. Add a **Plugin action** node → call **List Accounts** (to get the customer's accounts)
3. Add a **Question** node → "Which account?" with dynamic choices from the account list
4. Add a **Plugin action** node → call **Get Account Balance** with the selected account ID
5. Add a **Message** node → send the adaptive card from
   [`adaptive-cards/account-balance-card.json`](adaptive-cards/account-balance-card.json)
6. Add a **Question** node → "Is there anything else I can help with?"
7. Branch: Yes → redirect to Welcome; No → end conversation

#### 6.3 Topic: Recent Transactions

1. Create topic with trigger phrases: "transactions", "recent activity", "purchases"
2. Call **List Accounts** → ask which account → ask how many transactions (5/10/30)
3. Call **Get Recent Transactions** with the account ID and count
4. Display the adaptive card from
   [`adaptive-cards/transaction-list-card.json`](adaptive-cards/transaction-list-card.json)
5. Ask "Anything else?" → branch

#### 6.4 Topic: List All Accounts

1. Create topic with trigger phrases: "my accounts", "list accounts", "account overview"
2. Call **List Accounts**
3. Display the adaptive card from
   [`adaptive-cards/account-list-card.json`](adaptive-cards/account-list-card.json)
4. Ask "Want to see details for a specific account?" → redirect to balance or transactions

#### 6.5 Topic: Customer Profile

1. Create topic with trigger phrases: "my profile", "my address", "contact information"
2. Call **Get Customer Profile**
3. Display the adaptive card from
   [`adaptive-cards/customer-profile-card.json`](adaptive-cards/customer-profile-card.json)
4. Add note: "To update profile information, visit a branch or call 1-800-555-0199"

#### 6.6 Configure Fallback Behavior

1. Go to **Topics** → **System** → **Fallback**
2. Ensure generative answers is the first response (uses your knowledge sources)
3. If generative answers can't help, display: "I'm not sure I can help with that.
   Would you like me to connect you with a human agent?"

---

### Step 7: Test the Agent

#### 7.1 Test in Copilot Studio

Use the built-in **Test** panel to verify each conversation flow:

| Test Scenario | Expected Result |
|---|---|
| "Hi" | Welcome message with capability list |
| "What's my checking balance?" | Prompts for account selection → shows balance card |
| "Show my recent transactions" | Account selection → count selection → transaction card |
| "List all my accounts" | Account list card with totals |
| "What's my address?" | Profile card with contact information |
| "What are your branch hours?" | Generative answer from knowledge source |
| "I want to buy a car" | Fallback → generative answer or escalation offer |

#### 7.2 Test Edge Cases

| Test | Expected Behavior |
|---|---|
| Ask about another customer's account | Agent should refuse / only show authenticated user's data |
| Request to change address | Agent explains it's read-only and directs to branch/phone |
| Empty or gibberish input | Graceful fallback |
| Ask for 100 transactions | Handles gracefully (cap or return all available) |

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
- [ ] Working Copilot Studio agent with custom instructions and persona
- [ ] Knowledge source added with generative answers working
- [ ] 4 Power Automate flows created and tested (List Accounts, Get Balance, Get Transactions, Get Profile)
- [ ] Flows registered as Copilot Studio actions
- [ ] 5 topics built (Welcome, Balance, Transactions, Account List, Profile)
- [ ] Adaptive cards displaying financial data correctly
- [ ] Fallback/escalation behavior configured
- [ ] Agent published and tested in Microsoft Teams
- [ ] Solution exported (`.zip`)

---

## Common Issues and Troubleshooting

| Issue | Solution |
|---|---|
| Power Automate flow not appearing in Copilot Studio | Ensure the flow uses the "Run a flow from Copilot" trigger and is in the same environment |
| Dataverse query returns no results | Check the OData filter syntax; verify data was imported to the correct environment |
| Adaptive card not rendering | Validate JSON at [adaptivecards.io/designer](https://adaptivecards.io/designer/); check Teams version compatibility |
| Agent doesn't route to the correct topic | Review trigger phrases for overlap; check action descriptions are specific |
| Generative answers returning irrelevant content | Review knowledge source content; adjust moderation level in settings |
| "Access denied" on Dataverse | Verify maker role and table-level security roles are assigned |

---

## Key Concepts Recap

| Concept | What You Learned |
|---|---|
| **Declarative agents** | Configure behavior through instructions, not code |
| **Knowledge grounding** | Generative answers sourced from uploaded documents |
| **Topics** | Structured conversation flows with triggers and branching |
| **Plugin actions** | Power Automate flows exposed as agent capabilities |
| **Adaptive cards** | Rich, structured UI for displaying data in chat |
| **Managed orchestration** | Copilot Studio routes user intent to the right topic/action |

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
