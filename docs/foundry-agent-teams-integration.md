# Integrating an Azure AI Foundry Agent into Microsoft Teams

This guide covers two paths for bringing an AI Foundry agent into Teams:

1. **Copilot Studio (Low-Code)** — Connect a Foundry agent as a knowledge source or action, then publish to Teams
2. **M365 Agents SDK (Pro-Code)** — Build a custom agent in .NET/TypeScript that calls Foundry, deploy to Teams

Choose based on your team's skill set and requirements:

| Criteria | Copilot Studio | M365 Agents SDK |
|----------|---------------|-----------------|
| Builder | Citizen dev / IT Pro | Pro developer (.NET / TS) |
| Time to deploy | Hours | Days |
| Customization | Low–medium | Full control |
| Multi-channel | Teams + M365 Copilot | Teams + Outlook + M365 Copilot |
| ALM | Power Platform solutions | Git + CI/CD |

**Contact:** Brad Lawrence (Brad.Lawrence@microsoft.com)

---

## Prerequisites (Both Paths)

| Requirement | Details |
|-------------|---------|
| Azure subscription | With an Azure AI Foundry project deployed |
| Foundry agent | A working agent (with model deployment, tools, etc.) |
| Microsoft 365 tenant | With Teams enabled |
| Entra ID permissions | Ability to register apps / grant consent |
| Admin access | Teams Admin Center access (for org-wide distribution) |

---

## Path 1: Copilot Studio (Low-Code)

### Overview

```
┌──────────────┐       ┌────────────────────┐       ┌───────────┐
│  Microsoft   │       │   Copilot Studio    │       │  Azure AI │
│    Teams     │◄─────►│   (Orchestration)   │◄─────►│  Foundry  │
└──────────────┘       └────────────────────┘       └───────────┘
                              │
                    Topics, Actions, Knowledge
```

### Step 1: Create an Agent in Copilot Studio

