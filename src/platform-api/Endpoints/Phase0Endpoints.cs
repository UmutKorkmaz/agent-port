using AgentPort.PlatformApi.Contracts;
using AgentPort.PlatformApi.Data;
using AgentPort.PlatformApi.Infrastructure;
using AgentPort.PlatformApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AgentPort.PlatformApi.Endpoints;

public static class Phase0Endpoints
{
    public static IEndpointRouteBuilder MapPhase0Api(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1").WithTags("Phase 0");

        api.MapGet("/status", () => new StatusResponse(
            "AgentPort.PlatformApi",
            "0.1.0",
            "operational",
            DateTime.UtcNow));

        api.MapPost("/bootstrap/local", async (
            IHostEnvironment environment,
            IConfiguration configuration,
            LocalBootstrapService bootstrap,
            CancellationToken cancellationToken) =>
        {
            if (!DevEndpointGate.IsEnabled(environment, configuration))
            {
                return DevEndpointGate.Disabled();
            }

            var result = await bootstrap.BootstrapLocalAsync(cancellationToken);
            return Results.Ok(result);
        });

        api.MapGet("/workspaces", ListWorkspaces).RequireApiKey("workspaces:read");
        api.MapPost("/workspaces", CreateWorkspace).RequireApiKey("workspaces:write");

        api.MapGet("/projects", ListProjects).RequireApiKey("projects:read");
        api.MapPost("/projects", CreateProject).RequireApiKey("projects:write");

        api.MapGet("/model-routes", ListModelRoutes).RequireApiKey("models:route");
        api.MapPost("/model-routes", CreateModelRoute).RequireApiKey("models:route");

        api.MapGet("/agent-definitions", ListAgentDefinitions).RequireApiKey("agents:read");
        api.MapPost("/agent-definitions", CreateAgentDefinition).RequireApiKey("agents:write");
        api.MapPost("/agent-definitions/{agentId:guid}/chat", ChatWithAgent);

        api.MapGet("/datasets", ListDatasets).RequireApiKey("datasets:read");
        api.MapPost("/datasets", CreateDataset).RequireApiKey("datasets:write");
        api.MapPost("/datasets/{datasetId:guid}/documents", UploadDatasetDocument)
            .RequireApiKey("datasets:write")
            .DisableAntiforgery();
        api.MapGet("/ingestion-jobs", ListIngestionJobs).RequireApiKey("datasets:read");
        api.MapGet("/runs", ListRuns).RequireApiKey("runs:read");
        api.MapGet("/runs/{runId:guid}", GetRun).RequireApiKey("runs:read");
        api.MapGet("/traces/{traceId:guid}", GetTrace).RequireApiKey("runs:read");

        api.MapGet("/billing/provider-configs", ListPaymentProviderConfigs).RequireApiKey("billing:read");

        return routes;
    }

