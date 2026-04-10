# Agent Modernization Workshops

Hands-on workshop series for modernizing Azure Bot Framework SDK bots to Microsoft's
current agent platform: **Copilot Studio**, **Azure AI Foundry**, and the
**Microsoft 365 Agents SDK**.

---

## Program Summary

| | |
|---|---|
| **Format** | 8 bi-weekly workshops (90–120 min) + paired hands-on labs |
| **Audience** | Developers, architects, platform engineers, security & governance teams |
| **Outcome** | Working agents, approved environments, and a repeatable modernization playbook |

### What You'll Learn

1. **Decision framework** — When to use Copilot Studio vs Azure AI Foundry vs Agents SDK
2. **Working reference agents** — Built across the low-code → pro-code spectrum
3. **Operational readiness** — Evaluation, observability, and governance-aligned environments
4. **Migration playbook** — Patterns for moving Bot Framework bots to modern agents

---

## Agent Tooling Landscape

| Capability | Primary Tool | Notes |
|---|---|---|
| Low / No-Code Agents | Microsoft Copilot Studio | Managed SaaS, Power Platform based |
| Pro-Code Agents | Azure AI Foundry | SDK-driven, flexible hosting |
| Runtime & Channels | Microsoft 365 Agents SDK | Bot Framework successor |
| Hybrid / Multi-Agent | Copilot Studio + Foundry | Connected agents pattern |

---

## Workshop Schedule

| Week | Topic | Lab Focus |
|---|---|---|
| 1 | [From Bots to Agents](labs/lab01-bot-to-agent-mapping/) | Map existing bots to agent patterns (design only) |
| 2 | [Copilot Studio](labs/lab02-copilot-studio/) | Build a low-code agent with generative answers |
| 3 | [Azure AI Foundry](labs/lab03-foundry-agent/) | Build a pro-code agent with AI Search |
| 4 | [Microsoft 365 Agents SDK](labs/lab04-agents-sdk/) | Scaffold an agent and publish to Teams |
| 5 | [Hybrid Agents](labs/lab05-hybrid-agent/) | Connect Copilot Studio to a Foundry agent |
| 6 | [Multi-Agent Orchestration](labs/lab06-multi-agent/) | Build a router + specialist agent system |
| 7 | [Testing & Observability](labs/lab07-eval-observability/) | Evaluation pipelines and monitoring |
| 8 | [Capstone](labs/lab08-capstone/) | Team presentations and architecture review |

---

## Prerequisites

### Identity & Access
- Microsoft Entra ID tenant access
- Ability to create app registrations, managed identities, and service principals

### Networking
- Outbound HTTPS to Azure AI Foundry / Azure OpenAI endpoints
- Outbound HTTPS to Microsoft 365 services (Teams, Copilot)
- Proxy and firewall rules documented

### Governance
- Non-production Azure subscriptions and Power Platform environments
- Logging and telemetry approved

### Developer Workstations
- VS Code or Visual Studio installed
- Git client with access to source control
- Azure CLI authenticated
- Required extensions: Copilot Studio, Agents Toolkit, Azure

> See [docs/environment-checklist.md](docs/environment-checklist.md) for the full
> environment approval checklist.

---

## Repository Structure

```
agent-modernization-workshops/
├── README.md                         # This file
├── docs/                             # Workshop docs, decision trees, reference architectures
├── labs/
│   ├── lab01-bot-to-agent-mapping/   # Week 1 – Design & analysis
│   ├── lab02-copilot-studio/         # Week 2 – Low-code agent
│   ├── lab03-foundry-agent/          # Week 3 – Pro-code agent
│   ├── lab04-agents-sdk/             # Week 4 – Agents SDK + Teams
│   ├── lab05-hybrid-agent/           # Week 5 – Copilot Studio ↔ Foundry
│   ├── lab06-multi-agent/            # Week 6 – Multi-agent orchestration
│   ├── lab07-eval-observability/     # Week 7 – Eval & monitoring
│   └── lab08-capstone/               # Week 8 – Capstone
├── shared/                           # Cross-lab utilities, IaC, sample data
├── .devcontainer/                    # Dev container for consistent environments
└── .github/workflows/                # CI pipelines
```

---

## Getting Started

1. **Clone this repo**
   ```bash
   git clone https://github.com/<org>/agent-modernization-workshops.git
   cd agent-modernization-workshops
   ```

2. **Open in VS Code** (recommended — uses the dev container)
   ```bash
   code .
   ```

3. **Start with Week 1** → [`labs/lab01-bot-to-agent-mapping/`](labs/lab01-bot-to-agent-mapping/)

---

## Environment Readiness Timeline

Environment provisioning runs **in parallel** with the workshops. Submit requests early
to avoid blocking lab work.

| Timeframe | Required Action |
|---|---|
| Pre-Week 1 | Power Platform environment request submitted |
| Pre-Week 2 | Azure subscription + AI Foundry enabled |
| Pre-Week 3 | Model access approved |
| Pre-Week 4 | App registrations + Teams publishing permissions |

---

## Enterprise Fallback Paths

| Constraint | Fallback |
|---|---|
| No Power Platform available | Use Agents SDK + Foundry only (pro-code path) |
| No Copilot access | Publish agents to Teams channel directly |
| Restricted networking | Private endpoints + managed identity |

---

## Contact

**Brad Lawrence** — Solution Architect, Microsoft ISD
📧 [Brad.Lawrence@microsoft.com](mailto:Brad.Lawrence@microsoft.com)

---

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.

## License

This project is licensed under the [MIT License](LICENSE).
