# MCP Loan Calculator Server

A ready-to-run MCP server that exposes a **loan payment calculator** tool for use
with Copilot Studio (or any MCP-compatible agent).

Uses the **Streamable HTTP** transport — a single `/mcp` endpoint handles all
JSON-RPC messages. Also provides a REST `/calculate` endpoint for use as a
Power Platform custom connector.

## Endpoints

| Endpoint | Method | Purpose |
|---|---|---|
| `/mcp` | POST | MCP JSON-RPC messages (initialize, tools/list, tools/call) |
| `/mcp` | GET | SSE stream for server-to-client notifications |
| `/mcp` | DELETE | Session termination |
| `/calculate` | POST | Direct REST endpoint (non-MCP) for loan calculation |
| `/swagger.json` | GET | Swagger 2.0 spec for Power Platform custom connector import |
| `/health` | GET | Health check |

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

## Using with Copilot Studio (Native MCP)

Use the native MCP connection in Copilot Studio:
1. **Tools** → **+ Add a tool** → **New tool** → **Model Context Protocol**
2. Enter your tunnel URL + `/mcp`
3. Copilot Studio auto-discovers the `calculate_loan_payment` tool

## Using as a Power Platform Custom Connector

If you need to export the Copilot Studio solution with environment variables,
or use the tool from Power Apps/Power Automate, use the custom connector path:

### Option A: Import from URL

1. In Power Platform, go to **Custom Connectors** → **+ New** → **Import from URL**
2. Enter: `https://<your-tunnel>.devtunnels.ms/swagger.json`
3. Update the **Host** field to your tunnel hostname (without `https://`)
4. Test the connector using the `/health` or `/calculate` endpoint
5. Save and create a connection

### Option B: Import from file

1. Download `swagger.json` from this directory
2. Edit the `"host"` field to your tunnel hostname (e.g., `abc123xyz-3000.usw2.devtunnels.ms`)
3. In Power Platform, go to **Custom Connectors** → **+ New** → **Import an OpenAPI file**
4. Upload your edited `swagger.json`
5. Test and save

### Using the REST Endpoint Directly

The `/calculate` endpoint provides a simple REST alternative to MCP JSON-RPC:

```bash
curl -X POST https://<your-tunnel>.devtunnels.ms/calculate \
  -H "Content-Type: application/json" \
  -d '{"principal": 25000, "annualRate": 5.25, "termMonths": 60}'
```

Response:
```json
{
  "monthlyPayment": 474.65,
  "totalInterest": 3478.98,
  "totalCost": 28478.98,
  "principal": 25000,
  "annualRate": 5.25,
  "termMonths": 60
}
```

## Architecture Notes

This server implements the MCP Streamable HTTP transport without using the MCP SDK.
The reason: **Copilot Studio negotiates protocol version `2024-11-05`**, while the
official MCP SDK responds with `2025-03-26`. This version mismatch causes Copilot
Studio to ignore discovered tools. By handling the JSON-RPC messages directly, the
server mirrors the client's protocol version and returns clean JSON responses.

The server also includes CORS headers to allow cross-origin requests from Copilot
Studio and the Power Platform.

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
