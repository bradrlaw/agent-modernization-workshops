# Lab 02 – Copilot Studio: Build a Virtual Banking Assistant

## Overview

Build a **Virtual Banking Assistant** using Microsoft Copilot Studio — a low-code agent
that helps customers check account balances, review recent transactions, list accounts,
look up profile information, query live loan rates from an external API, and calculate
loan payments via an MCP tool.

This lab uses Copilot Studio's **generative orchestration** model: instead of manually
wiring topic flows and trigger phrases, you define **agent instructions** and register
**actions with rich descriptions**. The LLM orchestrator decides which action to call,
what clarifying questions to ask, and how to present the results — all based on the
conversation context.

The lab demonstrates three action patterns:
1. **Power Automate + Dataverse** — Standard data queries (accounts, transactions, profile)
2. **Power Automate + HTTP connector** — Calling an external REST API (loan rates via APIM)
3. **MCP tool** — Connecting to a Model Context Protocol server (loan payment calculator)

> 📖 **Scenario details:** See [`scenario.md`](scenario.md) for the complete use case
> definition, data model, conversation flows, and success criteria.

---

## Learning Objectives

By the end of this lab you will be able to:

- Create a Copilot Studio agent driven by **generative orchestration**
- Write effective **agent instructions** that shape behavior without rigid flows
- Register **plugin actions** with descriptions the LLM uses for routing
- Call an **external REST API** via Power Automate's HTTP connector (APIM mock backend)
- Connect a **Model Context Protocol (MCP)** tool server to Copilot Studio
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
│   ├── flow-design-guide.md   # Power Automate flow designs and Dataverse setup
│   ├── apim-mock-setup.md     # Azure APIM mock backend for loan rates API
│   └── mcp-server-setup.md    # MCP loan payment calculator server
└── adaptive-cards/
    ├── account-balance-card.json    # Balance display card
    ├── transaction-list-card.json   # Transaction history card
    ├── account-list-card.json       # All accounts summary card
    ├── customer-profile-card.json   # Profile information card
    ├── loan-rates-card.json         # Loan rates display card
    └── loan-calculator-card.json    # Loan payment estimate card
```

---

## Lab Steps

### Step 1: Set Up Mock Data in Dataverse

Before building the agent, create Dataverse tables and import mock data so the
Power Automate actions have something to query.

#### 1.1 Create Dataverse Tables

1. Open [https://make.powerapps.com](https://make.powerapps.com)
2. In the **top-right**, click the environment picker and select your **developer environment**
3. In the left navigation, click **Tables**
4. Click **+ New table** → **New table**

**Table 1: Banking Customers**

5. Set the **Display name** to `Banking Customers`
6. Set the **Primary column** display name to `Customer ID`
7. Click **Save**
8. Your table is created. Now add columns — click **+ New column** for each:

| Display Name | Data Type | Required |
|---|---|---|
| First Name | Single line of text | Yes |
| Last Name | Single line of text | Yes |
| Email | Single line of text | No |
| Phone | Single line of text | No |
| Street | Single line of text | No |
| City | Single line of text | No |
| State | Single line of text | No |
| Zip | Single line of text | No |
| Member Since | Date only | No |

9. Click **Save table** after adding all columns

**Table 2: Banking Accounts**

10. Go back to **Tables** → **+ New table** → **New table**
11. Display name: `Banking Accounts`
12. Primary column: `Account ID`
13. Click **Save**, then add columns:

| Display Name | Data Type | Required | Notes |
|---|---|---|---|
| Customer ID | Single line of text | Yes | For simplicity, use text (not a lookup) |
| Account Type | Single line of text | Yes | Values: Checking, Savings, Certificate |
| Nickname | Single line of text | No | |
| Last 4 | Single line of text | No | |
| Current Balance | Currency | No | |
| Available Balance | Currency | No | |
| Status | Single line of text | No | Values: Active, Inactive, Closed |
| Opened Date | Date only | No | |

14. Click **Save table**

**Table 3: Banking Transactions**

15. **Tables** → **+ New table** → **New table**
16. Display name: `Banking Transactions`
17. Primary column: `Transaction ID`
18. Click **Save**, then add columns:

| Display Name | Data Type | Required | Notes |
|---|---|---|---|
| Account ID | Single line of text | Yes | Matches Account ID in Banking Accounts |
| Date | Date and time | Yes | |
| Description | Single line of text | Yes | |
| Amount | Currency | Yes | Negative for debits, positive for credits |
| Type | Single line of text | No | Values: Credit, Debit |
| Category | Single line of text | No | e.g., Income, Groceries, Utilities |
| Running Balance | Currency | No | |

19. Click **Save table**

> 💡 **Note on lookup columns:** For this lab, we use simple text columns for Customer ID
> and Account ID rather than Dataverse lookup relationships. This keeps the table creation
> and data import simple. In production, you'd use proper lookup columns for referential
> integrity.

#### 1.2 Import Sample Data

CSV files are provided in [`sample-data/`](sample-data/) for easy import.

> 💡 Since we're using plain text columns (not Dataverse lookups), import order doesn't
> matter — there are no foreign key constraints. If you had used lookup columns instead,
> you'd need to import Customers → Accounts → Transactions in that order.

1. In Power Apps, go to **Tables** → click **Banking Customers**
2. In the table toolbar, click the **▼ dropdown** next to "Edit" → **Import** → **Import data from Excel**
3. Click **Upload** and select `sample-data/customers.csv`
4. Review the column mapping — Power Apps will attempt to auto-map columns by name
5. Fix any unmapped columns by clicking the column header and selecting the matching table column
6. Click **Import**
7. Wait for the import to complete, then verify: you should see **3 rows** in the table

8. Repeat for **Banking Accounts** using `sample-data/accounts.csv` → verify **7 rows**
9. Repeat for **Banking Transactions** using `sample-data/transactions.csv` → verify **20 rows**

> ⚠️ **If the import option doesn't appear:** Some environments require you to go to
> **Power Apps** → **Dataflows** → **Import data** instead. You can also manually enter
> a few rows to test with — you don't need all 20 transactions to proceed.

> 💡 **Tip:** Start with Customer **CUST-1001 (Alex Morgan)** who has the most accounts
> and transactions for thorough testing.

---

### Step 2: Create the Agent

#### 2.1 Open Copilot Studio

1. Navigate to [https://copilotstudio.microsoft.com](https://copilotstudio.microsoft.com)
2. In the **top-right**, click the environment name and select your **developer environment**
   (the same one where you created your Dataverse tables)
3. On the **Home** page, click **Create an agent** (or **+ Create** depending on your UI version)
4. You'll see an agent creation wizard — click **Skip to configure** (bottom left)
   to go directly to the configuration page

   > 💡 The wizard offers a chat-based setup experience. For this lab, skip it so you
   > can configure everything manually and understand each setting.

#### 2.2 Configure Agent Identity

On the agent configuration page:

1. **Name:** Enter `Virtual Banking Assistant`
2. **Description:** Enter `A self-service agent that helps customers check balances, review transactions, list accounts, and view profile information.`
3. **Icon:** Optionally upload a custom icon (or leave the default)

#### 2.3 Write Agent Instructions

The instructions are the **most important part** of a generative agent. They replace
the rigid topic/trigger architecture with natural language guidance that tells the LLM
how to behave, what it can and cannot do, and how to format responses.

1. On the agent configuration page, find the **Instructions** text box
   (it's the large text area below the name and description)
2. Clear any default text
3. Paste the following:

```
You are a Virtual Banking Assistant for a retail financial institution.

