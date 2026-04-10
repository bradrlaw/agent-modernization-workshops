import express from "express";
import { randomUUID } from "crypto";

const app = express();
app.use(express.json());
const PORT = process.env.PORT || 3000;

// Loan payment calculation
function calculateLoan(principal, annualRate, termMonths) {
  const monthlyRate = annualRate / 100 / 12;
  const monthlyPayment =
    (principal * (monthlyRate * Math.pow(1 + monthlyRate, termMonths))) /
    (Math.pow(1 + monthlyRate, termMonths) - 1);
  const totalCost = monthlyPayment * termMonths;
  const totalInterest = totalCost - principal;
  return {
    monthlyPayment: Math.round(monthlyPayment * 100) / 100,
    totalInterest: Math.round(totalInterest * 100) / 100,
    totalCost: Math.round(totalCost * 100) / 100,
    principal, annualRate, termMonths,
  };
}

// Track initialized sessions
const sessions = new Set();

// Single MCP endpoint — handles all JSON-RPC messages
app.post("/mcp", (req, res) => {
  const msg = req.body;
  const sessionId = req.headers["mcp-session-id"];

  console.log(`\nPOST /mcp — method: ${msg.method || "(notification)"}, session: ${sessionId || "(new)"}`);

  // Handle initialize
  if (msg.method === "initialize") {
    const newId = randomUUID();
    sessions.add(newId);
    // Mirror the client's protocolVersion
    const clientVersion = msg.params?.protocolVersion || "2024-11-05";
    const response = {
      jsonrpc: "2.0",
      id: msg.id,
      result: {
        protocolVersion: clientVersion,
        capabilities: { tools: { listChanged: false } },
        serverInfo: { name: "loan-calculator", version: "1.0.0" },
      },
    };
    console.log(`  → initialize OK, session: ${newId}, protocolVersion: ${clientVersion}`);
    res.setHeader("mcp-session-id", newId);
    res.setHeader("Content-Type", "application/json");
    return res.json(response);
  }

  // Handle notifications (initialized, etc.) — no response body needed
  if (!msg.id) {
    console.log(`  → notification accepted`);
    return res.status(202).end();
  }

  // All other requests need a valid session
  if (!sessionId || !sessions.has(sessionId)) {
    console.log(`  → session not found: ${sessionId}`);
    return res.status(400).json({
      jsonrpc: "2.0",
      id: msg.id,
      error: { code: -32000, message: "Bad Request: Server not initialized" },
    });
  }

  // Handle tools/list
  if (msg.method === "tools/list") {
    const response = {
      jsonrpc: "2.0",
      id: msg.id,
      result: {
        tools: [
          {
            name: "calculate_loan_payment",
            description:
              "Calculates the monthly payment, total interest, and total cost for a loan " +
              "given the principal amount, annual interest rate, and term in months.",
            inputSchema: {
              type: "object",
              properties: {
                principal: {
                  type: "number",
                  description: "Loan amount in dollars (e.g., 25000)",
                },
                annualRate: {
                  type: "number",
                  description: "Annual interest rate as a percentage (e.g., 5.25 for 5.25%)",
                },
                termMonths: {
                  type: "integer",
                  description: "Loan term in months (e.g., 60 for a 5-year loan)",
                },
              },
              required: ["principal", "annualRate", "termMonths"],
            },
          },
        ],
      },
    };
    console.log(`  → tools/list OK (1 tool)`);
    res.setHeader("Content-Type", "application/json");
    return res.json(response);
  }

  // Handle tools/call
  if (msg.method === "tools/call") {
    const { name, arguments: args } = msg.params;
    if (name === "calculate_loan_payment") {
      const result = calculateLoan(args.principal, args.annualRate, args.termMonths);
      const response = {
        jsonrpc: "2.0",
        id: msg.id,
        result: {
          content: [{ type: "text", text: JSON.stringify(result, null, 2) }],
        },
      };
      console.log(`  → tools/call OK: $${result.monthlyPayment}/mo`);
      res.setHeader("Content-Type", "application/json");
      return res.json(response);
    }
    return res.json({
      jsonrpc: "2.0",
      id: msg.id,
      error: { code: -32602, message: `Unknown tool: ${name}` },
    });
  }

  // Unknown method
  res.json({
    jsonrpc: "2.0",
    id: msg.id,
    error: { code: -32601, message: `Method not found: ${msg.method}` },
  });
});

app.get("/health", (req, res) => {
  res.json({ status: "ok", server: "loan-calculator", version: "1.0.0" });
});

app.listen(PORT, () => {
  console.log(`MCP Loan Calculator Server running on http://localhost:${PORT}`);
  console.log(`Endpoint: http://localhost:${PORT}/mcp`);
  console.log(`Health check: http://localhost:${PORT}/health`);
});
