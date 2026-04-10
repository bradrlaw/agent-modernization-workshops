# Loan Rate API – APIM Mock Backend Setup

This guide walks through creating a mock Loan Rate API using **Azure API Management**
with policy-based mock responses. No backend compute (Functions, App Service) is needed —
APIM returns static JSON directly from policy configuration.

---

## Architecture

```
Copilot Studio                    Azure API Management
┌──────────────┐    HTTPS     ┌─────────────────────────┐
│ Power Automate│ ──────────→ │  APIM Mock Policy        │
│ HTTP Connector│ ←────────── │  (no backend compute)    │
└──────────────┘              └─────────────────────────┘
```

---

## Step 1: Create an API Management Instance

> If you already have an APIM instance, skip to Step 2.

1. Go to the [Azure Portal](https://portal.azure.com)
2. **Create a resource** → search **API Management**
3. Configure:

| Setting | Value |
|---|---|
| Resource group | Your lab resource group |
| Name | `banking-workshop-apim` (must be globally unique) |
| Region | Your preferred region |
| Pricing tier | **Consumption** (pay-per-call, no idle cost) |
| Organization name | Workshop |
| Administrator email | Your email |

4. Click **Review + Create** → **Create**

> ⏱ The Consumption tier deploys in ~1 minute. Other tiers take 30–45 minutes.

---

## Step 2: Create the Loan Rates API

1. In your APIM instance, go to **APIs** → **+ Add API** → **Blank API**
2. Configure:

| Setting | Value |
|---|---|
| Display name | Loan Rates API |
| Name | loan-rates-api |
| API URL suffix | `loans` |
| Description | Returns current loan interest rates by product type |

3. Click **Create**

---

## Step 3: Add the GET /rates Operation

1. In the Loan Rates API, click **+ Add operation**
2. Configure:

| Setting | Value |
|---|---|
| Display name | Get Loan Rates |
| HTTP method | GET |
| URL | `/rates` |
| Description | Returns current loan rates for all product types |

3. Click **Save**

---

## Step 4: Add the Mock Response Policy

1. Select the **Get Loan Rates** operation
2. Go to the **Inbound processing** section → click **</>** (Code view)
3. Replace the `<inbound>` section with:

```xml
<inbound>
    <base />
    <mock-response status-code="200" content-type="application/json" />
</inbound>
```

4. Go to the **Responses** tab of the operation
5. Click **+ Add response** → status code **200**
6. Under **Representations**, click **+ Add representation**
   - Content type: `application/json`
   - Sample:

```json
{
  "rates": [
    {
      "productType": "Auto Loan - New",
      "term": "36 months",
      "minRate": 4.25,
      "maxRate": 6.75,
      "asOfDate": "2026-04-01"
    },
    {
      "productType": "Auto Loan - Used",
      "term": "36 months",
      "minRate": 4.75,
      "maxRate": 7.50,
      "asOfDate": "2026-04-01"
    },
    {
      "productType": "Personal Loan",
      "term": "12-60 months",
      "minRate": 8.99,
      "maxRate": 17.99,
      "asOfDate": "2026-04-01"
    },
    {
      "productType": "Home Equity Line of Credit",
      "term": "Variable",
      "minRate": 7.25,
      "maxRate": 11.50,
      "asOfDate": "2026-04-01"
    },
    {
      "productType": "30-Year Fixed Mortgage",
      "term": "360 months",
      "minRate": 6.125,
      "maxRate": 6.875,
      "asOfDate": "2026-04-01"
    },
    {
      "productType": "15-Year Fixed Mortgage",
      "term": "180 months",
      "minRate": 5.375,
      "maxRate": 5.875,
      "asOfDate": "2026-04-01"
    }
  ],
  "disclaimer": "Rates shown are for illustrative purposes. Actual rates depend on creditworthiness and other factors.",
  "lastUpdated": "2026-04-01T00:00:00Z"
}
```

7. Click **Save**

---

## Step 5: Configure Security (Subscription Key)

1. Go to **Subscriptions** in your APIM instance
2. A default subscription exists — click to reveal the **Primary key**
3. Copy this key — you'll need it in the Power Automate flow
4. The key is passed via the `Ocp-Apim-Subscription-Key` header

> For the workshop: Share this key with participants during the lab.
> For public use: Participants create their own APIM instance.

---

## Step 6: Test the API

### Test in APIM Portal

1. Go to **APIs** → **Loan Rates API** → **Get Loan Rates**
2. Click the **Test** tab
3. Click **Send**
4. Verify you get a 200 response with the mock JSON

### Test with curl

```bash
curl -H "Ocp-Apim-Subscription-Key: YOUR_KEY" \
  https://banking-workshop-apim.azure-api.net/loans/rates
```

---

## Step 7: Add a GET /rates/{productType} Operation (Optional)

For a more targeted lookup, add a parameterized endpoint:

1. **+ Add operation**
2. Configure:

| Setting | Value |
|---|---|
| Display name | Get Rate by Product |
| HTTP method | GET |
| URL | `/rates/{productType}` |
| Template parameter | `productType` (string) |

3. Add mock response policy and a sample response for a single product:

```json
{
  "productType": "Auto Loan - New",
  "term": "36 months",
  "minRate": 4.25,
  "maxRate": 6.75,
  "asOfDate": "2026-04-01",
  "disclaimer": "Rate depends on creditworthiness and other factors."
}
```

---

## API Endpoint Summary

| Endpoint | Method | Description |
|---|---|---|
| `/loans/rates` | GET | Returns all current loan rates |
| `/loans/rates/{productType}` | GET | Returns rate for a specific product (optional) |

**Base URL:** `https://<your-apim-name>.azure-api.net`
**Auth:** `Ocp-Apim-Subscription-Key` header

---

## Workshop Facilitator Notes

If you're running this as a facilitated workshop:

1. Create the APIM instance in your subscription before the workshop
2. Share the base URL and subscription key with participants
3. Participants skip Steps 1–5 and go directly to the Power Automate flow setup
4. After the workshop, you can delete the APIM instance to avoid costs

If participants are self-paced:
- They follow Steps 1–6 to create their own APIM instance
- Consumption tier has no idle cost — they only pay for API calls during the lab
