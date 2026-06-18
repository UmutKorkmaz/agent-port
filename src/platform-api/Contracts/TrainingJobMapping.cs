using System.Text.Json.Serialization;

namespace AgentPort.PlatformApi.Contracts;

// Outbound config carried into the ai-services /v1/train payload. Field names
// (snake_case) and defaults mirror ai-services agentport.models.TrainJobConfig
// so the platform-api request and the Python validator agree on the wire shape.
public sealed record TrainJobConfig(
    [property: JsonPropertyName("target_column")] string TargetColumn,
    [property: JsonPropertyName("task")] string Task)
{
    [JsonPropertyName("test_size")]
    public double TestSize { get; init; } = 0.2;

    [JsonPropertyName("random_state")]
    public int RandomState { get; init; } = 42;

    [JsonPropertyName("max_features")]
    public int MaxFeatures { get; init; } = 1000;

    [JsonPropertyName("model_type")]
    public string ModelType { get; init; } = "logistic_regression";
}

// Single mapping seam from the operator-console training request to both the
// persisted training-job config and the ai-services /v1/train payload. Keeping
// it isolated lets the WS-1 carry-through (Kind -> task, TargetColumn ->
// target_column) be unit-tested without a live ai-services round trip.
public static class TrainingJobMapping
{
    public const string DefaultTask = "classification";

    public static string NormalizeTask(string? kind) =>
        string.IsNullOrWhiteSpace(kind) ? DefaultTask : kind.Trim();

    public static TrainJobConfig ToConfig(CreateTrainingJobRequest request) => new(
        TargetColumn: request.TargetColumn?.Trim() ?? string.Empty,
        Task: NormalizeTask(request.Kind));
}
