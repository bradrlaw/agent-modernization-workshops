# Agent Tool Selection Decision Tree

Use this framework to determine which Microsoft agent technology best fits a given use case.

---

## Quick Reference

| Factor | Copilot Studio | Azure AI Foundry | Agents SDK |
|---|---|---|---|
| **Persona** | Makers, citizen devs | Pro developers | Pro developers |
| **Complexity** | Low–Medium | Medium–High | Medium–High |
| **Hosting** | Managed (SaaS) | Azure (flexible) | Azure (self-hosted) |
| **Customization** | Low-code, declarative | Full SDK control | Full SDK control |
| **AI Orchestration** | Built-in | Custom | Custom |
| **Channels** | Teams, Web, M365 | Any (via Agents SDK) | Teams, Web, custom |
| **Knowledge** | Built-in grounding | Azure AI Search, custom | Custom integration |
| **Multi-agent** | Connected agents | Full orchestration | Activity protocol |
| **Governance** | Power Platform DLP | Azure RBAC + policies | Azure RBAC + policies |

---

## Decision Flow

```
Start
  │
  ├─ Do you need full control over orchestration, model selection, or hosting?
  │   ├─ YES → Azure AI Foundry (pro-code agent)
  │   │         Consider Agents SDK if you need Teams/M365 channel publishing
  │   │
  │   └─ NO ──┐
  │            │
  ├─ Is the use case primarily Q&A, knowledge retrieval, or guided conversation?
  │   ├─ YES → Copilot Studio (low-code agent)
  │   │         Add Foundry backend if complex reasoning is needed (hybrid pattern)
  │   │
  │   └─ NO ──┐
  │            │
  ├─ Do you need to replace an existing Bot Framework bot?
  │   ├─ YES → Microsoft 365 Agents SDK (direct successor)
  │   │         Evaluate if Copilot Studio can handle the simplified use case
  │   │
  │   └─ NO ──┐
  │            │
  ├─ Does the scenario require multiple specialized agents working together?
  │   ├─ YES → Multi-agent pattern
  │   │         Use Foundry for orchestration + specialist agents
  │   │         Optional: Copilot Studio as the user-facing front-end
  │   │
  │   └─ NO → Start with Copilot Studio; escalate to Foundry if requirements grow
  │
  End
```

---

## Pattern Recommendations

### 1. Simple Knowledge Agent
**Tool:** Copilot Studio
- Grounded in organizational knowledge (SharePoint, Dataverse, websites)
- Generative answers with managed orchestration
- No custom code required

### 2. Custom Reasoning Agent
**Tool:** Azure AI Foundry
- Custom model selection and prompt engineering
- Tool-calling with external APIs
- Complex multi-step reasoning chains
- Azure AI Search for RAG patterns

### 3. Bot Framework Migration
**Tool:** Microsoft 365 Agents SDK
- Drop-in replacement for Bot Framework message handling
- Same activity protocol, modern runtime
- Publish to Teams and other M365 channels

### 4. Hybrid Agent (Fusion Team)
**Tools:** Copilot Studio + Azure AI Foundry
- Copilot Studio handles user interaction (low-code)
- Foundry agent handles complex backend logic (pro-code)
- Connected via the connected agents pattern
- Best for teams with mixed skill sets

### 5. Multi-Agent System
**Tools:** Azure AI Foundry (orchestrator) + multiple specialist agents
- Router agent delegates to domain-specific agents
- Shared memory and context via Azure storage
- Evaluation and observability across the system

---

## Key Considerations

- **Governance**: Copilot Studio falls under Power Platform DLP policies; Foundry/Agents SDK under Azure RBAC
- **Skill mix**: Fusion teams benefit from the hybrid pattern (Week 5 lab)
- **Networking**: Regulated environments may require private endpoints for Foundry
- **Evolution**: Start with Copilot Studio, graduate to Foundry as complexity grows
