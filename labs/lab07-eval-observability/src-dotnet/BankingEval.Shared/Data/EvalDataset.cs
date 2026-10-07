using System.Text.Json;

namespace BankingEval.Shared.Data;

/// <summary>Loads and saves the JSONL evaluation dataset / response files.</summary>
public static class EvalDataset
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static List<EvalCase> Load(string path)
    {
        var cases = new List<EvalCase>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var evalCase = JsonSerializer.Deserialize<EvalCase>(line, Options);
            if (evalCase is not null)
            {
                cases.Add(evalCase);
            }
        }

        return cases;
    }

    public static void Save(string path, IEnumerable<EvalCase> cases)
    {
        using var writer = new StreamWriter(path, append: false);
        foreach (var evalCase in cases)
        {
            writer.WriteLine(JsonSerializer.Serialize(evalCase, Options));
        }
    }

    /// <summary>
    /// Locates a file under the lab's <c>data/</c> folder by walking up from the
    /// running assembly, so the console works regardless of the current directory.
    /// </summary>
    public static string FindDataFile(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "data", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate data/{fileName} by walking up from {AppContext.BaseDirectory}");
    }
}
