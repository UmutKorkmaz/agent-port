using AgentPort.PlatformApi.Contracts;
using AgentPort.PlatformApi.Data;
using AgentPort.PlatformApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgentPort.PlatformApi.Services;

public sealed class LocalBootstrapService
{
    private static readonly string[] BootstrapScopes =
        ["workspace:admin", "models:route", "agents:write", "datasets:write", "runs:write", "billing:read"];

    private readonly AgentPortDbContext _db;

    public LocalBootstrapService(AgentPortDbContext db)
    {
        _db = db;
    }

    public async Task<LocalBootstrapResponse> BootstrapLocalAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var workspace = await _db.Workspaces
            .FirstOrDefaultAsync(item => item.Slug == "local", cancellationToken);
        if (workspace is null)
        {
            workspace = new Workspace
            {
                Name = "Local Workspace",
                Slug = "local",
                MetadataJson = """{"bootstrap":"local"}"""
            };
            _db.Workspaces.Add(workspace);
        }

        var ownerUser = await _db.Users
            .FirstOrDefaultAsync(item => item.Email == "owner@agentport.local", cancellationToken);
        if (ownerUser is null)
        {
            ownerUser = new User
            {
                Email = "owner@agentport.local",
                DisplayName = "Local Owner",
                MetadataJson = """{"bootstrap":"local"}"""
            };
            _db.Users.Add(ownerUser);
        }

        var membership = await _db.Memberships
            .FirstOrDefaultAsync(item => item.WorkspaceId == workspace.Id && item.UserId == ownerUser.Id, cancellationToken);
        if (membership is null)
        {
            _db.Memberships.Add(new Membership
            {
                WorkspaceId = workspace.Id,
                UserId = ownerUser.Id,
                Role = "owner",
                MetadataJson = """{"bootstrap":"local"}"""
            });
        }

        var project = await _db.Projects
            .FirstOrDefaultAsync(item => item.WorkspaceId == workspace.Id && item.Slug == "default", cancellationToken);
        if (project is null)
        {
            project = new Project
            {
                WorkspaceId = workspace.Id,
                Name = "Default Project",
                Slug = "default",
                MetadataJson = """{"bootstrap":"local"}"""
            };
            _db.Projects.Add(project);
        }

        var environment = await _db.Environments
            .FirstOrDefaultAsync(
                item => item.WorkspaceId == workspace.Id && item.ProjectId == project.Id && item.Slug == "dev",
                cancellationToken);
        if (environment is null)
        {
            environment = new AgentEnvironment
            {
                WorkspaceId = workspace.Id,
                ProjectId = project.Id,
                Name = "Development",
                Slug = "dev",
                Kind = "development",
                IsDefault = true,
                MetadataJson = """{"bootstrap":"local"}"""
            };
            _db.Environments.Add(environment);
        }

        var serviceAccount = await _db.ServiceAccounts
            .FirstOrDefaultAsync(
                item => item.WorkspaceId == workspace.Id && item.Slug == "local-bootstrap",
                cancellationToken);
        if (serviceAccount is null)
        {
            serviceAccount = new ServiceAccount
            {
                WorkspaceId = workspace.Id,
                ProjectId = project.Id,
                Name = "Local Bootstrap",
                Slug = "local-bootstrap",
                MetadataJson = """{"bootstrap":"local"}"""
            };
            _db.ServiceAccounts.Add(serviceAccount);
        }

        var policy = await _db.Policies
            .FirstOrDefaultAsync(item => item.WorkspaceId == workspace.Id && item.Name == "default-local-policy", cancellationToken);
        if (policy is null)
        {
            policy = new Policy
            {
                WorkspaceId = workspace.Id,
                ProjectId = project.Id,
                Name = "default-local-policy",
                Kind = "workspace",
                IsDefault = true,
                DocumentJson = """{"allowedModelProviders":["ollama"],"maxMonthlySpend":0,"environment":"local"}""",
                MetadataJson = """{"bootstrap":"local"}"""
            };
            _db.Policies.Add(policy);
        }

