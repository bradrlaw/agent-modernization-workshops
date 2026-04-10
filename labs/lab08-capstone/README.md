# Lab 08 – Capstone: Architecture Decisions & Presentations

## Overview

Teams present their agent modernization plans, demonstrate working agents, and
receive architecture review feedback. This is the culmination of the 8-week program.

## Learning Objectives

- Synthesize learnings from Weeks 1–7 into a coherent modernization strategy
- Present working agent demonstrations
- Apply the tool selection decision framework to real scenarios
- Create actionable migration plans

## Prerequisites

- Completed labs from Weeks 1–7 (or subset based on environment availability)
- Prepared presentation and demo

## Capstone Format

| Segment | Duration | Description |
|---|---|---|
| Team presentation | 15–20 min | Architecture overview + live demo |
| Q&A and review | 10–15 min | Panel feedback and discussion |
| Scoring and feedback | 5 min | Rubric-based evaluation |

## Presentation Requirements

Each team should cover:

### 1. Architecture Overview
- Which agent tools were selected and why (reference the [decision tree](../../docs/tool-selection-decision-tree.md))
- Architecture diagram showing agent topology
- Integration points (channels, knowledge sources, APIs)

### 2. Live Demo
- Demonstrate at least one working agent
- Show the user interaction flow
- Highlight a non-trivial capability (tool-calling, knowledge grounding, multi-agent delegation)

### 3. Migration Plan
- Which existing bots are candidates for migration?
- Proposed migration approach (rewrite, wrap, or hybrid)
- Timeline and dependency considerations

### 4. Operational Readiness
- Evaluation results from Lab 07
- Observability and monitoring setup
- Governance and compliance alignment

## Evaluation Rubric

| Criteria | Weight | Scoring (1–5) |
|---|---|---|
| **Tool selection rationale** | 20% | Clear use of decision framework |
| **Architecture quality** | 25% | Well-designed, scalable, maintainable |
| **Working demo** | 25% | Functional agent with meaningful capabilities |
| **Migration plan** | 15% | Realistic, actionable, risk-aware |
| **Operational readiness** | 15% | Evaluation, monitoring, governance addressed |

## Templates

### Architecture Decision Record (ADR)

```markdown
# ADR: [Title]

## Status
Proposed / Accepted / Deprecated

## Context
[What is the problem or decision to be made?]

## Decision
[What tool/approach was chosen and why?]

## Consequences
[What are the tradeoffs? What does this enable or constrain?]
```

### Migration Checklist

- [ ] Existing bot analyzed and classified (Lab 01)
- [ ] Target agent architecture selected
- [ ] Environment provisioned and approved
- [ ] Agent built and tested
- [ ] Evaluation pipeline configured
- [ ] Observability enabled
- [ ] Teams publishing confirmed
- [ ] Documentation and runbook created
- [ ] Stakeholder sign-off obtained

## Deliverables

- [ ] Team presentation delivered
- [ ] Live demo of working agent(s)
- [ ] Architecture Decision Record (ADR) submitted
- [ ] Migration plan documented
- [ ] Feedback incorporated into final documentation

## Program Wrap-Up

After capstone presentations:

1. **Collect feedback** — Survey participants on program effectiveness
2. **Distribute final artifacts** — Decision tree, reference architectures, lab repos
3. **Identify next steps** — Migration timelines, additional support needs
4. **Celebrate** 🎉 — Teams have built real agents and have a clear path forward
