using System.Net;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace BankingConcierge.Memory;

/// <summary>
/// Azure Cosmos DB backing for cross-session memory — the SHARED, multi-instance BYO tier.
/// Same <see cref="IMemoryStore"/> contract as <see cref="LocalFileStore"/>, so the app, the agents,
/// and the demo are byte-for-byte identical; only the store swaps. One document per customer,
/// partitioned by <c>/userId</c>, so a customer's whole memory lives in one logical partition and a
/// single point read serves recall.
///
/// <para><b>Auth is keyless by default.</b> Set <c>COSMOS_ENDPOINT</c> and the app authenticates with
/// <see cref="DefaultAzureCredential"/> — no keys in config. The identity needs the Cosmos DB
/// <b>Built-in Data Contributor</b> DATA-PLANE role; control-plane Owner/Contributor does NOT grant
/// data access (that 403 is the #1 keyless gotcha). For the local Cosmos emulator, set
/// <c>COSMOS_CONNECTION_STRING</c> instead (the emulator is key-only and serves a self-signed cert).
/// See <c>infra/provision-cosmos.ps1</c> and README Part E.</para>
/// </summary>
public sealed class CosmosStore : IMemoryStore, IAsyncDisposable
{
    private readonly CosmosClient _client;
    private readonly string _databaseId;
    private readonly string _containerId;
    private readonly string _endpointLabel;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private Container? _container;

    private CosmosStore(CosmosClient client, string databaseId, string containerId, string endpointLabel)
    {
        _client = client;
        _databaseId = databaseId;
        _containerId = containerId;
        _endpointLabel = endpointLabel;
    }

    public string Describe() => $"Azure Cosmos DB — {_databaseId}/{_containerId} at {_endpointLabel}";

    /// <summary>
    /// Builds a <see cref="CosmosStore"/> from the environment, or returns <c>null</c> with a
    /// <paramref name="reason"/> when Cosmos isn't configured — the caller then falls back to local
    /// with a visible warning. If Cosmos IS configured but unreachable/unauthorized, that surfaces as
    /// an exception on first use (fail loud), never a silent downgrade mid-demo.
    /// </summary>
    public static CosmosStore? FromEnvironment(out string reason)
    {
        var databaseId = Environment.GetEnvironmentVariable("COSMOS_DATABASE") ?? "agentmemory";
        var containerId = Environment.GetEnvironmentVariable("COSMOS_CONTAINER") ?? "conversations";
        var connectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");
        var endpoint = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT");

        // Emulator / dev: key-based connection string (the only thing the emulator accepts).
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            reason = string.Empty;
            var label = IsEmulator(connectionString) ? "local emulator" : "account (connection string)";
            return new CosmosStore(
                new CosmosClient(connectionString, BuildOptions(connectionString)), databaseId, containerId, label);
        }

        // Production: keyless. DefaultAzureCredential + the Cosmos Built-in Data Contributor role.
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            reason = string.Empty;
            return new CosmosStore(
                new CosmosClient(endpoint, new DefaultAzureCredential(), BuildOptions(endpoint)),
                databaseId, containerId, endpoint);
        }

        reason = "COSMOS_ENDPOINT (keyless) or COSMOS_CONNECTION_STRING (emulator) is not set";
        return null;
    }

    private static bool IsEmulator(string endpointOrConnectionString) =>
        endpointOrConnectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase)
        || endpointOrConnectionString.Contains("127.0.0.1", StringComparison.Ordinal);

    private static CosmosClientOptions BuildOptions(string endpointOrConnectionString)
    {
        var options = new CosmosClientOptions { ApplicationName = "BankingConcierge-Lab06" };

        // The local emulator serves a self-signed cert over its gateway — trust it (DEV ONLY).
        if (IsEmulator(endpointOrConnectionString))
        {
            options.ConnectionMode = ConnectionMode.Gateway;
            options.HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });
        }

        return options;
    }

    private async Task<Container> GetContainerAsync(CancellationToken ct)
    {
        if (_container is not null)
        {
            return _container;
        }

        await _initLock.WaitAsync(ct);
        try
        {
            // Serverless-friendly: create with NO throughput. Passing throughput fails on a
            // serverless account; the IaC (infra/provision-cosmos.ps1) creates these up front, so
            // this is normally a no-op that also lets a first run bootstrap its own container.
            _container ??= (await (await _client.CreateDatabaseIfNotExistsAsync(_databaseId, cancellationToken: ct))
                    .Database.CreateContainerIfNotExistsAsync(
                        new ContainerProperties(_containerId, "/userId"), cancellationToken: ct))
                .Container;
        }
        finally
        {
            _initLock.Release();
        }

        return _container;
    }

    public async Task<MemoryRecord?> LoadAsync(string userId, CancellationToken ct = default)
    {
        var container = await GetContainerAsync(ct);
        try
        {
            var response = await container.ReadItemAsync<Doc>(userId, new PartitionKey(userId), cancellationToken: ct);
            return response.Resource.Record;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task SaveAsync(MemoryRecord record, CancellationToken ct = default)
    {
        var container = await GetContainerAsync(ct);
        var doc = new Doc { Id = record.UserId, UserId = record.UserId, Record = record };
        await container.UpsertItemAsync(doc, new PartitionKey(record.UserId), cancellationToken: ct);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        _initLock.Dispose();
        return ValueTask.CompletedTask;
    }

    // Cosmos requires a lowercase `id`. We key it by userId (one doc per customer) and partition by
    // /userId; the full MemoryRecord rides along under `record`, so the shared model is untouched.
    private sealed class Doc
    {
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;

        [JsonProperty("userId")] public string UserId { get; set; } = string.Empty;

        [JsonProperty("record")] public MemoryRecord Record { get; set; } = new();
    }
}