1. Sign in to [Copilot Studio](https://copilotstudio.microsoft.com)
2. Select **Create** → **New agent**
3. Describe your agent's purpose (e.g., "Help employees with banking questions using our AI model")
4. Set the **Name**, **Description**, and **Instructions** for your agent
5. Select **Create**

> 💡 **Tip:** The instructions field shapes the agent's personality and boundaries. Be specific about what the agent should and shouldn't do.

### Step 2: Connect Azure AI Foundry as a Knowledge Source (Foundry IQ)

This option allows Copilot Studio to use your Foundry project's knowledge base for RAG-style answers.

1. In your agent, go to the **Build** tab
2. Under **Knowledge**, select **Add knowledge**
3. Choose **Microsoft IQ** → **Foundry IQ**
4. Select **Create new connection**:
   - **Endpoint:** Your Foundry project endpoint (e.g., `https://your-resource.services.ai.azure.com/api/projects/your-project`)
   - **Authentication:** Choose one of:
     - API Key
     - Microsoft Entra ID (recommended for production)
     - Service Principal
5. Select the knowledge base to connect
6. Select **Add to agent** → **Save**

> **Source:** [Connect to Foundry IQ from an agent (preview)](https://learn.microsoft.com/en-us/microsoft-copilot-studio/agents-experience/foundry-iq-connect)

---

### Step 3: Connect Azure AI Foundry as an Action (Bring Your Own Model)

This option lets you call a Foundry-deployed model directly from Copilot Studio prompts.

1. In your agent, go to **Actions** → **Add an action**
2. Select **New action** → **Prompt**
3. In the prompt editor, select **Model** → **Change model**
4. Choose **Azure AI Foundry** and enter:
   - **Endpoint:** Your model deployment endpoint
   - **Deployment name:** The model deployment (e.g., `gpt-4o-1`)
   - **Authentication:** API Key or Entra ID
5. Write your prompt template with input/output variables
6. Save and test the action

**Alternative — Connect via MCP or HTTP:**

If your Foundry agent exposes tools via MCP or REST endpoints:

1. Go to **Actions** → **Add an action** → **Connector**
2. Choose **Custom connector** and provide your Swagger/OpenAPI spec
3. Or choose **MCP tool server** and enter the Streamable HTTP URL
4. Map inputs/outputs to your agent's topic flow

> **Source:** [Azure AI Foundry Model in Copilot Studio Custom Prompts](https://www.matthewdevaney.com/azure-ai-foundry-model-in-copilot-studio-custom-prompts/)  
> **Source:** [Integrating Azure AI Foundry with Copilot Studio](https://dev.to/holgerimbery/triggering-the-backend-integrating-azure-ai-foundry-with-microsoft-copilot-studio-53ol)

---

### Step 4: Test Your Agent

1. Use the **Test your agent** panel (right side of Copilot Studio)
2. Send messages that exercise your Foundry integration:
   - Knowledge queries (if using Foundry IQ)
   - Action triggers (if using BYOM/MCP)
3. Verify responses are returning from Foundry (check citations, latency)
4. Iterate on instructions and prompt templates until quality is satisfactory

---

### Step 5: Publish the Agent

1. Select **Publish** in the top bar
2. Confirm by selecting **Publish** again
3. Wait for the green success banner

> ⚠️ **Important:** You must publish at least once before the agent can be added to Teams.

---

### Step 6: Connect to the Teams Channel

1. In Copilot Studio, go to **Channels** (left nav)
2. Select **Microsoft Teams and Microsoft 365 Copilot**
3. (Optional) Enable **Make agent available in Microsoft 365 Copilot** for dual presence
4. Select **Add channel**
5. Customize appearance:
   - **Icon:** PNG, < 72 KB, 192×192 px max
   - **Color:** Brand color for the app card
   - **Short description:** What the agent does (visible in Teams store)
   - **Long description:** Detailed explanation for the About tab
6. Select **Save**

---

### Step 7: Install and Test in Teams

1. Select **See agent in Teams** (opens Teams with the agent)
2. Select **Add** to install it to your profile
3. Test the agent in Teams chat — send the same queries you tested in Step 4
4. Verify formatting, adaptive cards, and response quality in the Teams context

---

### Step 8: Distribute to Your Organization

**Option A — Share link (quick, limited audience):**

1. Go to **Channels** → **Availability options**
2. Select **Copy link**
3. Share the link with specific users (they must have been [shared](https://learn.microsoft.com/en-us/microsoft-copilot-studio/admin-share-bots) on the agent)

**Option B — Teams app store (broader audience):**

1. Go to **Availability options**
2. Select **Show to my teammates and shared users**
3. Confirm **Show in Built By Your Colleagues** is checked
4. Select **Update**

**Option C — Admin-approved (organization-wide):**

1. Go to **Availability options**
2. Select **Show to everyone in my org**
3. Select **Submit for admin approval**
4. An admin reviews in [Teams Admin Center](https://admin.teams.microsoft.com/) → **Manage apps**
5. Once approved, the agent appears in the **Built for your org** section

> **Source:** [Connect and configure an agent for Teams](https://learn.microsoft.com/en-us/microsoft-copilot-studio/publication-add-bot-to-microsoft-teams)

---

### Step 9: Monitor and Iterate

1. In Copilot Studio, go to **Analytics** to view:
   - Session count, resolution rate, escalation rate
   - User satisfaction scores
   - Topic-level performance
2. Review **Conversation transcripts** for quality issues
3. Update knowledge, prompts, or topics as needed
4. Republish after changes (existing conversations won't see updates until user starts a new session)

---

## Path 2: M365 Agents SDK (Pro-Code)

### Overview

```
┌──────────────┐       ┌──────────────────────┐       ┌───────────┐
│  Microsoft   │       │  Your Agent Service   │       │  Azure AI │
│    Teams     │◄─────►│  (M365 Agents SDK)    │◄─────►│  Foundry  │
│  + Outlook   │       │  Azure App Service    │       │  (Models) │
└──────────────┘       └──────────────────────┘       └───────────┘
```

This path gives you full control: custom orchestration, multi-model routing, complex business logic, and deep Teams integration.

---

### Step 1: Set Up Your Development Environment

**Required tools:**

```bash
# .NET 8+ SDK
dotnet --version    # Should be 8.0+

# Azure CLI
az --version

# Teams Toolkit (VS Code extension or CLI)
# Install from VS Code marketplace: "Teams Toolkit"
```

**Install the M365 Agents SDK packages:**

```bash
dotnet new web -n MyFoundryAgent
cd MyFoundryAgent

# Core M365 Agents SDK packages
dotnet add package Microsoft.Agents.Builder
dotnet add package Microsoft.Agents.Hosting.AspNetCore
dotnet add package Microsoft.Agents.Authentication.Msal

# Azure AI packages for Foundry integration
dotnet add package Azure.AI.Projects
dotnet add package Azure.Identity
```

---

### Step 2: Register Your App in Entra ID

1. Go to [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations**
2. Select **New registration**:
   - **Name:** `MyFoundryAgent`
   - **Supported account types:** Accounts in this organizational directory only
   - **Redirect URI:** (leave blank for now)
3. After creation, note:
   - **Application (client) ID**
   - **Directory (tenant) ID**
4. Go to **Certificates & secrets** → **New client secret** → note the value
5. Go to **API permissions** → Add:
   - `Microsoft Graph` → Delegated → `User.Read`
   - `BotFramework` → Application → `BotFramework.ReadWrite.All` (if needed)

---

### Step 3: Create the Azure Bot Resource

1. In Azure Portal, search for **Azure Bot** → **Create**
2. Configure:
   - **Bot handle:** `my-foundry-agent`
   - **Pricing tier:** Standard
   - **Microsoft App ID:** Use the App ID from Step 2 (select "Use existing app registration")
3. After creation, go to **Channels** → **Microsoft Teams** → **Apply**
4. Go to **Configuration** → Set the **Messaging endpoint** to your service URL:
   - For local dev: `https://your-devtunnel-url/api/messages`
   - For production: `https://your-app-service.azurewebsites.net/api/messages`

---

### Step 4: Implement the Agent

**Program.cs** — Configure the agent host:

```csharp
using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Azure.AI.Projects;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add M365 Agents SDK services
builder.AddAgents(agents =>
{
    agents.AddAgent<FoundryBankingAgent>("MyFoundryAgent");
});

// Register Azure AI Foundry client
builder.Services.AddSingleton(sp =>
{
    var endpoint = builder.Configuration["AzureAI:Endpoint"];
    return new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
});

var app = builder.Build();
app.MapAgents();
app.Run();
```

**FoundryBankingAgent.cs** — Agent logic with Foundry integration:

```csharp
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Core.Models;
using Azure.AI.Projects;
using Azure.AI.Inference;

public class FoundryBankingAgent : AgentApplication
{
    private readonly AIProjectClient _aiClient;
    private readonly List<ChatMessage> _conversationHistory = new();

    public FoundryBankingAgent(AgentApplicationOptions options, AIProjectClient aiClient)
        : base(options)
    {
        _aiClient = aiClient;

        // Handle incoming messages
        OnActivity(ActivityTypes.Message, async (turnContext, cancellationToken) =>
        {
            var userMessage = turnContext.Activity.Text;

            // Show typing indicator
            await turnContext.SendActivityAsync(
                new Activity { Type = ActivityTypes.Typing }, cancellationToken);

            // Call Azure AI Foundry
            var response = await CallFoundryAgent(userMessage, cancellationToken);

            // Send response back to Teams
            await turnContext.SendActivityAsync(
                MessageFactory.Text(response), cancellationToken);
        });

        // Handle conversation start
        OnConversationUpdate(async (turnContext, cancellationToken) =>
        {
            foreach (var member in turnContext.Activity.MembersAdded ?? [])
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("Hello! I'm your AI assistant powered by Azure AI Foundry. How can I help?"),
                        cancellationToken);
                }
            }
        });
    }

    private async Task<string> CallFoundryAgent(string userMessage, CancellationToken ct)
    {
        _conversationHistory.Add(new ChatMessage(ChatRole.User, userMessage));

        var chatClient = _aiClient.GetChatClient("gpt-4o-1");  // your deployment name

        var options = new ChatCompletionOptions
        {
            Temperature = 0.7f,
            MaxOutputTokenCount = 1024,
        };

        // Add system prompt
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System,
                "You are a helpful assistant. Answer questions clearly and concisely.")
        };
        messages.AddRange(_conversationHistory);

        var response = await chatClient.CompleteAsync(messages, options, ct);
        var assistantMessage = response.Value.Content[0].Text;

        _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, assistantMessage));
        return assistantMessage;
    }
}
```

**appsettings.json:**

```json
{
  "AzureAI": {
    "Endpoint": "https://your-resource.services.ai.azure.com/api/projects/your-project"
  },
  "Agents": {
    "Connections": {
      "BotServiceConnection": {
        "Assembly": "Microsoft.Agents.Authentication.Msal",
        "Type": "MsalAuth",
        "Settings": {
          "AuthType": "ClientSecret",
          "ClientId": "<YOUR_APP_CLIENT_ID>",
          "ClientSecret": "<YOUR_APP_CLIENT_SECRET>",
          "TenantId": "<YOUR_TENANT_ID>",
          "AuthorityEndpoint": "https://login.microsoftonline.com/"
        }
      }
    }
  }
}
```

---

### Step 5: Test Locally with Dev Tunnels

1. Start a dev tunnel for local testing:

   ```bash
   devtunnel create --allow-anonymous
   devtunnel port create -p 5000
   devtunnel host
   ```

2. Copy the tunnel URL (e.g., `https://abc123.devtunnels.ms`)
3. Update the Azure Bot **Messaging endpoint** to: `https://abc123.devtunnels.ms/api/messages`
4. Run the agent:

   ```bash
   dotnet run
   ```

5. In Teams, search for your bot by name and send a message
6. Verify the response comes back from Foundry

---

### Step 6: Add Rich Teams Features (Optional)

**Adaptive Cards for structured responses:**

```csharp
// In your message handler, send an Adaptive Card instead of plain text
var card = new AdaptiveCard("1.5")
{
    Body = new List<AdaptiveElement>
    {
        new AdaptiveTextBlock { Text = "Account Summary", Size = AdaptiveTextSize.Large, Weight = AdaptiveTextWeight.Bolder },
        new AdaptiveFactSet
        {
            Facts = new List<AdaptiveFact>
            {
                new("Checking", "$5,234.12"),
                new("Savings", "$12,500.00"),
                new("Credit Card", "-$1,200.50"),
            }
        }
    }
};

var attachment = new Attachment
{
    ContentType = "application/vnd.microsoft.card.adaptive",
    Content = card,
};

await turnContext.SendActivityAsync(
    MessageFactory.Attachment(attachment), cancellationToken);
```

**Suggested actions (quick reply buttons):**

```csharp
var reply = MessageFactory.Text("What would you like to do?");
reply.SuggestedActions = new SuggestedActions
{
    Actions = new List<CardAction>
    {
        new() { Title = "Check Balance", Type = ActionTypes.ImBack, Value = "check balance" },
        new() { Title = "Transfer Funds", Type = ActionTypes.ImBack, Value = "transfer funds" },
        new() { Title = "Recent Transactions", Type = ActionTypes.ImBack, Value = "recent transactions" },
    }
};
await turnContext.SendActivityAsync(reply, cancellationToken);
```

---

### Step 7: Deploy to Azure App Service

1. Create an App Service:

   ```bash
   az webapp create \
     --resource-group <your-rg> \
     --plan <your-plan> \
     --name my-foundry-agent \
     --runtime "dotnet:8"
   ```

2. Configure app settings:

   ```bash
   az webapp config appsettings set \
     --resource-group <your-rg> \
     --name my-foundry-agent \
     --settings \
       AzureAI__Endpoint="https://your-resource.services.ai.azure.com/api/projects/your-project" \
       Agents__Connections__BotServiceConnection__Settings__ClientId="<APP_ID>" \
       Agents__Connections__BotServiceConnection__Settings__ClientSecret="<SECRET>" \
       Agents__Connections__BotServiceConnection__Settings__TenantId="<TENANT_ID>"
   ```

3. Deploy:

   ```bash
   dotnet publish -c Release -o ./publish
   cd publish
   zip -r ../deploy.zip .
   az webapp deploy --resource-group <your-rg> --name my-foundry-agent --src-path ../deploy.zip
   ```

4. Update the Azure Bot messaging endpoint to:
   `https://my-foundry-agent.azurewebsites.net/api/messages`

---

### Step 8: Create the Teams App Package

Create a `manifest.json` for Teams:

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/teams/v1.17/MicrosoftTeams.schema.json",
  "manifestVersion": "1.17",
  "version": "1.0.0",
  "id": "<YOUR_APP_CLIENT_ID>",
  "developer": {
    "name": "Your Organization",
    "websiteUrl": "https://your-org.com",
    "privacyUrl": "https://your-org.com/privacy",
    "termsOfUseUrl": "https://your-org.com/terms"
  },
  "name": {
    "short": "AI Banking Assistant",
    "full": "AI Banking Assistant powered by Azure AI Foundry"
  },
  "description": {
    "short": "Ask questions about your accounts",
    "full": "An AI-powered banking assistant that uses Azure AI Foundry for intelligent responses about accounts, transactions, and financial products."
  },
  "icons": {
    "outline": "outline.png",
    "color": "color.png"
  },
  "accentColor": "#0078D4",
  "bots": [
    {
      "botId": "<YOUR_APP_CLIENT_ID>",
      "scopes": ["personal", "team", "groupChat"],
      "supportsFiles": false,
      "isNotificationOnly": false,
      "commandLists": [
        {
          "scopes": ["personal"],
          "commands": [
            { "title": "help", "description": "Get help using this assistant" },
            { "title": "balance", "description": "Check account balances" }
          ]
        }
      ]
    }
  ],
  "permissions": ["identity", "messageTeamMembers"],
  "validDomains": ["my-foundry-agent.azurewebsites.net"]
}
```

**Package the app:**

```bash
# Create a zip with manifest.json + two icon PNGs (color.png 192x192, outline.png 32x32)
zip teams-app.zip manifest.json color.png outline.png
```

---

### Step 9: Deploy to Teams

**For personal testing:**

1. Open Teams → **Apps** → **Manage your apps** → **Upload a custom app**
2. Select your `teams-app.zip`
3. Select **Add** to install

**For organization-wide distribution:**

1. Go to [Teams Admin Center](https://admin.teams.microsoft.com/)
2. Navigate to **Teams apps** → **Manage apps** → **Upload new app**
3. Upload `teams-app.zip`
4. Set app policies to control who can access it
5. (Optional) Pin the app in the left rail for specific user groups

---

### Step 10: Monitor in Production

| Tool | What to Monitor |
|------|----------------|
| **Azure App Service** → Metrics | Request count, response time, failures |
| **Application Insights** | End-to-end traces, exceptions, dependency calls to Foundry |
| **Azure Bot Analytics** | Message volume, channels, user retention |
| **Azure AI Foundry** → Monitoring | Token usage, model latency, content safety triggers |

---

## Comparison Summary

| Aspect | Copilot Studio Path | M365 Agents SDK Path |
|--------|--------------------|--------------------|
| Setup time | ~2-4 hours | ~2-3 days |
| Code required | None (configuration only) | Full .NET/TS application |
| Foundry integration | Foundry IQ, BYOM, MCP | Direct SDK calls (full control) |
| Rich UI (Adaptive Cards) | Limited (topic responses) | Full (any card schema) |
| Multi-turn state | Managed by platform | You manage (in-memory or external store) |
| Authentication | Platform-handled SSO | You configure (MSAL + Entra) |
| CI/CD | Power Platform ALM (solutions) | Git + Azure DevOps / GitHub Actions |
| Cost model | Per-message Copilot Studio pricing | App Service + Foundry consumption |
| Multi-channel | Teams + M365 Copilot | Teams + Outlook + M365 Copilot + Web |
| Best for | Quick pilots, business-owned agents | Production workloads, complex orchestration |

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Agent doesn't respond in Teams | Verify messaging endpoint URL is correct and accessible |
| 401 Unauthorized from Foundry | Run `az login` or check credential configuration |
| Adaptive Cards not rendering | Verify schema version (1.5+ for Teams), check JSON validity |
| Bot not appearing in Teams search | Ensure Teams channel is enabled on Azure Bot resource |
| Copilot Studio can't reach Foundry | Check network/firewall rules; Foundry endpoint must be public or via VNET |
| "App not found" in Teams | Ensure manifest `id` matches the App Registration Client ID |
| Slow responses | Add timeout handling; Foundry cold starts can take 30-60s (see Lab 03 timeout fix) |

---

## References

| Resource | Link |
|----------|------|
| Copilot Studio: Connect agent to Teams | https://learn.microsoft.com/en-us/microsoft-copilot-studio/publication-add-bot-to-microsoft-teams |
| Copilot Studio: Connect to Foundry IQ | https://learn.microsoft.com/en-us/microsoft-copilot-studio/agents-experience/foundry-iq-connect |
| BYOM in Copilot Studio | https://www.matthewdevaney.com/azure-ai-foundry-model-in-copilot-studio-custom-prompts/ |
| M365 Agents SDK documentation | https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/bf-migration-guidance |
| M365 Agents SDK (.NET migration) | https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/bf-migration-dotnet |
| Azure Bot Service: Teams channel | https://learn.microsoft.com/en-us/azure/bot-service/channel-connect-teams |
| Teams app manifest schema | https://learn.microsoft.com/en-us/microsoftteams/platform/resources/schema/manifest-schema |
| Foundry + Copilot Studio integration blog | https://dev.to/holgerimbery/triggering-the-backend-integrating-azure-ai-foundry-with-microsoft-copilot-studio-53ol |
| Multi-agent architecture (Copilot Studio + Foundry) | https://www.rbaconsulting.com/blog/from-copilot-studio-to-azure-ai-foundry-building-a-scalable-multi-agent-ai-architecture/ |
| Video: Connect Foundry agent in Copilot Studio | https://microsoft.github.io/mcscatblog/posts/connect-foundry-agents-in-copilotstudio/ |