        var modelProvider = await _db.ModelProviders
            .FirstOrDefaultAsync(item => item.WorkspaceId == workspace.Id && item.Name == "ollama", cancellationToken);
        if (modelProvider is null)
        {
            modelProvider = new ModelProvider
            {
                WorkspaceId = workspace.Id,
                Name = "ollama",
                Kind = "ollama",
                BaseUrl = "http://localhost:11434",
                IsEnabled = true,
                MetadataJson = """{"bootstrap":"local","storesProviderKeys":false}"""
            };
            _db.ModelProviders.Add(modelProvider);
        }

        var seededProviders = new Dictionary<string, ModelProvider>(StringComparer.OrdinalIgnoreCase)
        {
            ["ollama"] = modelProvider
        };
        var providerSeeds = new[]
        {
            ("openai", "openai-compatible", "https://api.openai.com/v1"),
            ("anthropic", "anthropic", "https://api.anthropic.com"),
            ("google_gemini", "google", "https://generativelanguage.googleapis.com"),
            ("xai_grok", "openai-compatible", "https://api.x.ai/v1"),
            ("zhipu_glm", "openai-compatible", "https://open.bigmodel.cn/api/paas/v4"),
            ("moonshot_kimi", "openai-compatible", "https://api.moonshot.ai/v1"),
            ("mistral", "openai-compatible", "https://api.mistral.ai/v1"),
            ("cohere", "cohere", "https://api.cohere.com"),
            ("deepseek", "openai-compatible", "https://api.deepseek.com/v1"),
            ("groq", "openai-compatible", "https://api.groq.com/openai/v1"),
            ("together", "openai-compatible", "https://api.together.xyz/v1"),
            ("fireworks", "openai-compatible", "https://api.fireworks.ai/inference/v1"),
            ("replicate", "replicate", "https://api.replicate.com/v1"),
            ("huggingface", "huggingface", "https://api-inference.huggingface.co"),
            ("openrouter", "openai-compatible", "https://openrouter.ai/api/v1"),
            ("perplexity", "openai-compatible", "https://api.perplexity.ai"),
            ("azure_openai", "azure-openai", null),
            ("google_vertex_ai", "vertex-ai", null),
            ("aws_bedrock", "bedrock", null),
            ("vllm", "openai-compatible", null),
            ("huggingface_tgi", "openai-compatible", null)
        };
        foreach (var providerSeed in providerSeeds)
        {
            var existingProvider = await _db.ModelProviders
                .FirstOrDefaultAsync(
                    item => item.WorkspaceId == workspace.Id && item.Name == providerSeed.Item1,
                    cancellationToken);
            if (existingProvider is null)
            {
                existingProvider = new ModelProvider
                {
                    WorkspaceId = workspace.Id,
                    Name = providerSeed.Item1,
                    Kind = providerSeed.Item2,
                    BaseUrl = providerSeed.Item3,
                    IsEnabled = false,
                    MetadataJson = """{"bootstrap":"local","disabledUntilConfigured":true,"storesProviderKeys":false}"""
                };
                _db.ModelProviders.Add(existingProvider);
            }

            seededProviders[providerSeed.Item1] = existingProvider;
        }

        var ollamaAccount = await _db.ProviderAccounts
            .FirstOrDefaultAsync(
                item => item.WorkspaceId == workspace.Id
                    && item.ProviderId == modelProvider.Id
                    && item.Slug == "local-ollama",
                cancellationToken);
        if (ollamaAccount is null)
        {
            ollamaAccount = new ProviderAccount
            {
                WorkspaceId = workspace.Id,
                ProjectId = project.Id,
                ProviderId = modelProvider.Id,
                Name = "Local Ollama",
                Slug = "local-ollama",
                AccountType = "local",
                Status = "configured",
                IsEnabled = true,
                CapabilitiesJson = """{"chat":true,"embeddings":false,"local":true}""",
                MetadataJson = """{"bootstrap":"local","storesProviderKeys":false}"""
            };
            _db.ProviderAccounts.Add(ollamaAccount);
        }

