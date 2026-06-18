using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentPort.PlatformApi.Data;

public sealed partial class AgentPortDbContext : DbContext
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
}
