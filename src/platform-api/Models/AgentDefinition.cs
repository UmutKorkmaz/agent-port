namespace AgentPort.PlatformApi.Data;

public sealed class AiSystem : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class AgentDefinition : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? AiSystemId { get; set; }
    public Guid? ModelRouteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string Instructions { get; set; } = string.Empty;
    public string ToolsJson { get; set; } = "[]";
    public string MetadataJson { get; set; } = "{}";
}