        await SeedModelCatalogAsync(workspace.Id, seededProviders, cancellationToken);
        await SeedLocalModelInventoryAsync(workspace.Id, modelProvider.Id, cancellationToken);

        var modelRoute = await _db.ModelRoutes
            .FirstOrDefaultAsync(item => item.WorkspaceId == workspace.Id && item.Slug == "ollama-default", cancellationToken);
        if (modelRoute is null)
        {
            modelRoute = new ModelRoute
            {
                WorkspaceId = workspace.Id,
                ProjectId = project.Id,
                ProviderId = modelProvider.Id,
                Name = "Default Ollama Route",
                Slug = "ollama-default",
                ModelName = "gemma4:e4b",
                RouteType = "chat",
                Priority = 100,
                IsDefault = true,
                IsEnabled = true,
                ParametersJson = "{}",
                MetadataJson = """{"bootstrap":"local"}"""
            };
            _db.ModelRoutes.Add(modelRoute);
        }
        else if (modelRoute.ModelName != "gemma4:e4b")
        {
            modelRoute.ModelName = "gemma4:e4b";
            modelRoute.UpdatedAt = DateTime.UtcNow;
        }


        var aiSystem = await _db.AiSystems
            .FirstOrDefaultAsync(
                item => item.WorkspaceId == workspace.Id && item.ProjectId == project.Id && item.Slug == "document-qa",
                cancellationToken);
        if (aiSystem is null)
        {
            aiSystem = new AiSystem
            {
                WorkspaceId = workspace.Id,
                ProjectId = project.Id,
                Name = "Document QA",
                Slug = "document-qa",
                Description = "Local system for document question answering.",
                MetadataJson = """{"bootstrap":"local"}"""
            };
            _db.AiSystems.Add(aiSystem);
        }

        var agentDefinition = await _db.AgentDefinitions
            .FirstOrDefaultAsync(
                item => item.WorkspaceId == workspace.Id && item.ProjectId == project.Id && item.Slug == "document-qa",
                cancellationToken);
        if (agentDefinition is null)
        {
            agentDefinition = new AgentDefinition
            {
                WorkspaceId = workspace.Id,
                ProjectId = project.Id,
                AiSystemId = aiSystem.Id,
                ModelRouteId = modelRoute.Id,
                Name = "Document QA",
                Slug = "document-qa",
                Status = "draft",
                Instructions = string.Empty,
                ToolsJson = "[]",
                MetadataJson = """{"bootstrap":"local","emptyDefinition":true}"""
            };
            _db.AgentDefinitions.Add(agentDefinition);
        }

        var dataset = await _db.Datasets
            .FirstOrDefaultAsync(
                item => item.WorkspaceId == workspace.Id && item.ProjectId == project.Id && item.Slug == "document-qa-sources",
                cancellationToken);
        if (dataset is null)
        {
            dataset = new Dataset
            {
                WorkspaceId = workspace.Id,
                ProjectId = project.Id,
                Name = "Document QA Sources",
                Slug = "document-qa-sources",
                Kind = "documents",
                MetadataJson = """{"bootstrap":"local","ragDefault":true}"""
            };
            _db.Datasets.Add(dataset);
        }

        var knowledgeBase = await _db.KnowledgeBases
            .FirstOrDefaultAsync(
                item => item.WorkspaceId == workspace.Id && item.ProjectId == project.Id && item.Slug == "document-qa",
                cancellationToken);
        if (knowledgeBase is null)
        {
            knowledgeBase = new KnowledgeBase
            {
                WorkspaceId = workspace.Id,
                ProjectId = project.Id,
                DatasetId = dataset.Id,
                AgentDefinitionId = agentDefinition.Id,
                Name = "Document QA Knowledge",
                Slug = "document-qa",
                RetrievalStrategy = "basic-rag",
                EmbeddingModel = "intfloat/multilingual-e5-base",
                VectorStore = "pgvector",
                Status = "ready",
                MetadataJson = """{"bootstrap":"local","ragDefault":true,"embeddingDimensions":768}"""
            };
            _db.KnowledgeBases.Add(knowledgeBase);
        }