    private static async Task<IResult> ListWorkspaces(AgentPortDbContext db, CancellationToken cancellationToken)
    {
        var items = await db.Workspaces
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new WorkspaceResponse(item.Id, item.Name, item.Slug, item.CreatedAt, item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateWorkspace(
        CreateWorkspaceRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = ValidateNameAndSlug(request.Name, request.Slug);
        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        var slug = SlugGenerator.FromName(request.Slug ?? request.Name, "workspace");
        var exists = await db.Workspaces.AnyAsync(item => item.Slug == slug, cancellationToken);
        if (exists)
        {
            return Problem(StatusCodes.Status409Conflict, "workspace_slug_conflict", $"Workspace slug '{slug}' already exists.");
        }

        var workspace = new Workspace
        {
            Name = request.Name.Trim(),
            Slug = slug
        };

        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/workspaces/{workspace.Id}", ToResponse(workspace));
    }

    private static async Task<IResult> ListProjects(
        Guid? workspaceId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.Projects.AsNoTracking();
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == workspaceId.Value);
        }

        var items = await query
            .OrderBy(item => item.Name)
            .Select(item => new ProjectResponse(item.Id, item.WorkspaceId, item.Name, item.Slug, item.CreatedAt, item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateProject(
        CreateProjectRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = ValidateNameAndSlug(request.Name, request.Slug);
        if (request.WorkspaceId == Guid.Empty)
        {
            errors["workspaceId"] = ["WorkspaceId is required."];
        }

        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        var workspaceExists = await db.Workspaces.AnyAsync(item => item.Id == request.WorkspaceId, cancellationToken);
        if (!workspaceExists)
        {
            return Problem(StatusCodes.Status404NotFound, "workspace_not_found", "Workspace was not found.");
        }

        var slug = SlugGenerator.FromName(request.Slug ?? request.Name, "project");
        var exists = await db.Projects.AnyAsync(
            item => item.WorkspaceId == request.WorkspaceId && item.Slug == slug,
            cancellationToken);
        if (exists)
        {
            return Problem(StatusCodes.Status409Conflict, "project_slug_conflict", $"Project slug '{slug}' already exists.");
        }

        var project = new Project
        {
            WorkspaceId = request.WorkspaceId,
            Name = request.Name.Trim(),
            Slug = slug
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/projects/{project.Id}", ToResponse(project));
    }

    private static async Task<IResult> ListModelRoutes(
        Guid? workspaceId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.ModelRoutes.AsNoTracking();
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == workspaceId.Value);
        }

        var items = await query
            .OrderByDescending(item => item.IsDefault)
            .ThenBy(item => item.Priority)
            .ThenBy(item => item.Name)
            .Select(item => new ModelRouteResponse(
                item.Id,
                item.WorkspaceId,
                item.ProjectId,
                item.ProviderId,
                item.Name,
                item.Slug,
                item.ModelName,
                item.RouteType,
                item.Priority,
                item.IsDefault,
                item.IsEnabled,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateModelRoute(
        CreateModelRouteRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = ValidateNameAndSlug(request.Name, request.Slug);
        if (request.WorkspaceId == Guid.Empty)
        {
            errors["workspaceId"] = ["WorkspaceId is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ModelName))
        {
            errors["modelName"] = ["ModelName is required."];
        }

        if (request.ProviderId is null && string.IsNullOrWhiteSpace(request.ProviderName))
        {
            errors["provider"] = ["ProviderId or ProviderName is required."];
        }

        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        var workspaceExists = await db.Workspaces.AnyAsync(item => item.Id == request.WorkspaceId, cancellationToken);
        if (!workspaceExists)
        {
            return Problem(StatusCodes.Status404NotFound, "workspace_not_found", "Workspace was not found.");
        }

        if (request.ProjectId.HasValue)
        {
            var projectExists = await db.Projects.AnyAsync(
                item => item.Id == request.ProjectId.Value && item.WorkspaceId == request.WorkspaceId,
                cancellationToken);
            if (!projectExists)
            {
                return Problem(StatusCodes.Status404NotFound, "project_not_found", "Project was not found in this workspace.");
            }
        }

        var provider = request.ProviderId.HasValue
            ? await db.ModelProviders.FirstOrDefaultAsync(
                item => item.Id == request.ProviderId.Value
                    && (item.WorkspaceId == null || item.WorkspaceId == request.WorkspaceId),
                cancellationToken)
            : await db.ModelProviders.FirstOrDefaultAsync(
                item => item.Name == request.ProviderName!.Trim()
                    && (item.WorkspaceId == null || item.WorkspaceId == request.WorkspaceId),
                cancellationToken);
        if (provider is null)
        {
            return Problem(StatusCodes.Status404NotFound, "model_provider_not_found", "Model provider was not found.");
        }

        var slug = SlugGenerator.FromName(request.Slug ?? request.Name, "model-route");
        var exists = await db.ModelRoutes.AnyAsync(
            item => item.WorkspaceId == request.WorkspaceId && item.Slug == slug,
            cancellationToken);
        if (exists)
        {
            return Problem(StatusCodes.Status409Conflict, "model_route_slug_conflict", $"Model route slug '{slug}' already exists.");
        }

        var route = new ModelRoute
        {
            WorkspaceId = request.WorkspaceId,
            ProjectId = request.ProjectId,
            ProviderId = provider.Id,
            Name = request.Name.Trim(),
            Slug = slug,
            ModelName = request.ModelName.Trim(),
            RouteType = string.IsNullOrWhiteSpace(request.RouteType) ? "chat" : request.RouteType.Trim(),
            Priority = request.Priority ?? 100,
            IsDefault = request.IsDefault ?? false,
            IsEnabled = request.IsEnabled ?? true
        };

        db.ModelRoutes.Add(route);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/model-routes/{route.Id}", ToResponse(route));
    }

    private static async Task<IResult> ListAgentDefinitions(
        Guid? workspaceId,
        Guid? projectId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.AgentDefinitions.AsNoTracking();
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == workspaceId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(item => item.ProjectId == projectId.Value);
        }

        var items = await query
            .OrderBy(item => item.Name)
            .Select(item => new AgentDefinitionResponse(
                item.Id,
                item.WorkspaceId,
                item.ProjectId,
                item.AiSystemId,
                item.ModelRouteId,
                item.Name,
                item.Slug,
                item.Status,
                item.Instructions,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateAgentDefinition(
        CreateAgentDefinitionRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = ValidateNameAndSlug(request.Name, request.Slug);
        if (request.WorkspaceId == Guid.Empty)
        {
            errors["workspaceId"] = ["WorkspaceId is required."];
        }

        if (request.ProjectId == Guid.Empty)
        {
            errors["projectId"] = ["ProjectId is required."];
        }

        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        var projectExists = await db.Projects.AnyAsync(
            item => item.Id == request.ProjectId && item.WorkspaceId == request.WorkspaceId,
            cancellationToken);
        if (!projectExists)
        {
            return Problem(StatusCodes.Status404NotFound, "project_not_found", "Project was not found in this workspace.");
        }

        if (request.AiSystemId.HasValue)
        {
            var aiSystemExists = await db.AiSystems.AnyAsync(
                item => item.Id == request.AiSystemId.Value
                    && item.WorkspaceId == request.WorkspaceId
                    && item.ProjectId == request.ProjectId,
                cancellationToken);
            if (!aiSystemExists)
            {
                return Problem(StatusCodes.Status404NotFound, "ai_system_not_found", "AI system was not found in this project.");
            }
        }

        if (request.ModelRouteId.HasValue)
        {
            var routeExists = await db.ModelRoutes.AnyAsync(
                item => item.Id == request.ModelRouteId.Value && item.WorkspaceId == request.WorkspaceId,
                cancellationToken);
            if (!routeExists)
            {
                return Problem(StatusCodes.Status404NotFound, "model_route_not_found", "Model route was not found in this workspace.");
            }
        }

        var slug = SlugGenerator.FromName(request.Slug ?? request.Name, "agent");
        var exists = await db.AgentDefinitions.AnyAsync(
            item => item.WorkspaceId == request.WorkspaceId
                && item.ProjectId == request.ProjectId
                && item.Slug == slug,
            cancellationToken);
        if (exists)
        {
            return Problem(StatusCodes.Status409Conflict, "agent_definition_slug_conflict", $"Agent definition slug '{slug}' already exists.");
        }

        var agent = new AgentDefinition
        {
            WorkspaceId = request.WorkspaceId,
            ProjectId = request.ProjectId,
            AiSystemId = request.AiSystemId,
            ModelRouteId = request.ModelRouteId,
            Name = request.Name.Trim(),
            Slug = slug,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "draft" : request.Status.Trim(),
            Instructions = request.Instructions?.Trim() ?? string.Empty
        };

        db.AgentDefinitions.Add(agent);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/agent-definitions/{agent.Id}", ToResponse(agent));
    }

    private static async Task<IResult> ListPaymentProviderConfigs(
        Guid? workspaceId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.PaymentProviderConfigs.AsNoTracking();
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == workspaceId.Value);
        }

        var items = await query
            .OrderBy(item => item.Provider)
            .ThenBy(item => item.Mode)
            .Select(item => new PaymentProviderConfigResponse(
                item.Id,
                item.WorkspaceId,
                item.Provider,
                item.Mode,
                item.IsEnabled,
                item.ApiKeySecretReferenceId,
                item.WebhookSecretReferenceId,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> ListDatasets(
        Guid? workspaceId,
        Guid? projectId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.Datasets.AsNoTracking();
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == workspaceId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(item => item.ProjectId == projectId.Value);
        }

        var datasets = await query
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var datasetIds = datasets.Select(item => item.Id).ToArray();
        var knowledgeBases = await db.KnowledgeBases
            .AsNoTracking()
            .Where(item => datasetIds.Contains(item.DatasetId))
            .ToDictionaryAsync(item => item.DatasetId, item => item.Id, cancellationToken);
        var documentCounts = await db.DocumentAssets
            .AsNoTracking()
            .Where(item => datasetIds.Contains(item.DatasetId) && item.IsActive && item.DeletedAt == null)
            .GroupBy(item => item.DatasetId)
            .Select(group => new { DatasetId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.DatasetId, item => item.Count, cancellationToken);
        var chunkCounts = await db.DocumentChunks
            .AsNoTracking()
            .Where(item => datasetIds.Contains(item.DatasetId) && item.IsActive)
            .GroupBy(item => item.DatasetId)
            .Select(group => new { DatasetId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.DatasetId, item => item.Count, cancellationToken);

        return Results.Ok(datasets.Select(item => new DatasetResponse(
            item.Id,
            item.WorkspaceId,
            item.ProjectId,
            knowledgeBases.GetValueOrDefault(item.Id),
            item.Name,
            item.Slug,
            item.Kind,
            documentCounts.GetValueOrDefault(item.Id),
            chunkCounts.GetValueOrDefault(item.Id),
            item.CreatedAt,
            item.UpdatedAt)));
    }

    private static async Task<IResult> CreateDataset(
        CreateDatasetRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = ValidateNameAndSlug(request.Name, request.Slug);
        if (request.WorkspaceId == Guid.Empty)
        {
            errors["workspaceId"] = ["WorkspaceId is required."];
        }

        if (request.ProjectId == Guid.Empty)
        {
            errors["projectId"] = ["ProjectId is required."];
        }

        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        var projectExists = await db.Projects.AnyAsync(
            item => item.Id == request.ProjectId && item.WorkspaceId == request.WorkspaceId,
            cancellationToken);
        if (!projectExists)
        {
            return Problem(StatusCodes.Status404NotFound, "project_not_found", "Project was not found in this workspace.");
        }

        var slug = SlugGenerator.FromName(request.Slug ?? request.Name, "dataset");
        var exists = await db.Datasets.AnyAsync(
            item => item.WorkspaceId == request.WorkspaceId && item.ProjectId == request.ProjectId && item.Slug == slug,
            cancellationToken);
        if (exists)
        {
            return Problem(StatusCodes.Status409Conflict, "dataset_slug_conflict", $"Dataset slug '{slug}' already exists.");
        }

        var dataset = new Dataset
        {
            WorkspaceId = request.WorkspaceId,
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            Slug = slug,
            Kind = string.IsNullOrWhiteSpace(request.Kind) ? "documents" : request.Kind.Trim()
        };
        db.Datasets.Add(dataset);

        var knowledgeBase = new KnowledgeBase
        {
            WorkspaceId = request.WorkspaceId,
            ProjectId = request.ProjectId,
            DatasetId = dataset.Id,
            AgentDefinitionId = request.AgentDefinitionId,
            Name = $"{dataset.Name} Knowledge",
            Slug = slug,
            RetrievalStrategy = "basic-rag",
            EmbeddingModel = "intfloat/multilingual-e5-base",
            VectorStore = "pgvector",
            Status = "ready"
        };
        db.KnowledgeBases.Add(knowledgeBase);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/datasets/{dataset.Id}", new DatasetResponse(
            dataset.Id,
            dataset.WorkspaceId,
            dataset.ProjectId,
            knowledgeBase.Id,
            dataset.Name,
            dataset.Slug,
            dataset.Kind,
            0,
            0,
            dataset.CreatedAt,
            dataset.UpdatedAt));
    }

    private static async Task<IResult> UploadDatasetDocument(
        Guid datasetId,
        IFormFile file,
        AgentPortDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(Phase0Endpoints));
        if (file.Length == 0)
        {
            return ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["File is required."] });
        }

        var dataset = await db.Datasets.AsNoTracking().FirstOrDefaultAsync(item => item.Id == datasetId, cancellationToken);
        if (dataset is null)
        {
            return Problem(StatusCodes.Status404NotFound, "dataset_not_found", "Dataset was not found.");
        }

        var knowledgeBase = await db.KnowledgeBases
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.DatasetId == datasetId, cancellationToken);
        if (knowledgeBase is null)
        {
            return Problem(StatusCodes.Status409Conflict, "knowledge_base_missing", "Dataset does not have a knowledge base.");
        }

        await using var uploadStream = file.OpenReadStream();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(dataset.WorkspaceId.ToString()), "workspace_id");
        content.Add(new StringContent(dataset.ProjectId.ToString()), "project_id");
        content.Add(new StringContent(dataset.Id.ToString()), "dataset_id");
        content.Add(new StringContent(knowledgeBase.Id.ToString()), "knowledge_base_id");
        if (knowledgeBase.AgentDefinitionId.HasValue)
        {
            content.Add(new StringContent(knowledgeBase.AgentDefinitionId.Value.ToString()), "agent_definition_id");
        }

        var fileContent = new StreamContent(uploadStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var aiServicesUrl = configuration["AI_SERVICES_URL"] ?? "http://localhost:5002";
        var client = httpClientFactory.CreateClient();
        using var ingestRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{aiServicesUrl.TrimEnd('/')}/v1/ingest")
        {
            Content = content
        };
        AddInternalServiceToken(ingestRequest, configuration);
        using var response = await client.SendAsync(ingestRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "ai-services ingest proxy returned non-success status {StatusCode} for dataset {DatasetId}.",
                (int)response.StatusCode,
                dataset.Id);
        }

        return Results.Content(
            responseBody,
            "application/json",
            statusCode: (int)response.StatusCode);
    }

    private static async Task<IResult> ListIngestionJobs(
        Guid? datasetId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.IngestionJobs.AsNoTracking();
        if (datasetId.HasValue)
        {
            query = query.Where(item => item.DatasetId == datasetId.Value);
        }

        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Take(50)
            .Select(item => new IngestionJobResponse(
                item.Id,
                item.DatasetId,
                item.KnowledgeBaseId,
                item.DocumentAssetId,
                item.Status,
                item.ChunkCount,
                item.ErrorMessage,
                item.StartedAt,
                item.CompletedAt,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> ChatWithAgent(
        Guid agentId,
        AgentChatRequest request,
        HttpContext httpContext,
        AgentPortDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(Phase0Endpoints));
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return ValidationProblem(new Dictionary<string, string[]> { ["question"] = ["Question is required."] });
        }

        var agent = await db.AgentDefinitions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == agentId, cancellationToken);
        if (agent is null)
        {
            return Problem(StatusCodes.Status404NotFound, "agent_not_found", "Agent definition was not found.");
        }

        var apiKeyValidation = await ValidateRequiredApiKeyAsync(httpContext, db, agent.WorkspaceId, "runs:write", cancellationToken);
        if (apiKeyValidation.Problem is not null)
        {
            return apiKeyValidation.Problem;
        }

        var route = request.ModelRouteId.HasValue
            ? await db.ModelRoutes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.ModelRouteId.Value, cancellationToken)
            : agent.ModelRouteId.HasValue
                ? await db.ModelRoutes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == agent.ModelRouteId.Value, cancellationToken)
                : null;
        if (request.ModelRouteId.HasValue && route is null)
        {
            return Problem(StatusCodes.Status404NotFound, "model_route_not_found", "Model route was not found.");
        }

        if (route is not null)
        {
            if (route.WorkspaceId != agent.WorkspaceId)
            {
                return Problem(StatusCodes.Status409Conflict, "model_route_workspace_mismatch", "Model route must belong to the agent workspace.");
            }

            if (route.ProjectId.HasValue && route.ProjectId.Value != agent.ProjectId)
            {
                return Problem(StatusCodes.Status409Conflict, "model_route_project_mismatch", "Model route must belong to the agent project.");
            }
        }

        var provider = route is null
            ? null
            : await db.ModelProviders.AsNoTracking().FirstOrDefaultAsync(item => item.Id == route.ProviderId, cancellationToken);

        var aiServicesUrl = configuration["AI_SERVICES_URL"] ?? "http://localhost:5002";
        var client = httpClientFactory.CreateClient();
        var payload = JsonSerializer.Serialize(new
        {
            agent_id = agent.Id,
            question = request.Question.Trim(),
            top_k = Math.Clamp(request.TopK ?? 4, 1, 12),
            score_threshold = request.ScoreThreshold,
            model_route_id = route?.Id,
            model_route = route is null ? null : new
            {
                id = route.Id,
                name = route.Name,
                slug = route.Slug,
                model_name = route.ModelName,
                route_type = route.RouteType,
                is_enabled = route.IsEnabled,
                parameters = JsonDocument.Parse(route.ParametersJson).RootElement.Clone(),
                metadata = JsonDocument.Parse(route.MetadataJson).RootElement.Clone()
            },
            provider_id = provider?.Id,
            provider_name = provider?.Name,
            provider_kind = provider?.Kind,
            provider_base_url = provider?.BaseUrl,
            provider_metadata = provider is null
                ? (object?)null
                : JsonDocument.Parse(provider.MetadataJson).RootElement.Clone(),
            api_key_id = apiKeyValidation.ApiKey!.Id,
            auth_mode = apiKeyValidation.ApiKey.KeyType,
            auth_metadata = new
            {
                api_key_prefix = apiKeyValidation.ApiKey.Prefix,
                scopes = apiKeyValidation.ApiKey.Scopes
            }
        });
        using var chatRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{aiServicesUrl.TrimEnd('/')}/v1/chat")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        AddInternalServiceToken(chatRequest, configuration);
        using var response = await client.SendAsync(chatRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            await EnrichRunTraceAuthAsync(
                responseBody,
                db,
                apiKeyValidation.ApiKey!,
                Math.Clamp(request.TopK ?? 4, 1, 12),
                request.ScoreThreshold,
                logger,
                cancellationToken);
        }
        else
        {
            logger.LogWarning(
                "ai-services chat proxy returned non-success status {StatusCode} for agent {AgentId}.",
                (int)response.StatusCode,
                agent.Id);
        }

        return Results.Content(responseBody, "application/json", statusCode: (int)response.StatusCode);
    }

    private static async Task<IResult> ListRuns(
        Guid? agentId,
        Guid? projectId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.AgentRuns.AsNoTracking();
        if (agentId.HasValue)
        {
            query = query.Where(item => item.AgentDefinitionId == agentId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(item => item.ProjectId == projectId.Value);
        }

        var runs = await query
            .OrderByDescending(item => item.CreatedAt)
            .Take(50)
            .Select(item => ToResponse(item))
            .ToListAsync(cancellationToken);

        return Results.Ok(runs);
    }

    private static async Task<IResult> GetRun(
        Guid runId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var run = await db.AgentRuns.AsNoTracking().FirstOrDefaultAsync(item => item.Id == runId, cancellationToken);
        return run is null
            ? Problem(StatusCodes.Status404NotFound, "run_not_found", "Run was not found.")
            : Results.Ok(ToResponse(run));
    }

    private static async Task<IResult> GetTrace(
        Guid traceId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var trace = await db.TraceRecords.AsNoTracking().FirstOrDefaultAsync(item => item.Id == traceId, cancellationToken);
        return trace is null
            ? Problem(StatusCodes.Status404NotFound, "trace_not_found", "Trace was not found.")
            : Results.Ok(new TraceRecordResponse(
                trace.Id,
                trace.WorkspaceId,
                trace.ProjectId,
                trace.AgentDefinitionId,
                trace.ModelRouteId,
                trace.CorrelationId,
                trace.TraceType,
                trace.Status,
                trace.InputTokens,
                trace.OutputTokens,
                trace.CostAmount,
                trace.StartedAt,
                trace.EndedAt,
                trace.MetadataJson,
                trace.CreatedAt));
    }

    private static Dictionary<string, string[]> ValidateNameAndSlug(string name, string? slug)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Name is required."];
        }

        if (slug is not null && string.IsNullOrWhiteSpace(slug))
        {
            errors["slug"] = ["Slug cannot be blank."];
        }

        return errors;
    }

    private static IResult ValidationProblem(Dictionary<string, string[]> errors)
    {
        return Results.ValidationProblem(errors, title: "Validation failed");
    }

    private static IResult Problem(int statusCode, string code, string detail)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: code,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    private static WorkspaceResponse ToResponse(Workspace item)
    {
        return new WorkspaceResponse(item.Id, item.Name, item.Slug, item.CreatedAt, item.UpdatedAt);
    }

    private static ProjectResponse ToResponse(Project item)
    {
        return new ProjectResponse(item.Id, item.WorkspaceId, item.Name, item.Slug, item.CreatedAt, item.UpdatedAt);
    }

    private static ModelRouteResponse ToResponse(ModelRoute item)
    {
        return new ModelRouteResponse(
            item.Id,
            item.WorkspaceId,
            item.ProjectId,
            item.ProviderId,
            item.Name,
            item.Slug,
            item.ModelName,
            item.RouteType,
            item.Priority,
            item.IsDefault,
            item.IsEnabled,
            item.CreatedAt,
            item.UpdatedAt);
    }

    private static AgentDefinitionResponse ToResponse(AgentDefinition item)
    {
        return new AgentDefinitionResponse(
            item.Id,
            item.WorkspaceId,
            item.ProjectId,
            item.AiSystemId,
            item.ModelRouteId,
            item.Name,
            item.Slug,
            item.Status,
            item.Instructions,
            item.CreatedAt,
            item.UpdatedAt);
    }

    private static AgentRunResponse ToResponse(AgentRun item)
    {
        return new AgentRunResponse(
            item.Id,
            item.WorkspaceId,
            item.ProjectId,
            item.AgentDefinitionId,
            item.KnowledgeBaseId,
            item.TraceRecordId,
            item.Question,
            item.Answer,
            item.Status,
            item.FallbackMode,
            item.CitationsJson,
            item.RetrievedChunksJson,
            item.LatencyMs,
            item.EstimatedCost,
            item.CreatedAt);
    }

    private static async Task<ApiKeyValidation> ValidateRequiredApiKeyAsync(
        HttpContext httpContext,
        AgentPortDbContext db,
        Guid workspaceId,
        string requiredScope,
        CancellationToken cancellationToken)
    {
        var rawApiKey = GetRawApiKey(httpContext);
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            return new ApiKeyValidation(null, Problem(StatusCodes.Status401Unauthorized, "api_key_missing", "API key is required."));
        }

        var keyHash = ApiKeyHasher.Hash(rawApiKey);
        var apiKey = await db.ApiKeys.FirstOrDefaultAsync(item => item.KeyHash == keyHash, cancellationToken);
        if (apiKey is null)
        {
            return new ApiKeyValidation(null, Problem(StatusCodes.Status401Unauthorized, "api_key_invalid", "API key is invalid."));
        }

        if (apiKey.WorkspaceId != workspaceId)
        {
            return new ApiKeyValidation(null, Problem(StatusCodes.Status403Forbidden, "api_key_workspace_mismatch", "API key cannot access this workspace."));
        }

        if (apiKey.RevokedAt.HasValue || (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value <= DateTime.UtcNow))
        {
            return new ApiKeyValidation(null, Problem(StatusCodes.Status401Unauthorized, "api_key_inactive", "API key is inactive."));
        }

        if (!apiKey.Scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase)
            && !apiKey.Scopes.Contains("workspace:admin", StringComparer.OrdinalIgnoreCase))
        {
            return new ApiKeyValidation(null, Problem(StatusCodes.Status403Forbidden, "api_key_scope_missing", $"API key requires '{requiredScope}'."));
        }

