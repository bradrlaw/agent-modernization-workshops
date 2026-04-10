# Lab 05 – Hybrid Agents: Copilot Studio + Azure AI Foundry

## Overview

Build a hybrid agent where Copilot Studio handles user interaction (low-code) and
Azure AI Foundry handles complex backend logic (pro-code), connected via the
connected agents pattern.

## Learning Objectives

- Understand the fusion development model for agent teams
- Configure Copilot Studio to call a Foundry agent as a connected agent
- Align identity and observability across both platforms
- Test the end-to-end hybrid flow

## Prerequisites

- **Week 2 environment:** Copilot Studio agent from Lab 02 (or a new one)
- **Week 3 environment:** Foundry agent from Lab 03 deployed to Azure
- Managed identities aligned between Power Platform and Azure
- Application Insights enabled for both agents

> This lab builds on Labs 02 and 03. Complete those first.

## Lab Steps

### Step 1: Prepare the Foundry Agent as a Backend

1. Ensure your Lab 03 Foundry agent is deployed and accessible via API
2. Add or verify an authentication endpoint (managed identity or API key)
3. Document the agent's capabilities and expected input/output schema

### Step 2: Register as a Connected Agent in Copilot Studio

1. Open your Copilot Studio agent
2. Navigate to **Actions** → **Connected agents**
3. Register the Foundry agent endpoint
4. Configure authentication (managed identity recommended)
5. Map the connected agent's capabilities to Copilot Studio actions

### Step 3: Create a Hybrid Topic

1. Create a new topic in Copilot Studio
2. Add trigger phrases that require complex reasoning
3. Route to the connected Foundry agent for processing
4. Return the Foundry agent's response to the user

### Step 4: Test End-to-End

1. Test in the Copilot Studio chat panel
2. Verify the request flows: User → Copilot Studio → Foundry Agent → Response
3. Check Application Insights for telemetry from both agents
4. Validate error handling when the Foundry agent is unavailable

### Step 5: Shared Observability

1. Verify correlation IDs flow between Copilot Studio and Foundry
2. Review Copilot Studio analytics for the connected agent calls
3. Review Application Insights for Foundry agent traces
4. Create a simple dashboard showing the hybrid flow

## Deliverables

- [ ] Copilot Studio agent calling a Foundry agent via connected agents
- [ ] Hybrid topic that routes complex requests to the backend
- [ ] End-to-end flow tested and working
- [ ] Observability across both agents verified

## Architecture

```
┌─────────────────────┐     Connected Agent     ┌─────────────────────┐
│   Copilot Studio    │ ──────────────────────→  │  Azure AI Foundry   │
│   (Low-Code Front)  │ ←──────────────────────  │  (Pro-Code Backend) │
│                     │                          │                     │
│  • User interaction │                          │  • Complex reasoning│
│  • Topic routing    │                          │  • Tool calling     │
│  • Generative Q&A   │                          │  • AI Search / RAG  │
└─────────────────────┘                          └─────────────────────┘
         │                                                │
         └──────────── Application Insights ──────────────┘
```

## Next Steps

→ [Lab 06: Multi-Agent Orchestration](../lab06-multi-agent/) — Build a router + specialist system
