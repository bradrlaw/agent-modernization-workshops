using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace BankingEval.Shared.Evaluators;

/// <summary>
/// Fails if any dollar amount is not formatted with a leading "$" and exactly
/// two decimal places (e.g., "$3,842.56").
/// </summary>
public sealed partial class CurrencyFormatEvaluator : IEvaluator
{
    public const string MetricName = "Currency Format";

    public IReadOnlyCollection<string> EvaluationMetricNames => new[] { MetricName };

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var response = CustomEvaluatorHelpers.GetResponseText(modelResponse);
        var bad = new List<string>();

        foreach (Match match in CurrencyTokenRegex().Matches(response))
        {
            var token = match.Value.Trim();
            if (!CurrencyOkRegex().IsMatch(token))
            {
                bad.Add(token);
            }
        }

        var passed = bad.Count == 0;
        var reason = passed
            ? "All currency amounts use '$' and two decimal places."
            : "Improperly formatted currency amounts: " + string.Join(", ", bad);

        return new ValueTask<EvaluationResult>(
            CustomEvaluatorHelpers.PassFail(MetricName, passed, reason));
    }

    [GeneratedRegex(@"\$\s?\d[\d,]*(?:\.\d+)?")]
    private static partial Regex CurrencyTokenRegex();

    [GeneratedRegex(@"^\$\s?\d{1,3}(?:,\d{3})*\.\d{2}$")]
    private static partial Regex CurrencyOkRegex();
}
