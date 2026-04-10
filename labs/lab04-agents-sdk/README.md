# Lab 04 – Microsoft 365 Agents SDK: Bot Framework Successor

## Overview

Scaffold an agent using the Microsoft 365 Agents SDK (the direct successor to Bot
Framework), replace legacy message handling, and publish to Microsoft Teams.

## Learning Objectives

- Understand the Agents SDK architecture and activity protocol
- Scaffold a new agent using the Agents Toolkit
- Migrate Bot Framework message handling patterns to the Agents SDK
- Publish and test in Microsoft Teams

## Prerequisites

- Azure hosting available (App Service, Functions, or Container Apps)
- App registration created (OAuth scopes and redirect URIs configured)
- Microsoft 365 test tenant or non-production Teams environment
- Teams app publishing permissions
- Node.js 18+ or .NET 8+ installed locally
- VS Code with Agents Toolkit extension

> ⚠️ See [environment checklist](../../docs/environment-checklist.md) section A3 if
> your environment is not yet provisioned.

## Lab Steps

### Step 1: Scaffold with Agents Toolkit

1. Open VS Code
2. Open the Agents Toolkit extension
3. Create a new agent project from a template
4. Review the generated project structure

### Step 2: Implement Message Handling

<!-- TODO: Add starter code and detailed instructions -->

1. Open the main agent handler
2. Implement `onMessage` to handle incoming user messages
3. Add adaptive card responses
4. Implement `onMembersAdded` for welcome messages

### Step 3: Compare with Bot Framework Patterns

| Bot Framework Pattern | Agents SDK Equivalent |
|---|---|
| `ActivityHandler.onMessage` | `AgentApplication.onMessage` |
| `BotFrameworkAdapter` | Activity protocol (built-in) |
| `TurnContext.sendActivity` | `TurnContext.sendActivity` (same concept) |
| Bot Channels Registration | Agents SDK app registration |
| `.bot` file | `manifest.json` |

### Step 4: Configure Teams App Manifest

1. Update the Teams app manifest (`manifest.json`)
2. Configure bot endpoint URL
3. Set required permissions and scopes

### Step 5: Deploy and Publish to Teams

1. Deploy the agent to Azure (App Service or Functions)
2. Register the app in Teams Admin Center (or sideload for testing)
3. Test the agent in Teams
4. Verify messages, adaptive cards, and welcome flow

## Deliverables

- [ ] Working agent built with the Agents SDK
- [ ] Deployed to Azure
- [ ] Published and testable in Microsoft Teams
- [ ] Bot Framework vs Agents SDK comparison documented

## Project Structure

```
lab04-agents-sdk/
├── README.md          # This file
├── src/               # Agent source code (to be added)
├── manifest/          # Teams app manifest (to be added)
├── infra/             # Deployment templates (to be added)
└── tests/             # Agent tests (to be added)
```

## Next Steps

→ [Lab 05: Hybrid Agents](../lab05-hybrid-agent/) — Connect Copilot Studio to Foundry
