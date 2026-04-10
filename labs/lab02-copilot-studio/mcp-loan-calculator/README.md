# MCP Loan Calculator Server

A ready-to-run MCP server that exposes a **loan payment calculator** tool for use
with Copilot Studio (or any MCP-compatible agent).

Uses the **Streamable HTTP** transport — a single `/mcp` endpoint handles all
JSON-RPC messages. No SDK required; pure Express handling raw MCP protocol.

## Quick Start

### Prerequisites

- [Node.js](https://nodejs.org/) v18 or later
- [Dev Tunnels CLI](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started) (`devtunnel`)

### 1. Install Dependencies

```bash
cd mcp-loan-calculator
npm install
```

### 2. Start the Server

```bash
npm start
```

You should see:
```
MCP Loan Calculator Server running on http://localhost:3000
Endpoint: http://localhost:3000/mcp
Health check: http://localhost:3000/health
```

### 3. Verify It Works

Open a new terminal and run:
```bash
curl http://localhost:3000/health
```

You should get:
```json
{"status":"ok","server":"loan-calculator","version":"1.0.0"}
```

## Exposing with Dev Tunnels

Copilot Studio needs a publicly accessible URL to connect to your MCP server.
Dev Tunnels creates a secure tunnel from the internet to your local machine.

### Install Dev Tunnels CLI

If you don't have it already:

```bash
winget install Microsoft.devtunnel
```

Or download from: https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started

### Login

```bash
devtunnel user login
```

This opens a browser for Microsoft authentication.

### Create and Host a Tunnel

```bash
devtunnel host -p 3000 --allow-anonymous
```

You'll see output like:
```
Connect via browser: https://abc123xyz.usw2.devtunnels.ms
Inspect network activity: https://abc123xyz-3000.usw2.devtunnels.ms

Hosting port: 3000
```

**Copy the tunnel URL** — you'll need it for Copilot Studio. Your MCP endpoint is:
```
https://abc123xyz-3000.usw2.devtunnels.ms/mcp
```

> ⚠️ **Keep this terminal open** — closing it stops the tunnel. The MCP server and
> dev tunnel must both be running for Copilot Studio to connect.

## Architecture Notes

This server implements the MCP Streamable HTTP transport without using the MCP SDK.
The reason: **Copilot Studio negotiates protocol version `2024-11-05`**, while the
official MCP SDK responds with `2025-03-26`. This version mismatch causes Copilot
Studio to ignore discovered tools. By handling the JSON-RPC messages directly, the
server mirrors the client's protocol version and returns clean JSON responses.

## Tool: calculate_loan_payment

Calculates monthly payment, total interest, and total cost for a loan.

**Inputs:**

| Parameter | Type | Description |
|---|---|---|
| `principal` | number | Loan amount in dollars (e.g., 25000) |
| `annualRate` | number | Annual interest rate as % (e.g., 5.25) |
| `termMonths` | number | Loan term in months (e.g., 60) |

**Output:**

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
