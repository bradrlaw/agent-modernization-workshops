using System.Text.Json;

namespace BankingConcierge.Memory;

/// <summary>
/// File-backed conversation memory — one JSON document per customer under a local
/// <c>.memory/</c> folder. Persists across process restarts, so it demonstrates
/// cross-session recall with <b>no Azure resource</b>. This is the lab default for the BYO tier.
///
/// For a shared, multi-instance store you would swap in the Cosmos DB backend (same interface);
/// the production-recommended path is Foundry-managed Memory. See README Part E.
/// </summary>
public sealed class LocalFileStore : IMemoryStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly string _dir;

    public LocalFileStore(string? directory = null)
    {
        _dir = directory ?? DefaultDir();
        Directory.CreateDirectory(_dir);
    }

    public string Describe() => $"local files at {_dir}";

    public async Task<MemoryRecord?> LoadAsync(string userId, CancellationToken ct = default)
    {
        var path = PathFor(userId);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<MemoryRecord>(stream, Json, ct);
    }

    public async Task SaveAsync(MemoryRecord record, CancellationToken ct = default)
    {
        await using var stream = File.Create(PathFor(record.UserId));
        await JsonSerializer.SerializeAsync(stream, record, Json, ct);
    }

    private string PathFor(string userId) => Path.Combine(_dir, Sanitize(userId) + ".json");

    private static string Sanitize(string userId) =>
        string.Concat(userId.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

    // Put .memory/ next to the project (the folder holding the .csproj), not under bin/.
    private static string DefaultDir()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (current.EnumerateFiles("*.csproj").Any())
            {
                return Path.Combine(current.FullName, ".memory");
            }

            current = current.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), ".memory");
    }
}