## Your capabilities
You help authenticated customers with these self-service actions:
- Check the current and available balance of any of their accounts
- View recent transaction history for a specific account
- List all accounts they hold with their balances
- Look up their profile information (name, address, phone, email)
- Look up current loan interest rates by product type
- Calculate estimated monthly loan payments given a principal, rate, and term

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
- When a customer asks about loan rates, retrieve the current rates and present
  them clearly. If they ask about a specific product, highlight that rate.
- When a customer wants to calculate loan payments, collect the loan amount,
  interest rate, and term. If they've already looked up rates, offer to use
  one of those rates. Calculate the payment and show the breakdown.

## Formatting
- Always use the provided adaptive card templates to display account data,
  transactions, profile information, loan rates, and payment calculations.
  Do not render financial data as plain text.
- Format all currency values with 2 decimal places.

## Security and boundaries
- Never reveal information about other customers.
- You cannot modify account data, transfer funds, or change profile information.
  If asked, explain what you can do and offer to connect them with a human agent.
- Do not speculate about account activity or provide financial advice.
- Loan calculations are estimates only — always include the disclaimer.

## Tone
Be professional, concise, and helpful. Acknowledge the customer's request
before taking action.
```

> 💡 **Why this matters:** In generative orchestration, the instructions are your primary
> control mechanism. The LLM reads them on every turn to decide how to respond, which
> actions to call, and what follow-up questions to ask. Well-written instructions eliminate
> the need for most manual topic flows.

#### 2.4 Configure Generative AI Settings

1. In your agent, click **Settings** (gear icon in the top-right corner of the agent editor)
2. In the Settings panel, click **Generative AI** in the left sidebar
3. Under **Use generative AI orchestration for your agent's responses?**, select **Generative** (not Classic)

   > ⚠️ **This is critical.** If left on "Classic," the agent will use the traditional
   > topic-trigger pattern instead of LLM-based orchestration. The entire lab depends
   > on Generative mode being enabled.

4. Under **Content moderation**, set to **Moderate**
5. Ensure **Allow ungrounded responses** is toggled **off** — for a
   financial services agent, you want responses grounded only in your knowledge sources
   and actions, not the LLM's general training data. Leaving this on could lead to
   inaccurate or inappropriate answers outside the agent's purpose.
6. Click **Save** at the bottom of the Settings panel
7. Close the Settings panel to return to the agent editor

---

### Step 3: Add Knowledge Sources

Knowledge sources let the agent answer general questions (like branch hours or policies)
that don't require calling an action.

#### 3.1 Create a Knowledge FAQ Document

First, create a file on your computer to upload as a knowledge source.

1. Open **Notepad**, **Word**, or any text editor on your computer
2. Create a new file called `banking-faq.docx` (or `.txt`) and paste the following content:

```
What are your branch hours?
Our branches are open Monday–Friday 9:00 AM to 5:00 PM, and Saturday 9:00 AM to 1:00 PM.

How do I report a lost or stolen card?
Call our 24/7 support line at 1-800-555-0199 immediately.

What is the daily ATM withdrawal limit?
The standard daily ATM withdrawal limit is $500. Contact us to request a temporary increase.

How do I set up direct deposit?
Provide your employer with your routing number (555-000-123) and your account number.

