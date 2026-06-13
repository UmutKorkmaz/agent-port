namespace AgentPort.PlatformApi.Data;

public sealed class TrainingJob : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? DatasetId { get; set; }
    public Guid? DatasetVersionId { get; set; }
    public Guid? ModelRouteId { get; set; }
    public Guid? WalletReservationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Kind { get; set; } = "classification";
    public string Status { get; set; } = "queued";
    public string ConfigJson { get; set; } = "{}";
    public string HyperparametersJson { get; set; } = "{}";
    public string ArtifactsJson { get; set; } = "{}";
    public string MetricsJson { get; set; } = "{}";
    public string LogsJson { get; set; } = "[]";
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class Experiment : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? DatasetVersionId { get; set; }
    public Guid? AgentDefinitionId { get; set; }
    public Guid? ModelRouteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string ConfigJson { get; set; } = "{}";
    public string MetricsJson { get; set; } = "{}";
    public string ArtifactsJson { get; set; } = "{}";
}

public sealed class ModelVersion : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? TrainingJobId { get; set; }
    public Guid? ExperimentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Kind { get; set; } = "classifier";
    public string Status { get; set; } = "candidate";
    public string? ArtifactUri { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public string CapabilitiesJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ModelAlias : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ModelVersionId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class EvalRun : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid EvalSuiteId { get; set; }
    public Guid? ModelVersionId { get; set; }
    public Guid? AgentDefinitionId { get; set; }
    public string Status { get; set; } = "queued";
    public decimal? Score { get; set; }
    public decimal? Threshold { get; set; }
    public bool Passed { get; set; }
    public string ResultsJson { get; set; } = "{}";
    public string FailuresJson { get; set; } = "[]";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class HumanReviewRecord : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? AgentRunId { get; set; }
    public Guid? TraceRecordId { get; set; }
    public string Queue { get; set; } = "failed_eval";
    public string Status { get; set; } = "pending";
    public string? Label { get; set; }
    public string? Severity { get; set; }
    public string? Reason { get; set; }
    public string? FollowUpAction { get; set; }
    public string? ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool CanReuseForTraining { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
