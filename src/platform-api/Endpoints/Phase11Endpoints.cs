using AgentPort.PlatformApi.Contracts;
using AgentPort.PlatformApi.Data;
using AgentPort.PlatformApi.Infrastructure;
using AgentPort.PlatformApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;

namespace AgentPort.PlatformApi.Endpoints;

public static class Phase11Endpoints
{
    public static IEndpointRouteBuilder MapPhase11Api(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1").WithTags("Phase 1.1");

        api.MapGet("/dev/seed-status", GetDevSeedStatus);
        api.MapPost("/dev/reset", ResetDevSeed);

        api.MapGet("/documents", ListDocuments);
        api.MapDelete("/documents/{documentId:guid}", DeleteDocument);
        api.MapPost("/documents/{documentId:guid}/reingest", ReingestDocument);

        api.MapPost("/api-keys/validate", ValidateApiKey);

        api.MapGet("/widget/config", GetWidgetConfig);
        api.MapPost("/widget/sessions", CreateWidgetSession);

        return routes;
    }

    private static async Task<IResult> GetDevSeedStatus(
        IHostEnvironment environment,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return Problem(StatusCodes.Status404NotFound, "dev_endpoint_disabled", "Dev seed status is only available in Development.");
        }

        var workspace = await FindLocalWorkspace(db).FirstOrDefaultAsync(cancellationToken);
        if (workspace is null)
        {
            return Results.Ok(new DevSeedStatusResponse(
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                0,
                0,
                0,
                0,
                0,
                0,
                DateTime.UtcNow));
        }

