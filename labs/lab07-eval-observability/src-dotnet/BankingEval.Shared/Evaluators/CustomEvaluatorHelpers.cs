using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace BankingEval.Shared.Evaluators;

/// <summary>
/// Shared helpers for the deterministic, rule-based custom evaluators. These
/// encode banking-specific policy that general-purpose quality evaluators do
/// not. They never call an LLM, so they are fast, free, and deterministic —
/// ideal for an offline CI quality gate and for unit testing.
/// </summary>
internal static class CustomEvaluatorHelpers
{
    public static string GetResponseText(ChatResponse modelResponse) =>
        modelResponse.Text ?? string.Empty;

    public static EvaluationResult PassFail(string metricName, bool passed, string reason)
    {
        var metric = new BooleanMetric(metricName, value: passed)
        {
            Interpretation = new EvaluationMetricInterpretation(
                passed ? EvaluationRating.Exceptional : EvaluationRating.Unacceptable,
                failed: !passed,
                reason: reason),
        };

        return new EvaluationResult(metric);
    }
}
