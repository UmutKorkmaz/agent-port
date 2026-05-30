namespace AgentPort.PlatformApi.Contracts;

public sealed record DevSeedStatusResponse(
    bool IsSeeded,
    Guid? WorkspaceId,
    Guid? ProjectId,
    Guid? EnvironmentId,
    Guid? AgentDefinitionId,
    Guid? DatasetId,
    Guid? KnowledgeBaseId,
    int DocumentCount,
    int ChunkCount,
    int IngestionJobCount,
    int RunCount,
    int TraceCount,
    int ApiKeyCount,
    DateTime CheckedAt);

public sealed record DevResetRequest(
    bool? Reseed);

public sealed record DevResetResponse(
    string Status,
    int ClearedWorkspaces,
    int ClearedUsers,
    bool Reseeded,
    LocalBootstrapResponse? Seed,
    DateTime CompletedAt);

public sealed record DocumentAssetResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid DatasetId,
    Guid KnowledgeBaseId,
    string FileName,
    string ContentType,
    string ObjectKey,
    string Status,
    long SizeBytes,
    int ChunkCount,
    Guid? LatestIngestionJobId,
    string? LatestIngestionStatus,
    DateTime? LastIngestedAt,
    string MetadataJson,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? ContentHash,
    int DocumentVersion,
    bool IsActive,
    DateTime? ArchivedAt,
    DateTime? DeletedAt);

public sealed record DeleteDocumentResponse(
    Guid DocumentAssetId,
    string Status,
    int DeletedChunkCount,
    DateTime DeletedAt,
    string Mode);

public sealed record ReingestDocumentRequest(
    bool? Force);

public sealed record ReingestDocumentResponse(
    Guid DocumentAssetId,
    Guid IngestionJobId,
    string Status,
    string Message,
    DateTime QueuedAt);

public sealed record ValidateApiKeyRequest(
    string? ApiKey,
    Guid? WorkspaceId,
    string? RequiredScope);

public sealed record ApiKeyValidationResponse(
    bool Valid,
    Guid ApiKeyId,
    Guid WorkspaceId,
    Guid? ProjectId,
    Guid? UserId,
    Guid? ServiceAccountId,
    string Prefix,
    IReadOnlyList<string> Scopes,
    string? RequiredScope,
    bool HasRequiredScope,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    DateTime ValidatedAt);

public sealed record WidgetConfigResponse(
    Guid? DeploymentId,
    Guid AgentDefinitionId,
    Guid WorkspaceId,
    Guid ProjectId,
    string ApiBaseUrl,
    string ChatPath,
    IReadOnlyList<string> AllowedOrigins,
    string RateLimit,
    bool RequiresSession,
    string EmbedSnippet,
    string MetadataJson);

public sealed record CreateWidgetSessionRequest(
    Guid? DeploymentId,
    Guid? AgentDefinitionId,
    string? Origin);

public sealed record WidgetSessionResponse(
    string SessionToken,
    Guid? DeploymentId,
    Guid AgentDefinitionId,
    Guid WorkspaceId,
    Guid ProjectId,
    DateTime ExpiresAt,
    string Scope,
    string Status);