        var project = await db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.WorkspaceId == workspace.Id && item.Slug == "default", cancellationToken);
        var environmentRow = project is null
            ? null
            : await db.Environments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.WorkspaceId == workspace.Id && item.ProjectId == project.Id && item.Slug == "dev",
                    cancellationToken);
        var agent = project is null
            ? null
            : await db.AgentDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.WorkspaceId == workspace.Id && item.ProjectId == project.Id && item.Slug == "document-qa",
                    cancellationToken);
        var dataset = project is null
            ? null
            : await db.Datasets
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.WorkspaceId == workspace.Id && item.ProjectId == project.Id && item.Slug == "document-qa-sources",
                    cancellationToken);
        var knowledgeBase = project is null
            ? null
            : await db.KnowledgeBases
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.WorkspaceId == workspace.Id && item.ProjectId == project.Id && item.Slug == "document-qa",
                    cancellationToken);

        var documentCount = await db.DocumentAssets.CountAsync(item => item.WorkspaceId == workspace.Id, cancellationToken);
        var chunkCount = await db.DocumentChunks.CountAsync(item => item.WorkspaceId == workspace.Id, cancellationToken);
        var ingestionJobCount = await db.IngestionJobs.CountAsync(item => item.WorkspaceId == workspace.Id, cancellationToken);
        var runCount = await db.AgentRuns.CountAsync(item => item.WorkspaceId == workspace.Id, cancellationToken);
        var traceCount = await db.TraceRecords.CountAsync(item => item.WorkspaceId == workspace.Id, cancellationToken);
        var apiKeyCount = await db.ApiKeys.CountAsync(item => item.WorkspaceId == workspace.Id, cancellationToken);

        return Results.Ok(new DevSeedStatusResponse(
            project is not null && environmentRow is not null && agent is not null && dataset is not null && knowledgeBase is not null,
            workspace.Id,
            project?.Id,
            environmentRow?.Id,
            agent?.Id,
            dataset?.Id,
            knowledgeBase?.Id,
            documentCount,
            chunkCount,
            ingestionJobCount,
            runCount,
            traceCount,
            apiKeyCount,
            DateTime.UtcNow));
    }

    private static async Task<IResult> ResetDevSeed(
        [FromBody] DevResetRequest? request,
        IHostEnvironment environment,
        AgentPortDbContext db,
        LocalBootstrapService bootstrap,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return Problem(StatusCodes.Status404NotFound, "dev_endpoint_disabled", "Dev reset is only available in Development.");
        }

        var reseed = request?.Reseed ?? true;
        var workspaceIds = await FindLocalWorkspace(db)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var userIds = await db.Users
            .Where(item => item.Email == "owner@agentport.local")
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        if (workspaceIds.Count > 0)
        {
            await ClearLocalWorkspaceData(db, workspaceIds, cancellationToken);
        }

        var clearedUsers = userIds.Count == 0
            ? 0
            : await db.Users.Where(item => userIds.Contains(item.Id)).ExecuteDeleteAsync(cancellationToken);
        var seed = reseed
            ? await bootstrap.BootstrapLocalAsync(cancellationToken)
            : null;

        return Results.Ok(new DevResetResponse(
            "succeeded",
            workspaceIds.Count,
            clearedUsers,
            reseed,
            seed,
            DateTime.UtcNow));
    }

    private static async Task<IResult> ListDocuments(
        Guid? workspaceId,
        Guid? projectId,
        Guid? datasetId,
        string? status,
        bool? includeDeleted,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.DocumentAssets.AsNoTracking();
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == workspaceId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(item => item.ProjectId == projectId.Value);
        }

        if (datasetId.HasValue)
        {
            query = query.Where(item => item.DatasetId == datasetId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(item => item.Status == normalizedStatus);
        }
        else if (includeDeleted != true)
        {
            query = query.Where(item => item.IsActive && item.DeletedAt == null && item.Status != "deleted");
        }

        var documents = await query
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        var documentIds = documents.Select(item => item.Id).ToArray();

        var chunkCounts = documentIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await db.DocumentChunks
                .AsNoTracking()
                .Where(item => documentIds.Contains(item.DocumentAssetId) && item.IsActive)
                .GroupBy(item => item.DocumentAssetId)
                .Select(group => new { DocumentAssetId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.DocumentAssetId, item => item.Count, cancellationToken);
        var latestJobs = documentIds.Length == 0
            ? new Dictionary<Guid, LatestIngestionJob>()
            : (await db.IngestionJobs
                .AsNoTracking()
                .Where(item => documentIds.Contains(item.DocumentAssetId))
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new LatestIngestionJob(
                    item.DocumentAssetId,
                    item.Id,
                    item.Status,
                    item.CompletedAt ?? item.StartedAt ?? item.CreatedAt))
                .ToListAsync(cancellationToken))
                .GroupBy(item => item.DocumentAssetId)
                .ToDictionary(group => group.Key, group => group.First());

        var response = documents.Select(item =>
        {
            latestJobs.TryGetValue(item.Id, out var latestJob);
            return new DocumentAssetResponse(
                item.Id,
                item.WorkspaceId,
                item.ProjectId,
                item.DatasetId,
                item.KnowledgeBaseId,
                item.FileName,
                item.ContentType,
                item.ObjectKey,
                item.Status,
                item.SizeBytes,
                chunkCounts.GetValueOrDefault(item.Id),
                latestJob?.IngestionJobId,
                latestJob?.Status,
                latestJob?.LastIngestedAt,
                item.MetadataJson,
                item.CreatedAt,
                item.UpdatedAt,
                item.ContentHash,
                item.DocumentVersion,
                item.IsActive,
                item.ArchivedAt,
                item.DeletedAt);
        });

        return Results.Ok(response);
    }

    private static async Task<IResult> DeleteDocument(
        Guid documentId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var document = await db.DocumentAssets.FirstOrDefaultAsync(item => item.Id == documentId, cancellationToken);
        if (document is null)
        {
            return Problem(StatusCodes.Status404NotFound, "document_not_found", "Document was not found.");
        }

        var deactivatedChunkCount = await db.DocumentChunks
            .Where(item => item.DocumentAssetId == documentId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.IsActive, false)
                    .SetProperty(item => item.InactiveAt, DateTime.UtcNow)
                    .SetProperty(item => item.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
        var deletedAt = DateTime.UtcNow;
        document.Status = "deleted";
        document.IsActive = false;
        document.DeletedAt = deletedAt;
        document.UpdatedAt = deletedAt;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new DeleteDocumentResponse(
            document.Id,
            document.Status,
            deactivatedChunkCount,
            deletedAt,
            "soft_deleted_chunks_deactivated"));
    }

    private static async Task<IResult> ReingestDocument(
        Guid documentId,
        [FromBody] ReingestDocumentRequest? request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var document = await db.DocumentAssets.FirstOrDefaultAsync(item => item.Id == documentId, cancellationToken);
        if (document is null)
        {
            return Problem(StatusCodes.Status404NotFound, "document_not_found", "Document was not found.");
        }

        if (document.Status == "deleted" && request?.Force != true)
        {
            return Problem(StatusCodes.Status409Conflict, "document_deleted", "Deleted documents require force=true before reingest can be queued.");
        }

        if (string.IsNullOrWhiteSpace(document.ObjectKey))
        {
            return Problem(StatusCodes.Status409Conflict, "document_source_missing", "Document ObjectKey is missing; reingest cannot be queued.");
        }

        var queuedAt = DateTime.UtcNow;
        var job = new IngestionJob
        {
            WorkspaceId = document.WorkspaceId,
            ProjectId = document.ProjectId,
            DatasetId = document.DatasetId,
            KnowledgeBaseId = document.KnowledgeBaseId,
            DocumentAssetId = document.Id,
            Status = "queued",
            Operation = "reingest",
            ContentHash = document.ContentHash,
            DocumentVersion = document.DocumentVersion,
            StartedAt = null,
            CompletedAt = null,
            ChunkCount = 0,
            MetadataJson = $$"""{"phase":"1.1","trigger":"api-reingest","placeholder":true,"objectKey":"{{document.ObjectKey}}"}""",
            CreatedAt = queuedAt,
            UpdatedAt = queuedAt
        };

        document.Status = "queued";
        document.IsActive = true;
        document.DeletedAt = null;
        document.UpdatedAt = queuedAt;
        db.IngestionJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Accepted(
            $"/api/v1/ingestion-jobs?datasetId={document.DatasetId}",
            new ReingestDocumentResponse(
                document.Id,
                job.Id,
                job.Status,
                "Reingest job queued as a Phase 1.1 placeholder; worker execution is not implemented in the current data model.",
                queuedAt));
    }

    private static async Task<IResult> ValidateApiKey(
        [FromBody] ValidateApiKeyRequest? request,
        HttpContext httpContext,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var rawApiKey = request?.ApiKey;
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            rawApiKey = GetRawApiKey(httpContext);
        }

        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            return Problem(StatusCodes.Status401Unauthorized, "api_key_missing", "API key is required.");
        }

        var keyHash = ApiKeyHasher.Hash(rawApiKey.Trim());
        var apiKey = await db.ApiKeys.FirstOrDefaultAsync(item => item.KeyHash == keyHash, cancellationToken);
        if (apiKey is null)
        {
            return Problem(StatusCodes.Status401Unauthorized, "api_key_invalid", "API key is invalid.");
        }

        if (request?.WorkspaceId.HasValue == true && apiKey.WorkspaceId != request.WorkspaceId.Value)
        {
            return Problem(StatusCodes.Status403Forbidden, "api_key_workspace_mismatch", "API key cannot access this workspace.");
        }

        if (apiKey.RevokedAt.HasValue || (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value <= DateTime.UtcNow))
        {
            return Problem(StatusCodes.Status401Unauthorized, "api_key_inactive", "API key is inactive.");
        }

        var requiredScope = string.IsNullOrWhiteSpace(request?.RequiredScope) ? null : request.RequiredScope.Trim();
        var hasRequiredScope = requiredScope is null
            || apiKey.Scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase)
            || apiKey.Scopes.Contains("workspace:admin", StringComparer.OrdinalIgnoreCase);
        if (!hasRequiredScope)
        {
            return Problem(StatusCodes.Status403Forbidden, "api_key_scope_missing", $"API key requires '{requiredScope}'.");
        }

        apiKey.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new ApiKeyValidationResponse(
            true,
            apiKey.Id,
            apiKey.WorkspaceId,
            apiKey.ProjectId,
            apiKey.UserId,
            apiKey.ServiceAccountId,
            apiKey.Prefix,
            apiKey.Scopes,
            requiredScope,
            hasRequiredScope,
            apiKey.ExpiresAt,
            apiKey.LastUsedAt,
            DateTime.UtcNow));
    }

    private static async Task<IResult> GetWidgetConfig(
        Guid? deploymentId,
        Guid? agentDefinitionId,
        HttpContext httpContext,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveWidgetAgent(db, deploymentId, agentDefinitionId, cancellationToken);
        if (resolved.Problem is not null)
        {
            return resolved.Problem;
        }

        var agent = resolved.Agent!;
        var apiBaseUrl = GetApiBaseUrl(httpContext);
        var chatPath = $"/api/v1/agent-definitions/{agent.Id}/chat";
        var allowedOrigins = ParseStringArray(
            resolved.Deployment?.AllowedOriginsJson,
            ["http://localhost:3002", "http://127.0.0.1:3002"]);
        var rateLimit = string.IsNullOrWhiteSpace(resolved.Deployment?.RateLimitJson)
            ? "placeholder:60 requests/minute/origin"
            : resolved.Deployment!.RateLimitJson;

        return Results.Ok(new WidgetConfigResponse(
            resolved.Deployment?.Id,
            agent.Id,
            agent.WorkspaceId,
            agent.ProjectId,
            apiBaseUrl,
            chatPath,
            allowedOrigins,
            rateLimit,
            true,
            BuildEmbedSnippet(apiBaseUrl, resolved.Deployment?.Id, agent.Id),
            resolved.Deployment?.WidgetConfigJson ?? resolved.Deployment?.MetadataJson ?? "{}"));
    }

    private static async Task<IResult> CreateWidgetSession(
        CreateWidgetSessionRequest request,
        HttpContext httpContext,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveWidgetAgent(db, request.DeploymentId, request.AgentDefinitionId, cancellationToken);
        if (resolved.Problem is not null)
        {
            return resolved.Problem;
        }

        var agent = resolved.Agent!;
        var now = DateTime.UtcNow;

        // TODO: Persist WidgetSession.TokenHash, WidgetSession.ExpiresAt, WidgetSession.AllowedOrigin, and WidgetSession.DeploymentId.
        return Results.Ok(new WidgetSessionResponse(
            $"ap_widget_{Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()}",
            resolved.Deployment?.Id,
            agent.Id,
            agent.WorkspaceId,
            agent.ProjectId,
            now.AddMinutes(15),
            "widget:chat",
            "placeholder_not_persisted"));
    }

    private static IQueryable<Workspace> FindLocalWorkspace(AgentPortDbContext db)
    {
        return db.Workspaces.AsNoTracking().Where(item => item.Slug == "local");
    }

    private static async Task ClearLocalWorkspaceData(
        AgentPortDbContext db,
        IReadOnlyCollection<Guid> workspaceIds,
        CancellationToken cancellationToken)
    {
        var datasetIds = await db.Datasets
            .Where(item => workspaceIds.Contains(item.WorkspaceId))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var walletAccountIds = await db.WalletAccounts
            .Where(item => workspaceIds.Contains(item.WorkspaceId))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var providerIds = await db.ModelProviders
            .Where(item => item.WorkspaceId.HasValue && workspaceIds.Contains(item.WorkspaceId.Value))
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        await db.ProviderUsageEvents.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.ProviderStatusChecks.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.ProviderSyncJobs.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.LocalModelInventory.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.ModelCatalogEntries
            .Where(item => item.WorkspaceId.HasValue && workspaceIds.Contains(item.WorkspaceId.Value))
            .ExecuteDeleteAsync(cancellationToken);
        await db.ProviderAccounts.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.HumanReviewRecords.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.EvalRuns.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.ModelAliases.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.ModelVersions.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.Experiments.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.TrainingJobs.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);

        if (walletAccountIds.Count > 0)
        {
            await db.WalletReservations.Where(item => walletAccountIds.Contains(item.WalletAccountId)).ExecuteDeleteAsync(cancellationToken);
            await db.WalletTransactions.Where(item => walletAccountIds.Contains(item.WalletAccountId)).ExecuteDeleteAsync(cancellationToken);
        }

        await db.UsageEvents.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.AgentRuns.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.TraceRecords.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.DocumentChunks.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.IngestionJobs.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.DocumentAssets.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.KnowledgeBases.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);

        if (datasetIds.Count > 0)
        {
            await db.DatasetVersions.Where(item => datasetIds.Contains(item.DatasetId)).ExecuteDeleteAsync(cancellationToken);
        }

        await db.Datasets.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.EvalSuites.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.Deployments.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.AgentDefinitions.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.AiSystems.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.ModelRoutes.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);

        if (providerIds.Count > 0)
        {
            await db.ProviderPriceSnapshots.Where(item => providerIds.Contains(item.ProviderId)).ExecuteDeleteAsync(cancellationToken);
        }

        await db.ModelProviders
            .Where(item => item.WorkspaceId.HasValue && workspaceIds.Contains(item.WorkspaceId.Value))
            .ExecuteDeleteAsync(cancellationToken);
        await db.PaymentProviderConfigs.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.Policies.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.SecretReferences.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.ApiKeys.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.ServiceAccounts.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.Environments.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.AuditEvents
            .Where(item => item.WorkspaceId.HasValue && workspaceIds.Contains(item.WorkspaceId.Value))
            .ExecuteDeleteAsync(cancellationToken);
        await db.Memberships.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.Projects.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.WalletAccounts.Where(item => workspaceIds.Contains(item.WorkspaceId)).ExecuteDeleteAsync(cancellationToken);
        await db.Workspaces.Where(item => workspaceIds.Contains(item.Id)).ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task<WidgetResolution> ResolveWidgetAgent(
        AgentPortDbContext db,
        Guid? deploymentId,
        Guid? agentDefinitionId,
        CancellationToken cancellationToken)
    {
        Deployment? deployment = null;
        AgentDefinition? agent = null;

        if (deploymentId.HasValue)
        {
            deployment = await db.Deployments
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == deploymentId.Value, cancellationToken);
            if (deployment is null)
            {
                return new WidgetResolution(null, null, Problem(StatusCodes.Status404NotFound, "deployment_not_found", "Deployment was not found."));
            }

            agent = await db.AgentDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == deployment.AgentDefinitionId, cancellationToken);
        }
        else if (agentDefinitionId.HasValue)
        {
            agent = await db.AgentDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == agentDefinitionId.Value, cancellationToken);
        }
        else
        {
            agent = await db.AgentDefinitions
                .AsNoTracking()
                .OrderByDescending(item => item.Slug == "document-qa")
                .ThenBy(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return agent is null
            ? new WidgetResolution(null, null, Problem(StatusCodes.Status404NotFound, "agent_not_found", "Agent definition was not found."))
            : new WidgetResolution(deployment, agent, null);
    }

    private static string GetRawApiKey(HttpContext httpContext)
    {
        var explicitHeader = httpContext.Request.Headers["x-agentport-api-key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(explicitHeader))
        {
            return explicitHeader.Trim();
        }

        var authorization = httpContext.Request.Headers.Authorization.FirstOrDefault();
        const string bearerPrefix = "Bearer ";
        return !string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[bearerPrefix.Length..].Trim()
            : string.Empty;
    }

    private static string GetApiBaseUrl(HttpContext httpContext)
    {
        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    }

    private static string BuildEmbedSnippet(string apiBaseUrl, Guid? deploymentId, Guid agentDefinitionId)
    {
        var idQuery = deploymentId.HasValue
            ? $"deploymentId={deploymentId.Value}"
            : $"agentDefinitionId={agentDefinitionId}";
        return $"""<iframe src="{apiBaseUrl}/widget/agentport.html?{idQuery}" title="AgentPort Chat" loading="lazy"></iframe>""";
    }

    private static IReadOnlyList<string> ParseStringArray(string? json, IReadOnlyList<string> fallback)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return fallback;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<string[]>(json);
            return parsed is { Length: > 0 } ? parsed : fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static IResult Problem(int statusCode, string code, string detail)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: code,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    private sealed record LatestIngestionJob(
        Guid DocumentAssetId,
        Guid IngestionJobId,
        string Status,
        DateTime LastIngestedAt);

    private sealed record WidgetResolution(
        Deployment? Deployment,
        AgentDefinition? Agent,
        IResult? Problem);
}