How do I dispute a transaction?
Contact our support team within 60 days of the transaction date. You can call
1-800-555-0199 or visit any branch.
```

3. Save the file to a location you can easily find (e.g., your Desktop)

#### 3.2 Add to Copilot Studio

1. Return to [Copilot Studio](https://copilotstudio.microsoft.com) and open your **Virtual Banking Assistant** agent
2. In the agent editor, click **Knowledge** in the top navigation bar
3. Click **+ Add knowledge**
4. In the panel that appears, select **Files** as the source type
5. Click **Upload** and browse to the `banking-faq.docx` (or `.txt`) file you just created
6. Click **Add** to upload the file
7. Wait for the status to show **Ready** — this means the file has been indexed

   > ⏱️ Indexing typically takes 1–2 minutes. You can continue working while it processes.

#### 3.3 Test Generative Answers

1. In the agent editor, click the **Test** button (bottom-left of the Copilot Studio screen,
   or the chat bubble icon) to open the test panel
2. Type the following messages one at a time and verify the agent responds correctly:

- "What are your branch hours?"
- "How do I report a stolen card?"
- "What's the ATM limit?"

✅ The agent should answer accurately, citing the knowledge source. If it says "I don't know,"
verify the knowledge source shows **Ready** status in the Knowledge tab.

---

### Step 4: Build Power Automate Flows (Backend Actions)

The agent needs actions to retrieve data from Dataverse. You'll create four
Power Automate cloud flows. The LLM will decide **when** to call each one based on
the action's name and description — no trigger phrases needed.

> 📖 Full flow designs with output schemas: [`flows/flow-design-guide.md`](flows/flow-design-guide.md)

> ⚠️ **Important:** Make sure you're working in your **developer environment** (not the
> default). Check the environment picker in the top-right of Power Automate.

#### 4.1 Flow: List Accounts

This flow returns all accounts belonging to a customer.

1. Open [https://make.powerautomate.com](https://make.powerautomate.com)
2. Confirm your **developer environment** is selected (top-right environment picker)
3. In the left nav, click **My flows**
4. Click **+ New flow** → **Instant cloud flow**
5. Give it a name: `List Accounts`
6. Under "Choose how to trigger this flow", select **Run a flow from Copilot**
7. Click **Create**

**Add input parameter:**

8. Click the **Run a flow from Copilot** trigger card to expand it
9. Click **+ Add an input**
10. Select **Text**
11. Rename the input to `CustomerId`
12. Add a description: `The authenticated customer's ID`

**Add the Dataverse query:**

13. Click **+ New step** (or the **+** button below the trigger)
14. Search for **Dataverse** in the action search box
15. Select **List rows**
16. Configure:
    - **Table name:** Select your Banking Accounts table
    - Click **Show advanced options**
    - **Filter rows:** Enter `cr_customerid eq '@{triggerBody()['text']}'`

    > 💡 The exact column name depends on your table's schema prefix. It may be
    > `cr_customerid`, `crf9a_customerid`, or similar. Check your table's column
    > names in Power Apps → Tables → Banking Accounts → Columns.

**Add the response:**

17. Click **+ New step**
18. Search for **Respond to Copilot**
19. Select **Respond to a Copilot action**
20. Click **+ Add an output** → **Text**
    - Name: `AccountList`
    - Value: Click in the value field → select **Expression** tab → enter:
      `string(outputs('List_rows')?['body/value'])`

    > This returns the Dataverse rows as a JSON string. Alternatively, you can use
    > a **Select** action between List rows and Respond to map specific fields.

21. Click **Save** (top right)

**Test the flow:**

22. Click **Test** (top right) → **Manually** → **Test**
23. Enter `CustomerId`: `CUST-1001`
24. Click **Run flow**
25. Verify you get a successful run with account data in the output

---

#### 4.2 Flow: Get Account Balance

This flow returns detailed balance info for a single account.

1. Go back to **My flows** → **+ New flow** → **Instant cloud flow**
2. Name: `Get Account Balance`
3. Trigger: **Run a flow from Copilot**
4. Click **Create**

**Add input parameter:**

5. Expand the trigger → **+ Add an input** → **Text**
6. Name: `AccountId`
7. Description: `The account ID to look up`

**Add the Dataverse lookup:**

8. **+ New step** → search **Dataverse** → select **Get a row by ID**
9. Configure:
    - **Table name:** Banking Accounts
    - **Row ID:** Click the value field → from the **Dynamic content** panel, select `AccountId`

    > ⚠️ **Row ID** expects the Dataverse GUID, not your custom Account ID field.
    > If you're using a custom text primary key, use **List rows** with a filter
    > instead: `cr_accountid eq '@{triggerBody()['text']}'` and take the first row.

**Add the response:**

10. **+ New step** → **Respond to a Copilot action**
11. Add outputs for each field you want to return:
    - **Text** output `AccountId` → Dynamic content: Account ID column
    - **Text** output `AccountType` → Dynamic content: Account Type column
    - **Text** output `Nickname` → Dynamic content: Nickname column
    - **Text** output `CurrentBalance` → Dynamic content: Current Balance column
    - **Text** output `AvailableBalance` → Dynamic content: Available Balance column
    - **Text** output `Status` → Dynamic content: Status column

    > 💡 Alternatively, return the entire row as a JSON string using
    > `string(outputs('Get_a_row_by_ID')?['body'])` in a single Text output.

12. **Save** → **Test** with `AccountId`: use the Dataverse GUID of ACCT-4521
    (or use the List rows approach with the text ID)

---

#### 4.3 Flow: Get Recent Transactions

This flow returns recent transactions for a specific account with summary totals.

1. **My flows** → **+ New flow** → **Instant cloud flow**
2. Name: `Get Recent Transactions`
3. Trigger: **Run a flow from Copilot** → **Create**

**Add input parameters:**

4. **+ Add an input** → **Text**
   - Name: `AccountId`
   - Description: `The account ID to get transactions for`
5. **+ Add an input** → **Number**
   - Name: `Count`
   - Description: `Number of recent transactions to return (default 5)`

**Add the Dataverse query:**

