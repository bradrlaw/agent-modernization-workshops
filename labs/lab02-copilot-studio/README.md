# Lab 02 – Copilot Studio: Build a Low-Code Agent

## Overview

Build a declarative agent using Microsoft Copilot Studio with generative answers,
knowledge grounding, and managed orchestration. Publish to a test channel.

## Learning Objectives

- Create and configure a Copilot Studio agent
- Add knowledge sources for generative grounding
- Configure topics and actions
- Publish to a test channel (Teams or web)

## Prerequisites

- Power Platform non-production environment
- Copilot Studio enabled in tenant
- Dataverse enabled
- Maker role assigned to your account

> ⚠️ See [environment checklist](../../docs/environment-checklist.md) section A1 if
> your environment is not yet provisioned.

## Lab Steps

### Step 1: Create a New Agent

1. Navigate to [Copilot Studio](https://copilotstudio.microsoft.com)
2. Select your non-production environment
3. Create a new agent
4. Provide a name, description, and instructions

### Step 2: Add Knowledge Sources

1. Navigate to the **Knowledge** tab
2. Add one or more knowledge sources:
   - SharePoint site
   - Public website URL
   - Uploaded documents
3. Test generative answers in the chat panel

### Step 3: Create a Custom Topic

1. Navigate to **Topics**
2. Create a new topic with trigger phrases
3. Add a message node with an adaptive card response
4. Add a question node to collect user input
5. Test the topic flow

### Step 4: Add an Action (Optional)

1. Create a Power Automate flow or connector action
2. Wire it into your topic as a plugin action
3. Pass parameters from the conversation to the action

### Step 5: Publish to Test Channel

1. Go to **Channels**
2. Enable the **Teams** channel (or Demo website)
3. Publish the agent
4. Open in Teams and test end-to-end

## Deliverables

- [ ] Working Copilot Studio agent with generative answers
- [ ] At least one custom topic
- [ ] Agent published to a test channel
- [ ] Exported solution file (`.zip`) saved to this lab folder

## Fallback

If Power Platform is unavailable, review the Copilot Studio documentation and proceed
to [Lab 03](../lab03-foundry-agent/) for the pro-code path.

## Next Steps

→ [Lab 03: Azure AI Foundry](../lab03-foundry-agent/) — Build a pro-code agent
