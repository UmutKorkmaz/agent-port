namespace AgentPort.PlatformApi.Contracts;

public sealed record StatusResponse(
    string Service,
    string Version,
    string Status,
    DateTime Timestamp);

public sealed record WorkspaceResponse(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateWorkspaceRequest(
    string Name,
    string? Slug);

public sealed record ProjectResponse(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string Slug,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateProjectRequest(
    Guid WorkspaceId,
    string Name,
    string? Slug);

public sealed record ModelRouteResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? ProjectId,
    Guid ProviderId,
    string Name,
    string Slug,
    string ModelName,
    string RouteType,
    int Priority,
    bool IsDefault,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateModelRouteRequest(
    Guid WorkspaceId,
    Guid? ProjectId,
    Guid? ProviderId,
    string? ProviderName,
    string Name,
    string? Slug,
    string ModelName,
    string? RouteType,
    int? Priority,
    bool? IsDefault,
    bool? IsEnabled);

public sealed record AgentDefinitionResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? AiSystemId,
    Guid? ModelRouteId,
    string Name,
    string Slug,
    string Status,
    string Instructions,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record DatasetResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? KnowledgeBaseId,
    string Name,
    string Slug,
    string Kind,
    int DocumentCount,
    int ChunkCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateDatasetRequest(
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? AgentDefinitionId,
    string Name,
    string? Slug,
    string? Kind);

public sealed record IngestionJobResponse(
    Guid Id,
    Guid DatasetId,
    Guid KnowledgeBaseId,
    Guid DocumentAssetId,
    string Status,
    int ChunkCount,
    string? ErrorMessage,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);

public sealed record AgentChatRequest(
    string Question,
    int? TopK,
    decimal? ScoreThreshold,
    Guid? ModelRouteId);

public sealed record AgentRunResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid AgentDefinitionId,
    Guid? KnowledgeBaseId,
    Guid? TraceRecordId,
    string Question,
    string Answer,
    string Status,
    string FallbackMode,
    string CitationsJson,
    string RetrievedChunksJson,
    int LatencyMs,
    decimal EstimatedCost,
    DateTime CreatedAt);

public sealed record TraceRecordResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? AgentDefinitionId,
    Guid? ModelRouteId,
    string? CorrelationId,
    string TraceType,
    string Status,
    long InputTokens,
    long OutputTokens,
    decimal CostAmount,
    DateTime StartedAt,
    DateTime? EndedAt,
    string MetadataJson,
    DateTime CreatedAt);

public sealed record CreateAgentDefinitionRequest(
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? AiSystemId,
    Guid? ModelRouteId,
    string Name,
    string? Slug,
    string? Status,
    string? Instructions);

public sealed record PaymentProviderConfigResponse(
    Guid Id,
    Guid WorkspaceId,
    string Provider,
    string Mode,
    bool IsEnabled,
    Guid? ApiKeySecretReferenceId,
    Guid? WebhookSecretReferenceId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record LocalBootstrapResponse(
    Guid WorkspaceId,
    Guid OwnerUserId,
    Guid ProjectId,
    Guid EnvironmentId,
    Guid ServiceAccountId,
    Guid PolicyId,
    Guid ModelProviderId,
    Guid ModelRouteId,
    Guid AiSystemId,
    Guid AgentDefinitionId,
    Guid DatasetId,
    Guid KnowledgeBaseId,
    Guid WalletAccountId,
    IReadOnlyList<Guid> PaymentProviderConfigIds,
    string? ApiKey,
    string ApiKeyMessage);