6. **+ New step** → **Dataverse** → **List rows**
7. Configure:
    - **Table name:** Banking Transactions
    - Click **Show advanced options**
    - **Filter rows:** `cr_accountid eq '@{triggerBody()['number']}'`

      > Adjust the column name to match your schema. Use the Account ID column name.

    - **Sort by:** `cr_date desc` (your Date column name + ` desc`)
    - **Row count:** Click the field → **Dynamic content** → select `Count`

      > If Count is blank/zero, you can add a **Condition** or **Compose** step
      > to default to 5: `if(equals(triggerBody()?['number'], null), 5, triggerBody()?['number'])`

**Add the response:**

8. **+ New step** → **Respond to a Copilot action**
9. Add a **Text** output `Transactions` with the expression:
   `string(outputs('List_rows')?['body/value'])`
10. **Save** → **Test** with `AccountId` for ACCT-4521 and `Count`: `5`

---

#### 4.4 Flow: Get Customer Profile

This flow returns the customer's profile information.

1. **My flows** → **+ New flow** → **Instant cloud flow**
2. Name: `Get Customer Profile`
3. Trigger: **Run a flow from Copilot** → **Create**

**Add input parameter:**

4. **+ Add an input** → **Text**
   - Name: `CustomerId`
   - Description: `The customer ID to look up`

**Add the Dataverse lookup:**

5. **+ New step** → **Dataverse** → **List rows** (or **Get a row by ID** if using GUIDs)
6. Configure:
    - **Table name:** Banking Customers
    - **Filter rows:** `cr_customerid eq '@{triggerBody()['text']}'`

**Add the response:**

7. **+ New step** → **Respond to a Copilot action**
8. Add a **Text** output `Profile` with the expression:
   `string(outputs('List_rows')?['body/value'])`
9. **Save** → **Test** with `CustomerId`: `CUST-1001`

---

#### 4.5 Test Each Flow

Before connecting to Copilot Studio, verify each flow runs successfully:

| Flow | Test Input | Expected Output |
|---|---|---|
| List Accounts | `CustomerId: CUST-1001` | 3 accounts (Checking, Savings, CD) |
| Get Account Balance | AccountId for ACCT-4521 | Primary Checking, $3,842.56 |
| Get Recent Transactions | AccountId for ACCT-4521, Count: 5 | 5 most recent transactions |
| Get Customer Profile | `CustomerId: CUST-1001` | Alex Morgan's profile |

> 🔧 **Troubleshooting:**
> - **"Table not found"** — Make sure you're in the correct environment in Power Automate
> - **"No rows returned"** — Check the filter column name matches your Dataverse schema
>   (go to Power Apps → Tables → Columns to find the exact internal name)
> - **"Invalid expression"** — Make sure you're using the Expression tab, not Dynamic content,
>   when entering formulas like `string(...)`

---

### Step 5: Register Actions (LLM-Routed)

This is where the generative orchestration model differs most from classic Copilot Studio.
Instead of wiring actions into topic nodes, you register them as **plugin actions** with
**descriptions the LLM reads** to decide when to invoke them.

#### 5.1 Add Each Flow as an Action

For each of the four Dataverse flows you created in Step 4, register it as a plugin action:

