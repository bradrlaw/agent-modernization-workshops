using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace BankingAssistant.Shared.Configuration;

public sealed record AzureOpenAiSettings(string Endpoint, string DeploymentName);

public static class AppSupport
{
    public static void LoadDotEnv()
    {
        var envPath = FindFileInParents(".env")
                      ?? FindFileInParents(Path.Combine("src", ".env"));
        if (envPath is null || !File.Exists(envPath))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    public static AzureOpenAiSettings LoadAzureOpenAiSettings(string? appSettingsFileName = "appsettings.json")
    {
        LoadDotEnv();

        string? endpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT");
        string? deploymentName = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME");

        var appSettingsPath = FindFileInParents(appSettingsFileName ?? "appsettings.json");
        if (appSettingsPath is not null && File.Exists(appSettingsPath))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
            if (document.RootElement.TryGetProperty("AzureOpenAI", out var azureOpenAiSection))
            {
                endpoint ??= TryGetString(azureOpenAiSection, "Endpoint");
                deploymentName ??= TryGetString(azureOpenAiSection, "DeploymentName");
            }
        }

        endpoint = string.IsNullOrWhiteSpace(endpoint) ? null : endpoint;
        deploymentName = string.IsNullOrWhiteSpace(deploymentName) ? null : deploymentName;

        if (endpoint is null)
        {
            throw new InvalidOperationException("PROJECT_ENDPOINT or AzureOpenAI:Endpoint must be configured.");
        }

        if (deploymentName is null)
        {
            throw new InvalidOperationException("MODEL_DEPLOYMENT_NAME or AzureOpenAI:DeploymentName must be configured.");
        }

        return new AzureOpenAiSettings(endpoint, deploymentName);
    }

    public static ChatClient CreateChatClient(AzureOpenAiSettings settings)
    {
        TokenCredential credential;
        try
        {
            credential = new DefaultAzureCredential();
            credential.GetToken(new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]), default);
        }
        catch
        {
            credential = new AzureCliCredential();
        }

        var client = new AzureOpenAIClient(new Uri(settings.Endpoint), credential);
        return client.GetChatClient(settings.DeploymentName);
    }

    public static string? FindFileInParents(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
