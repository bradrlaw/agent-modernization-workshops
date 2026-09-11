namespace BankingConcierge.Memory;

/// <summary>
/// A durable memory backend keyed by customer (userId). Implementations:
///   <list type="bullet">
///     <item><see cref="LocalFileStore"/> — JSON on disk, no Azure resource (lab default for BYO).</item>
///     <item><c>CosmosStore</c> — Azure Cosmos DB, shared across instances (production BYO).</item>
///   </list>
/// The managed alternative is Foundry-managed Memory (the memory search tool), which does not
/// use this interface at all — the service owns storage. See README Part E.
/// </summary>
public interface IMemoryStore
{
    Task<MemoryRecord?> LoadAsync(string userId, CancellationToken ct = default);

    Task SaveAsync(MemoryRecord record, CancellationToken ct = default);

    /// <summary>Short human-readable description of where memory lives (for the startup line).</summary>
    string Describe();
}
