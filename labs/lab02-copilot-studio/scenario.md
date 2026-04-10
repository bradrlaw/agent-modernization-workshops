# Lab 02 Scenario – Virtual Banking Assistant

## Use Case Overview

You are building a **Virtual Banking Assistant** — a conversational agent that helps
customers of a retail financial institution perform common self-service account actions.
The agent will be built in Microsoft Copilot Studio using a low-code approach.

This scenario is intentionally representative of common financial services use cases and
will be reused across later labs (pro-code in Lab 03, hybrid in Lab 05, multi-agent in
Lab 06) to demonstrate the same scenario built with different tools.

---

## Agent Persona

| Field | Value |
|---|---|
| **Agent Name** | Virtual Banking Assistant |
| **Tone** | Professional, helpful, concise |
| **Role** | Self-service account assistant for authenticated customers |
| **Channels** | Microsoft Teams, Demo website |

### System Instructions (Agent Description)

> You are a Virtual Banking Assistant for a retail financial institution. You help
> authenticated customers check account balances, review recent transactions, list their
> accounts, and look up their profile information. Always be professional, accurate,
> and security-conscious. Never share information about other customers. If you cannot
> fulfill a request, offer to connect the customer with a human agent.

---

## Core Capabilities

### 1. Get Account Balance

**User intent:** Check the current balance of a specific account.

**Example conversations:**
- "What's my checking account balance?"
- "How much do I have in savings?"
- "Show me the balance on my account ending in 4521"

**Required data:**
- Customer ID (from authentication context)
- Account ID or account type selection

**Response includes:**
- Account name and last 4 digits
- Current balance
- Available balance
- As-of date/time

---

### 2. Recent Transactions

**User intent:** View recent transaction history for an account.

**Example conversations:**
- "Show me my recent transactions"
- "What were my last 5 purchases?"
- "Any deposits this week?"

**Required data:**
- Customer ID (from authentication context)
- Account ID
- Optional: date range, transaction count, filter (deposits/withdrawals)

**Response includes:**
- Transaction list (date, description, amount, running balance)
- Summary (total deposits, total withdrawals, net change)
- Displayed in an adaptive card for readability

---

### 3. List Accounts

**User intent:** See all accounts associated with their profile.

**Example conversations:**
- "What accounts do I have?"
- "Show me all my accounts"
- "List my accounts and balances"

**Required data:**
- Customer ID (from authentication context)

**Response includes:**
- Account list with type, nickname, last 4 digits, and current balance
- Total across all accounts

---

### 4. Customer Profile

**User intent:** View or confirm personal information on file.

**Example conversations:**
- "What address do you have on file for me?"
- "Show me my profile"
- "What's my email address?"

**Required data:**
- Customer ID (from authentication context)

**Response includes:**
- Name, address, phone, email
- Membership/account open date
- Note: Agent should NOT be able to modify profile information (read-only)

---

## Conversation Flow Diagram

```
                         ┌──────────────┐
                         │  User Starts │
                         │ Conversation │
                         └──────┬───────┘
                                │
                         ┌──────▼───────┐
                         │   Welcome    │
                         │   Message    │
                         └──────┬───────┘
                                │
                    ┌───────────┼───────────┐
                    │           │           │
              ┌─────▼─────┐ ┌──▼───┐ ┌────▼─────┐
              │ Account   │ │Trans-│ │ Profile  │
              │ Actions   │ │actions│ │  Lookup  │
              └─────┬─────┘ └──┬───┘ └────┬─────┘
                    │          │          │
              ┌─────▼─────┐   │    ┌─────▼─────┐
              │  Balance   │   │    │  Display  │
              │  or List   │   │    │  Profile  │
              └─────┬─────┘   │    └─────┬─────┘
                    │          │          │
                    └──────────┼──────────┘
                               │
                        ┌──────▼───────┐
                        │  Anything    │
                        │  else?       │
                        └──────┬───────┘
                               │
                        ┌──────▼───────┐
                        │   End or     │
                        │   Continue   │
                        └──────────────┘
```

---

## Data Model

### Customer

| Field | Type | Example |
|---|---|---|
| customerId | string | "CUST-1001" |
| firstName | string | "Alex" |
| lastName | string | "Morgan" |
| email | string | "alex.morgan@example.com" |
| phone | string | "(555) 123-4567" |
| address.street | string | "742 Evergreen Terrace" |
| address.city | string | "Springfield" |
| address.state | string | "VA" |
| address.zip | string | "22150" |
| memberSince | date | "2018-03-15" |

### Account

| Field | Type | Example |
|---|---|---|
| accountId | string | "ACCT-4521" |
| customerId | string | "CUST-1001" |
| type | string | "Checking" |
| nickname | string | "Primary Checking" |
| last4 | string | "4521" |
| currentBalance | number | 3842.56 |
| availableBalance | number | 3742.56 |
| status | string | "Active" |

### Transaction

| Field | Type | Example |
|---|---|---|
| transactionId | string | "TXN-90001" |
| accountId | string | "ACCT-4521" |
| date | datetime | "2026-04-08T14:23:00Z" |
| description | string | "Direct Deposit - Employer" |
| amount | number | 2450.00 |
| type | string | "Credit" |
| category | string | "Income" |
| runningBalance | number | 3842.56 |

---

## Authentication Model (Simplified for Lab)

For this lab, authentication is **simulated**. The agent will prompt the user to select
a demo customer profile rather than integrating with a real identity provider.

In a production scenario, the agent would:
1. Authenticate via Microsoft Entra ID (SSO through Teams)
2. Map the authenticated identity to a customer record
3. Scope all data queries to that customer's accounts

> **Lab 04** and **Lab 05** will introduce real authentication patterns.

---

## Success Criteria

By the end of this lab, your agent should be able to:

- [ ] Greet the user and explain available capabilities
- [ ] Look up and display account balances (formatted with adaptive cards)
- [ ] Show recent transactions for a selected account
- [ ] List all accounts for a customer
- [ ] Display customer profile information
- [ ] Handle unrecognized requests gracefully (fallback to generative answers or escalation)
- [ ] Be published and testable in Teams or the demo website