        var walletAccount = await _db.WalletAccounts
            .FirstOrDefaultAsync(item => item.WorkspaceId == workspace.Id && item.Currency == "USD", cancellationToken);
        if (walletAccount is null)
        {
            walletAccount = new WalletAccount
            {
                WorkspaceId = workspace.Id,
                Currency = "USD",
                Balance = 10.00m,
                ReservedBalance = 0,
                Status = "active",
                MetadataJson = """{"bootstrap":"local","seedBalanceUsd":10.00}"""
            };
            _db.WalletAccounts.Add(walletAccount);
        }
        else if (walletAccount.Balance < 10.00m)
        {
            walletAccount.Balance = 10.00m;
            walletAccount.MetadataJson = """{"bootstrap":"local","seedBalanceUsd":10.00}""";
            walletAccount.UpdatedAt = DateTime.UtcNow;
        }

        var paymentProviderConfigs = new List<PaymentProviderConfig>();
        foreach (var provider in new[]
        {
            "stripe",
            "paypal_braintree",
            "adyen",
            "checkout_com",
            "paddle",
            "lemon_squeezy",
            "wise",
            "iyzico",
            "craftgate",
            "paytr",
            "param_pos",
            "sipay",
            "paynet",
            "shopier",
            "papara",
            "paycell",
            "hepsipay",
            "bank_vpos",
            "havale_eft_fast"
        })
        {
            var config = await _db.PaymentProviderConfigs
                .FirstOrDefaultAsync(
                    item => item.WorkspaceId == workspace.Id && item.Provider == provider && item.Mode == "test",
                    cancellationToken);
            if (config is null)
            {
                config = new PaymentProviderConfig
                {
                    WorkspaceId = workspace.Id,
                    Provider = provider,
                    Mode = "test",
                    IsEnabled = false,
                    MetadataJson = """{"bootstrap":"local","disabledUntilConfigured":true}"""
                };
                _db.PaymentProviderConfigs.Add(config);
            }

            paymentProviderConfigs.Add(config);
        }

        // Generate a cryptographically random key per bootstrap. The raw value is returned once in
        // the response and never persisted; only its hash is stored. The "local-bootstrap" row
        // remains idempotent (same identity, looked up by name) — re-running bootstrap rotates the
        // key on the existing row rather than creating duplicates.
        var rawApiKey = ApiKeyHasher.GenerateRawKey();
        var apiKey = await _db.ApiKeys
            .FirstOrDefaultAsync(
                item => item.WorkspaceId == workspace.Id
                    && item.ServiceAccountId == serviceAccount.Id
                    && item.Name == "local-bootstrap",
                cancellationToken);
        string apiKeyMessage;
        if (apiKey is null)
        {
            apiKey = new ApiKey
            {
                WorkspaceId = workspace.Id,
                ProjectId = project.Id,
                ServiceAccountId = serviceAccount.Id,
                Name = "local-bootstrap",
                Prefix = ApiKeyHasher.GetPrefix(rawApiKey),
                KeyHash = ApiKeyHasher.Hash(rawApiKey),
                Scopes = [.. BootstrapScopes],
                MetadataJson = """{"bootstrap":"local","randomDevKey":true}"""
            };
            _db.ApiKeys.Add(apiKey);
            apiKeyMessage = "Created local bootstrap API key. Store it now; it cannot be retrieved later.";
        }
        else
        {
            apiKey.Prefix = ApiKeyHasher.GetPrefix(rawApiKey);
            apiKey.KeyHash = ApiKeyHasher.Hash(rawApiKey);
            apiKey.Scopes = [.. BootstrapScopes];
            apiKey.MetadataJson = """{"bootstrap":"local","randomDevKey":true,"rotated":true}""";
            apiKey.RevokedAt = null;
            apiKey.ExpiresAt = null;
            apiKey.UpdatedAt = DateTime.UtcNow;
            apiKeyMessage = "Rotated local bootstrap API key. Store the new value now; it cannot be retrieved later.";
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new LocalBootstrapResponse(
            workspace.Id,
            ownerUser.Id,
            project.Id,
            environment.Id,
            serviceAccount.Id,
            policy.Id,
            modelProvider.Id,
            modelRoute.Id,
            aiSystem.Id,
            agentDefinition.Id,
            dataset.Id,
            knowledgeBase.Id,
            walletAccount.Id,
            paymentProviderConfigs.Select(item => item.Id).ToArray(),
            rawApiKey,
            apiKeyMessage);
    }

