using System.Text;
using System.Text.RegularExpressions;

namespace BankingConcierge.Memory;

/// <summary>
/// Turns a growing transcript into compact, durable memory — <b>deterministically, with no LLM
/// call</b>. As history grows we keep only the last few verbatim turns and roll the rest into a
/// summary plus distilled facts / decisions / open items. This is context compaction: recall the
/// gist instead of replaying thousands of tokens.
///
/// The extraction here is intentionally simple (regex + keyword) so the lab is fast, free, and
/// unbreakable. In production you would let Foundry-managed Memory (or the Azure Cosmos DB Agent
/// Memory Toolkit) do LLM-based extraction, summarization, and de-duplication. See README Part E.
/// </summary>
public static partial class Compactor
{
    private const int KeepRecentTurns = 6;
    private const int MaxAsks = 8;
    private const int MaxItems = 8;
    private const int AskLength = 160;

    private static readonly string[] ProductKeywords =
    [
        "auto loan", "car loan", "mortgage", "home loan", "personal loan", "student loan",
        "refinance", "credit card", "debit card", "checking", "savings", "dispute", "overdraft",
    ];

    /// <summary>Folds <paramref name="newTurns"/> into <paramref name="record"/> in place.</summary>
    public static void Compact(MemoryRecord record, IReadOnlyList<MemoryTurn> newTurns)
    {
        foreach (var turn in newTurns)
        {
            var text = Collapse(turn.Text);
            if (text.Length == 0)
            {
                continue;
            }

            if (turn.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                AddCapped(record.AskHistory, Truncate(text, AskLength), MaxAsks);
                foreach (var fact in ExtractFacts(text))
                {
                    AddCapped(record.ImportantFacts, fact, MaxItems);
                }
            }
            else
            {
                foreach (var decision in ExtractDecisions(text))
                {
                    AddCapped(record.Decisions, decision, MaxItems);
                }

                foreach (var pending in ExtractPending(text))
                {
                    AddCapped(record.PendingActions, pending, MaxItems);
                }
            }
        }

        record.RecentTurns.AddRange(newTurns);
        if (record.RecentTurns.Count > KeepRecentTurns)
        {
            record.RecentTurns.RemoveRange(0, record.RecentTurns.Count - KeepRecentTurns);
        }

        record.ConversationSummary = record.AskHistory.Count == 0
            ? string.Empty
            : "Across earlier sessions the customer asked:\n"
                + string.Join("\n", record.AskHistory.Select(a => "  - " + a));
        record.LastUpdated = DateTimeOffset.UtcNow;
    }

    private static IEnumerable<string> ExtractFacts(string text)
    {
        var lower = text.ToLowerInvariant();

        foreach (Match m in MoneyRegex().Matches(text))
        {
            yield return m.Value;
        }

        foreach (Match m in TermRegex().Matches(lower))
        {
            yield return $"{m.Groups[1].Value}-month term";
        }

        foreach (var keyword in ProductKeywords)
        {
            if (lower.Contains(keyword, StringComparison.Ordinal))
            {
                yield return keyword;
            }
        }
    }

    private static IEnumerable<string> ExtractDecisions(string text)
    {
        foreach (Match m in MonthlyPaymentRegex().Matches(text))
        {
            yield return $"quoted monthly payment {m.Value}";
        }

        if (Regex.IsMatch(text, @"\b(apr|rate)\b", RegexOptions.IgnoreCase)
            && MoneyRegex().Match(text) is { Success: true })
        {
            foreach (Match m in PercentRegex().Matches(text))
            {
                yield return $"rate discussed: {m.Value} APR";
                break;
            }
        }
    }

    private static IEnumerable<string> ExtractPending(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("dispute"))
        {
            yield return "an open card dispute to follow up on";
        }

        if (lower.Contains("escalat"))
        {
            yield return "an escalation in progress";
        }

        if (lower.Contains("next step") || lower.Contains("follow up") || lower.Contains("follow-up"))
        {
            yield return "agreed next steps to revisit";
        }
    }

    private static void AddCapped(List<string> list, string value, int cap)
    {
        if (string.IsNullOrWhiteSpace(value)
            || list.Any(v => v.Equals(value, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        list.Add(value);
        if (list.Count > cap)
        {
            list.RemoveAt(0);
        }
    }

    private static string Collapse(string text) => WhitespaceRegex().Replace(text, " ").Trim();

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max].TrimEnd() + "…";

    [GeneratedRegex(@"\$\s?\d[\d,]*(?:\.\d{2})?")]
    private static partial Regex MoneyRegex();

    [GeneratedRegex(@"\b(\d{1,3})\s*(?:month|months|mo)\b")]
    private static partial Regex TermRegex();

    [GeneratedRegex(@"\$\s?\d[\d,]*(?:\.\d{2})?\s*/?\s*(?:mo|month|monthly)")]
    private static partial Regex MonthlyPaymentRegex();

    [GeneratedRegex(@"\d+(?:\.\d+)?\s?%")]
    private static partial Regex PercentRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
