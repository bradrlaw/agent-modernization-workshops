import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { SSEServerTransport } from "@modelcontextprotocol/sdk/server/sse.js";
import express from "express";
import { z } from "zod";

const server = new McpServer({
  name: "loan-calculator",
  version: "1.0.0",
});

// Register the loan payment calculator tool
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

// Set up Express with SSE transport for HTTP-based MCP clients (Copilot Studio)
const app = express();
const PORT = process.env.PORT || 3000;

// Store active transports for session management
const transports = {};

app.get("/sse", async (req, res) => {
  const transport = new SSEServerTransport("/messages", res);
  transports[transport.sessionId] = transport;
  
  res.on("close", () => {
    delete transports[transport.sessionId];
  });

  await server.connect(transport);
});

app.post("/messages", async (req, res) => {
  const sessionId = req.query.sessionId;
  const transport = transports[sessionId];
  if (transport) {
    await transport.handlePostMessage(req, res);
  } else {
    res.status(400).send("No active session for this sessionId");
  }
});

app.get("/health", (req, res) => {
  res.json({ status: "ok", server: "loan-calculator", version: "1.0.0" });
});

app.listen(PORT, () => {
  console.log(`MCP Loan Calculator Server running on http://localhost:${PORT}`);
  console.log(`SSE endpoint: http://localhost:${PORT}/sse`);
  console.log(`Health check: http://localhost:${PORT}/health`);
});
