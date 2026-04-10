# Lab 01 – From Bots to Agents: Mapping & Assessment

## Overview

This is a **design-only** lab — no coding or environment setup required. Teams will analyze
existing Bot Framework bots and map them to modern agent patterns.

## Learning Objectives

- Understand why Azure Bot Framework SDK is deprecated
- Distinguish agent-driven thinking from dialog-driven bot models
- Map dialogs → tools → orchestration in the modern agent stack
- Assess existing bots for modernization readiness

## Prerequisites

- Access to your organization's existing Bot Framework bot source code (or sample bots)
- Whiteboard or diagramming tool (Miro, Visio, draw.io)

## Lab Steps

### Step 1: Inventory Existing Bots

Create a table for each bot:

| Field | Value |
|---|---|
| Bot name | |
| Primary purpose | |
| Channels (Teams, Web, etc.) | |
| Dialog count | |
| External integrations | |
| Authentication method | |
| Data sources | |

### Step 2: Classify by Complexity

For each bot, determine where it falls on the spectrum:

| Complexity | Characteristics | Recommended Tool |
|---|---|---|
| **Low** | Q&A, knowledge retrieval, simple guided flows | Copilot Studio |
| **Medium** | Multi-step workflows, API integrations, custom logic | Copilot Studio + Foundry (hybrid) |
| **High** | Custom orchestration, multi-model, complex reasoning | Azure AI Foundry |

### Step 3: Map Bot Framework Concepts to Agent Concepts

| Bot Framework | Modern Agent Equivalent |
|---|---|
| Dialog / Waterfall | Tool / Action |
| LUIS Intent | LLM-based intent routing |
| QnA Maker | Generative answers / AI Search grounding |
| Adaptive Cards | Adaptive Cards (unchanged) |
| Bot Channels Registration | Agents SDK channel publishing |
| Bot State | Agent memory / conversation context |

### Step 4: Create Migration Priority Matrix

Plot each bot on a 2×2 matrix:

```
High Business Value
        │
   Migrate    │   Migrate
    First     │   Second
──────────────┼──────────────
   Evaluate   │   Retire /
    Later     │   Consolidate
        │
Low Business Value
   Low Effort ───── High Effort
```

## Deliverables

- [ ] Bot inventory table (completed for each bot)
- [ ] Complexity classification with tool recommendations
- [ ] Migration priority matrix
- [ ] Top 2–3 candidates identified for lab exercises in Weeks 2–4

## Next Steps

→ [Lab 02: Copilot Studio](../lab02-copilot-studio/) — Build your first low-code agent
