using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPort.PlatformApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase2TrainingEvalModelLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowsEval",
                table: "datasets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsTraining",
                table: "datasets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ConsentFlagsJson",
                table: "datasets",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<bool>(
                name: "IsGolden",
                table: "datasets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "License",
                table: "datasets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PiiClassification",
                table: "datasets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityNotes",
                table: "datasets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchemaJson",
                table: "datasets",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "datasets",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitsJson",
                table: "datasets",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateTable(
                name: "experiments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatasetVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModelRouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    MetricsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    ArtifactsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experiments_agent_definitions_AgentDefinitionId",
                        column: x => x.AgentDefinitionId,
                        principalTable: "agent_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_experiments_dataset_versions_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalTable: "dataset_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_experiments_model_routes_ModelRouteId",
                        column: x => x.ModelRouteId,
                        principalTable: "model_routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_experiments_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_experiments_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "human_review_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    TraceRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    Queue = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    FollowUpAction = table.Column<string>(type: "text", nullable: true),
                    ReviewerId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReviewerName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanReuseForTraining = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_human_review_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_human_review_records_agent_runs_AgentRunId",
                        column: x => x.AgentRunId,
                        principalTable: "agent_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_human_review_records_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_human_review_records_trace_records_TraceRecordId",
                        column: x => x.TraceRecordId,
                        principalTable: "trace_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_human_review_records_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "training_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uuid", nullable: true),
                    DatasetVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModelRouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    WalletReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    HyperparametersJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    ArtifactsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    MetricsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    LogsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    EstimatedCost = table.Column<decimal>(type: "numeric(19,6)", precision: 19, scale: 6, nullable: false),
                    ActualCost = table.Column<decimal>(type: "numeric(19,6)", precision: 19, scale: 6, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_training_jobs_dataset_versions_DatasetVersionId",
                        column: x => x.DatasetVersionId,
                        principalTable: "dataset_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_training_jobs_datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_training_jobs_model_routes_ModelRouteId",
                        column: x => x.ModelRouteId,
                        principalTable: "model_routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_training_jobs_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_training_jobs_wallet_reservations_WalletReservationId",
                        column: x => x.WalletReservationId,
                        principalTable: "wallet_reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_training_jobs_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "model_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExperimentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ArtifactUri = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CapabilitiesJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_model_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_model_versions_experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "experiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_model_versions_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_model_versions_training_jobs_TrainingJobId",
                        column: x => x.TrainingJobId,
                        principalTable: "training_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_model_versions_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "eval_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvalSuiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(19,6)", precision: 19, scale: 6, nullable: true),
                    Threshold = table.Column<decimal>(type: "numeric(19,6)", precision: 19, scale: 6, nullable: true),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    ResultsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    FailuresJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eval_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eval_runs_agent_definitions_AgentDefinitionId",
                        column: x => x.AgentDefinitionId,
                        principalTable: "agent_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_eval_runs_eval_suites_EvalSuiteId",
                        column: x => x.EvalSuiteId,
                        principalTable: "eval_suites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eval_runs_model_versions_ModelVersionId",
                        column: x => x.ModelVersionId,
                        principalTable: "model_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_eval_runs_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_eval_runs_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "model_aliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Alias = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_model_aliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_model_aliases_model_versions_ModelVersionId",
                        column: x => x.ModelVersionId,
                        principalTable: "model_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_model_aliases_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_model_aliases_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_eval_runs_AgentDefinitionId",
                table: "eval_runs",
                column: "AgentDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_eval_runs_EvalSuiteId",
                table: "eval_runs",
                column: "EvalSuiteId");

            migrationBuilder.CreateIndex(
                name: "IX_eval_runs_ModelVersionId",
                table: "eval_runs",
                column: "ModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_eval_runs_ProjectId",
                table: "eval_runs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_eval_runs_WorkspaceId_ProjectId_EvalSuiteId_CreatedAt",
                table: "eval_runs",
                columns: new[] { "WorkspaceId", "ProjectId", "EvalSuiteId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_experiments_AgentDefinitionId",
                table: "experiments",
                column: "AgentDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_experiments_DatasetVersionId",
                table: "experiments",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_experiments_ModelRouteId",
                table: "experiments",
                column: "ModelRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_experiments_ProjectId",
                table: "experiments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_experiments_WorkspaceId_ProjectId_Slug",
                table: "experiments",
                columns: new[] { "WorkspaceId", "ProjectId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_human_review_records_AgentRunId",
                table: "human_review_records",
                column: "AgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_human_review_records_ProjectId",
                table: "human_review_records",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_human_review_records_TraceRecordId",
                table: "human_review_records",
                column: "TraceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_human_review_records_WorkspaceId_Queue_Status_CreatedAt",
                table: "human_review_records",
                columns: new[] { "WorkspaceId", "Queue", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_model_aliases_ModelVersionId",
                table: "model_aliases",
                column: "ModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_model_aliases_ProjectId",
                table: "model_aliases",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_model_aliases_WorkspaceId_ProjectId_Alias",
                table: "model_aliases",
                columns: new[] { "WorkspaceId", "ProjectId", "Alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_model_versions_ExperimentId",
                table: "model_versions",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_model_versions_ProjectId",
                table: "model_versions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_model_versions_TrainingJobId",
                table: "model_versions",
                column: "TrainingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_model_versions_WorkspaceId_ProjectId_Slug",
                table: "model_versions",
                columns: new[] { "WorkspaceId", "ProjectId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_jobs_DatasetId",
                table: "training_jobs",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_training_jobs_DatasetVersionId",
                table: "training_jobs",
                column: "DatasetVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_training_jobs_ModelRouteId",
                table: "training_jobs",
                column: "ModelRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_training_jobs_ProjectId",
                table: "training_jobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_training_jobs_WalletReservationId",
                table: "training_jobs",
                column: "WalletReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_training_jobs_WorkspaceId_ProjectId_Slug",
                table: "training_jobs",
                columns: new[] { "WorkspaceId", "ProjectId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eval_runs");

            migrationBuilder.DropTable(
                name: "human_review_records");

            migrationBuilder.DropTable(
                name: "model_aliases");

            migrationBuilder.DropTable(
                name: "model_versions");

            migrationBuilder.DropTable(
                name: "experiments");

            migrationBuilder.DropTable(
                name: "training_jobs");

            migrationBuilder.DropColumn(
                name: "AllowsEval",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "AllowsTraining",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "ConsentFlagsJson",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "IsGolden",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "License",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "PiiClassification",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "QualityNotes",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "SchemaJson",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "SplitsJson",
                table: "datasets");
        }
    }
}