    private async Task SeedModelCatalogAsync(
        Guid workspaceId,
        IReadOnlyDictionary<string, ModelProvider> providers,
        CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            new ModelCatalogSeed("ollama", "gemma4:e4b", "Gemma 4 E4B", true, "chat", 8_192, 2_048, false, true, """{"local":true,"defaultRoute":true}"""),
            new ModelCatalogSeed("ollama", "gemma4:e2b", "Gemma 4 E2B", true, "chat", 8_192, 2_048, false, true, """{"local":true,"smallFallback":true}"""),
            new ModelCatalogSeed("ollama", "llama3.2:1b", "Llama 3.2 1B", true, "chat", 8_192, 2_048, false, true, """{"local":true,"smallFallback":true}"""),
            new ModelCatalogSeed("ollama", "llama3.2:3b", "Llama 3.2 3B", true, "chat", 8_192, 2_048, false, true, """{"local":true,"smallFallback":true}"""),
            new ModelCatalogSeed("ollama", "phi4-mini", "Phi-4 Mini", true, "chat", 128_000, 8_192, false, true, """{"local":true,"smallFallback":true}"""),
            new ModelCatalogSeed("openai", "gpt-4.1-mini", "GPT-4.1 Mini", false, "chat", 1_047_576, 32_768, true, true, """{"remote":true,"requiresProviderAccount":true}"""),
            new ModelCatalogSeed("anthropic", "claude-3-5-haiku-latest", "Claude 3.5 Haiku", false, "chat", 200_000, 8_192, true, true, """{"remote":true,"requiresProviderAccount":true}"""),
            new ModelCatalogSeed("google_gemini", "gemini-1.5-flash", "Gemini 1.5 Flash", false, "chat", 1_000_000, 8_192, true, true, """{"remote":true,"requiresProviderAccount":true}"""),
            new ModelCatalogSeed("deepseek", "deepseek-chat", "DeepSeek Chat", false, "chat", 64_000, 8_192, true, true, """{"remote":true,"requiresProviderAccount":true}"""),
            new ModelCatalogSeed("mistral", "mistral-large-latest", "Mistral Large", false, "chat", 128_000, 8_192, true, true, """{"remote":true,"requiresProviderAccount":true}"""),
            new ModelCatalogSeed("groq", "llama-3.1-8b-instant", "Llama 3.1 8B Instant", false, "chat", 128_000, 8_192, true, true, """{"remote":true,"requiresProviderAccount":true}""")
        };
        var now = DateTime.UtcNow;

