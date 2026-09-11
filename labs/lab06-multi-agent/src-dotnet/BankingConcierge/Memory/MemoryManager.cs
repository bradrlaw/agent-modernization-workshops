using System.Text;

namespace BankingConcierge.Memory;

/// <summary>Result of a recall: the instruction preamble to inject, plus the raw record.</summary>
public sealed record Recall(string Preamble, MemoryRecord? Record)
{
    public bool HasMemory => Preamble.Length > 0;
}

/// <summary>
/// The BYO (bring-your-own-store) memory tier for Lab 06. Ties a durable
/// <see cref="IMemoryStore"/> to the two lifecycle moments every memory system needs:
///   <list type="bullet">
///     <item><b>Recall</b> (before a run): load the customer's memory and compose a compact
///       preamble to inject into the agents' instructions — the equivalent of a context
///       provider's <c>before_run</c>.</item>
///     <item><b>Record</b> (after a run): fold the new turns into the record via
///       <see cref="Compactor"/> and persist — the equivalent of <c>after_run</c>.</item>
///   </list>
/// The managed alternative (Foundry-managed Memory) does both automatically inside the service;
/// this class exists to make the pattern visible and runnable with no extra Azure resource.
/// </summary>
public sealed class MemoryManager
{
    private readonly IMemoryStore _store;

    public MemoryManager(IMemoryStore store) => _store = store;

    public string Backend => _store.Describe();

    /// <summary>Creates the store for a BYO mode. <c>off</c> is handled by the caller.</summary>
    /// <remarks>
    /// <c>local</c> → <see cref="LocalFileStore"/>; <c>cosmos</c> → <see cref="CosmosStore"/> when
    /// configured (COSMOS_ENDPOINT keyless, or COSMOS_CONNECTION_STRING for the emulator), otherwise a
    /// visible warning + local fallback. If Cosmos is configured but unreachable it fails loudly on
    /// first use rather than downgrading silently. <c>foundry</c> never reaches here — it is a managed,
    /// single-agent path handled by <see cref="FoundryMemoryDemo"/> before any store is created. See README Part E.
    /// </remarks>
    public static IMemoryStore CreateStore(string mode)
    {
        if (mode == "cosmos")
        {
            var store = CosmosStore.FromEnvironment(out var reason);
            if (store is not null)
            {
                return store;
            }

            Console.WriteLine($"⚠ Memory: --memory cosmos requested but {reason}. Falling back to "
                + "local files. Provision with infra/provision-cosmos.ps1 (see README Part E).");
        }

        return new LocalFileStore();
    }

    public async Task<Recall> RecallAsync(string userId, CancellationToken ct = default)
    {
        var record = await _store.LoadAsync(userId, ct);
        return record is null || record.IsEmpty
            ? new Recall(string.Empty, null)
            : new Recall(BuildPreamble(record), record);
    }

    public async Task<int> RecordAsync(
        string userId, string? threadId, IReadOnlyList<MemoryTurn> turns, CancellationToken ct = default)
    {
        if (turns.Count == 0)
        {
            return 0;
        }

        var record = await _store.LoadAsync(userId, ct) ?? new MemoryRecord { UserId = userId };
        record.ThreadId = threadId ?? record.ThreadId;
        record.SessionCount += 1;
        Compactor.Compact(record, turns);
        await _store.SaveAsync(record, ct);
        return turns.Count;
    }

    private static string BuildPreamble(MemoryRecord r)
    {
        var sb = new StringBuilder();
        sb.Append("\n\n# Memory recall — what you already know about this customer from earlier sessions\n");
        sb.Append("Use this to avoid re-asking; confirm details rather than repeat questions. "
            + "Do not invent memory beyond what is listed here.\n");

        if (!string.IsNullOrWhiteSpace(r.ConversationSummary))
        {
            sb.Append('\n').Append(r.ConversationSummary).Append('\n');
        }

        AppendList(sb, "Known facts", r.ImportantFacts);
        AppendList(sb, "Prior decisions", r.Decisions);
        AppendList(sb, "Open items to follow up", r.PendingActions);

        if (r.RecentTurns.Count > 0)
        {
            sb.Append("\nMost recent exchange:\n");
            foreach (var turn in r.RecentTurns.TakeLast(3))
            {
                var who = turn.Agent ?? turn.Role;
                var text = turn.Text.Length <= 200 ? turn.Text : turn.Text[..200].TrimEnd() + "…";
                sb.Append("  ").Append(who).Append(": ").Append(text).Append('\n');
            }
        }

        return sb.ToString();
    }

    private static void AppendList(StringBuilder sb, string label, List<string> items)
    {
        if (items.Count > 0)
        {
            sb.Append(label).Append(": ").Append(string.Join("; ", items)).Append('\n');
        }
    }
}
