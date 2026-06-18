namespace AgentPort.PlatformApi.Data;

public sealed class AgentRun : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? EnvironmentId { get; set; }
    public Guid? ApiKeyId { get; set; }
    public Guid AgentDefinitionId { get; set; }
    public Guid? KnowledgeBaseId { get; set; }
    public Guid? TraceRecordId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Status { get; set; } = "succeeded";
    public string FallbackMode { get; set; } = "extractive";
    public string AuthMode { get; set; } = "anonymous";
    public string AuthMetadataJson { get; set; } = "{}";
    public int TopK { get; set; } = 4;
    public decimal? ScoreThreshold { get; set; }
    public decimal? BestRetrievalScore { get; set; }
    public string QualityStatus { get; set; } = "not_evaluated";
    public string? NoAnswerReason { get; set; }
    public string CitationsJson { get; set; } = "[]";
    public string RetrievedChunksJson { get; set; } = "[]";
    public int LatencyMs { get; set; }
    public decimal EstimatedCost { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class EvalSuite : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? AgentDefinitionId { get; set; }
    public Guid? DatasetVersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}