1. Open your **Virtual Banking Assistant** agent in [Copilot Studio](https://copilotstudio.microsoft.com)
2. Click **Actions** in the top navigation bar
3. Click **+ Add an action**
4. In the panel that appears, you'll see categories. Click **Power Automate** (under "Choose an action")
5. You'll see a list of flows available in your environment. Find **List Accounts** and click it

   > ⚠️ **Don't see your flow?** Make sure:
   > - You're in the same environment in Copilot Studio as where you created the flows
   > - The flow uses the "Run a flow from Copilot" trigger
   > - The flow has been saved (not just drafted)

6. Copilot Studio will show the flow's inputs and outputs
7. Review the **input parameters** — for List Accounts, you should see `CustomerID` (text)
8. Review the **output parameters** — you should see the response body text
9. Click **Next**

**Configure the action description:**

10. In the **Name** field, enter: `List Accounts`
11. In the **Description** field, enter the description from the table below
12. Click **Finish**

**Repeat steps 3–12 for each remaining flow:**

| Flow | Action Name | Description |
|---|---|---|
| **List Accounts** | List Accounts | Use this action to retrieve all bank accounts belonging to the authenticated customer. Returns account IDs, types (Checking, Savings, Certificate), nicknames, last 4 digits, current balances, available balances, and status. Call this first when the customer asks about any account — you need the account list to know what accounts they have. |
| **Get Account Balance** | Get Account Balance | Use this action to get detailed balance information for a single specific account. Requires an AccountId. Use this after the customer has selected or identified a specific account from their account list. Returns current balance, available balance, account type, nickname, and status. |
| **Get Recent Transactions** | Get Recent Transactions | Use this action to retrieve recent transaction history for a specific account. Requires an AccountId and optionally a Count (defaults to 5). Returns a list of transactions with date, description, amount, type (Credit/Debit), category, and running balance, plus summary totals (total credits, total debits, net change). |
| **Get Customer Profile** | Get Customer Profile | Use this action to look up the customer's personal information on file. Returns first name, last name, email, phone number, mailing address, and member-since date. This is read-only information — the customer cannot update their profile through this agent. |

> 💡 **Why descriptions matter so much:** In generative orchestration, the LLM reads every
> action description on each turn to decide which action (if any) to call. Vague descriptions
> like "Gets account data" lead to routing errors. Specific descriptions that explain
> **when** to use the action and **what it returns** give the LLM the context to make
> good decisions.

#### 5.2 Verify Actions Are Enabled

After adding all four actions, verify them:

1. Click **Actions** in the top navigation bar
2. You should see all four actions listed
3. Each action should show a green **On** status toggle — if any are off, click the toggle to enable them
4. Click on any action to review its description and parameters

#### 5.3 Configure Adaptive Card Responses

For each action, you can configure the output to use adaptive card templates so the
agent displays rich, formatted cards instead of plain text.

> 💡 **Note:** Adaptive card output formatting is configured in the **action's output settings**
> within Copilot Studio. Click on an action → **Output** → choose how to format the response.
> The card templates are in the [`adaptive-cards/`](adaptive-cards/) folder for reference.
> In some configurations, you may need to use a **Send a message** node in a topic to render
> the card. See the [Adaptive Cards documentation](https://learn.microsoft.com/en-us/microsoft-copilot-studio/authoring-send-message#add-an-adaptive-card) for details.

| Action | Adaptive Card |
|---|---|
| List Accounts | [`account-list-card.json`](adaptive-cards/account-list-card.json) |
| Get Account Balance | [`account-balance-card.json`](adaptive-cards/account-balance-card.json) |
| Get Recent Transactions | [`transaction-list-card.json`](adaptive-cards/transaction-list-card.json) |
| Get Customer Profile | [`customer-profile-card.json`](adaptive-cards/customer-profile-card.json) |
| Get Loan Rates | [`loan-rates-card.json`](adaptive-cards/loan-rates-card.json) |
| Calculate Loan Payment (MCP) | [`loan-calculator-card.json`](adaptive-cards/loan-calculator-card.json) |

---

### Step 6: Configure Minimal Topics (Guardrails Only)

With generative orchestration, you need very few topics. The LLM handles the
conversation flow. Topics serve only as **guardrails** for specific edge cases.

> 📖 See [`topics/topic-design-guide.md`](topics/topic-design-guide.md) for details.

#### 6.1 System Topics (Review Defaults)

System topics are built-in topics that handle common conversational scenarios. You need
to review them to make sure they're configured correctly for generative orchestration.

1. Open your agent in [Copilot Studio](https://copilotstudio.microsoft.com)
2. Click **Topics** in the top navigation bar
3. At the top, you'll see two tabs: **Custom** and **System**
4. Click the **System** tab to view built-in system topics

Review and configure each:

**Greeting:**
1. Click on the **Greeting** topic
2. You'll see a flow editor with a trigger and message node
3. For generative orchestration, you have two options:
   - **Option A (recommended):** Leave it as-is — the generative orchestrator will use
     your agent instructions to craft a greeting
   - **Option B:** Edit the message to something simple like:
     "👋 Welcome to the Virtual Banking Assistant! I can help you check balances,
     view transactions, list your accounts, or look up your profile. Just ask!"
4. Click **Save** if you made changes

**Fallback:**
1. Click on the **Fallback** topic
2. This topic fires when the orchestrator can't determine the user's intent
3. Ensure it has a message like: "I'm not sure how to help with that. Could you rephrase,
   or would you like me to connect you with a human agent?"
4. Click **Save** if you made changes

**Escalation:**
1. Click on the **Escalation** topic
2. This handles requests to talk to a human
3. Ensure it has a message like: "Let me connect you with a human agent."
4. If you have a handoff channel configured (e.g., Omnichannel for Customer Service),
   add a **Transfer conversation** node. For this lab, a message is sufficient.
5. Click **Save** if you made changes

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

### Step 7: Add External API Action (HTTP Connector → APIM)

This step demonstrates calling an **external REST API** from Copilot Studio — a pattern
you'll use whenever the data isn't in Dataverse (existing microservices, third-party APIs,
legacy systems).

> 📖 Full APIM setup guide: [`flows/apim-mock-setup.md`](flows/apim-mock-setup.md)

#### 7.1 Set Up the Loan Rate API (or Get the Endpoint)

**If facilitator-led:** Your workshop facilitator will provide the APIM endpoint URL
and subscription key. Skip to 7.2.

**If self-paced:** Follow [`flows/apim-mock-setup.md`](flows/apim-mock-setup.md) to create
an APIM instance with a mock Loan Rates endpoint. This takes ~5 minutes with the
Consumption tier.

#### 7.2 Create the Power Automate Flow

1. Open [https://make.powerautomate.com](https://make.powerautomate.com)
2. Confirm your **developer environment** is selected (top-right environment picker)
3. In the left nav, click **My flows**
4. Click **+ New flow** → **Instant cloud flow**
5. Name: `Get Loan Rates`
6. Trigger: select **Run a flow from Copilot** (no input parameters needed for this flow)
7. Click **Create**

**Add the HTTP action:**

8. Click **+ New step** (or the **+** button below the trigger)
9. Search for `HTTP` in the action search box
10. Select **HTTP** (the built-in premium connector)

    > ⚠️ **Premium connector note:** The HTTP connector requires a premium license. In trial
    > and developer environments, this is typically included. If you see a license error,
    > ask your facilitator for a workaround.

11. Configure the HTTP action:
    - **Method:** `GET`
    - **URI:** Paste the APIM endpoint URL provided by your facilitator, e.g.:
      `https://<apim-name>.azure-api.net/loans/rates`
    - **Headers:** Click **+ Add new item** and add:
      - **Key:** `Ocp-Apim-Subscription-Key`
      - **Value:** Paste the subscription key provided by your facilitator

**Parse the response:**

12. Click **+ New step**
13. Search for `Parse JSON` and select it
14. In the **Content** field, click it, then select the **Dynamic content** tab, and choose **Body** (from the HTTP step)
15. In the **Schema** field, paste the following schema:

```json
{
  "type": "object",
  "properties": {
    "rates": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "product": { "type": "string" },
          "minAPR": { "type": "number" },
          "maxAPR": { "type": "number" },
          "term": { "type": "string" }
        }
      }
    },
    "effectiveDate": { "type": "string" },
    "disclaimer": { "type": "string" }
  }
}
```

**Return the result to Copilot:**

16. Click **+ New step**
17. Search for `Respond to Copilot` and select **Respond to Copilot**
18. Click **+ Add an output** → select **Text**
19. In **Enter a title**, type: `LoanRatesJSON`
20. In the **Enter a value to respond** field, click the field, switch to the **Expression** tab, and enter:
    ```
    string(body('Parse_JSON'))
    ```
21. Click **OK** to insert the expression
22. Click **Save** (top-right)
23. Click **Test** → **Manually** → **Run** to verify the flow executes successfully

    > ⚠️ If the test fails with a 401 or 403 error, double-check your APIM subscription key.
    > If it fails with a connection error, ensure the APIM endpoint URL is correct.

#### 7.3 Register as an Action

1. Return to your agent in [Copilot Studio](https://copilotstudio.microsoft.com)
2. Click **Actions** in the top navigation bar
3. Click **+ Add an action**
4. Select **Power Automate** → find and click **Get Loan Rates**
5. This flow has no input parameters, so just review the output
6. Click **Next**
7. Set the **Name** to: `Get Loan Rates`
8. Set the **Description** to: `Use this action to retrieve current loan interest rates offered by the institution. Returns rates for all product types (auto loans, personal loans, mortgages, HELOC) with minimum and maximum APR ranges. Use this when the customer asks about loan rates, interest rates, or financing options.`
9. Click **Finish**
10. Verify the action appears in your Actions list with a green **On** toggle

#### 7.4 Test

1. In Copilot Studio, open the **Test** panel (bottom-left chat bubble icon)
2. Click **Reset** (circular arrow icon at the top of the test panel) to start a fresh conversation
3. Type the following messages one at a time:

- "What are your current loan rates?"
- "How much is a car loan?"
- "What's the mortgage rate?"

4. For each, verify:
   - The agent calls the **Get Loan Rates** action (you'll see it in the conversation trace)
   - The response includes rate information for the requested product
   - If you configured adaptive cards, the [`loan-rates-card.json`](adaptive-cards/loan-rates-card.json) card renders

> 💡 **To see the conversation trace:** In the test panel, look for a small info icon or
> expandable section under each agent response. Click it to see which action was called
> and the raw input/output data.

#### Why This Matters

Most enterprise agents need to call existing APIs — not just Dataverse. The HTTP
connector pattern works with any REST endpoint: internal microservices, third-party
APIs, or API Management facades. APIM adds rate limiting, caching, authentication
policies, and monitoring without changing the backend.

---

### Step 8: Add MCP Tool (Loan Payment Calculator)

This step demonstrates connecting Copilot Studio to a **Model Context Protocol (MCP)**
server — an open standard for exposing tools to AI agents. Unlike Power Automate actions,
MCP tools are **discovered dynamically** by the agent from the server's tool manifest.

> 📖 Full MCP server setup: [`flows/mcp-server-setup.md`](flows/mcp-server-setup.md)

#### 8.1 Set Up the MCP Server (or Get the Endpoint)

**If facilitator-led:** Your workshop facilitator will provide the MCP server endpoint
URL. Skip to 8.2.

**If self-paced:** Follow [`flows/mcp-server-setup.md`](flows/mcp-server-setup.md) to
build and host the loan payment calculator MCP server. Options:
- **Node.js** or **Python** implementation
- Host on Azure Container Apps, or use Dev Tunnels for local testing

#### 8.2 Register the MCP Server in Copilot Studio

1. Open your **Virtual Banking Assistant** agent in [Copilot Studio](https://copilotstudio.microsoft.com)
2. Click **Actions** in the top navigation bar
3. Click **+ Add an action**
4. In the action type panel, look for **MCP Server** (Model Context Protocol)

   > 💡 **Note on MCP support:** MCP integration in Copilot Studio may be in preview.
   > If you don't see an "MCP Server" option, check [Copilot Studio release notes](https://learn.microsoft.com/en-us/microsoft-copilot-studio/whats-new)
   > for the latest availability. As an alternative, you can call the MCP server via
   > a Power Automate HTTP flow (same pattern as Step 7) pointing to the MCP server's
   > HTTP endpoint.

5. Enter the **MCP server endpoint URL** provided by your facilitator (or the URL of the
   server you deployed in Step 8.1), e.g.:
   `https://<your-app>.azurecontainerapps.io/sse`
6. Copilot Studio will connect to the server and **discover tools automatically** — you should
   see `calculate_loan_payment` appear in the tool list
7. Review the tool's **description** and **parameter schema** — these come from the MCP server's
   tool manifest (you don't need to write them like you did for Power Automate actions)
8. Enable the tool by toggling it **On**
9. Click **Save**

> 💡 **Key difference from Power Automate actions:** You don't write the action description —
> it comes from the MCP server's tool manifest. The server declares what it can do, and
> Copilot Studio discovers it. This is how MCP enables interoperability across platforms.

#### 8.3 Test

1. In Copilot Studio, open the **Test** panel (bottom-left chat bubble icon)
2. Click **Reset** (circular arrow icon) to start a fresh conversation
3. Type the following messages one at a time and verify the agent calls the MCP tool:

- "What would my payments be on a $25,000 car loan at 5.25% for 5 years?"
- "Calculate payments for a $300,000 mortgage at 6.5% for 30 years"
- "If I borrow 15 thousand at 10 percent for 3 years, what's the monthly?"

4. For each, verify in the conversation trace:
   - The orchestrator **extracted** the principal, rate, and term from natural language
   - It called the MCP `calculate_loan_payment` tool (not a Power Automate flow)
   - The response includes monthly payment, total interest, and total cost
   - If configured, the [`loan-calculator-card.json`](adaptive-cards/loan-calculator-card.json) card renders

#### 8.4 Test the Combined Flow (Rates → Calculator)

This multi-turn test shows the orchestrator chaining an HTTP action and an MCP tool:

| Turn | User Message | Expected Behavior |
|---|---|---|
| 1 | "What are your auto loan rates?" | Calls Get Loan Rates → shows rates card |
| 2 | "Calculate payments for a $30,000 loan at the lowest rate for 5 years" | Knows the lowest auto rate from context → calls MCP calculator → shows payment card |

#### Why This Matters

MCP is an emerging open standard that enables:
- **Tool portability** — The same MCP server works with Copilot Studio, Claude, and other MCP-compatible agents
- **Dynamic discovery** — Agents discover tools at runtime; no manual action registration per tool
- **Separation of concerns** — Tool logic lives in the MCP server, not in the agent platform
- **Ecosystem growth** — As more tools become MCP-compatible, agents gain capabilities automatically

---

### Step 9: Test the Complete Agent

Now that all actions are registered (4 Dataverse + 1 HTTP + 1 MCP), test the agent
end-to-end. This is where you'll see the power of generative orchestration — the agent
handles natural variations, multi-turn conversations, and edge cases without explicit
topic flows.

#### 9.0 Open the Test Panel

1. Open your agent in [Copilot Studio](https://copilotstudio.microsoft.com)
2. Click the **Test** button (bottom-left of the screen — it's a chat bubble icon)
3. Click **Reset** (circular arrow icon at the top of the test panel) to ensure a fresh session
4. For each test below, type the message in the test panel and verify the expected behavior

> 💡 **Viewing the trace:** After each agent response, look for an expandable section
> or info icon below the response. Click it to see:
> - Which action (if any) was called
> - The input parameters sent to the action
> - The raw output received
> This is invaluable for debugging routing issues.

#### 9.1 Basic Capability Tests

| User Message | Expected Behavior |
|---|---|
| "Hi" | Greets the user, explains capabilities |
| "What's my checking balance?" | Calls List Accounts → identifies checking → calls Get Balance → shows card |
| "Show me my recent transactions" | Calls List Accounts → asks which account → calls Get Transactions → shows card |
| "What accounts do I have?" | Calls List Accounts → shows account list card |
| "What's my address?" | Calls Get Customer Profile → shows profile card |
| "What are your loan rates?" | Calls Get Loan Rates (HTTP/APIM) → shows rates card |
| "Calculate payments on a $20k loan at 6% for 4 years" | Calls MCP calculator → shows payment card |

#### 9.2 Multi-Turn Conversation Tests

These test the LLM's ability to maintain context across turns:

| Turn | User Message | Expected Behavior |
|---|---|---|
| 1 | "Show my accounts" | Lists all accounts |
| 2 | "What's the balance on the savings one?" | Knows which savings account from context → shows balance |
| 3 | "And the transactions?" | Knows to show transactions for the same savings account |
| 4 | "What about my checking?" | Switches context to checking → asks "balance or transactions?" |

#### 9.3 Cross-Action Chaining Tests

| Turn | User Message | Expected Behavior |
|---|---|---|
| 1 | "What are your auto loan rates?" | Calls Get Loan Rates → shows rates card |
| 2 | "What would I pay monthly on a $25k loan at the lowest rate for 60 months?" | Uses rate from context → calls MCP calculator → shows payment card |
| 3 | "What about for 36 months instead?" | Reuses principal and rate → calls MCP with new term |

#### 9.4 Natural Language Variation Tests

The LLM should handle these **without** explicit trigger phrases:

| User Message | Should Still Work |
|---|---|
| "How much money do I have?" | Routes to List Accounts or Get Balance |
| "Did I get paid this week?" | Routes to Get Transactions, understands "paid" = credit |
| "Where do you think I live?" | Routes to Get Customer Profile, shows address |
| "Show me everything" | Lists accounts, or asks what specifically they'd like |
| "What did I spend at restaurants?" | Routes to transactions, filters may not apply but shows recent |
| "How much would a car loan cost me?" | May call loan rates, then ask for details to calculate |
| "I want to borrow 50 grand" | Routes to loan calculator, asks for rate and term |

#### 9.5 Guardrail / Boundary Tests

| User Message | Expected Behavior |
|---|---|
| "Transfer $500 to savings" | Politely declines — explains it can only view, not modify |
| "Show me John's account" | Refuses — only shows authenticated user's data |
| "Change my email address" | Explains profile is read-only, directs to branch or 1-800-555-0199 |
| "Should I invest in crypto?" | Declines — no financial advice per instructions |
| "What are your branch hours?" | Answers from knowledge source (generative answers) |

#### 9.6 Troubleshoot Routing Issues

If the LLM calls the wrong action or doesn't call any:

1. **Check action descriptions** — Are they specific enough? Do they explain when to use the action?
2. **Check agent instructions** — Do they cover the scenario the user is asking about?
3. **Check orchestration mode** — Ensure it's set to **Generative**, not Classic
4. **Review the conversation trace** — In the test panel, expand the trace to see which
   action the orchestrator considered and why

---

### Step 10: Publish to Microsoft Teams

Once testing is complete, publish the agent to Microsoft Teams so users can interact
with it in a familiar chat interface.

#### 10.1 Publish the Agent

Before configuring channels, you need to publish the latest version of your agent:

1. Open your agent in [Copilot Studio](https://copilotstudio.microsoft.com)
2. Click **Publish** in the top-right corner of the agent editor
3. Click **Publish** again in the confirmation dialog
4. Wait for the publishing process to complete — you'll see a green "Published" status

   > ⏱️ Publishing typically takes 1–3 minutes. You can proceed to configure the Teams
   > channel while it publishes.

#### 10.2 Configure the Teams Channel

1. In your agent, click **Channels** in the left sidebar (or navigation bar)
2. Under **Messaging**, find and click **Microsoft Teams**
3. Review the configuration:
   - **Agent name** — how it appears in Teams (defaults to your agent name)
   - **Icon** — optionally upload a custom icon
   - **Description** — appears when users search for the agent
4. Click **Turn on Teams** (or **Add** if shown)
5. You'll see a confirmation with a link to open the agent in Teams

#### 10.3 Test in Teams

1. Click the **Open in Teams** link from Copilot Studio, or:
   - Open **Microsoft Teams** (desktop or web)
   - Go to the **Chat** section
   - Click **+ New chat** and search for your agent name "Virtual Banking Assistant"
   - Alternatively, look for the agent in the **Apps** section of Teams
2. Start a conversation with the agent
3. Run through the key test scenarios from Step 9:
   - "What accounts do I have?"
   - "Show me my checking balance"
   - "What are your loan rates?"
   - "Calculate a $20,000 loan at 6% for 4 years"
4. Verify:
   - The agent responds correctly to each query
   - Adaptive cards render properly in the Teams chat window
   - Multi-turn context is maintained between messages

> 💡 **Tip:** Adaptive cards may render slightly differently in Teams vs the Copilot Studio
> test panel. Always validate in Teams before considering the lab complete.
>
> ⚠️ **If the agent doesn't appear in Teams:** Make sure publishing completed successfully
> in Copilot Studio. Also ensure you're signed into Teams with the same tenant as your
> Copilot Studio environment.

---

### Step 11: Export and Save

Export your completed agent as a Power Platform solution file. This is how agents are
moved between environments (dev → test → production) in real projects.

#### 11.1 Find Your Solution

1. Open [https://make.powerapps.com](https://make.powerapps.com)
2. Confirm your **developer environment** is selected (top-right environment picker)
3. In the left navigation, click **Solutions**
4. Look for the solution that contains your agent. If you built the agent in Copilot Studio,
   it's typically in a solution named after your agent or in the **Default Solution**
5. If you don't see it in the list, try clicking **Managed** or **All** tabs to change
   the view filter

   > 💡 **Tip:** If your agent isn't in a dedicated solution, you can create one:
   > Click **+ New solution** → name it "Virtual Banking Assistant" → select a publisher →
   > click **Create**. Then open the solution and click **Add existing** → **Agent** to
   > include your Copilot Studio agent.

#### 11.2 Export the Solution

1. Click the **checkbox** next to your solution to select it
2. Click **Export** in the top toolbar
3. You'll see an export wizard:
   - **Publish before export:** Click **Next** (this ensures you export the latest version)
   - **Export as:** Select **Unmanaged** (for lab purposes — unmanaged allows you to
     re-import and edit; managed is for production deployments where changes are locked)
   - Click **Export**
4. Wait for the export to complete — this may take 1–2 minutes
5. Your browser will download a `.zip` file (e.g., `VirtualBankingAssistant_1_0_0_0.zip`)

#### 11.3 Save for Reference

1. Save the exported `.zip` file to a known location on your computer
2. Optionally, place a copy in this lab folder for reference:
   `labs/lab02-copilot-studio/exported-solution/`

> 💡 **What's in the solution file?** The `.zip` contains XML definitions of your agent,
> its topics, actions, knowledge sources, and any related components. This is the artifact
> you'd check into source control and deploy through ALM pipelines in a real project.
> Lab 04 (CI/CD and ALM) will cover this in detail.

---

## Deliverables

- [ ] Dataverse tables created and populated with mock data (3 customers, 7 accounts, 20 transactions)
- [ ] Working Copilot Studio agent with generative orchestration enabled
- [ ] Agent instructions written and tested
- [ ] Knowledge source added with generative answers working
- [ ] 4 Power Automate flows for Dataverse actions (List Accounts, Get Balance, Get Transactions, Get Profile)
- [ ] 1 Power Automate flow calling external API via HTTP connector (Get Loan Rates via APIM)
- [ ] MCP server deployed and registered (Loan Payment Calculator)
- [ ] All actions registered with LLM-optimized descriptions
- [ ] Adaptive cards displaying financial data correctly (6 card templates)
- [ ] Multi-turn and cross-action chaining tested (rates → calculator flow)
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
| HTTP connector returns 401/403 | Verify APIM subscription key is correct in the flow header |
| MCP tool not discovered | Check the MCP server endpoint is accessible; verify SSE transport is configured |
| MCP tool called but returns error | Test the MCP server independently with the MCP Inspector |
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
| **HTTP connector** | Call any external REST API from Power Automate (APIM, microservices, third-party) |
| **MCP tools** | Open standard for connecting agents to external tool servers with dynamic discovery |
| **Adaptive cards** | Rich, structured UI for displaying data in chat |

### Three Action Patterns

This lab demonstrated three ways to give an agent capabilities:

| Pattern | Example | When to Use |
|---|---|---|
| **Power Automate + Dataverse** | Account queries | Data lives in Power Platform / Dataverse |
| **Power Automate + HTTP** | Loan rates via APIM | Calling existing REST APIs or external services |
| **MCP Tool** | Loan calculator | Reusable tool logic; cross-platform compatibility |

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
