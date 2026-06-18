namespace AgentPort.PlatformApi.Data;

public sealed class Dataset : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Kind { get; set; } = "documents";
    public string? License { get; set; }
    public string? Source { get; set; }
    public string? PiiClassification { get; set; }
    public bool IsGolden { get; set; }
    public bool AllowsTraining { get; set; } = true;
    public bool AllowsEval { get; set; } = true;
    public string ConsentFlagsJson { get; set; } = "{}";
    public string SplitsJson { get; set; } = "{}";
    public string SchemaJson { get; set; } = "{}";
    public string? QualityNotes { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class DatasetVersion : EntityBase
{
    public Guid DatasetId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? StorageUri { get; set; }
    public string SchemaJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}