        foreach (var seed in seeds)
        {
            if (!providers.TryGetValue(seed.ProviderName, out var provider))
            {
                continue;
            }

            var entry = await _db.ModelCatalogEntries.FirstOrDefaultAsync(
                item => item.WorkspaceId == workspaceId
                    && item.ProviderId == provider.Id
                    && item.ModelName == seed.ModelName,
                cancellationToken);
            if (entry is null)
            {
                entry = new ModelCatalogEntry
                {
                    WorkspaceId = workspaceId,
                    ProviderId = provider.Id,
                    ModelName = seed.ModelName,
                    MetadataJson = """{"bootstrap":"local"}"""
                };
                _db.ModelCatalogEntries.Add(entry);
            }

            entry.DisplayName = seed.DisplayName;
            entry.Modality = "text";
            entry.RouteType = seed.RouteType;
            entry.ContextWindowTokens = seed.ContextWindowTokens;
            entry.MaxOutputTokens = seed.MaxOutputTokens;
            entry.SupportsTools = seed.SupportsTools;
            entry.SupportsJsonMode = seed.SupportsJsonMode;
            entry.SupportsStreaming = true;
            entry.IsLocal = seed.IsLocal;
            entry.IsEnabled = true;
            entry.Status = "available";
            entry.LastSeenAt = now;
            entry.CapabilitiesJson = seed.CapabilitiesJson;
            entry.UpdatedAt = now;

            if (seed.IsLocal)
            {
                await SeedZeroPriceSnapshotAsync(provider.Id, seed.ModelName, cancellationToken);
            }
        }
    }

    private async Task SeedLocalModelInventoryAsync(
        Guid workspaceId,
        Guid providerId,
        CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            new LocalModelSeed("gemma4:e4b", "gemma", "4b", null, "seeded_unverified", """{"bootstrap":"local","defaultRoute":true,"installVerification":"not-run"}"""),
            new LocalModelSeed("gemma4:e2b", "gemma", "2b", null, "seeded_unverified", """{"bootstrap":"local","smallFallback":true,"installVerification":"not-run"}"""),
            new LocalModelSeed("llama3.2:1b", "llama", "1b", null, "seeded_unverified", """{"bootstrap":"local","smallFallback":true,"installVerification":"not-run"}"""),
            new LocalModelSeed("llama3.2:3b", "llama", "3b", null, "seeded_unverified", """{"bootstrap":"local","smallFallback":true,"installVerification":"not-run"}"""),
            new LocalModelSeed("phi4-mini", "phi", "3.8b", null, "seeded_unverified", """{"bootstrap":"local","smallFallback":true,"installVerification":"not-run"}""")
        };
        var now = DateTime.UtcNow;

        foreach (var seed in seeds)
        {
            var inventory = await _db.LocalModelInventory.FirstOrDefaultAsync(
                item => item.WorkspaceId == workspaceId
                    && item.ProviderId == providerId
                    && item.ModelName == seed.ModelName,
                cancellationToken);
            if (inventory is null)
            {
                inventory = new LocalModelInventory
                {
                    WorkspaceId = workspaceId,
                    ProviderId = providerId,
                    ModelName = seed.ModelName,
                    IsInstalled = false,
                    MetadataJson = seed.MetadataJson
                };
                _db.LocalModelInventory.Add(inventory);
            }

            inventory.Family = seed.Family;
            inventory.ParameterSize = seed.ParameterSize;
            inventory.Quantization = seed.Quantization;
            inventory.Status = seed.Status;
            inventory.MetadataJson = seed.MetadataJson;
            inventory.UpdatedAt = now;
        }
    }

    private async Task SeedZeroPriceSnapshotAsync(
        Guid providerId,
        string modelName,
        CancellationToken cancellationToken)
    {
        var hasSnapshot = await _db.ProviderPriceSnapshots.AnyAsync(
            item => item.ProviderId == providerId && item.ModelName == modelName,
            cancellationToken);
        if (hasSnapshot)
        {
            return;
        }

        _db.ProviderPriceSnapshots.Add(new ProviderPriceSnapshot
        {
            ProviderId = providerId,
            ModelName = modelName,
            Currency = "USD",
            InputTokenPricePerMillion = 0m,
            OutputTokenPricePerMillion = 0m,
            RequestPrice = 0m,
            CapturedAt = DateTime.UtcNow,
            MetadataJson = """{"bootstrap":"local","localModel":true}"""
        });
    }

    private sealed record ModelCatalogSeed(
        string ProviderName,
        string ModelName,
        string DisplayName,
        bool IsLocal,
        string RouteType,
        int? ContextWindowTokens,
        int? MaxOutputTokens,
        bool SupportsTools,
        bool SupportsJsonMode,
        string CapabilitiesJson);

    private sealed record LocalModelSeed(
        string ModelName,
        string? Family,
        string? ParameterSize,
        string? Quantization,
        string Status,
        string MetadataJson);
}
