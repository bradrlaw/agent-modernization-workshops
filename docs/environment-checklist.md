# Environment Approval Checklist

Use this checklist to provision environments **in parallel** with the workshop series.
Submit requests early — platform approvals (especially Power Platform) often have the
longest lead times in regulated organizations.

---

## Self-Service Environment Setup (Recommended)

The fastest way to get a fully working environment is to create your own **Microsoft 365
Developer tenant** with a **Power Platform trial**. This avoids CDX performance issues and
gives you a real, isolated tenant for building agents.

> 💡 Even without an Azure subscription, this path gives you everything needed for
> Copilot Studio, Power Automate, Dataverse, and Teams.

### Step 1 — Create a Microsoft 365 Developer Tenant (E5)

1. Go to the [Microsoft 365 Developer Program](https://developer.microsoft.com/microsoft-365/dev-program)
2. Click **Join** and sign in with a Microsoft account (personal or work)
3. Select **Create a Sandbox** → choose **Configurable Sandbox**
4. Pick a region close to you (this affects Dataverse latency)
5. Complete the setup — this provisions an **E5 dev tenant** with full Teams, Exchange,
   SharePoint, and Entra ID

> ✅ This tenant is renewable as long as you actively use it. It is a real tenant,
> not a demo.

### Step 2 — Add a Power Platform Trial Environment

The M365 Developer tenant does **not** include Dataverse by default. You must add a
Power Platform trial.

1. Go to [Power Platform Admin Center](https://aka.ms/ppac) and sign in with your dev
   tenant credentials
2. Navigate to **Environments** → click **+ New**
3. Create a new environment:
   - **Type**: Developer (or Trial)
   - **Add Dataverse**: ✅ Yes
   - Optionally deploy sample data for faster testing
4. Wait for provisioning (typically 2–5 minutes)

### Step 3 — Assign Trial Licenses

1. Go to [Microsoft 365 Admin Center](https://admin.microsoft.com) (signed in as your
   dev tenant admin)
2. Navigate to **Billing** → **Licenses** (or **Purchase services**)
3. Add a **Power Apps Trial** or **Copilot Studio Trial** license
4. Assign the license to your user account

This unlocks:
- ✅ Copilot Studio (agent creation, topics, tools, channels)
- ✅ Dataverse (tables, APIs, solutions)
- ✅ Power Automate (cloud flows with premium connectors)
- ✅ Model-driven apps

### Step 4 — Verify Copilot Studio Access

1. Go to [Copilot Studio](https://copilotstudio.microsoft.com)
2. Sign in with your dev tenant credentials
3. Select your new environment from the environment picker (top right)
4. Verify you can create a new agent

### Optional: Add an Azure Subscription

If you need Azure resources (API Management, AI Foundry, etc.):

- Use an existing Azure subscription and add it to your dev tenant, **or**
- Start an [Azure free trial](https://azure.microsoft.com/free/) with your dev tenant
  credentials

> ⚠️ An Azure subscription is **not required** for the Copilot Studio, Power Automate,
> and Dataverse portions of the labs. It is only needed for labs that use Azure services
> (APIM, AI Foundry, etc.).

### What NOT to Use

| Option | Why Not |
|---|---|
| CDX demo tenant | Shared infrastructure, severe throttling, not suitable for real development |
| Default environment | Poor isolation, messy ALM, shared with all users in the tenant |
| Power Apps Community Plan | Single-user only, limited sharing, no premium connectors |
| Corporate tenant trial | Policy conflicts, cleanup pain, potential DLP/governance issues |

---

## Enterprise Environment Setup

For organizations running these workshops with their own infrastructure, use the
checklists below. Submit requests early — platform approvals often have the longest
lead times.

---

## Baseline Prerequisites (All Weeks)

### Identity & Access
- [ ] Microsoft Entra ID tenant access confirmed
- [ ] Ability to create app registrations
- [ ] Ability to create managed identities
- [ ] Ability to create service principals

### Networking
- [ ] Outbound HTTPS allowed to Azure AI Foundry / Azure OpenAI endpoints
- [ ] Outbound HTTPS allowed to Microsoft 365 services (Teams, Copilot)
- [ ] Proxy and firewall rules documented for developer workstations

### Governance
- [ ] Non-production Azure subscriptions provisioned
- [ ] Non-production Power Platform environments provisioned
- [ ] Logging and telemetry approved

---

## A1. Power Platform / Copilot Studio (Required by Week 2)

- [ ] Non-production Power Platform environment created
- [ ] Copilot Studio enabled in tenant
- [ ] Dataverse enabled and compliant with data policies
- [ ] Maker roles assigned to lab participants
- [ ] Connectors policy reviewed (allowlist / DLP)

## A2. Azure Subscription / AI Foundry (Required by Week 3)

- [ ] Dedicated non-production subscription or resource group
- [ ] Azure AI Foundry enabled for the subscription
- [ ] Model access approval completed (per organizational governance)
- [ ] Azure AI Search provisioned (or equivalent knowledge store)
- [ ] Key Vault / managed identity strategy defined
- [ ] Application Insights enabled

## A3. Microsoft 365 Agents SDK / Teams (Required by Week 4)

- [ ] Microsoft 365 test tenant or non-production Teams environment available
- [ ] Teams app publishing permissions confirmed
- [ ] App registrations approved (OAuth scopes, redirect URIs)
- [ ] Hosting plan approved (Azure Functions / App Service / Container Apps)

## A4. Networking & Logging

- [ ] Outbound HTTPS allowed to all required Microsoft endpoints
- [ ] Proxy requirements documented for developer workstations
- [ ] Private endpoints planned (if internet egress is restricted)
- [ ] Application Insights + Log Analytics workspace enabled
- [ ] Conversation log retention and PII redaction policy confirmed

## A5. Developer Workstations

- [ ] VS Code or Visual Studio installed
- [ ] Git client with access to source control
- [ ] Azure CLI installed and authenticated
- [ ] Node.js / Python runtime installed (per lab requirements)
- [ ] Required VS Code extensions installed:
  - [ ] Copilot Studio
  - [ ] Agents Toolkit
  - [ ] Azure extensions

---

## Approval Timeline

| Timeframe | Required Action |
|---|---|
| Pre-Week 1 | Power Platform environment request submitted |
| Pre-Week 2 | Azure subscription + AI Foundry enabled |
| Pre-Week 3 | Model access approved |
| Pre-Week 4 | App registrations + Teams publishing permissions |

---

## Enterprise-Safe Fallback Paths

If any environment cannot be provisioned in time:

| Constraint | Fallback |
|---|---|
| No Power Platform | Use Agents SDK + Foundry only (skip Weeks 2/5 low-code portions) |
| No Copilot access | Publish agents to Teams channel directly |
| Restricted networking | Private endpoints + managed identity |
