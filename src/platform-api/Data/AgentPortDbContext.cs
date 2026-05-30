using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentPort.PlatformApi.Data;

public sealed class AgentPortDbContext : DbContext
{
    public AgentPortDbContext(DbContextOptions<AgentPortDbContext> options) : base(options) { }

    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<AgentEnvironment> Environments => Set<AgentEnvironment>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ServiceAccount> ServiceAccounts => Set<ServiceAccount>();
    public DbSet<SecretReference> SecretReferences => Set<SecretReference>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ModelProvider> ModelProviders => Set<ModelProvider>();
    public DbSet<ModelRoute> ModelRoutes => Set<ModelRoute>();
    public DbSet<ProviderPriceSnapshot> ProviderPriceSnapshots => Set<ProviderPriceSnapshot>();
    public DbSet<ProviderAccount> ProviderAccounts => Set<ProviderAccount>();
    public DbSet<ModelCatalogEntry> ModelCatalogEntries => Set<ModelCatalogEntry>();
    public DbSet<ProviderStatusCheck> ProviderStatusChecks => Set<ProviderStatusCheck>();
    public DbSet<LocalModelInventory> LocalModelInventory => Set<LocalModelInventory>();
    public DbSet<ProviderSyncJob> ProviderSyncJobs => Set<ProviderSyncJob>();
    public DbSet<ProviderUsageEvent> ProviderUsageEvents => Set<ProviderUsageEvent>();
    public DbSet<AiSystem> AiSystems => Set<AiSystem>();
    public DbSet<AgentDefinition> AgentDefinitions => Set<AgentDefinition>();
    public DbSet<Dataset> Datasets => Set<Dataset>();
    public DbSet<DatasetVersion> DatasetVersions => Set<DatasetVersion>();
    public DbSet<KnowledgeBase> KnowledgeBases => Set<KnowledgeBase>();
    public DbSet<DocumentAsset> DocumentAssets => Set<DocumentAsset>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();
    public DbSet<EvalSuite> EvalSuites => Set<EvalSuite>();
    public DbSet<TraceRecord> TraceRecords => Set<TraceRecord>();
    public DbSet<Deployment> Deployments => Set<Deployment>();
    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();
    public DbSet<WalletAccount> WalletAccounts => Set<WalletAccount>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<WalletReservation> WalletReservations => Set<WalletReservation>();
    public DbSet<PaymentProviderConfig> PaymentProviderConfigs => Set<PaymentProviderConfig>();
    public DbSet<TrainingJob> TrainingJobs => Set<TrainingJob>();
    public DbSet<Experiment> Experiments => Set<Experiment>();
    public DbSet<ModelVersion> ModelVersions => Set<ModelVersion>();
    public DbSet<ModelAlias> ModelAliases => Set<ModelAlias>();
    public DbSet<EvalRun> EvalRuns => Set<EvalRun>();
    public DbSet<HumanReviewRecord> HumanReviewRecords => Set<HumanReviewRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("vector");

