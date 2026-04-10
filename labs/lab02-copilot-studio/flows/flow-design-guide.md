# Power Automate Flow Designs

This document describes the Power Automate cloud flows used as **plugin actions** in
the Copilot Studio Virtual Banking Assistant. Each flow simulates a backend API call
to retrieve account data.

> **Note:** In a production scenario, these flows would call real banking APIs or
> query a secure database. For this lab, the flows return mock data from Dataverse
> tables or hardcoded JSON responses.

---

## Data Setup: Import Mock Data to Dataverse

Before creating the flows, import the sample data into Dataverse tables:

### Option A: Manual Dataverse Tables

Create three tables in your Power Platform environment:

#### Table: Banking Customers

| Column | Type | Description |
|---|---|---|
| Customer ID | Text (Primary) | e.g., "CUST-1001" |
| First Name | Text | |
| Last Name | Text | |
| Email | Text | |
| Phone | Text | |
| Street | Text | |
| City | Text | |
| State | Text | |
| Zip | Text | |
| Member Since | Date | |

#### Table: Banking Accounts

| Column | Type | Description |
|---|---|---|
| Account ID | Text (Primary) | e.g., "ACCT-4521" |
| Customer ID | Lookup → Banking Customers | |
| Account Type | Choice (Checking, Savings, Certificate) | |
| Nickname | Text | |
| Last 4 | Text | |
| Current Balance | Currency | |
| Available Balance | Currency | |
| Status | Choice (Active, Inactive, Closed) | |

#### Table: Banking Transactions

| Column | Type | Description |
|---|---|---|
| Transaction ID | Text (Primary) | e.g., "TXN-90001" |
| Account ID | Lookup → Banking Accounts | |
| Date | Date/Time | |
| Description | Text | |
| Amount | Currency | |
| Type | Choice (Credit, Debit) | |
| Category | Text | |
| Running Balance | Currency | |

### Option B: Import from JSON

Use the sample data files in [`sample-data/`](../sample-data/) and import via:
- Power Apps → Tables → Import data
- Or use a Power Automate flow to read the JSON and create records

---

## Flow 1: List Accounts

**Purpose:** Returns all accounts for a given customer.

### Input Parameters

| Parameter | Type | Description |
|---|---|---|
| CustomerId | Text | The authenticated customer's ID |

### Flow Steps

```
[When an action is performed]  (Copilot Studio trigger)
    → [List rows]  (Dataverse: Banking Accounts)
        Filter: Customer ID eq {CustomerId}
    → [Select]  (map to output schema)
    → [Respond to Copilot]  (return account list)
```

### Output Schema

```json
{
  "accounts": [
    {
      "accountId": "ACCT-4521",
      "type": "Checking",
      "nickname": "Primary Checking",
      "last4": "4521",
      "currentBalance": 3842.56,
      "availableBalance": 3742.56,
      "status": "Active"
    }
  ],
  "totalBalance": 21342.56
}
```

### Implementation Tips
- Use the **Dataverse** connector (not the legacy Common Data Service connector)
- Apply OData filter: `_customerid_value eq '{CustomerId}'`
- Use a **Select** action to flatten the Dataverse response into a clean schema
- Calculate `totalBalance` using a **Compose** action with an expression:
  `sum(body('Select')?['currentBalance'])`

---

## Flow 2: Get Account Balance

**Purpose:** Returns detailed balance information for a single account.

### Input Parameters

| Parameter | Type | Description |
|---|---|---|
| AccountId | Text | The selected account ID |

### Flow Steps

```
[When an action is performed]
    → [Get a row by ID]  (Dataverse: Banking Accounts)
        Row ID: {AccountId}
    → [Compose]  (format response)
    → [Respond to Copilot]
```

### Output Schema

```json
{
  "accountId": "ACCT-4521",
  "type": "Checking",
  "nickname": "Primary Checking",
  "last4": "4521",
  "currentBalance": 3842.56,
  "availableBalance": 3742.56,
  "status": "Active",
  "asOfDate": "2026-04-09T15:00:00Z"
}
```

---

## Flow 3: Get Recent Transactions

**Purpose:** Returns recent transactions for a specific account.

### Input Parameters

| Parameter | Type | Description |
|---|---|---|
| AccountId | Text | The selected account ID |
| Count | Integer | Number of transactions to return (default: 5) |

### Flow Steps

```
[When an action is performed]
    → [List rows]  (Dataverse: Banking Transactions)
        Filter: Account ID eq {AccountId}
        Order by: Date descending
        Top count: {Count}
    → [Select]  (map to output schema)
    → [Compose]  (calculate summary totals)
    → [Respond to Copilot]
```

### Output Schema

```json
{
  "accountId": "ACCT-4521",
  "transactions": [
    {
      "date": "2026-04-08",
      "description": "Direct Deposit - Employer",
      "amount": 2450.00,
      "type": "Credit",
      "category": "Income",
      "runningBalance": 3842.56
    }
  ],
  "summary": {
    "totalCredits": 2950.00,
    "totalDebits": 2340.39,
    "netChange": 609.61,
    "transactionCount": 10
  }
}
```

### Implementation Tips
- Use `$top` and `$orderby` in the OData query
- Calculate summary with expressions:
  - Credits: filter where type = 'Credit', sum amounts
  - Debits: filter where type = 'Debit', sum absolute amounts

---

## Flow 4: Get Customer Profile

**Purpose:** Returns the authenticated customer's profile information.

### Input Parameters

| Parameter | Type | Description |
|---|---|---|
| CustomerId | Text | The authenticated customer's ID |

### Flow Steps

```
[When an action is performed]
    → [Get a row by ID]  (Dataverse: Banking Customers)
        Row ID: {CustomerId}
    → [Compose]  (format response)
    → [Respond to Copilot]
```

### Output Schema

```json
{
  "customerId": "CUST-1001",
  "firstName": "Alex",
  "lastName": "Morgan",
  "email": "alex.morgan@example.com",
  "phone": "(555) 123-4567",
  "address": {
    "street": "742 Evergreen Terrace",
    "city": "Springfield",
    "state": "VA",
    "zip": "22150"
  },
  "memberSince": "2018-03-15"
}
```

---

## Registering Flows as Copilot Studio Actions

After creating each flow:

1. Open your agent in **Copilot Studio**
2. Go to **Actions** → **+ Add an action**
3. Select **Power Automate flow**
4. Choose the flow from the list
5. Map input/output parameters
6. Give the action a clear name and description so the orchestrator can route to it:

| Action Name | Description (for LLM routing) |
|---|---|
| List Accounts | Retrieves all bank accounts and balances for the authenticated customer |
| Get Account Balance | Gets the detailed balance for a specific bank account by account ID |
| Get Recent Transactions | Returns recent transaction history for a specific account with summary totals |
| Get Customer Profile | Retrieves the customer's personal information including name, address, and contact details |

> **Tip:** The description field is critical — Copilot Studio's orchestrator uses it
> to decide when to invoke the action. Be specific and include key terms users might say.

---

## Testing Flows

Test each flow independently before connecting to Copilot Studio:

1. Open the flow in Power Automate
2. Click **Test** → **Manually**
3. Provide sample input parameters (e.g., `CustomerId: CUST-1001`)
4. Verify the output matches the expected schema
5. Check that Dataverse queries return correct data
