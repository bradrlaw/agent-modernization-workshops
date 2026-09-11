namespace BankingConcierge.Memory;

/// <summary>One recorded exchange in a conversation (verbatim, kept only for the recent window).</summary>
public sealed record MemoryTurn(string Role, string? Agent, string Text, DateTimeOffset At);

/// <summary>
/// The durable memory we keep for a single customer across sessions.
///
/// The shape follows the internal Foundry "agent memory" pattern: a rolling
/// <see cref="ConversationSummary"/> plus distilled <see cref="ImportantFacts"/>,
/// <see cref="Decisions"/>, and <see cref="PendingActions"/> — NOT a full transcript.
/// That is the whole point of compaction: recall the gist, not thousands of tokens.
///
/// Scope key is <see cref="UserId"/> ("whose memory"); <see cref="ThreadId"/> is the current
/// conversation ("which conversation"). This is the same split every Foundry memory doc calls out.
/// </summary>
public sealed class MemoryRecord
{
    public string UserId { get; set; } = string.Empty;
    public string? ThreadId { get; set; }

    /// <summary>Rolling, human-readable gist (derived from <see cref="AskHistory"/>).</summary>
    public string ConversationSummary { get; set; } = string.Empty;

    /// <summary>The customer's recent requests, truncated — drives the summary.</summary>
    public List<string> AskHistory { get; set; } = new();

    public List<string> ImportantFacts { get; set; } = new();
    public List<string> Decisions { get; set; } = new();
    public List<string> PendingActions { get; set; } = new();

    /// <summary>The last few verbatim turns, for immediate continuity.</summary>
    public List<MemoryTurn> RecentTurns { get; set; } = new();

    public int SessionCount { get; set; }
    public DateTimeOffset LastUpdated { get; set; }

    public bool IsEmpty =>
        AskHistory.Count == 0 && ImportantFacts.Count == 0 && RecentTurns.Count == 0;
}
