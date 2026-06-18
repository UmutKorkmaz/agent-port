namespace AgentPort.PlatformApi.Contracts;

// Dataset card enhancements
public sealed record UpdateDatasetCardRequest(
    string? License,
    string? Source,
    string? PiiClassification,
    bool? IsGolden,
    bool? AllowsTraining,
    bool? AllowsEval,
    string? ConsentFlagsJson,
    string? SplitsJson,
    string? SchemaJson,
    string? QualityNotes);

// Training Job
public sealed record TrainingJobResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? DatasetId,
    Guid? DatasetVersionId,
    Guid? ModelRouteId,
    Guid? WalletReservationId,
    string Name,
    string Slug,
    string Kind,
    string Status,
    string ConfigJson,
    string HyperparametersJson,
    string ArtifactsJson,
    string MetricsJson,
    decimal EstimatedCost,
    decimal ActualCost,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateTrainingJobRequest(
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? DatasetId,
    Guid? DatasetVersionId,
    Guid? ModelRouteId,
    string Name,
    string? Slug,
    string? Kind,
    string? ConfigJson,
    string? HyperparametersJson,
    decimal? EstimatedCost,
    string? TargetColumn = null);

public sealed record PatchTrainingJobRequest(
    string? Status,
    string? MetricsJson,
    string? ArtifactsJson,
    string? LogsJson,
    decimal? ActualCost,
    string? FailureReason);

// Experiment
public sealed record ExperimentResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? DatasetVersionId,
    Guid? AgentDefinitionId,
    Guid? ModelRouteId,
    string Name,
    string Slug,
    string Status,
    string ConfigJson,
    string MetricsJson,
    string ArtifactsJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateExperimentRequest(
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? DatasetVersionId,
    Guid? AgentDefinitionId,
    Guid? ModelRouteId,
    string Name,
    string? Slug,
    string? ConfigJson);

// Model Version
public sealed record ModelVersionResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? TrainingJobId,
    Guid? ExperimentId,
    string Name,
    string Slug,
    string Kind,
    string Status,
    string? ArtifactUri,
    string ConfigJson,
    string CapabilitiesJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateModelVersionRequest(
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? TrainingJobId,
    Guid? ExperimentId,
    string Name,
    string? Slug,
    string? Kind,
    string? Status,
    string? ArtifactUri,
    string? ConfigJson,
    string? CapabilitiesJson);

// Model Alias
public sealed record ModelAliasResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid ModelVersionId,
    string Alias,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SetModelAliasRequest(
    Guid WorkspaceId,
    Guid ProjectId,
    Guid ModelVersionId,
    string Alias,
    string? Description);

// Eval Run
public sealed record EvalRunResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid EvalSuiteId,
    Guid? ModelVersionId,
    Guid? AgentDefinitionId,
    string Status,
    decimal? Score,
    decimal? Threshold,
    bool Passed,
    string ResultsJson,
    string FailuresJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateEvalRunRequest(
    Guid WorkspaceId,
    Guid ProjectId,
    Guid EvalSuiteId,
    Guid? ModelVersionId,
    Guid? AgentDefinitionId,
    decimal? Threshold);

public sealed record PatchEvalRunRequest(
    string? Status,
    decimal? Score,
    bool? Passed,
    string? ResultsJson,
    string? FailuresJson);

// Human Review Record
public sealed record HumanReviewRecordResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? ProjectId,
    Guid? AgentRunId,
    Guid? TraceRecordId,
    string Queue,
    string Status,
    string? Label,
    string? Severity,
    string? Reason,
    string? FollowUpAction,
    string? ReviewerId,
    string? ReviewerName,
    DateTime? ReviewedAt,
    bool CanReuseForTraining,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateHumanReviewRecordRequest(
    Guid WorkspaceId,
    Guid? ProjectId,
    Guid? AgentRunId,
    Guid? TraceRecordId,
    string Queue,
    string? Label,
    string? Severity,
    string? Reason,
    string? FollowUpAction);

public sealed record PatchHumanReviewRecordRequest(
    string? Status,
    string? Label,
    string? Reason,
    string? FollowUpAction,
    string? ReviewerId,
    string? ReviewerName,
    bool? CanReuseForTraining);
