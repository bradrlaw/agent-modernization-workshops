# MCP Loan Calculator Server (Authenticated)

A version of the MCP loan calculator server that requires **API Key authentication**
via the `x-api-key` header. Use this to demonstrate securing MCP/REST tool endpoints
and configuring authentication in Copilot Studio and Power Platform custom connectors.

## How It Differs from the Unauthenticated Version

| Aspect | `mcp-loan-calculator/` | `mcp-loan-calculator-auth/` (this) |
|---|---|---|
| Authentication | None | API Key (`x-api-key` header) |
| Default port | 3000 | 3001 |
| Env vars | None required | `MCP_API_KEY` required |
| Swagger | No security scheme | `apiKey` security definition |
| Health check | Open | Open (no auth needed) |

## Quick Start

### Prerequisites

- [Node.js](https://nodejs.org/) v18 or later
- [Dev Tunnels CLI](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started) (`devtunnel`)

### 1. Install Dependencies

```bash
cd mcp-loan-calculator-auth
npm install
```

### 2. Set the API Key

Choose any secret string as your API key:

**Windows (PowerShell):**
```powershell
$env:MCP_API_KEY = "my-secret-key-12345"
```

**Linux/macOS:**
```bash
export MCP_API_KEY="my-secret-key-12345"
```

> ⚠️ In production, use a strong random key. For the workshop, any string works.

### 3. Start the Server

```bash
npm start
```

You should see:
```
MCP Loan Calculator Server (AUTHENTICATED) running on http://localhost:3001
Endpoint: http://localhost:3001/mcp
Health check: http://localhost:3001/health
Auth: x-api-key header required (MCP_API_KEY env var)
```

### 4. Test Authentication

```bash
# Without key — should get 401
curl -s http://localhost:3001/calculate -X POST -H "Content-Type: application/json" -d "{\"principal\":25000,\"annualRate\":5.25,\"termMonths\":60}"

# With key — should get result
curl -s http://localhost:3001/calculate -X POST -H "Content-Type: application/json" -H "x-api-key: my-secret-key-12345" -d "{\"principal\":25000,\"annualRate\":5.25,\"termMonths\":60}"

# Health check — no auth needed
curl -s http://localhost:3001/health
```

## Exposing with Dev Tunnels

```bash
devtunnel host -p 3001 --allow-anonymous
```

Your authenticated MCP endpoint:
```
https://<your-tunnel-id>.devtunnels.ms/mcp
```

## Connecting to Copilot Studio (Native MCP)

1. **Tools** → **+ Add a tool** → **New tool** → **Model Context Protocol**
2. Enter your tunnel URL + `/mcp`
3. Set **Authentication** → **API Key**
4. Enter your API key value (the same string you set in `MCP_API_KEY`)
5. Set **Header name** to `x-api-key`
6. Copilot Studio auto-discovers the `calculate_loan_payment` tool

## Using as a Power Platform Custom Connector

1. **Custom Connectors** → **+ New** → **Import from URL**
2. Enter: `https://<your-tunnel>.devtunnels.ms/swagger.json`
3. On the **Security** tab:
   - **Authentication type**: API Key
   - **Parameter label**: API Key
   - **Parameter name**: `x-api-key`
   - **Parameter location**: Header
4. **Test** the connector — enter your API key when prompted
5. Save and create a connection

## Running Both Servers Simultaneously

Since this version defaults to port 3001, you can run both the unauthenticated
(port 3000) and authenticated (port 3001) servers at the same time:

```bash
# Terminal 1 — unauthenticated
cd mcp-loan-calculator && npm start

# Terminal 2 — authenticated
cd mcp-loan-calculator-auth && npm start
```

## Endpoints

| Endpoint | Method | Auth Required | Purpose |
|---|---|---|---|
| `/mcp` | POST | ✅ | MCP JSON-RPC messages |
| `/mcp` | GET | ✅ | SSE stream |
| `/mcp` | DELETE | ✅ | Session termination |
| `/calculate` | POST | ✅ | Direct REST loan calculation |
| `/swagger.json` | GET | ✅ | Swagger 2.0 spec |
| `/health` | GET | ❌ | Health check (always open) |
