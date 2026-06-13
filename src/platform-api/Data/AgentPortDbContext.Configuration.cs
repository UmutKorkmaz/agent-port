using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentPort.PlatformApi.Data;

public sealed partial class AgentPortDbContext
{
    private static void ConfigureEntityBase<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : EntityBase
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
    }

    private static void ConfigureWorkspace(EntityTypeBuilder<Workspace> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("workspaces");
        entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => e.Slug).IsUnique();
    }

    private static void ConfigureProject(EntityTypeBuilder<Project> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("projects");
        entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUser(EntityTypeBuilder<User> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("users");
        entity.Property(e => e.Email).HasColumnType("citext").HasMaxLength(320).IsRequired();
        entity.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => e.Email).IsUnique();
    }

    private static void ConfigureMembership(EntityTypeBuilder<Membership> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("memberships");
        entity.Property(e => e.Role).HasMaxLength(64).IsRequired();
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.UserId }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureEnvironment(EntityTypeBuilder<AgentEnvironment> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("environments");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Kind).HasMaxLength(64).IsRequired();
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureApiKey(EntityTypeBuilder<ApiKey> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("api_keys");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Prefix).HasMaxLength(32).IsRequired();
        entity.Property(e => e.KeyHash).HasMaxLength(128).IsRequired();
        entity.Property(e => e.KeyType).HasMaxLength(64).IsRequired().HasDefaultValue("server");
        entity.Property(e => e.Scopes).HasColumnType("text[]");
        entity.Property(e => e.AllowedOriginsJson).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        entity.Property(e => e.RateLimitJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.AuthMetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => e.KeyHash).IsUnique();
        entity.HasIndex(e => e.Prefix).IsUnique();
        entity.HasIndex(e => new { e.WorkspaceId, e.Name });
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.KeyType });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ServiceAccount>().WithMany().HasForeignKey(e => e.ServiceAccountId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureServiceAccount(EntityTypeBuilder<ServiceAccount> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("service_accounts");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureSecretReference(EntityTypeBuilder<SecretReference> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("secret_references");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Provider).HasMaxLength(64).IsRequired();
        entity.Property(e => e.ExternalReference).HasMaxLength(512).IsRequired();
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.EnvironmentId, e.Name }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<AgentEnvironment>().WithMany().HasForeignKey(e => e.EnvironmentId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigurePolicy(EntityTypeBuilder<Policy> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("policies");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Kind).HasMaxLength(64).IsRequired();
        entity.Property(e => e.DocumentJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.Name }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureAuditEvent(EntityTypeBuilder<AuditEvent> entity)
    {
        entity.ToTable("audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.EventType).HasMaxLength(128).IsRequired();
        entity.Property(e => e.ResourceType).HasMaxLength(128).IsRequired();
        entity.Property(e => e.ResourceId).HasMaxLength(128);
        entity.Property(e => e.IpAddress).HasMaxLength(64);
        entity.Property(e => e.UserAgent).HasMaxLength(512);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        entity.HasIndex(e => new { e.WorkspaceId, e.CreatedAt });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<User>().WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ServiceAccount>().WithMany().HasForeignKey(e => e.ActorServiceAccountId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureModelProvider(EntityTypeBuilder<ModelProvider> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("model_providers");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Kind).HasMaxLength(64).IsRequired();
        entity.Property(e => e.BaseUrl).HasMaxLength(512);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.Name }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureModelRoute(EntityTypeBuilder<ModelRoute> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("model_routes");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.ModelName).HasMaxLength(256).IsRequired();
        entity.Property(e => e.RouteType).HasMaxLength(64).IsRequired();
        entity.Property(e => e.ParametersJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ModelProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureProviderPriceSnapshot(EntityTypeBuilder<ProviderPriceSnapshot> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("provider_price_snapshots");
        entity.Property(e => e.ModelName).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        entity.Property(e => e.InputTokenPricePerMillion).HasPrecision(18, 8);
        entity.Property(e => e.OutputTokenPricePerMillion).HasPrecision(18, 8);
        entity.Property(e => e.RequestPrice).HasPrecision(18, 8);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.ProviderId, e.ModelName, e.CapturedAt });
        entity.HasOne<ModelProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureProviderAccount(EntityTypeBuilder<ProviderAccount> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("provider_accounts");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.AccountType).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.ExternalAccountId).HasMaxLength(256);
        entity.Property(e => e.CapabilitiesJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.LimitsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProviderId, e.Slug }).IsUnique();
        entity.HasIndex(e => new { e.WorkspaceId, e.Status, e.IsEnabled });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ModelProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<SecretReference>().WithMany().HasForeignKey(e => e.CredentialSecretReferenceId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureModelCatalogEntry(EntityTypeBuilder<ModelCatalogEntry> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("model_catalog_entries");
        entity.Property(e => e.ModelName).HasMaxLength(256).IsRequired();
        entity.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Modality).HasMaxLength(64).IsRequired();
        entity.Property(e => e.RouteType).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.CapabilitiesJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.ProviderId, e.WorkspaceId, e.ModelName }).IsUnique();
        entity.HasIndex(e => new { e.WorkspaceId, e.IsEnabled, e.Status });
        entity.HasIndex(e => new { e.ProviderId, e.RouteType, e.IsEnabled });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<ModelProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureProviderStatusCheck(EntityTypeBuilder<ProviderStatusCheck> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("provider_status_checks");
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.CheckType).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Target).HasMaxLength(512);
        entity.Property(e => e.ErrorCode).HasMaxLength(128);
        entity.Property(e => e.ErrorMessage).HasColumnType("text");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProviderId, e.CheckedAt });
        entity.HasIndex(e => new { e.ProviderAccountId, e.CheckedAt });
        entity.HasIndex(e => new { e.ModelRouteId, e.CheckedAt });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<ModelProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<ProviderAccount>().WithMany().HasForeignKey(e => e.ProviderAccountId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ModelRoute>().WithMany().HasForeignKey(e => e.ModelRouteId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureLocalModelInventory(EntityTypeBuilder<LocalModelInventory> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("local_model_inventory");
        entity.Property(e => e.ModelName).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Digest).HasMaxLength(256);
        entity.Property(e => e.Family).HasMaxLength(128);
        entity.Property(e => e.ParameterSize).HasMaxLength(64);
        entity.Property(e => e.Quantization).HasMaxLength(64);
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProviderId, e.ModelName }).IsUnique();
        entity.HasIndex(e => new { e.WorkspaceId, e.IsInstalled, e.Status });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<ModelProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureProviderSyncJob(EntityTypeBuilder<ProviderSyncJob> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("provider_sync_jobs");
        entity.Property(e => e.JobType).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.ErrorMessage).HasColumnType("text");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProviderId, e.CreatedAt });
        entity.HasIndex(e => new { e.ProviderAccountId, e.CreatedAt });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<ModelProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ProviderAccount>().WithMany().HasForeignKey(e => e.ProviderAccountId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureProviderUsageEvent(EntityTypeBuilder<ProviderUsageEvent> entity)
    {
        entity.ToTable("provider_usage_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.ModelName).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Operation).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        entity.Property(e => e.ProviderCost).HasPrecision(19, 6);
        entity.Property(e => e.PlatformFee).HasPrecision(19, 6);
        entity.Property(e => e.TotalCost).HasPrecision(19, 6);
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.OccurredAt });
        entity.HasIndex(e => new { e.ProviderId, e.ModelName, e.OccurredAt });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ModelProvider>().WithMany().HasForeignKey(e => e.ProviderId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ProviderAccount>().WithMany().HasForeignKey(e => e.ProviderAccountId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ModelRoute>().WithMany().HasForeignKey(e => e.ModelRouteId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<AgentDefinition>().WithMany().HasForeignKey(e => e.AgentDefinitionId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<TraceRecord>().WithMany().HasForeignKey(e => e.TraceRecordId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureAiSystem(EntityTypeBuilder<AiSystem> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("ai_systems");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(2048);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAgentDefinition(EntityTypeBuilder<AgentDefinition> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("agent_definitions");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Instructions).HasColumnType("text").IsRequired();
        entity.Property(e => e.ToolsJson).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<AiSystem>().WithMany().HasForeignKey(e => e.AiSystemId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ModelRoute>().WithMany().HasForeignKey(e => e.ModelRouteId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureDataset(EntityTypeBuilder<Dataset> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("datasets");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.Kind).HasMaxLength(64).IsRequired();
        entity.Property(e => e.License).HasMaxLength(128);
        entity.Property(e => e.Source).HasMaxLength(512);
        entity.Property(e => e.PiiClassification).HasMaxLength(64);
        entity.Property(e => e.ConsentFlagsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.SplitsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.SchemaJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.QualityNotes).HasColumnType("text");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureDatasetVersion(EntityTypeBuilder<DatasetVersion> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("dataset_versions");
        entity.Property(e => e.Version).HasMaxLength(64).IsRequired();
        entity.Property(e => e.StorageUri).HasMaxLength(1024);
        entity.Property(e => e.SchemaJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.DatasetId, e.Version }).IsUnique();
        entity.HasOne<Dataset>().WithMany().HasForeignKey(e => e.DatasetId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureKnowledgeBase(EntityTypeBuilder<KnowledgeBase> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("knowledge_bases");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.RetrievalStrategy).HasMaxLength(64).IsRequired();
        entity.Property(e => e.EmbeddingModel).HasMaxLength(255).IsRequired();
        entity.Property(e => e.VectorStore).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.DefaultTopK).HasDefaultValue(4);
        entity.Property(e => e.ScoreThreshold).HasPrecision(8, 6).HasDefaultValue(0m);
        entity.Property(e => e.NoAnswerFallback).HasMaxLength(512).IsRequired().HasDefaultValue("I could not find enough information in the ingested documents.");
        entity.Property(e => e.QualityConfigJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.QualityStatus).HasMaxLength(64).IsRequired().HasDefaultValue("not_evaluated");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Slug }).IsUnique();
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Status, e.QualityStatus });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Dataset>().WithMany().HasForeignKey(e => e.DatasetId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<AgentDefinition>().WithMany().HasForeignKey(e => e.AgentDefinitionId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureDocumentAsset(EntityTypeBuilder<DocumentAsset> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("document_assets");
        entity.Property(e => e.FileName).HasMaxLength(512).IsRequired();
        entity.Property(e => e.ContentType).HasMaxLength(255).IsRequired();
        entity.Property(e => e.ObjectKey).HasMaxLength(1024).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.ContentHash).HasMaxLength(64);
        entity.Property(e => e.DocumentVersion).HasDefaultValue(1);
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.DatasetId, e.CreatedAt });
        entity.HasIndex(e => new { e.KnowledgeBaseId, e.IsActive, e.Status, e.CreatedAt });
        entity.HasIndex(e => new { e.KnowledgeBaseId, e.ContentHash })
            .IsUnique()
            .HasFilter("\"ContentHash\" IS NOT NULL AND \"IsActive\" = TRUE");
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Dataset>().WithMany().HasForeignKey(e => e.DatasetId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<KnowledgeBase>().WithMany().HasForeignKey(e => e.KnowledgeBaseId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIngestionJob(EntityTypeBuilder<IngestionJob> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("ingestion_jobs");
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Operation).HasMaxLength(64).IsRequired().HasDefaultValue("ingest");
        entity.Property(e => e.ContentHash).HasMaxLength(64);
        entity.Property(e => e.DocumentVersion).HasDefaultValue(1);
        entity.Property(e => e.SkippedReason).HasMaxLength(128);
        entity.Property(e => e.ErrorMessage).HasColumnType("text");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.DatasetId, e.CreatedAt });
        entity.HasIndex(e => new { e.DatasetId, e.ContentHash, e.CreatedAt })
            .HasFilter("\"ContentHash\" IS NOT NULL");
        entity.HasIndex(e => new { e.DocumentAssetId, e.Operation, e.CreatedAt });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Dataset>().WithMany().HasForeignKey(e => e.DatasetId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<KnowledgeBase>().WithMany().HasForeignKey(e => e.KnowledgeBaseId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<DocumentAsset>().WithMany().HasForeignKey(e => e.DocumentAssetId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureDocumentChunk(EntityTypeBuilder<DocumentChunk> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("document_chunks");
        entity.Property(e => e.CitationId).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Text).HasColumnType("text").IsRequired();
        entity.Property(e => e.Section).HasMaxLength(255);
        entity.Property(e => e.Embedding).HasColumnType("vector(768)").IsRequired();
        entity.Property(e => e.ContentHash).HasMaxLength(64);
        entity.Property(e => e.DocumentVersion).HasDefaultValue(1);
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.KnowledgeBaseId, e.ChunkIndex });
        entity.HasIndex(e => new { e.KnowledgeBaseId, e.IsActive, e.ChunkIndex });
        entity.HasIndex(e => new { e.DocumentAssetId, e.IsActive, e.ChunkIndex });
        entity.HasIndex(e => e.CitationId)
            .IsUnique()
            .HasFilter("\"IsActive\" = TRUE");
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Dataset>().WithMany().HasForeignKey(e => e.DatasetId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<KnowledgeBase>().WithMany().HasForeignKey(e => e.KnowledgeBaseId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<DocumentAsset>().WithMany().HasForeignKey(e => e.DocumentAssetId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAgentRun(EntityTypeBuilder<AgentRun> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("agent_runs");
        entity.Property(e => e.Question).HasColumnType("text").IsRequired();
        entity.Property(e => e.Answer).HasColumnType("text").IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.FallbackMode).HasMaxLength(64).IsRequired();
        entity.Property(e => e.AuthMode).HasMaxLength(64).IsRequired().HasDefaultValue("anonymous");
        entity.Property(e => e.AuthMetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.TopK).HasDefaultValue(4);
        entity.Property(e => e.ScoreThreshold).HasPrecision(8, 6);
        entity.Property(e => e.BestRetrievalScore).HasPrecision(8, 6);
        entity.Property(e => e.QualityStatus).HasMaxLength(64).IsRequired().HasDefaultValue("not_evaluated");
        entity.Property(e => e.NoAnswerReason).HasMaxLength(256);
        entity.Property(e => e.CitationsJson).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        entity.Property(e => e.RetrievedChunksJson).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        entity.Property(e => e.EstimatedCost).HasPrecision(19, 6);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.CreatedAt });
        entity.HasIndex(e => new { e.AgentDefinitionId, e.CreatedAt });
        entity.HasIndex(e => new { e.EnvironmentId, e.CreatedAt });
        entity.HasIndex(e => new { e.ApiKeyId, e.CreatedAt });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<AgentEnvironment>().WithMany().HasForeignKey(e => e.EnvironmentId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ApiKey>().WithMany().HasForeignKey(e => e.ApiKeyId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<AgentDefinition>().WithMany().HasForeignKey(e => e.AgentDefinitionId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<KnowledgeBase>().WithMany().HasForeignKey(e => e.KnowledgeBaseId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<TraceRecord>().WithMany().HasForeignKey(e => e.TraceRecordId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureEvalSuite(EntityTypeBuilder<EvalSuite> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("eval_suites");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.ConfigJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<AgentDefinition>().WithMany().HasForeignKey(e => e.AgentDefinitionId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<DatasetVersion>().WithMany().HasForeignKey(e => e.DatasetVersionId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureTraceRecord(EntityTypeBuilder<TraceRecord> entity)
    {
        entity.ToTable("trace_records");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.CorrelationId).HasMaxLength(128);
        entity.Property(e => e.TraceType).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.CostAmount).HasPrecision(19, 6);
        entity.Property(e => e.AuthMode).HasMaxLength(64).IsRequired().HasDefaultValue("anonymous");
        entity.Property(e => e.AuthMetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.RateLimitDecisionJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.BudgetDecisionJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.PolicyDecisionId).HasMaxLength(128);
        entity.Property(e => e.TopK).HasDefaultValue(4);
        entity.Property(e => e.ScoreThreshold).HasPrecision(8, 6);
        entity.Property(e => e.BestRetrievalScore).HasPrecision(8, 6);
        entity.Property(e => e.QualityStatus).HasMaxLength(64).IsRequired().HasDefaultValue("not_evaluated");
        entity.Property(e => e.NoAnswerReason).HasMaxLength(256);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.CreatedAt });
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.QualityStatus, e.CreatedAt });
        entity.HasIndex(e => new { e.ApiKeyId, e.CreatedAt });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<AgentEnvironment>().WithMany().HasForeignKey(e => e.EnvironmentId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ApiKey>().WithMany().HasForeignKey(e => e.ApiKeyId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<AgentDefinition>().WithMany().HasForeignKey(e => e.AgentDefinitionId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ModelRoute>().WithMany().HasForeignKey(e => e.ModelRouteId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureDeployment(EntityTypeBuilder<Deployment> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("deployments");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Version).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.DeploymentType).HasMaxLength(64).IsRequired().HasDefaultValue("endpoint");
        entity.Property(e => e.AuthMode).HasMaxLength(64).IsRequired().HasDefaultValue("api_key");
        entity.Property(e => e.WidgetConfigJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.AllowedOriginsJson).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        entity.Property(e => e.RateLimitJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.EnvironmentId, e.Name, e.Version }).IsUnique();
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.DeploymentType, e.Status });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<AgentEnvironment>().WithMany().HasForeignKey(e => e.EnvironmentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<AgentDefinition>().WithMany().HasForeignKey(e => e.AgentDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUsageEvent(EntityTypeBuilder<UsageEvent> entity)
    {
        entity.ToTable("usage_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.Metric).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Unit).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        entity.Property(e => e.Quantity).HasPrecision(20, 6);
        entity.Property(e => e.Amount).HasPrecision(19, 6);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.OccurredAt });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ServiceAccount>().WithMany().HasForeignKey(e => e.ServiceAccountId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ModelRoute>().WithMany().HasForeignKey(e => e.ModelRouteId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<TraceRecord>().WithMany().HasForeignKey(e => e.TraceRecordId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureWalletAccount(EntityTypeBuilder<WalletAccount> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("wallet_accounts");
        entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Balance).HasPrecision(19, 4);
        entity.Property(e => e.ReservedBalance).HasPrecision(19, 4);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.Currency }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureWalletTransaction(EntityTypeBuilder<WalletTransaction> entity)
    {
        entity.ToTable("wallet_transactions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.Type).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        entity.Property(e => e.Amount).HasPrecision(19, 4);
        entity.Property(e => e.BalanceAfter).HasPrecision(19, 4);
        entity.Property(e => e.ExternalReference).HasMaxLength(256);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        entity.HasIndex(e => new { e.WalletAccountId, e.CreatedAt });
        entity.HasOne<WalletAccount>().WithMany().HasForeignKey(e => e.WalletAccountId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureWalletReservation(EntityTypeBuilder<WalletReservation> entity)
    {
        entity.ToTable("wallet_reservations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Amount).HasPrecision(19, 4);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        entity.HasIndex(e => new { e.WalletAccountId, e.Status });
        entity.HasOne<WalletAccount>().WithMany().HasForeignKey(e => e.WalletAccountId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurePaymentProviderConfig(EntityTypeBuilder<PaymentProviderConfig> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("payment_provider_configs");
        entity.Property(e => e.Provider).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Mode).HasMaxLength(32).IsRequired();
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.Provider, e.Mode }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<SecretReference>().WithMany().HasForeignKey(e => e.ApiKeySecretReferenceId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<SecretReference>().WithMany().HasForeignKey(e => e.WebhookSecretReferenceId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureTrainingJob(EntityTypeBuilder<TrainingJob> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("training_jobs");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.Kind).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.ConfigJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.HyperparametersJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.ArtifactsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetricsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.LogsJson).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        entity.Property(e => e.EstimatedCost).HasPrecision(19, 6);
        entity.Property(e => e.ActualCost).HasPrecision(19, 6);
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Dataset>().WithMany().HasForeignKey(e => e.DatasetId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<DatasetVersion>().WithMany().HasForeignKey(e => e.DatasetVersionId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ModelRoute>().WithMany().HasForeignKey(e => e.ModelRouteId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<WalletReservation>().WithMany().HasForeignKey(e => e.WalletReservationId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureExperiment(EntityTypeBuilder<Experiment> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("experiments");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.ConfigJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetricsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.ArtifactsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<DatasetVersion>().WithMany().HasForeignKey(e => e.DatasetVersionId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<AgentDefinition>().WithMany().HasForeignKey(e => e.AgentDefinitionId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<ModelRoute>().WithMany().HasForeignKey(e => e.ModelRouteId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureModelVersion(EntityTypeBuilder<ModelVersion> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("model_versions");
        entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
        entity.Property(e => e.Slug).HasMaxLength(96).IsRequired();
        entity.Property(e => e.Kind).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.ArtifactUri).HasMaxLength(1024);
        entity.Property(e => e.ConfigJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.CapabilitiesJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Slug }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<TrainingJob>().WithMany().HasForeignKey(e => e.TrainingJobId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<Experiment>().WithMany().HasForeignKey(e => e.ExperimentId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureModelAlias(EntityTypeBuilder<ModelAlias> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("model_aliases");
        entity.Property(e => e.Alias).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Description).HasMaxLength(512);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.Alias }).IsUnique();
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<ModelVersion>().WithMany().HasForeignKey(e => e.ModelVersionId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureEvalRun(EntityTypeBuilder<EvalRun> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("eval_runs");
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Score).HasPrecision(19, 6);
        entity.Property(e => e.Threshold).HasPrecision(19, 6);
        entity.Property(e => e.ResultsJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.Property(e => e.FailuresJson).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.ProjectId, e.EvalSuiteId, e.CreatedAt });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<EvalSuite>().WithMany().HasForeignKey(e => e.EvalSuiteId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<ModelVersion>().WithMany().HasForeignKey(e => e.ModelVersionId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<AgentDefinition>().WithMany().HasForeignKey(e => e.AgentDefinitionId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureHumanReviewRecord(EntityTypeBuilder<HumanReviewRecord> entity)
    {
        ConfigureEntityBase(entity);
        entity.ToTable("human_review_records");
        entity.Property(e => e.Queue).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
        entity.Property(e => e.Label).HasMaxLength(64);
        entity.Property(e => e.Severity).HasMaxLength(32);
        entity.Property(e => e.Reason).HasColumnType("text");
        entity.Property(e => e.FollowUpAction).HasColumnType("text");
        entity.Property(e => e.ReviewerId).HasMaxLength(128);
        entity.Property(e => e.ReviewerName).HasMaxLength(256);
        entity.Property(e => e.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        entity.HasIndex(e => new { e.WorkspaceId, e.Queue, e.Status, e.CreatedAt });
        entity.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<AgentRun>().WithMany().HasForeignKey(e => e.AgentRunId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne<TraceRecord>().WithMany().HasForeignKey(e => e.TraceRecordId).OnDelete(DeleteBehavior.SetNull);
    }
}
