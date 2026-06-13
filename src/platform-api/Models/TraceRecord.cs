namespace AgentPort.PlatformApi.Data;

public sealed class TraceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? EnvironmentId { get; set; }
    public Guid? ApiKeyId { get; set; }
    public Guid? AgentDefinitionId { get; set; }
    public Guid? ModelRouteId { get; set; }
    public string? CorrelationId { get; set; }
    public string TraceType { get; set; } = "run";
    public string Status { get; set; } = "started";
    public string AuthMode { get; set; } = "anonymous";
    public string AuthMetadataJson { get; set; } = "{}";
    public string RateLimitDecisionJson { get; set; } = "{}";
    public string BudgetDecisionJson { get; set; } = "{}";
    public string? PolicyDecisionId { get; set; }
    public int TopK { get; set; } = 4;
    public decimal? ScoreThreshold { get; set; }
    public decimal? BestRetrievalScore { get; set; }
    public string QualityStatus { get; set; } = "not_evaluated";
    public string? NoAnswerReason { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public decimal CostAmount { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
