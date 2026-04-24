# Teams Channel Setup Guide

This guide walks you through publishing the Virtual Banking Assistant as a
Microsoft Teams app. This is a **stretch goal** — complete the core lab first.

---

## Overview

To make the pro-code agent accessible in Teams, you:
1. Register an Azure Bot resource
2. Configure the messaging endpoint
3. Create a Teams app manifest
4. Sideload the app to Teams for testing

> 💡 **Lab 04** covers the Microsoft 365 Agents SDK in depth. This guide provides
> a simplified path to get the agent into Teams using the Azure Bot Service channel.

---

## Step 1: Register an Azure Bot

1. Go to [Azure Portal](https://portal.azure.com)
2. Search for **Azure Bot** and click **Create**
3. Configure:
   - **Bot handle**: `banking-assistant-bot`
   - **Subscription**: Your subscription
   - **Resource group**: Same as your Container App
   - **Pricing tier**: F0 (free)
   - **Type of App**: Multi-tenant
   - **Creation type**: Create new Microsoft App ID
4. Click **Review + create** → **Create**

## Step 2: Configure Messaging Endpoint

1. Open your Azure Bot resource
2. Go to **Configuration**
3. Set the **Messaging endpoint** to your deployed app URL:
   ```
   https://<your-container-app-fqdn>/api/messages
   ```
4. Note the **Microsoft App ID** and create a **client secret** in the
   associated App Registration

> ⚠️ The web chat app (`chat_web.py`) does not include a `/api/messages`
> endpoint for Bot Framework activity protocol. To fully integrate with Teams,
> you would need to add the Microsoft 365 Agents SDK adapter. This is covered
> in **Lab 04**.
>
> For a quick demo, you can use the Azure Bot's **Test in Web Chat** feature
> to verify the bot registration works, then point it at a proper Agents SDK
> app in Lab 04.

## Step 3: Enable Teams Channel

1. In your Azure Bot resource, go to **Channels**
2. Click **Microsoft Teams**
3. Accept the terms of service
4. Click **Apply**

## Step 4: Create Teams App Manifest

Create a `manifest.json` file:

```json
{
    "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.17/MicrosoftTeams.schema.json",
    "manifestVersion": "1.17",
    "version": "1.0.0",
    "id": "{{YOUR-BOT-APP-ID}}",
    "developer": {
        "name": "Workshop Team",
        "websiteUrl": "https://github.com/bradrlaw/agent-modernization-workshops",
        "privacyUrl": "https://github.com/bradrlaw/agent-modernization-workshops",
        "termsOfUseUrl": "https://github.com/bradrlaw/agent-modernization-workshops"
    },
    "name": {
        "short": "Banking Assistant",
        "full": "Virtual Banking Assistant (Pro-Code)"
    },
    "description": {
        "short": "AI-powered banking assistant built with Azure AI Foundry",
        "full": "A pro-code virtual banking assistant that helps customers check balances, view transactions, look up profiles, and calculate loan payments. Built with Azure AI Agent Service."
    },
    "icons": {
        "outline": "outline.png",
        "color": "color.png"
    },
    "accentColor": "#0078D4",
    "bots": [
        {
            "botId": "{{YOUR-BOT-APP-ID}}",
            "scopes": ["personal", "team", "groupChat"],
            "supportsFiles": false,
            "isNotificationOnly": false
        }
    ],
    "permissions": ["identity", "messageTeamMembers"],
    "validDomains": []
}
```

Replace `{{YOUR-BOT-APP-ID}}` with your Microsoft App ID from Step 2.

## Step 5: Package and Sideload

1. Create a ZIP file containing:
   - `manifest.json`
   - `color.png` (192×192 icon)
   - `outline.png` (32×32 icon)
2. In Teams, go to **Apps** → **Manage your apps** → **Upload an app**
3. Select **Upload a custom app** → choose your ZIP file
4. Click **Add** to install the bot in your personal scope

## Step 6: Test in Teams

1. Open the bot in Teams chat
2. Send a message like "Show me my accounts"
3. Verify the agent responds with banking data

---

## Notes

- This guide registers the bot channel only. Full Teams integration with the
  M365 Agents SDK activity protocol is covered in **Lab 04**.
- For production, configure managed identity instead of client secrets.
- The Teams manifest requires icon files — use any 192×192 and 32×32 PNG images.
