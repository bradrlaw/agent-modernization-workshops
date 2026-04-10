# Topic Design Guide – Copilot Studio Topics

This guide documents the topics to create in Copilot Studio for the Virtual Banking
Assistant. Each topic section includes trigger phrases, the node-by-node flow design,
and implementation notes.

---

## Topic 1: Welcome / Greeting

**Purpose:** Greet the user and present available capabilities.

### Trigger Phrases
- Hi
- Hello
- Hey
- Good morning
- Get started
- Help
- What can you do?

### Flow Design

```
[Trigger] → [Message: Welcome] → [Message: Capability List]
```

### Message: Welcome

> 👋 Welcome to the Virtual Banking Assistant! I can help you with the following:
>
> • **Check account balance** — View current and available balances
> • **Recent transactions** — See your latest account activity
> • **List accounts** — View all your accounts at a glance
> • **Profile information** — Review your contact details on file
>
> What would you like to do?

### Implementation Notes
- Set this as a high-priority topic so it triggers on generic greetings
- Consider adding a quick-reply / suggested actions for the four capabilities

---

## Topic 2: Check Account Balance

**Purpose:** Look up and display the current balance for a selected account.

### Trigger Phrases
- What's my balance?
- Check my balance
- Account balance
- How much money do I have?
- What's in my checking account?
- Show my savings balance
- Balance on account ending in {last4}

### Flow Design

```
[Trigger]
    → [Action: List Accounts]  (get accounts for customer)
    → [Condition: Single account?]
        → YES: Skip selection
        → NO:  [Question: Which account?]  (show as choices)
    → [Action: Get Balance]  (retrieve balance for selected account)
    → [Message: Balance Card]  (adaptive card)
    → [Question: Anything else?]
        → YES: [Redirect: Welcome]
        → NO:  [Message: Goodbye]
```

### Question: Which Account?

> Which account would you like to check?

Display as choice options:
- Primary Checking ••••4521
- Emergency Fund ••••7834
- 12-Month CD ••••2190

### Message: Balance Card

Use the adaptive card template: [`adaptive-cards/account-balance-card.json`](../adaptive-cards/account-balance-card.json)

### Implementation Notes
- The "List Accounts" action returns all accounts for the authenticated customer
- If the user mentions a specific account type or last 4 digits in the trigger, try to
  auto-select the account (use entity extraction or slot-filling)
- Format currency values with 2 decimal places and comma separators

---

## Topic 3: Recent Transactions

**Purpose:** Display recent transaction history for a selected account.

### Trigger Phrases
- Show my recent transactions
- Transaction history
- What were my last purchases?
- Recent activity
- Any deposits recently?
- Show transactions for checking
- What did I spend this week?

### Flow Design

```
[Trigger]
    → [Action: List Accounts]
    → [Question: Which account?]  (choice selection)
    → [Question: How many transactions?]  (default: 5)
    → [Action: Get Transactions]  (retrieve recent transactions)
    → [Message: Transaction Card]  (adaptive card with list)
    → [Question: Anything else?]
        → YES: [Redirect: Welcome]
        → NO:  [Message: Goodbye]
```

### Question: How Many Transactions?

> How many recent transactions would you like to see?

Options:
- Last 5 (default)
- Last 10
- Last 30

### Message: Transaction Card

Use the adaptive card template: [`adaptive-cards/transaction-list-card.json`](../adaptive-cards/transaction-list-card.json)

### Implementation Notes
- Default to the 5 most recent transactions if the user doesn't specify
- Calculate and display summary totals (deposits, withdrawals, net change)
- Color-code amounts: green for credits, red for debits

---

## Topic 4: List All Accounts

**Purpose:** Display all accounts and balances for the customer.

### Trigger Phrases
- Show my accounts
- List all accounts
- What accounts do I have?
- Account overview
- All my accounts

### Flow Design

```
[Trigger]
    → [Action: List Accounts]  (get all accounts with balances)
    → [Message: Account List Card]  (adaptive card)
    → [Question: Want to see details for a specific account?]
        → YES: [Redirect: Check Account Balance]
        → NO:  [Question: Anything else?]
```

### Message: Account List Card

Use the adaptive card template: [`adaptive-cards/account-list-card.json`](../adaptive-cards/account-list-card.json)

### Implementation Notes
- Calculate and display total balance across all accounts
- Include account status (Active, Inactive, etc.)
- Card includes action buttons to drill into balance or transactions

---

## Topic 5: Customer Profile

**Purpose:** Display the customer's profile information on file.

### Trigger Phrases
- Show my profile
- What's my address?
- What email do you have for me?
- My contact information
- Account holder information
- What phone number is on file?

### Flow Design

```
[Trigger]
    → [Action: Get Customer Profile]
    → [Message: Profile Card]  (adaptive card)
    → [Message: Update Notice]
    → [Question: Anything else?]
        → YES: [Redirect: Welcome]
        → NO:  [Message: Goodbye]
```

### Message: Profile Card

Use the adaptive card template: [`adaptive-cards/customer-profile-card.json`](../adaptive-cards/customer-profile-card.json)

### Message: Update Notice

> ℹ️ To update your profile information, please visit a branch or call our support line
> at 1-800-555-0199.

### Implementation Notes
- Profile is read-only — the agent should not offer to update profile information
- If the user asks to change their address/phone/email, explain they need to visit a
  branch or call support
- Be security-conscious: confirm the profile belongs to the authenticated user

---

## Topic 6: Fallback / Escalation

**Purpose:** Handle unrecognized requests gracefully.

### Trigger
This is the system fallback topic — it fires when no other topic matches.

### Flow Design

```
[Fallback Trigger]
    → [Condition: Can generative answers handle it?]
        → YES: [Generative Answer]  (grounded in knowledge sources)
        → NO:  [Message: Escalation offer]
    → [Question: Would you like to try something else?]
        → YES: [Redirect: Welcome]
        → NO:  [Message: Goodbye]
```

### Message: Escalation Offer

> I'm not sure I can help with that. Would you like me to connect you with a
> human agent?

### Implementation Notes
- Enable **generative answers** as the first fallback (uses knowledge sources)
- If generative answers also can't help, offer human escalation
- Log unrecognized intents for future topic development
