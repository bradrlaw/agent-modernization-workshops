# Lab 03 – Azure AI Foundry: Build a Pro-Code Agent

## Overview

Build an SDK-based agent using Azure AI Foundry with tool-calling, Azure AI Search
for knowledge retrieval, and deploy to Azure.

## Learning Objectives

- Understand the Foundry Agent Service architecture
- Build an agent with models, tools, and memory
- Integrate Azure AI Search for RAG-based knowledge grounding
- Deploy and test the agent on Azure

## Prerequisites

- Azure subscription (non-production)
- Azure AI Foundry enabled
- Model access approved (e.g., GPT-4o, GPT-4.1)
- Azure AI Search provisioned
- Application Insights enabled
- Python 3.10+ or .NET 8+ installed locally

> ⚠️ See [environment checklist](../../docs/environment-checklist.md) section A2 if
> your environment is not yet provisioned.

## Lab Steps

### Step 1: Set Up Azure AI Foundry Project

1. Open the [Azure AI Foundry portal](https://ai.azure.com)
2. Create a new project (or use an existing one)
3. Verify model deployment is available
4. Note the project connection string

### Step 2: Create the Agent

<!-- TODO: Add starter code and detailed instructions -->

1. Clone this repository and navigate to `labs/lab03-foundry-agent/src/`
2. Install dependencies
3. Configure the agent with:
   - System prompt defining the agent's persona and capabilities
   - Model selection
   - Tool definitions

### Step 3: Add Azure AI Search Tool

1. Create an AI Search index with sample data (see `shared/sample-data/`)
2. Register the search index as a tool for the agent
3. Test knowledge retrieval queries

### Step 4: Add Custom Function Tools

1. Define a custom function tool (e.g., lookup, calculator, API call)
2. Register it with the agent
3. Test tool-calling behavior

### Step 5: Deploy to Azure

1. Deploy the agent to Azure (App Service, Functions, or Container Apps)
2. Test the deployed agent via the API endpoint
3. Verify telemetry is flowing to Application Insights

## Deliverables

- [ ] Working Foundry agent with at least one knowledge tool and one function tool
- [ ] Agent deployed to Azure
- [ ] Application Insights telemetry verified
- [ ] Agent endpoint URL documented

## Project Structure

```
lab03-foundry-agent/
├── README.md          # This file
├── src/               # Agent source code (to be added)
├── infra/             # Bicep/Terraform for Azure resources (to be added)
└── tests/             # Agent evaluation tests (to be added)
```

## Next Steps

→ [Lab 04: Microsoft 365 Agents SDK](../lab04-agents-sdk/) — Publish an agent to Teams
