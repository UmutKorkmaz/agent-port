namespace AgentPort.PlatformApi.Data;

public sealed class Deployment : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid AgentDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = "planned";
    public string DeploymentType { get; set; } = "endpoint";
    public string AuthMode { get; set; } = "api_key";
    public string WidgetConfigJson { get; set; } = "{}";
    public string AllowedOriginsJson { get; set; } = "[]";
    public string RateLimitJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class UsageEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ServiceAccountId { get; set; }
    public Guid? ModelRouteId { get; set; }
    public Guid? TraceRecordId { get; set; }
    public string Metric { get; set; } = "tokens";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "token";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
