# Lab 06 – Multi-Agent Orchestration

## Overview

Build a multi-agent system with a router agent that delegates requests to specialized
agents, demonstrating delegation, synthesis, and shared memory patterns.

## Learning Objectives

- Design router vs specialist agent architectures
- Implement request routing and delegation logic
- Share context and memory across agents
- Handle synthesis (combining responses from multiple specialists)

## Prerequisites

- Azure AI Foundry environment from Lab 03
- Multiple agent deployments (or ability to deploy new ones)
- Shared Azure storage (Blob, Cosmos DB, or AI Search)
- Optional: Cosmos DB for conversational memory

> ⚠️ This lab requires the most Azure resources. See
> [environment checklist](../../docs/environment-checklist.md) sections A2 and A4.

## Lab Steps

### Step 1: Design the Multi-Agent System

Define your agent topology:

```
                    ┌──────────────┐
                    │   Router     │
          User ───→ │   Agent      │
                    └──────┬───────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
        ┌─────▼─────┐ ┌───▼────┐ ┌────▼─────┐
        │ Specialist │ │Specialist│ │Specialist│
        │  Agent A   │ │ Agent B  │ │ Agent C  │
        └────────────┘ └──────────┘ └──────────┘
```

1. Define 2–3 specialist agent domains (e.g., HR, IT Support, Finance)
2. Define routing criteria for the router agent
3. Document the delegation and response synthesis strategy

### Step 2: Build Specialist Agents

1. Create 2–3 Foundry agents, each with a focused system prompt
2. Give each specialist its own tools and knowledge sources
3. Deploy each agent to Azure
4. Test each specialist independently

### Step 3: Build the Router Agent

<!-- TODO: Add starter code and detailed instructions -->

1. Create a router agent with a system prompt that understands delegation
2. Implement routing logic (LLM-based intent classification or rule-based)
3. Configure the router to call specialist agents as tools
4. Handle delegation responses and format them for the user

### Step 4: Add Shared Memory

1. Choose a memory strategy:
   - **Conversation memory**: Cosmos DB or in-memory state
   - **Knowledge memory**: Shared AI Search index
   - **Session state**: Shared blob storage
2. Configure agents to read/write shared context
3. Test that context persists across agent handoffs

### Step 5: Test the Multi-Agent System

1. Send requests that should route to different specialists
2. Verify correct routing decisions
3. Test edge cases: ambiguous requests, multi-domain requests
4. Verify shared memory is updated correctly
5. Review observability across all agents

## Deliverables

- [ ] Router agent that delegates to 2+ specialist agents
- [ ] Specialist agents with domain-specific tools and knowledge
- [ ] Shared memory pattern implemented
- [ ] Multi-agent system tested end-to-end
- [ ] Architecture diagram documented

## Next Steps

→ [Lab 07: Testing & Observability](../lab07-eval-observability/) — Evaluate and monitor agents
