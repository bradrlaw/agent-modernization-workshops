using System.Text.Json.Serialization;

namespace BankingEval.Shared.Data;

/// <summary>
/// One row of the shared evaluation dataset (data/eval-dataset.jsonl). The same
/// shape is reused for generated responses — <see cref="Response"/> is null in
/// the dataset and populated once the agent has answered.
/// </summary>
public sealed class EvalCase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("customer_id")]
    public string CustomerId { get; set; } = "CUST-1001";

    [JsonPropertyName("query")]
    public string Query { get; set; } = "";

    [JsonPropertyName("context")]
    public string Context { get; set; } = "";

    [JsonPropertyName("ground_truth")]
    public string GroundTruth { get; set; } = "";

    [JsonPropertyName("expected_tools")]
    public List<string> ExpectedTools { get; set; } = new();

    [JsonPropertyName("response")]
    public string? Response { get; set; }
}
