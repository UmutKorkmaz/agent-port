using AgentPort.PlatformApi.Contracts;
using AgentPort.PlatformApi.Data;
using AgentPort.PlatformApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AgentPort.PlatformApi.Endpoints;

public static class Phase12Endpoints
{
    private const long DefaultQuoteInputTokens = 1_000;
    private const long DefaultQuoteOutputTokens = 1_000;

    public static IEndpointRouteBuilder MapPhase12Api(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1").WithTags("Phase 1.2");

        api.MapGet("/model-providers", ListModelProviders).RequireApiKey("models:route");
        api.MapPost("/model-providers", CreateModelProvider).RequireApiKey("models:route");
        api.MapPatch("/model-providers", PatchModelProvider).RequireApiKey("models:route");

        api.MapGet("/model-catalog", ListModelCatalog).RequireApiKey("models:route");
        api.MapGet("/provider-status", ListProviderStatus).RequireApiKey("models:route");
        api.MapGet("/local-models", ListLocalModels).RequireApiKey("models:route");
        api.MapGet("/local-model/status", GetLocalModelStatus).RequireApiKey("models:route");
        api.MapPost("/local-models/pull", QueueLocalModelPull).RequireApiKey("models:route");
        api.MapGet("/provider-price-snapshots", ListProviderPriceSnapshots).RequireApiKey("models:route");
        api.MapPost("/model-routes/{routeId:guid}/test", TestModelRoute).RequireApiKey("models:route");
        api.MapPatch("/agent-definitions/{agentId:guid}/model-route", PatchAgentDefinitionModelRoute).RequireApiKey("agents:write");

        return routes;
    }

