using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPort.PlatformApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase12ProviderCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "local_model_inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Digest = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Family = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ParameterSize = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Quantization = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    IsInstalled = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_model_inventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_local_model_inventory_model_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "model_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_local_model_inventory_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "model_catalog_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Modality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RouteType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContextWindowTokens = table.Column<int>(type: "integer", nullable: true),
                    MaxOutputTokens = table.Column<int>(type: "integer", nullable: true),
                    SupportsTools = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsJsonMode = table.Column<bool>(type: "boolean", nullable: false),
                    SupportsStreaming = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocal = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeprecatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CapabilitiesJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_model_catalog_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_model_catalog_entries_model_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "model_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_model_catalog_entries_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialSecretReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    AccountType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalAccountId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LastValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CapabilitiesJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    LimitsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_provider_accounts_model_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "model_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_provider_accounts_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_accounts_secret_references_CredentialSecretReferen~",
                        column: x => x.CredentialSecretReferenceId,
                        principalTable: "secret_references",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_accounts_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_status_checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModelRouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CheckType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Target = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    LatencyMs = table.Column<int>(type: "integer", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_status_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_provider_status_checks_model_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "model_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_provider_status_checks_model_routes_ModelRouteId",
                        column: x => x.ModelRouteId,
                        principalTable: "model_routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_status_checks_provider_accounts_ProviderAccountId",
                        column: x => x.ProviderAccountId,
                        principalTable: "provider_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_status_checks_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_sync_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DiscoveredModels = table.Column<int>(type: "integer", nullable: false),
                    UpsertedModels = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_sync_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_provider_sync_jobs_model_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "model_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_sync_jobs_provider_accounts_ProviderAccountId",
                        column: x => x.ProviderAccountId,
                        principalTable: "provider_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_sync_jobs_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "provider_usage_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModelRouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TraceRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModelName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InputTokens = table.Column<long>(type: "bigint", nullable: false),
                    OutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    RequestCount = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ProviderCost = table.Column<decimal>(type: "numeric(19,6)", precision: 19, scale: 6, nullable: false),
                    PlatformFee = table.Column<decimal>(type: "numeric(19,6)", precision: 19, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(19,6)", precision: 19, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_usage_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_provider_usage_events_agent_definitions_AgentDefinitionId",
                        column: x => x.AgentDefinitionId,
                        principalTable: "agent_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_usage_events_model_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "model_providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_provider_usage_events_model_routes_ModelRouteId",
                        column: x => x.ModelRouteId,
                        principalTable: "model_routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_usage_events_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_usage_events_provider_accounts_ProviderAccountId",
                        column: x => x.ProviderAccountId,
                        principalTable: "provider_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_usage_events_trace_records_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "trace_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_usage_events_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_local_model_inventory_ProviderId",
                table: "local_model_inventory",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_local_model_inventory_WorkspaceId_IsInstalled_Status",
                table: "local_model_inventory",
                columns: new[] { "WorkspaceId", "IsInstalled", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_local_model_inventory_WorkspaceId_ProviderId_ModelName",
                table: "local_model_inventory",
                columns: new[] { "WorkspaceId", "ProviderId", "ModelName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_model_catalog_entries_ProviderId_RouteType_IsEnabled",
                table: "model_catalog_entries",
                columns: new[] { "ProviderId", "RouteType", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_model_catalog_entries_ProviderId_WorkspaceId_ModelName",
                table: "model_catalog_entries",
                columns: new[] { "ProviderId", "WorkspaceId", "ModelName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_model_catalog_entries_WorkspaceId_IsEnabled_Status",
                table: "model_catalog_entries",
                columns: new[] { "WorkspaceId", "IsEnabled", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_accounts_CredentialSecretReferenceId",
                table: "provider_accounts",
                column: "CredentialSecretReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_accounts_ProjectId",
                table: "provider_accounts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_accounts_ProviderId",
                table: "provider_accounts",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_accounts_WorkspaceId_ProviderId_Slug",
                table: "provider_accounts",
                columns: new[] { "WorkspaceId", "ProviderId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_accounts_WorkspaceId_Status_IsEnabled",
                table: "provider_accounts",
                columns: new[] { "WorkspaceId", "Status", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_status_checks_ModelRouteId_CheckedAt",
                table: "provider_status_checks",
                columns: new[] { "ModelRouteId", "CheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_status_checks_ProviderAccountId_CheckedAt",
                table: "provider_status_checks",
                columns: new[] { "ProviderAccountId", "CheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_status_checks_ProviderId",
                table: "provider_status_checks",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_status_checks_WorkspaceId_ProviderId_CheckedAt",
                table: "provider_status_checks",
                columns: new[] { "WorkspaceId", "ProviderId", "CheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_sync_jobs_ProviderAccountId_CreatedAt",
                table: "provider_sync_jobs",
                columns: new[] { "ProviderAccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_sync_jobs_ProviderId",
                table: "provider_sync_jobs",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_sync_jobs_WorkspaceId_ProviderId_CreatedAt",
                table: "provider_sync_jobs",
                columns: new[] { "WorkspaceId", "ProviderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_events_AgentDefinitionId",
                table: "provider_usage_events",
                column: "AgentDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_events_ModelRouteId",
                table: "provider_usage_events",
                column: "ModelRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_events_ProjectId",
                table: "provider_usage_events",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_events_ProviderAccountId",
                table: "provider_usage_events",
                column: "ProviderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_events_ProviderId_ModelName_OccurredAt",
                table: "provider_usage_events",
                columns: new[] { "ProviderId", "ModelName", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_events_TraceRecordId",
                table: "provider_usage_events",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_usage_events_WorkspaceId_ProjectId_OccurredAt",
                table: "provider_usage_events",
                columns: new[] { "WorkspaceId", "ProjectId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "local_model_inventory");

            migrationBuilder.DropTable(
                name: "model_catalog_entries");

            migrationBuilder.DropTable(
                name: "provider_status_checks");

            migrationBuilder.DropTable(
                name: "provider_sync_jobs");

            migrationBuilder.DropTable(
                name: "provider_usage_events");

            migrationBuilder.DropTable(
                name: "provider_accounts");
        }
    }
}