        apiKey.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new ApiKeyValidation(apiKey, null);
    }

    private static async Task EnrichRunTraceAuthAsync(
        string responseBody,
        AgentPortDbContext db,
        ApiKey apiKey,
        int topK,
        decimal? scoreThreshold,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(responseBody);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Skipping run-trace enrichment: ai-services chat response was not valid JSON.");
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            var runId = TryGetGuid(root, "run_id", "runId");
            var traceId = TryGetGuid(root, "trace_id_record", "traceIdRecord");
            var retrieval = TryGetProperty(root, "retrieval");
            var noAnswer = retrieval.HasValue
                && TryGetBoolean(retrieval.Value, "no_answer", "noAnswer") == true;
            var bestScore = retrieval.HasValue ? TryGetDecimal(retrieval.Value, "max_score", "maxScore") : null;
            var responseScoreThreshold = retrieval.HasValue
                ? TryGetDecimal(retrieval.Value, "score_threshold", "scoreThreshold")
                : scoreThreshold;
            var qualityStatus = noAnswer ? "no_answer" : "passed";
            var noAnswerReason = noAnswer ? "retrieval_score_below_threshold" : null;
            var authMetadata = JsonSerializer.Serialize(new
            {
                api_key_prefix = apiKey.Prefix,
                key_type = apiKey.KeyType,
                scopes = apiKey.Scopes
            });

            if (runId.HasValue)
            {
                var run = await db.AgentRuns.FirstOrDefaultAsync(item => item.Id == runId.Value, cancellationToken);
                if (run is not null)
                {
                    run.ApiKeyId = apiKey.Id;
                    run.AuthMode = apiKey.KeyType;
                    run.AuthMetadataJson = authMetadata;
                    run.TopK = topK;
                    run.ScoreThreshold = responseScoreThreshold;
                    run.BestRetrievalScore = bestScore;
                    run.QualityStatus = qualityStatus;
                    run.NoAnswerReason = noAnswerReason;
                    run.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (traceId.HasValue)
            {
                var trace = await db.TraceRecords.FirstOrDefaultAsync(item => item.Id == traceId.Value, cancellationToken);
                if (trace is not null)
                {
                    trace.ApiKeyId = apiKey.Id;
                    trace.AuthMode = apiKey.KeyType;
                    trace.AuthMetadataJson = authMetadata;
                    trace.TopK = topK;
                    trace.ScoreThreshold = responseScoreThreshold;
                    trace.BestRetrievalScore = bestScore;
                    trace.QualityStatus = qualityStatus;
                    trace.NoAnswerReason = noAnswerReason;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static JsonElement? TryGetProperty(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var property))
            {
                return property;
            }
        }

        return null;
    }

    private static Guid? TryGetGuid(JsonElement root, params string[] names)
    {
        var value = TryGetString(root, names);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static string? TryGetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
        }

        return null;
    }

    private static bool? TryGetBoolean(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var property)
                && (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False))
            {
                return property.GetBoolean();
            }
        }

        return null;
    }

    private static decimal? TryGetDecimal(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var property)
                && property.ValueKind == JsonValueKind.Number
                && property.TryGetDecimal(out var value))
            {
                return value;
            }
        }

        return null;
    }

    // When AI_SERVICES_INTERNAL_TOKEN is configured, forward it on outbound calls to ai-services so
    // the ai-services side can enforce internal-only access. No-op when the token is absent.
    private static void AddInternalServiceToken(HttpRequestMessage request, IConfiguration configuration)
    {
        var internalToken = configuration["AI_SERVICES_INTERNAL_TOKEN"]
            ?? Environment.GetEnvironmentVariable("AI_SERVICES_INTERNAL_TOKEN");
        if (string.IsNullOrWhiteSpace(internalToken))
        {
            return;
        }

        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {internalToken}");
        request.Headers.TryAddWithoutValidation("x-internal-token", internalToken);
    }

    private static string? GetRawApiKey(HttpContext httpContext)
    {
        var explicitHeader = httpContext.Request.Headers["x-agentport-api-key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(explicitHeader))
        {
            return explicitHeader.Trim();
        }

        var authorization = httpContext.Request.Headers.Authorization.FirstOrDefault();
        const string bearerPrefix = "Bearer ";
        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[bearerPrefix.Length..].Trim();
        }

        return null;
    }

    private sealed record ApiKeyValidation(ApiKey? ApiKey, IResult? Problem);
}