    private static async Task<IResult> ListModelProviders(
        Guid? workspaceId,
        bool? includeDisabled,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.ModelProviders.AsNoTracking();
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == null || item.WorkspaceId == workspaceId.Value);
        }

        if (includeDisabled == false)
        {
            query = query.Where(item => item.IsEnabled);
        }

        var providers = await query
            .OrderByDescending(item => item.Name == "ollama")
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var providerIds = providers.Select(item => item.Id).ToArray();
        var accounts = await LoadAccountCountsAsync(db, providerIds, workspaceId, cancellationToken);
        var catalogCounts = await LoadCatalogCountsAsync(db, providerIds, workspaceId, cancellationToken);
        var latestChecks = await LoadLatestChecksAsync(db, providerIds, workspaceId, cancellationToken);

        return Results.Ok(providers.Select(item => ToProviderResponse(
            item,
            accounts.GetValueOrDefault(item.Id),
            catalogCounts.GetValueOrDefault(item.Id),
            latestChecks.GetValueOrDefault(item.Id))));
    }

    private static async Task<IResult> CreateModelProvider(
        [FromBody] CreateModelProviderCatalogRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = ValidateProviderInput(request.Name, request.Kind);
        if (request.ProjectId.HasValue && !request.WorkspaceId.HasValue)
        {
            errors["workspaceId"] = ["WorkspaceId is required when ProjectId is set."];
        }

        if (!string.IsNullOrWhiteSpace(request.AccountName) && !request.WorkspaceId.HasValue)
        {
            errors["accountName"] = ["Provider accounts require a WorkspaceId."];
        }

        if (errors.Count > 0)
        {
            return ValidationProblem(errors);
        }

        if (request.WorkspaceId.HasValue)
        {
            var workspaceExists = await db.Workspaces.AnyAsync(item => item.Id == request.WorkspaceId.Value, cancellationToken);
            if (!workspaceExists)
            {
                return Problem(StatusCodes.Status404NotFound, "workspace_not_found", "Workspace was not found.");
            }
        }

        if (request.ProjectId.HasValue)
        {
            var projectExists = await db.Projects.AnyAsync(
                item => item.Id == request.ProjectId.Value && item.WorkspaceId == request.WorkspaceId!.Value,
                cancellationToken);
            if (!projectExists)
            {
                return Problem(StatusCodes.Status404NotFound, "project_not_found", "Project was not found in this workspace.");
            }
        }

        var providerName = request.Name.Trim();
        var exists = await db.ModelProviders.AnyAsync(
            item => item.WorkspaceId == request.WorkspaceId && item.Name == providerName,
            cancellationToken);
        if (exists)
        {
            return Problem(StatusCodes.Status409Conflict, "model_provider_name_conflict", $"Model provider '{providerName}' already exists.");
        }

        var provider = new ModelProvider
        {
            WorkspaceId = request.WorkspaceId,
            Name = providerName,
            Kind = request.Kind.Trim(),
            BaseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? null : request.BaseUrl.Trim(),
            IsEnabled = request.IsEnabled ?? true,
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson.Trim()
        };
        db.ModelProviders.Add(provider);

        ProviderAccount? account = null;
        if (!string.IsNullOrWhiteSpace(request.AccountName) && request.WorkspaceId.HasValue)
        {
            var accountName = request.AccountName.Trim();
            account = new ProviderAccount
            {
                WorkspaceId = request.WorkspaceId.Value,
                ProjectId = request.ProjectId,
                ProviderId = provider.Id,
                CredentialSecretReferenceId = request.CredentialSecretReferenceId,
                Name = accountName,
                Slug = SlugGenerator.FromName(accountName, "provider-account"),
                Status = request.CredentialSecretReferenceId.HasValue ? "configured" : "pending_credentials",
                IsEnabled = request.IsEnabled ?? true,
                MetadataJson = """{"phase":"1.2","createdBy":"model-providers-api"}"""
            };
            db.ProviderAccounts.Add(account);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/v1/model-providers?id={provider.Id}",
            ToProviderResponse(
                provider,
                account is null ? null : new ProviderAccountCounts(provider.Id, 1, account.IsEnabled ? 1 : 0),
                0,
                null));
    }

    private static async Task<IResult> PatchModelProvider(
        [FromBody] PatchModelProviderCatalogRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        if (request.Id is null && string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblem(new Dictionary<string, string[]>
            {
                ["id"] = ["Id or Name is required."]
            });
        }

        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Name cannot be blank."]
            });
        }

        if (request.Kind is not null && string.IsNullOrWhiteSpace(request.Kind))
        {
            return ValidationProblem(new Dictionary<string, string[]>
            {
                ["kind"] = ["Kind cannot be blank."]
            });
        }

        var provider = request.Id.HasValue
            ? await db.ModelProviders.FirstOrDefaultAsync(item => item.Id == request.Id.Value, cancellationToken)
            : await db.ModelProviders.FirstOrDefaultAsync(
                item => item.WorkspaceId == request.WorkspaceId && item.Name == request.Name!.Trim(),
                cancellationToken);
        if (provider is null)
        {
            return Problem(StatusCodes.Status404NotFound, "model_provider_not_found", "Model provider was not found.");
        }

        if (request.Name is not null)
        {
            var newName = request.Name.Trim();
            if (!newName.Equals(provider.Name, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await db.ModelProviders.AnyAsync(
                    item => item.Id != provider.Id
                        && item.WorkspaceId == provider.WorkspaceId
                        && item.Name == newName,
                    cancellationToken);
                if (exists)
                {
                    return Problem(StatusCodes.Status409Conflict, "model_provider_name_conflict", $"Model provider '{newName}' already exists.");
                }

                provider.Name = newName;
            }
        }

        if (request.Kind is not null)
        {
            provider.Kind = request.Kind.Trim();
        }

        if (request.BaseUrl is not null)
        {
            provider.BaseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? null : request.BaseUrl.Trim();
        }

        if (request.IsEnabled.HasValue)
        {
            provider.IsEnabled = request.IsEnabled.Value;
        }

        if (request.MetadataJson is not null)
        {
            provider.MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson.Trim();
        }

        provider.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var accountCounts = await LoadAccountCountsAsync(db, [provider.Id], provider.WorkspaceId, cancellationToken);
        var catalogCounts = await LoadCatalogCountsAsync(db, [provider.Id], provider.WorkspaceId, cancellationToken);
        var latestChecks = await LoadLatestChecksAsync(db, [provider.Id], provider.WorkspaceId, cancellationToken);
        return Results.Ok(ToProviderResponse(
            provider,
            accountCounts.GetValueOrDefault(provider.Id),
            catalogCounts.GetValueOrDefault(provider.Id),
            latestChecks.GetValueOrDefault(provider.Id)));
    }

    private static async Task<IResult> ListModelCatalog(
        Guid? workspaceId,
        Guid? providerId,
        string? providerName,
        string? routeType,
        bool? includeDisabled,
        bool? localOnly,
        long? inputTokens,
        long? outputTokens,
        AgentPortDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var query = db.ModelCatalogEntries.AsNoTracking();
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == null || item.WorkspaceId == workspaceId.Value);
        }

        if (providerId.HasValue)
        {
            query = query.Where(item => item.ProviderId == providerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(providerName))
        {
            var normalizedProviderName = providerName.Trim();
            var providerIdsByNameQuery = db.ModelProviders
                .AsNoTracking()
                .Where(item => item.Name == normalizedProviderName);
            if (workspaceId.HasValue)
            {
                providerIdsByNameQuery = providerIdsByNameQuery.Where(
                    item => item.WorkspaceId == null || item.WorkspaceId == workspaceId.Value);
            }

            var providerIdsByName = await providerIdsByNameQuery
                .Select(item => item.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(item => providerIdsByName.Contains(item.ProviderId));
        }

        if (!string.IsNullOrWhiteSpace(routeType))
        {
            var normalizedRouteType = routeType.Trim();
            query = query.Where(item => item.RouteType == normalizedRouteType);
        }

        if (includeDisabled != true)
        {
            query = query.Where(item => item.IsEnabled);
        }

        if (localOnly == true)
        {
            query = query.Where(item => item.IsLocal);
        }

        var entries = await query
            .OrderByDescending(item => item.IsLocal)
            .ThenBy(item => item.DisplayName)
            .Take(250)
            .ToListAsync(cancellationToken);
        var providerIds = entries.Select(item => item.ProviderId).Distinct().ToArray();
        var providers = await db.ModelProviders
            .AsNoTracking()
            .Where(item => providerIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var priceSnapshots = await LoadLatestPriceSnapshotsAsync(db, providerIds, cancellationToken);
        var quoteInputTokens = NormalizeTokenCount(inputTokens, DefaultQuoteInputTokens);
        var quoteOutputTokens = NormalizeTokenCount(outputTokens, DefaultQuoteOutputTokens);
        var platformFee = GetPlatformFeeFixedUsd(configuration);

        return Results.Ok(entries.Select(item =>
        {
            var provider = providers[item.ProviderId];
            var snapshot = FindPriceSnapshot(priceSnapshots, item.ProviderId, item.ModelName);
            return new ModelCatalogEntryResponse(
                item.Id,
                item.WorkspaceId,
                item.ProviderId,
                provider.Name,
                provider.Kind,
                item.ModelName,
                item.DisplayName,
                item.Modality,
                item.RouteType,
                item.ContextWindowTokens,
                item.MaxOutputTokens,
                item.SupportsTools,
                item.SupportsJsonMode,
                item.SupportsStreaming,
                item.IsLocal,
                item.IsEnabled,
                item.Status,
                item.CapabilitiesJson,
                item.MetadataJson,
                BuildCostQuote(snapshot, quoteInputTokens, quoteOutputTokens, platformFee),
                item.CreatedAt,
                item.UpdatedAt);
        }));
    }

    private static async Task<IResult> ListProviderStatus(
        Guid? workspaceId,
        Guid? providerId,
        bool? includeDisabled,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var providerQuery = db.ModelProviders.AsNoTracking();
        if (workspaceId.HasValue)
        {
            providerQuery = providerQuery.Where(item => item.WorkspaceId == null || item.WorkspaceId == workspaceId.Value);
        }

        if (providerId.HasValue)
        {
            providerQuery = providerQuery.Where(item => item.Id == providerId.Value);
        }

        if (includeDisabled == false)
        {
            providerQuery = providerQuery.Where(item => item.IsEnabled);
        }

        var providers = await providerQuery
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var providerIds = providers.Select(item => item.Id).ToArray();
        var latestChecks = await LoadLatestChecksAsync(db, providerIds, workspaceId, cancellationToken);

        return Results.Ok(providers.Select(provider =>
        {
            var latest = latestChecks.GetValueOrDefault(provider.Id);
            return new ProviderStatusResponse(
                provider.Id,
                provider.WorkspaceId,
                provider.Name,
                provider.Kind,
                provider.IsEnabled,
                latest?.ProviderAccountId,
                latest?.ModelRouteId,
                latest?.Status ?? (provider.IsEnabled ? "not_checked" : "disabled"),
                latest?.CheckType ?? "derived",
                latest?.Target ?? provider.BaseUrl,
                latest?.LatencyMs,
                latest?.ErrorCode,
                latest?.ErrorMessage,
                latest?.CheckedAt,
                latest?.MetadataJson ?? provider.MetadataJson);
        }));
    }

    private static async Task<IResult> ListLocalModels(
        Guid? workspaceId,
        Guid? providerId,
        bool? includeMissing,
        long? inputTokens,
        long? outputTokens,
        AgentPortDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var query = db.LocalModelInventory.AsNoTracking();
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == workspaceId.Value);
        }

        if (providerId.HasValue)
        {
            query = query.Where(item => item.ProviderId == providerId.Value);
        }

        if (includeMissing == false)
        {
            query = query.Where(item => item.IsInstalled);
        }

        var models = await query
            .OrderByDescending(item => item.ModelName == "gemma4:e4b")
            .ThenBy(item => item.ModelName)
            .ToListAsync(cancellationToken);
        var providerIds = models.Select(item => item.ProviderId).Distinct().ToArray();
        var providers = await db.ModelProviders
            .AsNoTracking()
            .Where(item => providerIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var priceSnapshots = await LoadLatestPriceSnapshotsAsync(db, providerIds, cancellationToken);
        var quoteInputTokens = NormalizeTokenCount(inputTokens, DefaultQuoteInputTokens);
        var quoteOutputTokens = NormalizeTokenCount(outputTokens, DefaultQuoteOutputTokens);
        var platformFee = GetPlatformFeeFixedUsd(configuration);

        return Results.Ok(models.Select(item =>
        {
            var provider = providers[item.ProviderId];
            var snapshot = FindPriceSnapshot(priceSnapshots, item.ProviderId, item.ModelName);
            return new LocalModelInventoryResponse(
                item.Id,
                item.WorkspaceId,
                item.ProviderId,
                provider.Name,
                item.ModelName,
                item.Digest,
                item.Family,
                item.ParameterSize,
                item.Quantization,
                item.SizeBytes,
                item.IsInstalled,
                item.Status,
                item.LastSeenAt,
                item.MetadataJson,
                BuildCostQuote(snapshot, quoteInputTokens, quoteOutputTokens, platformFee));
        }));
    }

    private static async Task<IResult> GetLocalModelStatus(
        Guid? workspaceId,
        AgentPortDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var query = db.LocalModelInventory.AsNoTracking();
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == workspaceId.Value);
        }

        var models = await query
            .OrderByDescending(item => item.ModelName == "gemma4:e4b")
            .ThenBy(item => item.ModelName)
            .ToListAsync(cancellationToken);
        var selected = models.FirstOrDefault(item => item.ModelName == (configuration["OLLAMA_MODEL"] ?? "gemma4:e4b"))
            ?? models.FirstOrDefault(item => item.ModelName == "gemma4:e4b")
            ?? models.FirstOrDefault();
        var provider = selected is null
            ? await db.ModelProviders.AsNoTracking().FirstOrDefaultAsync(item => item.Name == "ollama", cancellationToken)
            : await db.ModelProviders.AsNoTracking().FirstOrDefaultAsync(item => item.Id == selected.ProviderId, cancellationToken);

        return Results.Ok(new
        {
            status = selected?.Status ?? "not_reported",
            provider = provider?.Name ?? "ollama",
            model = selected?.ModelName ?? configuration["OLLAMA_MODEL"] ?? "gemma4:e4b",
            modelName = selected?.ModelName ?? configuration["OLLAMA_MODEL"] ?? "gemma4:e4b",
            enabled = provider?.IsEnabled ?? false,
            baseUrl = provider?.BaseUrl ?? configuration["OLLAMA_BASE_URL"] ?? "http://localhost:11434",
            routeHealth = selected is null ? "missing_inventory" : selected.IsInstalled ? "ready" : "missing",
            checkedAt = selected?.LastSeenAt,
            models = models.Select(item => item.ModelName).ToArray(),
            metadata = new
            {
                pullProfile = configuration["OLLAMA_PULL_PROFILE"] ?? "core",
                installScript = "scripts/ollama-models.sh",
                liveCalls = configuration.GetValue("PROVIDER_LIVE_CALLS", false)
            }
        });
    }

    private static async Task<IResult> QueueLocalModelPull(
        [FromBody] PullLocalModelRequest request,
        AgentPortDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return ValidationProblem(new Dictionary<string, string[]> { ["workspaceId"] = ["WorkspaceId is required."] });
        }

        var modelName = string.IsNullOrWhiteSpace(request.ModelName)
            ? configuration["OLLAMA_MODEL"] ?? "gemma4:e4b"
            : request.ModelName.Trim();
        var workspaceExists = await db.Workspaces.AnyAsync(item => item.Id == request.WorkspaceId, cancellationToken);
        if (!workspaceExists)
        {
            return Problem(StatusCodes.Status404NotFound, "workspace_not_found", "Workspace was not found.");
        }

        var provider = await db.ModelProviders.FirstOrDefaultAsync(
            item => item.WorkspaceId == request.WorkspaceId && item.Name == "ollama",
            cancellationToken);
        if (provider is null)
        {
            return Problem(StatusCodes.Status404NotFound, "ollama_provider_missing", "Ollama provider was not found for this workspace.");
        }

        var inventory = await db.LocalModelInventory.FirstOrDefaultAsync(
            item => item.WorkspaceId == request.WorkspaceId
                && item.ProviderId == provider.Id
                && item.ModelName == modelName,
            cancellationToken);
        if (inventory is null)
        {
            inventory = new LocalModelInventory
            {
                WorkspaceId = request.WorkspaceId,
                ProviderId = provider.Id,
                ModelName = modelName,
                Family = modelName.Split(':')[0],
                Status = "pull_queued",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    phase = "1.2",
                    dryRun = true,
                    script = "scripts/ollama-models.sh"
                })
            };
            db.LocalModelInventory.Add(inventory);
        }
        else if (!inventory.IsInstalled)
        {
            inventory.Status = "pull_queued";
            inventory.UpdatedAt = DateTime.UtcNow;
        }

        var job = new ProviderSyncJob
        {
            WorkspaceId = request.WorkspaceId,
            ProviderId = provider.Id,
            JobType = "ollama_pull",
            Status = "queued",
            MetadataJson = JsonSerializer.Serialize(new
            {
                modelName,
                profile = request.Profile ?? configuration["OLLAMA_PULL_PROFILE"] ?? "core",
                dryRun = true,
                operatorCommand = $"scripts/ollama-models.sh {request.Profile ?? "core"}"
            })
        };
        db.ProviderSyncJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Accepted($"/api/v1/local-models?workspaceId={request.WorkspaceId}", new
        {
            jobId = job.Id,
            localModelInventoryId = inventory.Id,
            modelName,
            status = job.Status,
            dryRun = true,
            message = "Pull was queued in platform metadata. Run scripts/ollama-models.sh to download models on this host."
        });
    }

    private static async Task<IResult> ListProviderPriceSnapshots(
        Guid? workspaceId,
        Guid? providerId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.ProviderPriceSnapshots.AsNoTracking();
        if (providerId.HasValue)
        {
            query = query.Where(item => item.ProviderId == providerId.Value);
        }

        var snapshots = await query
            .Join(
                db.ModelProviders.AsNoTracking(),
                snapshot => snapshot.ProviderId,
                provider => provider.Id,
                (snapshot, provider) => new { snapshot, provider })
            .Where(item => !workspaceId.HasValue || item.provider.WorkspaceId == null || item.provider.WorkspaceId == workspaceId.Value)
            .OrderByDescending(item => item.snapshot.CapturedAt)
            .Take(250)
            .Select(item => new
            {
                item.snapshot.Id,
                item.snapshot.ProviderId,
                ProviderName = item.provider.Name,
                item.snapshot.ModelName,
                item.snapshot.Currency,
                item.snapshot.InputTokenPricePerMillion,
                item.snapshot.OutputTokenPricePerMillion,
                item.snapshot.RequestPrice,
                item.snapshot.CapturedAt,
                item.snapshot.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(snapshots);
    }

    private static async Task<IResult> TestModelRoute(
        Guid routeId,
        [FromBody] TestModelRouteRequest? request,
        AgentPortDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var route = await db.ModelRoutes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == routeId, cancellationToken);
        if (route is null)
        {
            return Problem(StatusCodes.Status404NotFound, "model_route_not_found", "Model route was not found.");
        }

        var provider = await db.ModelProviders.AsNoTracking().FirstOrDefaultAsync(item => item.Id == route.ProviderId, cancellationToken);
        if (provider is null)
        {
            return Problem(StatusCodes.Status409Conflict, "model_provider_missing", "Model route provider is missing.");
        }

        var account = await db.ProviderAccounts
            .AsNoTracking()
            .Where(item => item.WorkspaceId == route.WorkspaceId && item.ProviderId == provider.Id && item.IsEnabled)
            .OrderByDescending(item => item.Status == "configured")
            .ThenBy(item => item.Name)
            .FirstOrDefaultAsync(cancellationToken);
        var localModel = IsLocalProvider(provider)
            ? await db.LocalModelInventory
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.WorkspaceId == route.WorkspaceId
                        && item.ProviderId == provider.Id
                        && item.ModelName == route.ModelName,
                    cancellationToken)
            : null;
        var status = ResolveRouteStatus(route, provider, account, localModel);
        var checkedAt = DateTime.UtcNow;
        var prompt = string.IsNullOrWhiteSpace(request?.Prompt) ? null : request.Prompt.Trim();
        var inputTokens = NormalizeTokenCount(request?.InputTokens, prompt is null ? DefaultQuoteInputTokens : EstimatePromptTokens(prompt));
        var outputTokens = NormalizeTokenCount(request?.OutputTokens, DefaultQuoteOutputTokens);
        var snapshot = await db.ProviderPriceSnapshots
            .AsNoTracking()
            .Where(item => item.ProviderId == provider.Id && item.ModelName == route.ModelName)
            .OrderByDescending(item => item.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var quote = BuildCostQuote(snapshot, inputTokens, outputTokens, GetPlatformFeeFixedUsd(configuration));
        var message = BuildRouteTestMessage(status, provider, route, localModel);

        var check = new ProviderStatusCheck
        {
            WorkspaceId = route.WorkspaceId,
            ProviderId = provider.Id,
            ProviderAccountId = account?.Id,
            ModelRouteId = route.Id,
            Status = status,
            CheckType = "route_test",
            Target = $"{provider.Name}:{route.ModelName}",
            LatencyMs = 0,
            ErrorCode = status == "ready" ? null : status,
            ErrorMessage = status == "ready" ? null : message,
            CheckedAt = checkedAt,
            MetadataJson = JsonSerializer.Serialize(new
            {
                phase = "1.2",
                liveProviderCall = false,
                promptProvided = prompt is not null
            })
        };
        db.ProviderStatusChecks.Add(check);

        ProviderUsageEvent? usageEvent = null;
        if (request?.PersistUsageEvent != false)
        {
            usageEvent = new ProviderUsageEvent
            {
                WorkspaceId = route.WorkspaceId,
                ProjectId = route.ProjectId,
                ProviderId = provider.Id,
                ProviderAccountId = account?.Id,
                ModelRouteId = route.Id,
                ModelName = route.ModelName,
                Operation = "route_test",
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                RequestCount = 1,
                Currency = quote.Currency,
                ProviderCost = quote.ProviderCostUsd,
                PlatformFee = quote.PlatformFeeFixedUsd,
                TotalCost = quote.EstimatedTotalUsd,
                Status = status,
                OccurredAt = checkedAt,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    phase = "1.2",
                    liveProviderCall = false,
                    hasPriceSnapshot = quote.HasPriceSnapshot
                })
            };
            db.ProviderUsageEvents.Add(usageEvent);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new ModelRouteTestResponse(
            route.Id,
            route.WorkspaceId,
            route.ProjectId,
            provider.Id,
            provider.Name,
            route.ModelName,
            route.RouteType,
            status,
            message,
            false,
            check.Id,
            usageEvent?.Id,
            quote,
            checkedAt));
    }

    private static async Task<IResult> PatchAgentDefinitionModelRoute(
        Guid agentId,
        [FromBody] PatchAgentDefinitionModelRouteRequest request,
        AgentPortDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var agent = await db.AgentDefinitions.FirstOrDefaultAsync(item => item.Id == agentId, cancellationToken);
        if (agent is null)
        {
            return Problem(StatusCodes.Status404NotFound, "agent_definition_not_found", "Agent definition was not found.");
        }

        ModelRoute? route = null;
        ModelCostQuoteResponse? quote = null;
        if (request.ModelRouteId.HasValue)
        {
            route = await db.ModelRoutes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.ModelRouteId.Value, cancellationToken);
            if (route is null)
            {
                return Problem(StatusCodes.Status404NotFound, "model_route_not_found", "Model route was not found.");
            }

            if (route.WorkspaceId != agent.WorkspaceId)
            {
                return Problem(StatusCodes.Status409Conflict, "model_route_workspace_mismatch", "Model route must belong to the agent workspace.");
            }

            if (route.ProjectId.HasValue && route.ProjectId.Value != agent.ProjectId)
            {
                return Problem(StatusCodes.Status409Conflict, "model_route_project_mismatch", "Project-scoped model route must belong to the agent project.");
            }

            var snapshot = await db.ProviderPriceSnapshots
                .AsNoTracking()
                .Where(item => item.ProviderId == route.ProviderId && item.ModelName == route.ModelName)
                .OrderByDescending(item => item.CapturedAt)
                .FirstOrDefaultAsync(cancellationToken);
            quote = BuildCostQuote(snapshot, DefaultQuoteInputTokens, DefaultQuoteOutputTokens, GetPlatformFeeFixedUsd(configuration));
        }

        agent.ModelRouteId = request.ModelRouteId;
        agent.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new AgentDefinitionModelRouteResponse(
            agent.Id,
            agent.WorkspaceId,
            agent.ProjectId,
            agent.ModelRouteId,
            route is null ? null : ToModelRouteResponse(route),
            quote,
            agent.UpdatedAt));
    }

    private static async Task<Dictionary<Guid, ProviderAccountCounts>> LoadAccountCountsAsync(
        AgentPortDbContext db,
        IReadOnlyCollection<Guid> providerIds,
        Guid? workspaceId,
        CancellationToken cancellationToken)
    {
        if (providerIds.Count == 0)
        {
            return [];
        }

        var query = db.ProviderAccounts.AsNoTracking().Where(item => providerIds.Contains(item.ProviderId));
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == workspaceId.Value);
        }

        var counts = await query
            .GroupBy(item => item.ProviderId)
            .Select(group => new ProviderAccountCounts(
                group.Key,
                group.Count(),
                group.Count(item => item.IsEnabled)))
            .ToListAsync(cancellationToken);
        return counts.ToDictionary(item => item.ProviderId);
    }

    private static async Task<Dictionary<Guid, int>> LoadCatalogCountsAsync(
        AgentPortDbContext db,
        IReadOnlyCollection<Guid> providerIds,
        Guid? workspaceId,
        CancellationToken cancellationToken)
    {
        if (providerIds.Count == 0)
        {
            return [];
        }

        var query = db.ModelCatalogEntries.AsNoTracking().Where(item => providerIds.Contains(item.ProviderId));
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == null || item.WorkspaceId == workspaceId.Value);
        }

        return await query
            .GroupBy(item => item.ProviderId)
            .Select(group => new { ProviderId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ProviderId, item => item.Count, cancellationToken);
    }

    private static async Task<Dictionary<Guid, ProviderStatusCheck>> LoadLatestChecksAsync(
        AgentPortDbContext db,
        IReadOnlyCollection<Guid> providerIds,
        Guid? workspaceId,
        CancellationToken cancellationToken)
    {
        if (providerIds.Count == 0)
        {
            return [];
        }

        var query = db.ProviderStatusChecks.AsNoTracking().Where(item => providerIds.Contains(item.ProviderId));
        if (workspaceId.HasValue)
        {
            query = query.Where(item => item.WorkspaceId == workspaceId.Value);
        }

        var checks = await query
            .OrderByDescending(item => item.CheckedAt)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return checks
            .GroupBy(item => item.ProviderId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static async Task<Dictionary<PriceSnapshotKey, ProviderPriceSnapshot>> LoadLatestPriceSnapshotsAsync(
        AgentPortDbContext db,
        IReadOnlyCollection<Guid> providerIds,
        CancellationToken cancellationToken)
    {
        if (providerIds.Count == 0)
        {
            return [];
        }

        var snapshots = await db.ProviderPriceSnapshots
            .AsNoTracking()
            .Where(item => providerIds.Contains(item.ProviderId))
            .OrderByDescending(item => item.CapturedAt)
            .ToListAsync(cancellationToken);
        return snapshots
            .GroupBy(item => new PriceSnapshotKey(item.ProviderId, NormalizeModelName(item.ModelName)))
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static ProviderPriceSnapshot? FindPriceSnapshot(
        IReadOnlyDictionary<PriceSnapshotKey, ProviderPriceSnapshot> snapshots,
        Guid providerId,
        string modelName)
    {
        return snapshots.GetValueOrDefault(new PriceSnapshotKey(providerId, NormalizeModelName(modelName)));
    }

    private static ModelProviderCatalogResponse ToProviderResponse(
        ModelProvider item,
        ProviderAccountCounts? accounts,
        int catalogModelCount,
        ProviderStatusCheck? latestCheck)
    {
        return new ModelProviderCatalogResponse(
            item.Id,
            item.WorkspaceId,
            item.Name,
            item.Kind,
            item.BaseUrl,
            item.IsEnabled,
            accounts?.Count ?? 0,
            accounts?.EnabledCount ?? 0,
            catalogModelCount,
            latestCheck?.Status ?? (item.IsEnabled ? "not_checked" : "disabled"),
            latestCheck?.CheckedAt,
            item.MetadataJson,
            item.CreatedAt,
            item.UpdatedAt);
    }

    private static ModelRouteResponse ToModelRouteResponse(ModelRoute item)
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

    private static ModelCostQuoteResponse BuildCostQuote(
        ProviderPriceSnapshot? snapshot,
        long inputTokens,
        long outputTokens,
        decimal platformFeeFixedUsd)
    {
        var providerCost = snapshot is null
            ? 0m
            : ((inputTokens / 1_000_000m) * snapshot.InputTokenPricePerMillion)
                + ((outputTokens / 1_000_000m) * snapshot.OutputTokenPricePerMillion)
                + snapshot.RequestPrice;
        var total = providerCost + platformFeeFixedUsd;
        return new ModelCostQuoteResponse(
            snapshot?.Currency ?? "USD",
            snapshot is not null,
            snapshot?.InputTokenPricePerMillion,
            snapshot?.OutputTokenPricePerMillion,
            snapshot?.RequestPrice,
            platformFeeFixedUsd,
            inputTokens,
            outputTokens,
            Math.Round(providerCost, 6, MidpointRounding.AwayFromZero),
            Math.Round(total, 6, MidpointRounding.AwayFromZero),
            snapshot?.CapturedAt);
    }

    private static string ResolveRouteStatus(
        ModelRoute route,
        ModelProvider provider,
        ProviderAccount? account,
        LocalModelInventory? localModel)
    {
        if (!route.IsEnabled)
        {
            return "route_disabled";
        }

        if (!provider.IsEnabled)
        {
            return "provider_disabled";
        }

        if (IsLocalProvider(provider))
        {
            if (localModel is null)
            {
                return "model_not_in_inventory";
            }

            return localModel.IsInstalled ? "ready" : "model_unverified";
        }

        return account is null ? "not_configured" : "ready";
    }

    private static string BuildRouteTestMessage(
        string status,
        ModelProvider provider,
        ModelRoute route,
        LocalModelInventory? localModel)
    {
        return status switch
        {
            "ready" => "Route configuration is ready. No live provider call was executed by the platform API.",
            "model_unverified" => $"Local model '{route.ModelName}' is cataloged but not verified as installed by the platform API.",
            "model_not_in_inventory" => $"Local model '{route.ModelName}' is not present in local_model_inventory.",
            "provider_disabled" => $"Provider '{provider.Name}' is disabled.",
            "route_disabled" => $"Model route '{route.Name}' is disabled.",
            "not_configured" => $"Provider '{provider.Name}' does not have an enabled provider account.",
            _ => localModel?.Status ?? status
        };
    }

    private static Dictionary<string, string[]> ValidateProviderInput(string name, string kind)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Name is required."];
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            errors["kind"] = ["Kind is required."];
        }

        return errors;
    }

    private static bool IsLocalProvider(ModelProvider provider)
    {
        return provider.Kind.Equals("ollama", StringComparison.OrdinalIgnoreCase)
            || provider.Name.Equals("ollama", StringComparison.OrdinalIgnoreCase)
            || provider.Kind.Equals("openai-compatible-local", StringComparison.OrdinalIgnoreCase)
            || provider.Kind.Equals("local", StringComparison.OrdinalIgnoreCase);
    }

    private static long NormalizeTokenCount(long? value, long fallback)
    {
        return Math.Clamp(value ?? fallback, 0, 2_000_000);
    }

    private static long EstimatePromptTokens(string prompt)
    {
        return Math.Clamp((long)Math.Ceiling(prompt.Length / 4m), 1, 2_000_000);
    }

    private static decimal GetPlatformFeeFixedUsd(IConfiguration configuration)
    {
        var raw = configuration["AGENTPORT_FEE_FIXED_USD"];
        return decimal.TryParse(raw, out var value) && value >= 0 ? value : 0.10m;
    }

    private static string NormalizeModelName(string modelName)
    {
        return modelName.Trim().ToLowerInvariant();
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

    private sealed record ProviderAccountCounts(Guid ProviderId, int Count, int EnabledCount);

    private sealed record PriceSnapshotKey(Guid ProviderId, string ModelName);

    private sealed record PullLocalModelRequest(Guid WorkspaceId, string? ModelName, string? Profile);
}