        ConfigureWorkspace(modelBuilder.Entity<Workspace>());
        ConfigureProject(modelBuilder.Entity<Project>());
        ConfigureUser(modelBuilder.Entity<User>());
        ConfigureMembership(modelBuilder.Entity<Membership>());
        ConfigureEnvironment(modelBuilder.Entity<AgentEnvironment>());
        ConfigureApiKey(modelBuilder.Entity<ApiKey>());
        ConfigureServiceAccount(modelBuilder.Entity<ServiceAccount>());
        ConfigureSecretReference(modelBuilder.Entity<SecretReference>());
        ConfigurePolicy(modelBuilder.Entity<Policy>());
        ConfigureAuditEvent(modelBuilder.Entity<AuditEvent>());
        ConfigureModelProvider(modelBuilder.Entity<ModelProvider>());
        ConfigureModelRoute(modelBuilder.Entity<ModelRoute>());
        ConfigureProviderPriceSnapshot(modelBuilder.Entity<ProviderPriceSnapshot>());
        ConfigureProviderAccount(modelBuilder.Entity<ProviderAccount>());
        ConfigureModelCatalogEntry(modelBuilder.Entity<ModelCatalogEntry>());
        ConfigureProviderStatusCheck(modelBuilder.Entity<ProviderStatusCheck>());
        ConfigureLocalModelInventory(modelBuilder.Entity<LocalModelInventory>());
        ConfigureProviderSyncJob(modelBuilder.Entity<ProviderSyncJob>());
        ConfigureProviderUsageEvent(modelBuilder.Entity<ProviderUsageEvent>());
        ConfigureAiSystem(modelBuilder.Entity<AiSystem>());
        ConfigureAgentDefinition(modelBuilder.Entity<AgentDefinition>());
        ConfigureDataset(modelBuilder.Entity<Dataset>());
        ConfigureDatasetVersion(modelBuilder.Entity<DatasetVersion>());
        ConfigureKnowledgeBase(modelBuilder.Entity<KnowledgeBase>());
        ConfigureDocumentAsset(modelBuilder.Entity<DocumentAsset>());
        ConfigureIngestionJob(modelBuilder.Entity<IngestionJob>());
        ConfigureDocumentChunk(modelBuilder.Entity<DocumentChunk>());
        ConfigureAgentRun(modelBuilder.Entity<AgentRun>());
        ConfigureEvalSuite(modelBuilder.Entity<EvalSuite>());
        ConfigureTraceRecord(modelBuilder.Entity<TraceRecord>());
        ConfigureDeployment(modelBuilder.Entity<Deployment>());
        ConfigureUsageEvent(modelBuilder.Entity<UsageEvent>());
        ConfigureWalletAccount(modelBuilder.Entity<WalletAccount>());
        ConfigureWalletTransaction(modelBuilder.Entity<WalletTransaction>());
        ConfigureWalletReservation(modelBuilder.Entity<WalletReservation>());
        ConfigurePaymentProviderConfig(modelBuilder.Entity<PaymentProviderConfig>());
        ConfigureTrainingJob(modelBuilder.Entity<TrainingJob>());
        ConfigureExperiment(modelBuilder.Entity<Experiment>());
        ConfigureModelVersion(modelBuilder.Entity<ModelVersion>());
        ConfigureModelAlias(modelBuilder.Entity<ModelAlias>());
        ConfigureEvalRun(modelBuilder.Entity<EvalRun>());
        ConfigureHumanReviewRecord(modelBuilder.Entity<HumanReviewRecord>());
    }

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
        entity.Property(e => e.Embedding).HasColumnType("vector(64)").IsRequired();
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

public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

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
    public string Embedding { get; set; } = "[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]";
    public string? ContentHash { get; set; }
    public int DocumentVersion { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime? InactiveAt { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

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

public sealed class WalletAccount : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Balance { get; set; }
    public decimal ReservedBalance { get; set; }
    public string Status { get; set; } = "active";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WalletAccountId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ExternalReference { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class WalletReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WalletAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "active";
    public DateTime ExpiresAt { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PaymentProviderConfig : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Mode { get; set; } = "test";
    public bool IsEnabled { get; set; }
    public Guid? ApiKeySecretReferenceId { get; set; }
    public Guid? WebhookSecretReferenceId { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class TrainingJob : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? DatasetId { get; set; }
    public Guid? DatasetVersionId { get; set; }
    public Guid? ModelRouteId { get; set; }
    public Guid? WalletReservationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Kind { get; set; } = "classification";
    public string Status { get; set; } = "queued";
    public string ConfigJson { get; set; } = "{}";
    public string HyperparametersJson { get; set; } = "{}";
    public string ArtifactsJson { get; set; } = "{}";
    public string MetricsJson { get; set; } = "{}";
    public string LogsJson { get; set; } = "[]";
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class Experiment : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? DatasetVersionId { get; set; }
    public Guid? AgentDefinitionId { get; set; }
    public Guid? ModelRouteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string ConfigJson { get; set; } = "{}";
    public string MetricsJson { get; set; } = "{}";
    public string ArtifactsJson { get; set; } = "{}";
}

public sealed class ModelVersion : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? TrainingJobId { get; set; }
    public Guid? ExperimentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Kind { get; set; } = "classifier";
    public string Status { get; set; } = "candidate";
    public string? ArtifactUri { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public string CapabilitiesJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ModelAlias : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid ModelVersionId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class EvalRun : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid EvalSuiteId { get; set; }
    public Guid? ModelVersionId { get; set; }
    public Guid? AgentDefinitionId { get; set; }
    public string Status { get; set; } = "queued";
    public decimal? Score { get; set; }
    public decimal? Threshold { get; set; }
    public bool Passed { get; set; }
    public string ResultsJson { get; set; } = "{}";
    public string FailuresJson { get; set; } = "[]";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class HumanReviewRecord : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? AgentRunId { get; set; }
    public Guid? TraceRecordId { get; set; }
    public string Queue { get; set; } = "failed_eval";
    public string Status { get; set; } = "pending";
    public string? Label { get; set; }
    public string? Severity { get; set; }
    public string? Reason { get; set; }
    public string? FollowUpAction { get; set; }
    public string? ReviewerId { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool CanReuseForTraining { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
