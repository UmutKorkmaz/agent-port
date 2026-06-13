namespace AgentPort.PlatformApi.Data;

public sealed class Workspace : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class Project : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class User : EntityBase
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class Membership : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "member";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class AgentEnvironment : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Kind { get; set; } = "development";
    public bool IsDefault { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
