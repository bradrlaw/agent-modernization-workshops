# Environment Approval Checklist

Use this checklist to provision environments **in parallel** with the workshop series.
Submit requests early — platform approvals (especially Power Platform) often have the
longest lead times in regulated organizations.

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
