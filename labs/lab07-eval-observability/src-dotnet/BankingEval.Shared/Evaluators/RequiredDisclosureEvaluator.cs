using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace BankingEval.Shared.Evaluators;

/// <summary>
/// Any loan quote (a response mentioning a monthly payment or APR) must carry an
/// "estimate" / "subject to change" style disclosure. Non-loan responses pass.
/// </summary>
public sealed class RequiredDisclosureEvaluator : IEvaluator
{
    public const string MetricName = "Required Disclosure";

    private static readonly string[] LoanSignals =
    {
        "monthly payment", "apr", "interest rate", "loan payment", "per month", "/month",
    };

    private static readonly string[] DisclosurePhrases =
    {
        "estimate", "estimated", "subject to change", "subject to approval",
        "not a commitment", "for informational", "may vary", "approximate",
    };

    public IReadOnlyCollection<string> EvaluationMetricNames => new[] { MetricName };

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var text = CustomEvaluatorHelpers.GetResponseText(modelResponse).ToLowerInvariant();
        var isLoanQuote = LoanSignals.Any(signal => text.Contains(signal, StringComparison.Ordinal));

        if (!isLoanQuote)
        {
            return new ValueTask<EvaluationResult>(
                CustomEvaluatorHelpers.PassFail(MetricName, true, "Not a loan quote; disclosure not required."));
        }

        var hasDisclosure = DisclosurePhrases.Any(phrase => text.Contains(phrase, StringComparison.Ordinal));
        var reason = hasDisclosure
            ? "Loan quote includes a required disclosure."
            : "Loan quote is missing a required disclosure (e.g. 'estimate', 'subject to change').";

        return new ValueTask<EvaluationResult>(
            CustomEvaluatorHelpers.PassFail(MetricName, hasDisclosure, reason));
    }
}
