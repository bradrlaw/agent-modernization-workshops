# Lab 07 – Testing, Evaluation, and Observability

## Overview

Implement quality gates, evaluation pipelines, and enterprise monitoring for agents
built in previous labs. Ensure agents are production-ready with measurable quality.

## Learning Objectives

- Define quality metrics for conversational agents
- Build evaluation pipelines using Foundry evaluators or custom scripts
- Enable Copilot Studio analytics
- Configure Application Insights dashboards for agent observability

## Prerequisites

- Agents from Labs 02–06 deployed and accessible
- Application Insights / Log Analytics workspace enabled
- Access to conversation logs (non-PII or redacted)

> ⚠️ See [environment checklist](../../docs/environment-checklist.md) section A4.

## Lab Steps

### Step 1: Define Quality Metrics

Establish metrics for your agents:

| Metric | Description | Target |
|---|---|---|
| **Groundedness** | Are responses grounded in provided knowledge? | > 90% |
| **Relevance** | Do responses answer the user's question? | > 85% |
| **Coherence** | Are responses well-structured and clear? | > 90% |
| **Fluency** | Is the language natural and grammatical? | > 95% |
| **Safety** | Are responses free of harmful content? | 100% |
| **Completion rate** | Do users achieve their goal? | > 80% |

### Step 2: Build an Evaluation Dataset

1. Collect or create test conversations (10–20 examples minimum)
2. Include:
   - User messages (inputs)
   - Expected responses or acceptable response criteria
   - Context / knowledge sources used
3. Save as a structured dataset (JSON or CSV)

### Step 3: Run Evaluations

#### Option A: Azure AI Foundry Evaluators

1. Use built-in evaluators (groundedness, relevance, coherence)
2. Configure the evaluation pipeline
3. Run against your test dataset
4. Review evaluation scores

#### Option B: Custom Evaluation Pipeline

1. Create a script that sends test inputs to your agent
2. Compare agent responses against expected outcomes
3. Score using LLM-as-judge or rule-based criteria
4. Generate a summary report

### Step 4: Copilot Studio Analytics

1. Open Copilot Studio → **Analytics**
2. Review:
   - Session completion rates
   - Topic triggering accuracy
   - Escalation rates
   - User satisfaction scores
3. Identify topics that need improvement

### Step 5: Application Insights Observability

1. Open Application Insights for your deployed agents
2. Explore:
   - Request traces and latency
   - Tool-calling success/failure rates
   - Token usage and model performance
   - Error rates and exceptions
3. Create a dashboard with key agent health metrics
4. Set up alerts for critical failures

## Deliverables

- [ ] Quality metrics defined with targets
- [ ] Evaluation dataset created (10+ test cases)
- [ ] Evaluation pipeline run with results documented
- [ ] Copilot Studio analytics reviewed (if applicable)
- [ ] Application Insights dashboard created
- [ ] Key findings and improvement recommendations documented

## Next Steps

→ [Lab 08: Capstone](../lab08-capstone/) — Team presentations and architecture review
