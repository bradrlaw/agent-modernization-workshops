# Topic Design Guide – Minimal Topics for Generative Orchestration

In generative orchestration mode, the LLM handles conversation routing, disambiguation,
and multi-turn context based on your **agent instructions** and **action descriptions**.
Topics are reduced to **guardrails** — you only need them for edge cases the orchestrator
shouldn't handle on its own.

> **Key principle:** If the LLM can handle it via instructions + actions, don't create a
> topic for it. Every unnecessary topic adds maintenance burden and can conflict with
> the orchestrator's decisions.

---

## What Topics Do You Need?

| Topic | Type | Purpose |
|---|---|---|
| Welcome | Optional custom | First-contact greeting to set expectations |
| Fallback | System (built-in) | Generative answers + human escalation |
| Escalation | System (built-in) | Hand off to a live agent |

That's it. **Three topics total**, and two of them are built-in.

### What You Do NOT Need

❌ "Check Balance" topic with trigger phrases
❌ "View Transactions" topic with branching logic
❌ "List Accounts" topic with question nodes
❌ "Customer Profile" topic with disambiguation
❌ Any topic that maps 1:1 to an action

The orchestrator reads your action descriptions and decides when to invoke them.
Manual topics for these actions would **conflict** with the orchestrator and create
unpredictable routing behavior.

---

## Topic 1: Welcome (Optional)

**Purpose:** Greet the user on first contact and set expectations about capabilities.

> You can also skip this topic entirely and let the orchestrator generate a greeting
> from the agent instructions. Test both approaches and see which feels better.

### Configuration

1. Go to **Topics** → **+ Add a topic** → **From blank**
2. Name: `Welcome`
3. Trigger: Set as the **Greeting** system topic override, or use the
   `Conversation Start` trigger
4. Add a single **Message** node:

### Message Content

> 👋 Welcome to the Virtual Banking Assistant! I can help you with:
>
> • Check account balances
> • View recent transactions
> • List all your accounts
> • Look up your profile information
>
> Just ask me anything to get started!

### Implementation Notes
- Keep this short — the LLM takes over from here
- Do **not** add suggested actions or quick replies that funnel users into specific paths;
  let them ask naturally
- Do **not** add follow-up question nodes; the orchestrator handles the next turn

---

## Topic 2: Fallback (System)

**Purpose:** Handle requests that don't match any action and can't be answered from
knowledge sources.

### Configuration

1. Go to **Topics** → **System** → **Fallback**
2. Ensure **generative answers** is the first fallback behavior:
   - The orchestrator will attempt to answer from knowledge sources
   - If knowledge sources can't answer, it falls through to the escalation message

### Escalation Message

If generative answers also can't help:

> I wasn't able to find an answer to that question. Would you like me to connect you
> with a human agent, or can I help you with something else?

### Implementation Notes
- The fallback topic fires **only** when the orchestrator determines none of the
  registered actions are relevant AND knowledge sources don't have an answer
- Monitor fallback triggers in analytics — they reveal gaps in your action descriptions
  or knowledge sources
- If a specific request type consistently falls through, consider:
  1. First: Improve action descriptions or agent instructions
  2. Second: Add knowledge source content
  3. Last resort: Create a targeted topic (rare)

---

## Topic 3: Escalation (System)

**Purpose:** Transfer the conversation to a human agent.

### Configuration

1. Go to **Topics** → **System** → **Escalation**
2. If live agent handoff is configured (Omnichannel, Dynamics 365, etc.), wire it here
3. If not configured, display a message:

> I'm connecting you with a support representative. You can also reach us at
> 1-800-555-0199 or visit any branch. Thank you for your patience.

---

## How the Orchestrator Replaces Topics

Here's how the LLM handles scenarios that would traditionally require dedicated topics:

### User asks about balance
| What happens | Details |
|---|---|
| **Classic approach** | "Check Balance" topic triggers → question node asks which account → action node calls Get Balance |
| **Generative approach** | LLM reads action descriptions → calls **List Accounts** to see what the customer has → asks "Which account?" naturally → calls **Get Account Balance** → formats response |

### User asks a follow-up
| What happens | Details |
|---|---|
| **Classic approach** | Context is lost between topics; user has to start over or topic must explicitly pass state |
| **Generative approach** | LLM maintains conversation context → "What about transactions for that account?" just works |

### User asks something ambiguous
| What happens | Details |
|---|---|
| **Classic approach** | Falls through to fallback because no trigger phrase matches |
| **Generative approach** | LLM interprets "How much do I have?" as a balance request and calls the right action |

### User asks about something outside capabilities
| What happens | Details |
|---|---|
| **Classic approach** | Needs explicit "out of scope" topics with trigger phrases |
| **Generative approach** | Agent instructions say "you cannot transfer funds" → LLM politely declines and offers alternatives |

---

## When to Add a Custom Topic (Rare)

You should only add a custom topic if:

1. **Strict compliance requirement** — A specific request must always produce the exact
   same response, word for word, with no LLM variation
2. **The orchestrator consistently misroutes** — After tuning instructions and descriptions,
   a specific scenario still doesn't work (this is rare)
3. **Complex UI interaction** — You need a specific adaptive card flow that the orchestrator
   can't produce from action outputs alone

Even in these cases, try to solve it with better instructions or action descriptions first.
