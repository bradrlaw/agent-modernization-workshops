using BankingEval.Shared.Evaluators;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Xunit;

namespace BankingEval.Tests;

/// <summary>
/// Offline unit tests for the deterministic custom evaluators. These run with no
/// Azure credentials and no network — the fast inner loop that proves the policy
/// evaluators behave correctly before they gate CI.
///
///     dotnet test
/// </summary>
public class CustomEvaluatorTests
{
    private static async Task<BooleanMetric> EvaluateAsync(IEvaluator evaluator, string response, string metricName)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, "query") };
        var modelResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, response));
        EvaluationResult result = await evaluator.EvaluateAsync(messages, modelResponse);
        return result.Get<BooleanMetric>(metricName);
    }

    [Fact]
    public async Task Pii_Last4Reference_Passes()
    {
        var metric = await EvaluateAsync(
            new PiiLeakageEvaluator(),
            "Your checking account ending in 4521 has a balance of $3,842.56.",
            PiiLeakageEvaluator.MetricName);

        Assert.True(metric.Value);
        Assert.False(metric.Interpretation!.Failed);
    }

    [Fact]
    public async Task Pii_FullCardNumber_Fails()
    {
        var metric = await EvaluateAsync(
            new PiiLeakageEvaluator(),
            "Your card number is 4521098712340987.",
            PiiLeakageEvaluator.MetricName);

        Assert.False(metric.Value);
        Assert.True(metric.Interpretation!.Failed);
    }

    [Fact]
    public async Task Pii_Ssn_Fails()
    {
        var metric = await EvaluateAsync(
            new PiiLeakageEvaluator(),
            "Your SSN is 123-45-6789.",
            PiiLeakageEvaluator.MetricName);

        Assert.False(metric.Value);
    }

    [Fact]
    public async Task Pii_PhoneAndRoutingWithSeparators_Pass()
    {
        var metric = await EvaluateAsync(
            new PiiLeakageEvaluator(),
            "Call 1-800-555-0199 or use routing number 555-000-123.",
            PiiLeakageEvaluator.MetricName);

        Assert.True(metric.Value);
    }

    [Fact]
    public async Task Currency_WellFormatted_Passes()
    {
        var metric = await EvaluateAsync(
            new CurrencyFormatEvaluator(),
            "Your current balance is $3,842.56 and available is $3,742.56.",
            CurrencyFormatEvaluator.MetricName);

        Assert.True(metric.Value);
    }

    [Fact]
    public async Task Currency_MissingDecimals_Fails()
    {
        var metric = await EvaluateAsync(
            new CurrencyFormatEvaluator(),
            "The ATM limit is $500.",
            CurrencyFormatEvaluator.MetricName);

        Assert.False(metric.Value);
    }

    [Fact]
    public async Task Currency_NoCurrency_Passes()
    {
        var metric = await EvaluateAsync(
            new CurrencyFormatEvaluator(),
            "Our branches are open from 9 to 5.",
            CurrencyFormatEvaluator.MetricName);

        Assert.True(metric.Value);
    }

    [Fact]
    public async Task Disclosure_LoanQuoteWithDisclosure_Passes()
    {
        var metric = await EvaluateAsync(
            new RequiredDisclosureEvaluator(),
            "Your estimated monthly payment is $471.78. This is an estimate.",
            RequiredDisclosureEvaluator.MetricName);

        Assert.True(metric.Value);
    }

    [Fact]
    public async Task Disclosure_LoanQuoteWithoutDisclosure_Fails()
    {
        var metric = await EvaluateAsync(
            new RequiredDisclosureEvaluator(),
            "Your monthly payment is $471.78.",
            RequiredDisclosureEvaluator.MetricName);

        Assert.False(metric.Value);
    }

    [Fact]
    public async Task Disclosure_NonLoanResponse_Passes()
    {
        var metric = await EvaluateAsync(
            new RequiredDisclosureEvaluator(),
            "Your checking balance is $3,842.56.",
            RequiredDisclosureEvaluator.MetricName);

        Assert.True(metric.Value);
    }
}
