namespace AgentPort.PlatformApi.Contracts;

public sealed record ModelProviderCatalogResponse(
    Guid Id,
    Guid? WorkspaceId,
    string Name,
    string Kind,
    string? BaseUrl,
    bool IsEnabled,
    int AccountCount,
    int EnabledAccountCount,
    int CatalogModelCount,
    string LatestStatus,
    DateTime? LatestStatusCheckedAt,
    string MetadataJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateModelProviderCatalogRequest(
    Guid? WorkspaceId,
    string Name,
    string Kind,
    string? BaseUrl,
    bool? IsEnabled,
    string? MetadataJson,
    string? AccountName,
    Guid? ProjectId,
    Guid? CredentialSecretReferenceId);

public sealed record PatchModelProviderCatalogRequest(
    Guid? Id,
    Guid? WorkspaceId,
    string? Name,
    string? Kind,
    string? BaseUrl,
    bool? IsEnabled,
    string? MetadataJson);

public sealed record ProviderAccountResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? ProjectId,
    Guid ProviderId,
    Guid? CredentialSecretReferenceId,
    string Name,
    string Slug,
    string AccountType,
    string Status,
    bool IsEnabled,
    DateTime? LastValidatedAt,
    string MetadataJson);

public sealed record ModelCatalogEntryResponse(
    Guid Id,
    Guid? WorkspaceId,
    Guid ProviderId,
    string ProviderName,
    string ProviderKind,
    string ModelName,
    string DisplayName,
    string Modality,
    string RouteType,
    int? ContextWindowTokens,
    int? MaxOutputTokens,
    bool SupportsTools,
    bool SupportsJsonMode,
    bool SupportsStreaming,
    bool IsLocal,
    bool IsEnabled,
    string Status,
    string CapabilitiesJson,
    string MetadataJson,
    ModelCostQuoteResponse CostQuote,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ModelCostQuoteResponse(
    string Currency,
    bool HasPriceSnapshot,
    decimal? InputTokenPricePerMillion,
    decimal? OutputTokenPricePerMillion,
    decimal? RequestPrice,
    decimal PlatformFeeFixedUsd,
    long InputTokens,
    long OutputTokens,
    decimal ProviderCostUsd,
    decimal EstimatedTotalUsd,
    DateTime? PriceCapturedAt);

public sealed record ProviderStatusResponse(
    Guid ProviderId,
    Guid? WorkspaceId,
    string ProviderName,
    string ProviderKind,
    bool ProviderEnabled,
    Guid? ProviderAccountId,
    Guid? ModelRouteId,
    string Status,
    string CheckType,
    string? Target,
    int? LatencyMs,
    string? ErrorCode,
    string? ErrorMessage,
    DateTime? CheckedAt,
    string MetadataJson);

public sealed record LocalModelInventoryResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid ProviderId,
    string ProviderName,
    string ModelName,
    string? Digest,
    string? Family,
    string? ParameterSize,
    string? Quantization,
    long? SizeBytes,
    bool IsInstalled,
    string Status,
    DateTime? LastSeenAt,
    string MetadataJson,
    ModelCostQuoteResponse CostQuote);

public sealed record TestModelRouteRequest(
    string? Prompt,
    long? InputTokens,
    long? OutputTokens,
    bool? PersistUsageEvent);

public sealed record ModelRouteTestResponse(
    Guid ModelRouteId,
    Guid WorkspaceId,
    Guid? ProjectId,
    Guid ProviderId,
    string ProviderName,
    string ModelName,
    string RouteType,
    string Status,
    string Message,
    bool ExecutedLiveProviderCall,
    Guid ProviderStatusCheckId,
    Guid? ProviderUsageEventId,
    ModelCostQuoteResponse CostQuote,
    DateTime CheckedAt);

public sealed record PatchAgentDefinitionModelRouteRequest(
    Guid? ModelRouteId);

public sealed record AgentDefinitionModelRouteResponse(
    Guid AgentDefinitionId,
    Guid WorkspaceId,
    Guid ProjectId,
    Guid? ModelRouteId,
    ModelRouteResponse? ModelRoute,
    ModelCostQuoteResponse? CostQuote,
    DateTime UpdatedAt);
