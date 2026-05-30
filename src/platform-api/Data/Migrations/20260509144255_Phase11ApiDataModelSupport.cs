using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentPort.PlatformApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase11ApiDataModelSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ingestion_jobs_DocumentAssetId",
                table: "ingestion_jobs");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_CitationId",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_DocumentAssetId",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_assets_KnowledgeBaseId",
                table: "document_assets");

            migrationBuilder.DropIndex(
                name: "IX_agent_runs_WorkspaceId",
                table: "agent_runs");

            migrationBuilder.AddColumn<Guid>(
                name: "ApiKeyId",
                table: "trace_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthMetadataJson",
                table: "trace_records",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "AuthMode",
                table: "trace_records",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "anonymous");

            migrationBuilder.AddColumn<decimal>(
                name: "BestRetrievalScore",
                table: "trace_records",
                type: "numeric(8,6)",
                precision: 8,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BudgetDecisionJson",
                table: "trace_records",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "NoAnswerReason",
                table: "trace_records",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PolicyDecisionId",
                table: "trace_records",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityStatus",
                table: "trace_records",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "not_evaluated");

            migrationBuilder.AddColumn<string>(
                name: "RateLimitDecisionJson",
                table: "trace_records",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<decimal>(
                name: "ScoreThreshold",
                table: "trace_records",
                type: "numeric(8,6)",
                precision: 8,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TopK",
                table: "trace_records",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "DefaultTopK",
                table: "knowledge_bases",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<string>(
                name: "NoAnswerFallback",
                table: "knowledge_bases",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "I could not find enough information in the ingested documents.");

            migrationBuilder.AddColumn<string>(
                name: "QualityConfigJson",
                table: "knowledge_bases",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "QualityStatus",
                table: "knowledge_bases",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "not_evaluated");

            migrationBuilder.AddColumn<decimal>(
                name: "ScoreThreshold",
                table: "knowledge_bases",
                type: "numeric(8,6)",
                precision: 8,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "ingestion_jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentVersion",
                table: "ingestion_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Operation",
                table: "ingestion_jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "ingest");

            migrationBuilder.AddColumn<string>(
                name: "SkippedReason",
                table: "ingestion_jobs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "document_chunks",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentVersion",
                table: "document_chunks",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "InactiveAt",
                table: "document_chunks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "document_chunks",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "document_assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "document_assets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "document_assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentVersion",
                table: "document_assets",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "document_assets",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastIngestedAt",
                table: "document_assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedOriginsJson",
                table: "deployments",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "AuthMode",
                table: "deployments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "api_key");

            migrationBuilder.AddColumn<string>(
                name: "DeploymentType",
                table: "deployments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "endpoint");

            migrationBuilder.AddColumn<string>(
                name: "RateLimitJson",
                table: "deployments",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "WidgetConfigJson",
                table: "deployments",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "AllowedOriginsJson",
                table: "api_keys",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "AuthMetadataJson",
                table: "api_keys",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "KeyType",
                table: "api_keys",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "server");

            migrationBuilder.AddColumn<string>(
                name: "RateLimitJson",
                table: "api_keys",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<Guid>(
                name: "ApiKeyId",
                table: "agent_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthMetadataJson",
                table: "agent_runs",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "AuthMode",
                table: "agent_runs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "anonymous");

            migrationBuilder.AddColumn<decimal>(
                name: "BestRetrievalScore",
                table: "agent_runs",
                type: "numeric(8,6)",
                precision: 8,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EnvironmentId",
                table: "agent_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoAnswerReason",
                table: "agent_runs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityStatus",
                table: "agent_runs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "not_evaluated");

            migrationBuilder.AddColumn<decimal>(
                name: "ScoreThreshold",
                table: "agent_runs",
                type: "numeric(8,6)",
                precision: 8,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TopK",
                table: "agent_runs",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.CreateIndex(
                name: "IX_trace_records_ApiKeyId_CreatedAt",
                table: "trace_records",
                columns: new[] { "ApiKeyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_trace_records_WorkspaceId_ProjectId_QualityStatus_CreatedAt",
                table: "trace_records",
                columns: new[] { "WorkspaceId", "ProjectId", "QualityStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_bases_WorkspaceId_ProjectId_Status_QualityStatus",
                table: "knowledge_bases",
                columns: new[] { "WorkspaceId", "ProjectId", "Status", "QualityStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_DatasetId_ContentHash_CreatedAt",
                table: "ingestion_jobs",
                columns: new[] { "DatasetId", "ContentHash", "CreatedAt" },
                filter: "\"ContentHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_DocumentAssetId_Operation_CreatedAt",
                table: "ingestion_jobs",
                columns: new[] { "DocumentAssetId", "Operation", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_CitationId",
                table: "document_chunks",
                column: "CitationId",
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentAssetId_IsActive_ChunkIndex",
                table: "document_chunks",
                columns: new[] { "DocumentAssetId", "IsActive", "ChunkIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_KnowledgeBaseId_IsActive_ChunkIndex",
                table: "document_chunks",
                columns: new[] { "KnowledgeBaseId", "IsActive", "ChunkIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_document_assets_KnowledgeBaseId_ContentHash",
                table: "document_assets",
                columns: new[] { "KnowledgeBaseId", "ContentHash" },
                unique: true,
                filter: "\"ContentHash\" IS NOT NULL AND \"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_document_assets_KnowledgeBaseId_IsActive_Status_CreatedAt",
                table: "document_assets",
                columns: new[] { "KnowledgeBaseId", "IsActive", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_deployments_WorkspaceId_ProjectId_DeploymentType_Status",
                table: "deployments",
                columns: new[] { "WorkspaceId", "ProjectId", "DeploymentType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_WorkspaceId_ProjectId_KeyType",
                table: "api_keys",
                columns: new[] { "WorkspaceId", "ProjectId", "KeyType" });

            migrationBuilder.CreateIndex(
                name: "IX_agent_runs_ApiKeyId_CreatedAt",
                table: "agent_runs",
                columns: new[] { "ApiKeyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_agent_runs_EnvironmentId_CreatedAt",
                table: "agent_runs",
                columns: new[] { "EnvironmentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_agent_runs_WorkspaceId_ProjectId_CreatedAt",
                table: "agent_runs",
                columns: new[] { "WorkspaceId", "ProjectId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_agent_runs_api_keys_ApiKeyId",
                table: "agent_runs",
                column: "ApiKeyId",
                principalTable: "api_keys",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_runs_environments_EnvironmentId",
                table: "agent_runs",
                column: "EnvironmentId",
                principalTable: "environments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_trace_records_api_keys_ApiKeyId",
                table: "trace_records",
                column: "ApiKeyId",
                principalTable: "api_keys",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agent_runs_api_keys_ApiKeyId",
                table: "agent_runs");

            migrationBuilder.DropForeignKey(
                name: "FK_agent_runs_environments_EnvironmentId",
                table: "agent_runs");

            migrationBuilder.DropForeignKey(
                name: "FK_trace_records_api_keys_ApiKeyId",
                table: "trace_records");

            migrationBuilder.DropIndex(
                name: "IX_trace_records_ApiKeyId_CreatedAt",
                table: "trace_records");

            migrationBuilder.DropIndex(
                name: "IX_trace_records_WorkspaceId_ProjectId_QualityStatus_CreatedAt",
                table: "trace_records");

            migrationBuilder.DropIndex(
                name: "IX_knowledge_bases_WorkspaceId_ProjectId_Status_QualityStatus",
                table: "knowledge_bases");

            migrationBuilder.DropIndex(
                name: "IX_ingestion_jobs_DatasetId_ContentHash_CreatedAt",
                table: "ingestion_jobs");

            migrationBuilder.DropIndex(
                name: "IX_ingestion_jobs_DocumentAssetId_Operation_CreatedAt",
                table: "ingestion_jobs");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_CitationId",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_DocumentAssetId_IsActive_ChunkIndex",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_KnowledgeBaseId_IsActive_ChunkIndex",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_assets_KnowledgeBaseId_ContentHash",
                table: "document_assets");

            migrationBuilder.DropIndex(
                name: "IX_document_assets_KnowledgeBaseId_IsActive_Status_CreatedAt",
                table: "document_assets");

            migrationBuilder.DropIndex(
                name: "IX_deployments_WorkspaceId_ProjectId_DeploymentType_Status",
                table: "deployments");

            migrationBuilder.DropIndex(
                name: "IX_api_keys_WorkspaceId_ProjectId_KeyType",
                table: "api_keys");

            migrationBuilder.DropIndex(
                name: "IX_agent_runs_ApiKeyId_CreatedAt",
                table: "agent_runs");

            migrationBuilder.DropIndex(
                name: "IX_agent_runs_EnvironmentId_CreatedAt",
                table: "agent_runs");

            migrationBuilder.DropIndex(
                name: "IX_agent_runs_WorkspaceId_ProjectId_CreatedAt",
                table: "agent_runs");

            migrationBuilder.DropColumn(
                name: "ApiKeyId",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "AuthMetadataJson",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "AuthMode",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "BestRetrievalScore",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "BudgetDecisionJson",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "NoAnswerReason",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "PolicyDecisionId",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "RateLimitDecisionJson",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "ScoreThreshold",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "TopK",
                table: "trace_records");

            migrationBuilder.DropColumn(
                name: "DefaultTopK",
                table: "knowledge_bases");

            migrationBuilder.DropColumn(
                name: "NoAnswerFallback",
                table: "knowledge_bases");

            migrationBuilder.DropColumn(
                name: "QualityConfigJson",
                table: "knowledge_bases");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                table: "knowledge_bases");

            migrationBuilder.DropColumn(
                name: "ScoreThreshold",
                table: "knowledge_bases");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "ingestion_jobs");

            migrationBuilder.DropColumn(
                name: "DocumentVersion",
                table: "ingestion_jobs");

            migrationBuilder.DropColumn(
                name: "Operation",
                table: "ingestion_jobs");

            migrationBuilder.DropColumn(
                name: "SkippedReason",
                table: "ingestion_jobs");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "DocumentVersion",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "InactiveAt",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "document_assets");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "document_assets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "document_assets");

            migrationBuilder.DropColumn(
                name: "DocumentVersion",
                table: "document_assets");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "document_assets");

            migrationBuilder.DropColumn(
                name: "LastIngestedAt",
                table: "document_assets");

            migrationBuilder.DropColumn(
                name: "AllowedOriginsJson",
                table: "deployments");

            migrationBuilder.DropColumn(
                name: "AuthMode",
                table: "deployments");

            migrationBuilder.DropColumn(
                name: "DeploymentType",
                table: "deployments");

            migrationBuilder.DropColumn(
                name: "RateLimitJson",
                table: "deployments");

            migrationBuilder.DropColumn(
                name: "WidgetConfigJson",
                table: "deployments");

            migrationBuilder.DropColumn(
                name: "AllowedOriginsJson",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "AuthMetadataJson",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "KeyType",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "RateLimitJson",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "ApiKeyId",
                table: "agent_runs");

            migrationBuilder.DropColumn(
                name: "AuthMetadataJson",
                table: "agent_runs");

            migrationBuilder.DropColumn(
                name: "AuthMode",
                table: "agent_runs");

            migrationBuilder.DropColumn(
                name: "BestRetrievalScore",
                table: "agent_runs");

            migrationBuilder.DropColumn(
                name: "EnvironmentId",
                table: "agent_runs");

            migrationBuilder.DropColumn(
                name: "NoAnswerReason",
                table: "agent_runs");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                table: "agent_runs");

            migrationBuilder.DropColumn(
                name: "ScoreThreshold",
                table: "agent_runs");

            migrationBuilder.DropColumn(
                name: "TopK",
                table: "agent_runs");

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_DocumentAssetId",
                table: "ingestion_jobs",
                column: "DocumentAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_CitationId",
                table: "document_chunks",
                column: "CitationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentAssetId",
                table: "document_chunks",
                column: "DocumentAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_document_assets_KnowledgeBaseId",
                table: "document_assets",
                column: "KnowledgeBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_runs_WorkspaceId",
                table: "agent_runs",
                column: "WorkspaceId");
        }
    }
}
