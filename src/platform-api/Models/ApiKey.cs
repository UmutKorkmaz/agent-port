namespace AgentPort.PlatformApi.Data;

public sealed class ApiKey : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ServiceAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyType { get; set; } = "server";
    public List<string> Scopes { get; set; } = [];
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string AllowedOriginsJson { get; set; } = "[]";
    public string RateLimitJson { get; set; } = "{}";
    public string AuthMetadataJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ServiceAccount : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class SecretReference : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? EnvironmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class Policy : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "workspace";
    public string DocumentJson { get; set; } = "{}";
    public bool IsDefault { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? ActorServiceAccountId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
