# MCP Loan Payment Calculator Server

A simple [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that
exposes a **loan payment calculator** tool. When registered in Copilot Studio as an MCP
action, the agent can calculate monthly payments, total interest, and total cost for
any loan — using the same tool-calling pattern that LLMs use natively.

---

## What is MCP?

The **Model Context Protocol** is an open standard for connecting AI agents to external
tools and data sources. Instead of building custom connectors for each platform, you
expose tools via MCP and any MCP-compatible agent (Copilot Studio, Claude, etc.) can
discover and call them.

```
┌──────────────┐    MCP Protocol     ┌──────────────────┐
│ Copilot Studio│ ────────────────→  │  MCP Server       │
│ (MCP Client)  │ ←────────────────  │  (Tool Provider)  │
└──────────────┘                     └──────────────────┘
                                      Exposes tools:
                                      • calculate_loan_payment
```

---

## Tool Definition

### `calculate_loan_payment`

Calculates the monthly payment, total interest, and total cost of a loan.

**Input Parameters:**

| Parameter | Type | Required | Description |
|---|---|---|---|
| `principal` | number | Yes | Loan amount in dollars (e.g., 25000) |
| `annualRate` | number | Yes | Annual interest rate as a percentage (e.g., 5.25) |
| `termMonths` | number | Yes | Loan term in months (e.g., 60) |

**Output:**

| Field | Type | Description |
|---|---|---|
| `monthlyPayment` | number | Monthly payment amount |
| `totalInterest` | number | Total interest paid over the life of the loan |
| `totalCost` | number | Total amount paid (principal + interest) |
| `principal` | number | Original loan amount (echo back) |
| `annualRate` | number | Annual rate used (echo back) |
| `termMonths` | number | Term used (echo back) |

**Example:**

Input: `principal: 25000, annualRate: 5.25, termMonths: 60`

Output:
```json
{
  "monthlyPayment": 474.65,
  "totalInterest": 3478.76,
  "totalCost": 28478.76,
  "principal": 25000,
  "annualRate": 5.25,
  "termMonths": 60
}
```

---

## Implementation

### Option A: Node.js MCP Server (Recommended)

Create a simple MCP server using the official `@modelcontextprotocol/sdk`:

#### Install Dependencies

```bash
mkdir mcp-loan-calculator && cd mcp-loan-calculator
npm init -y
npm install @modelcontextprotocol/sdk zod
```

#### `server.js`

```javascript
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";

const server = new McpServer({
  name: "loan-calculator",
  version: "1.0.0",
});

server.tool(
  "calculate_loan_payment",
  "Calculates the monthly payment, total interest, and total cost for a loan " +
    "given the principal amount, annual interest rate, and term in months. " +
    "Use this when a customer wants to know what their loan payments would be.",
  {
    principal: z
      .number()
      .positive()
      .describe("Loan amount in dollars (e.g., 25000)"),
    annualRate: z
      .number()
      .positive()
      .describe("Annual interest rate as a percentage (e.g., 5.25 for 5.25%)"),
    termMonths: z
      .number()
      .int()
      .positive()
      .describe("Loan term in months (e.g., 60 for a 5-year loan)"),
  },
  async ({ principal, annualRate, termMonths }) => {
    const monthlyRate = annualRate / 100 / 12;
    const monthlyPayment =
      (principal * (monthlyRate * Math.pow(1 + monthlyRate, termMonths))) /
      (Math.pow(1 + monthlyRate, termMonths) - 1);

    const totalCost = monthlyPayment * termMonths;
    const totalInterest = totalCost - principal;

    return {
      content: [
        {
          type: "text",
          text: JSON.stringify(
            {
              monthlyPayment: Math.round(monthlyPayment * 100) / 100,
              totalInterest: Math.round(totalInterest * 100) / 100,
              totalCost: Math.round(totalCost * 100) / 100,
              principal,
              annualRate,
              termMonths,
            },
            null,
            2
          ),
        },
      ],
    };
  }
);

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Loan Calculator MCP Server running on stdio");
}

main().catch(console.error);
```

#### `package.json` (add type and bin)

```json
{
  "name": "mcp-loan-calculator",
  "version": "1.0.0",
  "type": "module",
  "bin": {
    "mcp-loan-calculator": "./server.js"
  }
}
```

#### Test Locally

```bash
node server.js
```

The server communicates over stdio. You can test it with the MCP Inspector:

```bash
npx @modelcontextprotocol/inspector node server.js
```

---

### Option B: Python MCP Server

```bash
mkdir mcp-loan-calculator && cd mcp-loan-calculator
pip install mcp
```

#### `server.py`

```python
from mcp.server.fastmcp import FastMCP

mcp = FastMCP("loan-calculator")


@mcp.tool()
def calculate_loan_payment(principal: float, annualRate: float, termMonths: int) -> dict:
    """Calculates the monthly payment, total interest, and total cost for a loan
    given the principal amount, annual interest rate, and term in months.
    Use this when a customer wants to know what their loan payments would be.

    Args:
        principal: Loan amount in dollars (e.g., 25000)
        annualRate: Annual interest rate as a percentage (e.g., 5.25 for 5.25%)
        termMonths: Loan term in months (e.g., 60 for a 5-year loan)
    """
    monthly_rate = annualRate / 100 / 12
    monthly_payment = (
        principal
        * (monthly_rate * (1 + monthly_rate) ** termMonths)
        / ((1 + monthly_rate) ** termMonths - 1)
    )
    total_cost = monthly_payment * termMonths
    total_interest = total_cost - principal

    return {
        "monthlyPayment": round(monthly_payment, 2),
        "totalInterest": round(total_interest, 2),
        "totalCost": round(total_cost, 2),
        "principal": principal,
        "annualRate": annualRate,
        "termMonths": termMonths,
    }


if __name__ == "__main__":
    mcp.run(transport="stdio")
```

---

## Hosting for Copilot Studio

Copilot Studio connects to MCP servers over **HTTP with SSE (Server-Sent Events)**
transport. For the workshop, you have several hosting options:

### Option A: Azure Container Apps (Recommended for Workshop)

1. Containerize the MCP server
2. Deploy to Azure Container Apps with HTTP ingress
3. Share the endpoint URL with participants

```dockerfile
FROM node:20-slim
WORKDIR /app
COPY package*.json ./
RUN npm ci --production
COPY server.js .
EXPOSE 3000
CMD ["node", "server.js"]
```

> Note: For HTTP/SSE transport, update the server to use `SSEServerTransport`
> instead of `StdioServerTransport`. See the MCP SDK docs for details.

### Option B: Local Development (Dev Tunnels)

1. Run the MCP server locally
2. Use [VS Code Dev Tunnels](https://code.visualstudio.com/docs/remote/tunnels) or
   `devtunnel` CLI to expose it
3. Use the tunnel URL in Copilot Studio

```bash
devtunnel host -p 3000 --allow-anonymous
```

### Option C: Azure Functions

Wrap the MCP server in an Azure Function with HTTP trigger.

---

## Registering in Copilot Studio

1. In Copilot Studio, go to **Actions** → **+ Add an action**
2. Select **MCP Server** (or **Model Context Protocol**)
3. Enter the MCP server endpoint URL
4. Copilot Studio discovers available tools automatically
5. The `calculate_loan_payment` tool appears with its description and parameters
6. Enable the tool and save

The orchestrator will now invoke the MCP tool when users ask about loan payments,
using the tool's description to decide when it's relevant.

---

## Adaptive Card for Loan Calculator Results

Use this adaptive card template to display calculator results:

See [`adaptive-cards/loan-calculator-card.json`](../adaptive-cards/loan-calculator-card.json)

---

## Example Conversations

Once registered, the agent handles loan calculator requests naturally:

| User Message | Agent Behavior |
|---|---|
| "What would my payments be on a $25,000 car loan?" | Asks for rate and term if not provided → calls MCP tool → shows card |
| "Calculate payments for a $300,000 mortgage at 6.5% for 30 years" | Has all parameters → calls MCP tool directly → shows card |
| "How much interest would I pay on a $10,000 personal loan at 12% for 3 years?" | Calls MCP tool → highlights total interest in response |
| "What's the monthly on a 15k loan?" | Asks for rate and term → calls MCP tool → shows card |

---

## Workshop Facilitator Notes

If facilitating:
1. Deploy the MCP server to Azure before the workshop
2. Share the endpoint URL with participants
3. Participants skip to "Registering in Copilot Studio"

If self-paced:
1. Participants build the MCP server locally (Option A or B above)
2. Use Dev Tunnels for testing with Copilot Studio
3. Optionally deploy to Azure Container Apps
