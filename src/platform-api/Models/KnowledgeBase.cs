namespace AgentPort.PlatformApi.Data;

public sealed class KnowledgeBase : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid DatasetId { get; set; }
    public Guid? AgentDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string RetrievalStrategy { get; set; } = "basic-rag";
    public string EmbeddingModel { get; set; } = "agentport-local-hash-64";
    public string VectorStore { get; set; } = "pgvector";
    public string Status { get; set; } = "ready";
    public int DefaultTopK { get; set; } = 4;
    public decimal ScoreThreshold { get; set; }
    public string NoAnswerFallback { get; set; } = "I could not find enough information in the ingested documents.";
    public string QualityConfigJson { get; set; } = "{}";
    public string QualityStatus { get; set; } = "not_evaluated";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class DocumentAsset : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid DatasetId { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string ObjectKey { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";
    public string? ContentHash { get; set; }
    public int DocumentVersion { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime? LastIngestedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long SizeBytes { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class IngestionJob : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid DatasetId { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public Guid DocumentAssetId { get; set; }
    public string Status { get; set; } = "queued";
    public string Operation { get; set; } = "ingest";
    public string? ContentHash { get; set; }
    public int DocumentVersion { get; set; } = 1;
    public string? SkippedReason { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ChunkCount { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class DocumentChunk : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid DatasetId { get; set; }
    public Guid KnowledgeBaseId { get; set; }
    public Guid DocumentAssetId { get; set; }
    public int ChunkIndex { get; set; }
    public string CitationId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int TokenEstimate { get; set; }
    public int? PageNumber { get; set; }
    public string? Section { get; set; }
    public string Embedding { get; set; } = string.Empty;
    public string? ContentHash { get; set; }
    public int DocumentVersion { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime? InactiveAt { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
