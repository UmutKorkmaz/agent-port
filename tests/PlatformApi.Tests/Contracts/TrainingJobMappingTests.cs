using System.Text.Json;
using AgentPort.PlatformApi.Contracts;
using FluentAssertions;
using Xunit;

namespace AgentPort.PlatformApi.Tests.Contracts;

// WS-1 training correctness: the operator console lets users pick a task
// ("classification" / "regression") and a target column. Those choices must
// survive the trip into the ai-services /v1/train payload, whose TrainJobConfig
// expects snake_case fields `task` and `target_column`. ToConfig is the single
// mapping seam pinned here so the carry-through cannot silently regress.
public sealed class TrainingJobMappingTests
{
    private static CreateTrainingJobRequest BaseRequest(string? kind, string? targetColumn) => new(
        WorkspaceId: Guid.NewGuid(),
        ProjectId: Guid.NewGuid(),
        DatasetId: Guid.NewGuid(),
        DatasetVersionId: null,
        ModelRouteId: null,
        Name: "Price model",
        Slug: null,
        Kind: kind,
        ConfigJson: null,
        HyperparametersJson: null,
        EstimatedCost: null,
        TargetColumn: targetColumn);

    [Fact]
    public void TrainingJobConfig_Carries_Kind_And_TargetColumn()
    {
        var req = BaseRequest(kind: "regression", targetColumn: "price");

        var cfg = TrainingJobMapping.ToConfig(req);

        cfg.Task.Should().Be("regression");
        cfg.TargetColumn.Should().Be("price");
    }

    [Fact]
    public void ToConfig_Defaults_Task_To_Classification_When_Kind_Missing()
    {
        var req = BaseRequest(kind: null, targetColumn: "label");

        var cfg = TrainingJobMapping.ToConfig(req);

        cfg.Task.Should().Be("classification");
        cfg.TargetColumn.Should().Be("label");
    }

    [Fact]
    public void ToConfig_Serializes_To_AiServices_SnakeCase_Payload_Fields()
    {
        var req = BaseRequest(kind: "regression", targetColumn: "price");

        var json = JsonSerializer.Serialize(TrainingJobMapping.ToConfig(req));
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("task").GetString().Should().Be("regression");
        doc.RootElement.GetProperty("target_column").GetString().Should().Be("price");
    }
}
