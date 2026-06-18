namespace AgentPort.PlatformApi.Data;

public sealed class ModelProvider : EntityBase
{
    public Guid? WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ModelRoute : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid ProviderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string RouteType { get; set; } = "chat";
    public int Priority { get; set; } = 100;
    public bool IsDefault { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string ParametersJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ProviderPriceSnapshot : EntityBase
{
    public Guid ProviderId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal InputTokenPricePerMillion { get; set; }
    public decimal OutputTokenPricePerMillion { get; set; }
    public decimal RequestPrice { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ProviderAccount : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid? CredentialSecretReferenceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string AccountType { get; set; } = "workspace";
    public string Status { get; set; } = "configured";
    public bool IsEnabled { get; set; } = true;
    public string? ExternalAccountId { get; set; }
    public DateTime? LastValidatedAt { get; set; }
    public string CapabilitiesJson { get; set; } = "{}";
    public string LimitsJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ModelCatalogEntry : EntityBase
{
    public Guid? WorkspaceId { get; set; }
    public Guid ProviderId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Modality { get; set; } = "text";
    public string RouteType { get; set; } = "chat";
    public int? ContextWindowTokens { get; set; }
    public int? MaxOutputTokens { get; set; }
    public bool SupportsTools { get; set; }
    public bool SupportsJsonMode { get; set; }
    public bool SupportsStreaming { get; set; } = true;
    public bool IsLocal { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string Status { get; set; } = "available";
    public DateTime? PublishedAt { get; set; }
    public DateTime? DeprecatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string CapabilitiesJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ProviderStatusCheck : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid? ProviderAccountId { get; set; }
    public Guid? ModelRouteId { get; set; }
    public string Status { get; set; } = "unknown";
    public string CheckType { get; set; } = "manual";
    public string? Target { get; set; }
    public int? LatencyMs { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class LocalModelInventory : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProviderId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string? Digest { get; set; }
    public string? Family { get; set; }
    public string? ParameterSize { get; set; }
    public string? Quantization { get; set; }
    public long? SizeBytes { get; set; }
    public bool IsInstalled { get; set; }
    public string Status { get; set; } = "unknown";
    public DateTime? LastSeenAt { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ProviderSyncJob : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? ProviderAccountId { get; set; }
    public string JobType { get; set; } = "catalog_sync";
    public string Status { get; set; } = "queued";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int DiscoveredModels { get; set; }
    public int UpsertedModels { get; set; }
    public string? ErrorMessage { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ProviderUsageEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid? ProviderAccountId { get; set; }
    public Guid? ModelRouteId { get; set; }
    public Guid? AgentDefinitionId { get; set; }
    public Guid? TraceRecordId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Operation { get; set; } = "chat";
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public int RequestCount { get; set; } = 1;
    public string Currency { get; set; } = "USD";
    public decimal ProviderCost { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = "recorded";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
