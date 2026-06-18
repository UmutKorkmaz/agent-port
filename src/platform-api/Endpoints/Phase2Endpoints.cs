using AgentPort.PlatformApi.Contracts;
using AgentPort.PlatformApi.Data;
using AgentPort.PlatformApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AgentPort.PlatformApi.Endpoints;

public static class Phase2Endpoints
{
    public static IEndpointRouteBuilder MapPhase2Api(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1").WithTags("Phase 2");

        // Dataset card
        api.MapPatch("/datasets/{datasetId:guid}/card", PatchDatasetCard).RequireApiKey("datasets:write");

        // Training jobs
        api.MapGet("/training-jobs", ListTrainingJobs).RequireApiKey("training:read");
        api.MapPost("/training-jobs", CreateTrainingJob).RequireApiKey("training:write");
        api.MapGet("/training-jobs/{jobId:guid}", GetTrainingJob).RequireApiKey("training:read");
        api.MapPatch("/training-jobs/{jobId:guid}", PatchTrainingJob).RequireApiKey("training:write");

        // Experiments
        api.MapGet("/experiments", ListExperiments).RequireApiKey("training:read");
        api.MapPost("/experiments", CreateExperiment).RequireApiKey("training:write");
        api.MapGet("/experiments/{experimentId:guid}", GetExperiment).RequireApiKey("training:read");

        // Model versions
        api.MapGet("/model-versions", ListModelVersions).RequireApiKey("models:route");
        api.MapPost("/model-versions", CreateModelVersion).RequireApiKey("models:route");
        api.MapGet("/model-versions/{versionId:guid}", GetModelVersion).RequireApiKey("models:route");

        // Model aliases
        api.MapGet("/model-aliases", ListModelAliases).RequireApiKey("models:route");
        api.MapPost("/model-aliases", SetModelAlias).RequireApiKey("models:route");
        api.MapGet("/model-aliases/{alias}", GetModelAliasByName).RequireApiKey("models:route");

        // Eval runs
        api.MapGet("/eval-runs", ListEvalRuns).RequireApiKey("training:read");
        api.MapPost("/eval-runs", CreateEvalRun).RequireApiKey("training:write");
        api.MapGet("/eval-runs/{runId:guid}", GetEvalRun).RequireApiKey("training:read");
        api.MapPatch("/eval-runs/{runId:guid}", PatchEvalRun).RequireApiKey("training:write");

        // Human review records
        api.MapGet("/human-review-records", ListHumanReviewRecords).RequireApiKey("training:read");
        api.MapPost("/human-review-records", CreateHumanReviewRecord).RequireApiKey("training:write");
        api.MapGet("/human-review-records/{recordId:guid}", GetHumanReviewRecord).RequireApiKey("training:read");
        api.MapPatch("/human-review-records/{recordId:guid}", PatchHumanReviewRecord).RequireApiKey("training:write");

        return routes;
    }

    private static async Task<IResult> PatchDatasetCard(
        Guid datasetId,
        UpdateDatasetCardRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var dataset = await db.Datasets.FirstOrDefaultAsync(d => d.Id == datasetId, cancellationToken);
        if (dataset is null)
        {
            return Problem(StatusCodes.Status404NotFound, "dataset_not_found", "Dataset was not found.");
        }

        if (request.License is not null) dataset.License = request.License;
        if (request.Source is not null) dataset.Source = request.Source;
        if (request.PiiClassification is not null) dataset.PiiClassification = request.PiiClassification;
        if (request.IsGolden.HasValue) dataset.IsGolden = request.IsGolden.Value;
        if (request.AllowsTraining.HasValue) dataset.AllowsTraining = request.AllowsTraining.Value;
        if (request.AllowsEval.HasValue) dataset.AllowsEval = request.AllowsEval.Value;
        if (request.ConsentFlagsJson is not null) dataset.ConsentFlagsJson = request.ConsentFlagsJson;
        if (request.SplitsJson is not null) dataset.SplitsJson = request.SplitsJson;
        if (request.SchemaJson is not null) dataset.SchemaJson = request.SchemaJson;
        if (request.QualityNotes is not null) dataset.QualityNotes = request.QualityNotes;

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToDatasetResponse(dataset));
    }

    private static async Task<IResult> ListTrainingJobs(
        Guid? workspaceId,
        Guid? projectId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.TrainingJobs.AsNoTracking();
        if (workspaceId.HasValue) query = query.Where(j => j.WorkspaceId == workspaceId.Value);
        if (projectId.HasValue) query = query.Where(j => j.ProjectId == projectId.Value);

        var items = await query.OrderByDescending(j => j.CreatedAt).Take(100)
            .Select(j => ToResponse(j)).ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateTrainingJob(
        CreateTrainingJobRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = ValidateNameAndSlug(request.Name, request.Slug);
        if (request.WorkspaceId == Guid.Empty) errors["workspaceId"] = ["WorkspaceId is required."];
        if (request.ProjectId == Guid.Empty) errors["projectId"] = ["ProjectId is required."];
        if (errors.Count > 0) return ValidationProblem(errors);

        var slug = SlugGenerator.FromName(request.Slug ?? request.Name, "training-job");
        var exists = await db.TrainingJobs.AnyAsync(
            j => j.WorkspaceId == request.WorkspaceId && j.ProjectId == request.ProjectId && j.Slug == slug, cancellationToken);
        if (exists) return Problem(StatusCodes.Status409Conflict, "training_job_slug_conflict", $"Training job slug '{slug}' already exists.");

        // Carry the operator-selected task + target column into the persisted
        // config (and, downstream, the ai-services /v1/train payload). An explicit
        // ConfigJson still wins so callers can pin a fully-specified TrainJobConfig.
        var config = TrainingJobMapping.ToConfig(request);
        var configJson = request.ConfigJson
            ?? System.Text.Json.JsonSerializer.Serialize(config);

        var job = new TrainingJob
        {
            WorkspaceId = request.WorkspaceId,
            ProjectId = request.ProjectId,
            DatasetId = request.DatasetId,
            DatasetVersionId = request.DatasetVersionId,
            ModelRouteId = request.ModelRouteId,
            Name = request.Name.Trim(),
            Slug = slug,
            Kind = config.Task,
            Status = "queued",
            ConfigJson = configJson,
            HyperparametersJson = request.HyperparametersJson ?? "{}",
            EstimatedCost = request.EstimatedCost ?? 0,
        };
        db.TrainingJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/training-jobs/{job.Id}", ToResponse(job));
    }

    private static async Task<IResult> GetTrainingJob(
        Guid jobId, AgentPortDbContext db, CancellationToken cancellationToken)
    {
        var job = await db.TrainingJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        return job is null
            ? Problem(StatusCodes.Status404NotFound, "training_job_not_found", "Training job was not found.")
            : Results.Ok(ToResponse(job));
    }

    private static async Task<IResult> PatchTrainingJob(
        Guid jobId,
        PatchTrainingJobRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var job = await db.TrainingJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (job is null) return Problem(StatusCodes.Status404NotFound, "training_job_not_found", "Training job was not found.");

        if (request.Status is not null) job.Status = request.Status;
        if (request.MetricsJson is not null) job.MetricsJson = request.MetricsJson;
        if (request.ArtifactsJson is not null) job.ArtifactsJson = request.ArtifactsJson;
        if (request.LogsJson is not null) job.LogsJson = request.LogsJson;
        if (request.ActualCost.HasValue) job.ActualCost = request.ActualCost.Value;
        if (request.FailureReason is not null) job.FailureReason = request.FailureReason;

        if (request.Status == "running" && job.StartedAt is null) job.StartedAt = DateTime.UtcNow;
        if (request.Status is "completed" or "failed" or "cancelled" && job.CompletedAt is null) job.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(job));
    }

    private static async Task<IResult> ListExperiments(
        Guid? workspaceId,
        Guid? projectId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.Experiments.AsNoTracking();
        if (workspaceId.HasValue) query = query.Where(e => e.WorkspaceId == workspaceId.Value);
        if (projectId.HasValue) query = query.Where(e => e.ProjectId == projectId.Value);

        var items = await query.OrderByDescending(e => e.CreatedAt).Take(100)
            .Select(e => ToResponse(e)).ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateExperiment(
        CreateExperimentRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = ValidateNameAndSlug(request.Name, request.Slug);
        if (request.WorkspaceId == Guid.Empty) errors["workspaceId"] = ["WorkspaceId is required."];
        if (request.ProjectId == Guid.Empty) errors["projectId"] = ["ProjectId is required."];
        if (errors.Count > 0) return ValidationProblem(errors);

        var slug = SlugGenerator.FromName(request.Slug ?? request.Name, "experiment");
        var exists = await db.Experiments.AnyAsync(
            e => e.WorkspaceId == request.WorkspaceId && e.ProjectId == request.ProjectId && e.Slug == slug, cancellationToken);
        if (exists) return Problem(StatusCodes.Status409Conflict, "experiment_slug_conflict", $"Experiment slug '{slug}' already exists.");

        var experiment = new Experiment
        {
            WorkspaceId = request.WorkspaceId,
            ProjectId = request.ProjectId,
            DatasetVersionId = request.DatasetVersionId,
            AgentDefinitionId = request.AgentDefinitionId,
            ModelRouteId = request.ModelRouteId,
            Name = request.Name.Trim(),
            Slug = slug,
            ConfigJson = request.ConfigJson ?? "{}",
        };
        db.Experiments.Add(experiment);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/experiments/{experiment.Id}", ToResponse(experiment));
    }

    private static async Task<IResult> GetExperiment(
        Guid experimentId, AgentPortDbContext db, CancellationToken cancellationToken)
    {
        var experiment = await db.Experiments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == experimentId, cancellationToken);
        return experiment is null
            ? Problem(StatusCodes.Status404NotFound, "experiment_not_found", "Experiment was not found.")
            : Results.Ok(ToResponse(experiment));
    }

    private static async Task<IResult> ListModelVersions(
        Guid? workspaceId,
        Guid? projectId,
        string? kind,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.ModelVersions.AsNoTracking();
        if (workspaceId.HasValue) query = query.Where(m => m.WorkspaceId == workspaceId.Value);
        if (projectId.HasValue) query = query.Where(m => m.ProjectId == projectId.Value);
        if (!string.IsNullOrWhiteSpace(kind)) query = query.Where(m => m.Kind == kind);

        var items = await query.OrderByDescending(m => m.CreatedAt).Take(100)
            .Select(m => ToResponse(m)).ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateModelVersion(
        CreateModelVersionRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = ValidateNameAndSlug(request.Name, request.Slug);
        if (request.WorkspaceId == Guid.Empty) errors["workspaceId"] = ["WorkspaceId is required."];
        if (request.ProjectId == Guid.Empty) errors["projectId"] = ["ProjectId is required."];
        if (errors.Count > 0) return ValidationProblem(errors);

        var slug = SlugGenerator.FromName(request.Slug ?? request.Name, "model-version");
        var exists = await db.ModelVersions.AnyAsync(
            m => m.WorkspaceId == request.WorkspaceId && m.ProjectId == request.ProjectId && m.Slug == slug, cancellationToken);
        if (exists) return Problem(StatusCodes.Status409Conflict, "model_version_slug_conflict", $"Model version slug '{slug}' already exists.");

        var version = new ModelVersion
        {
            WorkspaceId = request.WorkspaceId,
            ProjectId = request.ProjectId,
            TrainingJobId = request.TrainingJobId,
            ExperimentId = request.ExperimentId,
            Name = request.Name.Trim(),
            Slug = slug,
            Kind = string.IsNullOrWhiteSpace(request.Kind) ? "classifier" : request.Kind.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "candidate" : request.Status.Trim(),
            ArtifactUri = request.ArtifactUri,
            ConfigJson = request.ConfigJson ?? "{}",
            CapabilitiesJson = request.CapabilitiesJson ?? "{}",
        };
        db.ModelVersions.Add(version);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/model-versions/{version.Id}", ToResponse(version));
    }

    private static async Task<IResult> GetModelVersion(
        Guid versionId, AgentPortDbContext db, CancellationToken cancellationToken)
    {
        var version = await db.ModelVersions.AsNoTracking().FirstOrDefaultAsync(m => m.Id == versionId, cancellationToken);
        return version is null
            ? Problem(StatusCodes.Status404NotFound, "model_version_not_found", "Model version was not found.")
            : Results.Ok(ToResponse(version));
    }

    private static async Task<IResult> ListModelAliases(
        Guid? workspaceId,
        Guid? projectId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.ModelAliases.AsNoTracking().Where(a => a.IsActive);
        if (workspaceId.HasValue) query = query.Where(a => a.WorkspaceId == workspaceId.Value);
        if (projectId.HasValue) query = query.Where(a => a.ProjectId == projectId.Value);

        var items = await query.OrderBy(a => a.Alias).ThenByDescending(a => a.CreatedAt)
            .Select(a => ToResponse(a)).ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> SetModelAlias(
        SetModelAliasRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty || request.ProjectId == Guid.Empty || request.ModelVersionId == Guid.Empty)
        {
            return ValidationProblem(new Dictionary<string, string[]> {
                ["workspaceId"] = ["Required."],
                ["projectId"] = ["Required."],
                ["modelVersionId"] = ["Required."]
            });
        }

        if (string.IsNullOrWhiteSpace(request.Alias))
        {
            return ValidationProblem(new Dictionary<string, string[]> { ["alias"] = ["Alias is required."] });
        }

        var alias = request.Alias.Trim().ToLowerInvariant();
        if (alias is not ("candidate" or "staging" or "production" or "rollback"))
        {
            return Problem(StatusCodes.Status400BadRequest, "invalid_alias", "Alias must be candidate, staging, production, or rollback.");
        }

        var existing = await db.ModelAliases.FirstOrDefaultAsync(
            a => a.WorkspaceId == request.WorkspaceId && a.ProjectId == request.ProjectId && a.Alias == alias, cancellationToken);
        if (existing is not null)
        {
            existing.ModelVersionId = request.ModelVersionId;
            existing.Description = request.Description;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.ModelAliases.Add(new ModelAlias
            {
                WorkspaceId = request.WorkspaceId,
                ProjectId = request.ProjectId,
                ModelVersionId = request.ModelVersionId,
                Alias = alias,
                Description = request.Description,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        var result = await db.ModelAliases.AsNoTracking()
            .FirstAsync(a => a.WorkspaceId == request.WorkspaceId && a.ProjectId == request.ProjectId && a.Alias == alias, cancellationToken);
        return Results.Ok(ToResponse(result));
    }

    private static async Task<IResult> GetModelAliasByName(
        string alias,
        Guid? workspaceId,
        Guid? projectId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.ModelAliases.AsNoTracking().Where(a => a.Alias == alias.ToLowerInvariant() && a.IsActive);
        if (workspaceId.HasValue) query = query.Where(a => a.WorkspaceId == workspaceId.Value);
        if (projectId.HasValue) query = query.Where(a => a.ProjectId == projectId.Value);

        var item = await query.OrderByDescending(a => a.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        return item is null
            ? Problem(StatusCodes.Status404NotFound, "model_alias_not_found", "Model alias was not found.")
            : Results.Ok(ToResponse(item));
    }

    private static async Task<IResult> ListEvalRuns(
        Guid? workspaceId,
        Guid? projectId,
        Guid? evalSuiteId,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.EvalRuns.AsNoTracking();
        if (workspaceId.HasValue) query = query.Where(r => r.WorkspaceId == workspaceId.Value);
        if (projectId.HasValue) query = query.Where(r => r.ProjectId == projectId.Value);
        if (evalSuiteId.HasValue) query = query.Where(r => r.EvalSuiteId == evalSuiteId.Value);

        var items = await query.OrderByDescending(r => r.CreatedAt).Take(100)
            .Select(r => ToResponse(r)).ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateEvalRun(
        CreateEvalRunRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.WorkspaceId == Guid.Empty) errors["workspaceId"] = ["WorkspaceId is required."];
        if (request.ProjectId == Guid.Empty) errors["projectId"] = ["ProjectId is required."];
        if (request.EvalSuiteId == Guid.Empty) errors["evalSuiteId"] = ["EvalSuiteId is required."];
        if (errors.Count > 0) return ValidationProblem(errors);

        var run = new EvalRun
        {
            WorkspaceId = request.WorkspaceId,
            ProjectId = request.ProjectId,
            EvalSuiteId = request.EvalSuiteId,
            ModelVersionId = request.ModelVersionId,
            AgentDefinitionId = request.AgentDefinitionId,
            Status = "queued",
            Threshold = request.Threshold,
        };
        db.EvalRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/eval-runs/{run.Id}", ToResponse(run));
    }

    private static async Task<IResult> GetEvalRun(
        Guid runId, AgentPortDbContext db, CancellationToken cancellationToken)
    {
        var run = await db.EvalRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        return run is null
            ? Problem(StatusCodes.Status404NotFound, "eval_run_not_found", "Eval run was not found.")
            : Results.Ok(ToResponse(run));
    }

    private static async Task<IResult> PatchEvalRun(
        Guid runId,
        PatchEvalRunRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var run = await db.EvalRuns.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null) return Problem(StatusCodes.Status404NotFound, "eval_run_not_found", "Eval run was not found.");

        if (request.Status is not null) run.Status = request.Status;
        if (request.Score.HasValue) run.Score = request.Score.Value;
        if (request.Passed.HasValue) run.Passed = request.Passed.Value;
        if (request.ResultsJson is not null) run.ResultsJson = request.ResultsJson;
        if (request.FailuresJson is not null) run.FailuresJson = request.FailuresJson;

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(run));
    }

    private static async Task<IResult> ListHumanReviewRecords(
        Guid? workspaceId,
        Guid? projectId,
        string? queue,
        string? status,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.HumanReviewRecords.AsNoTracking();
        if (workspaceId.HasValue) query = query.Where(r => r.WorkspaceId == workspaceId.Value);
        if (projectId.HasValue) query = query.Where(r => r.ProjectId == projectId.Value);
        if (!string.IsNullOrWhiteSpace(queue)) query = query.Where(r => r.Queue == queue);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);

        var items = await query.OrderByDescending(r => r.CreatedAt).Take(100)
            .Select(r => ToResponse(r)).ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateHumanReviewRecord(
        CreateHumanReviewRecordRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.WorkspaceId == Guid.Empty) errors["workspaceId"] = ["WorkspaceId is required."];
        if (string.IsNullOrWhiteSpace(request.Queue)) errors["queue"] = ["Queue is required."];
        if (errors.Count > 0) return ValidationProblem(errors);

        var record = new HumanReviewRecord
        {
            WorkspaceId = request.WorkspaceId,
            ProjectId = request.ProjectId,
            AgentRunId = request.AgentRunId,
            TraceRecordId = request.TraceRecordId,
            Queue = request.Queue.Trim(),
            Label = request.Label,
            Severity = request.Severity,
            Reason = request.Reason,
            FollowUpAction = request.FollowUpAction,
        };
        db.HumanReviewRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/human-review-records/{record.Id}", ToResponse(record));
    }

    private static async Task<IResult> GetHumanReviewRecord(
        Guid recordId, AgentPortDbContext db, CancellationToken cancellationToken)
    {
        var record = await db.HumanReviewRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);
        return record is null
            ? Problem(StatusCodes.Status404NotFound, "human_review_record_not_found", "Human review record was not found.")
            : Results.Ok(ToResponse(record));
    }

    private static async Task<IResult> PatchHumanReviewRecord(
        Guid recordId,
        PatchHumanReviewRecordRequest request,
        AgentPortDbContext db,
        CancellationToken cancellationToken)
    {
        var record = await db.HumanReviewRecords.FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);
        if (record is null) return Problem(StatusCodes.Status404NotFound, "human_review_record_not_found", "Human review record was not found.");

        if (request.Status is not null) record.Status = request.Status;
        if (request.Label is not null) record.Label = request.Label;
        if (request.Reason is not null) record.Reason = request.Reason;
        if (request.FollowUpAction is not null) record.FollowUpAction = request.FollowUpAction;
        if (request.ReviewerId is not null) record.ReviewerId = request.ReviewerId;
        if (request.ReviewerName is not null) record.ReviewerName = request.ReviewerName;
        if (request.CanReuseForTraining.HasValue) record.CanReuseForTraining = request.CanReuseForTraining.Value;

        if (request.Status is "approved" or "rejected" or "labelled" && record.ReviewedAt is null)
        {
            record.ReviewedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(record));
    }

    private static DatasetResponse ToDatasetResponse(Dataset item)
    {
        return new DatasetResponse(
            item.Id,
            item.WorkspaceId,
            item.ProjectId,
            null,
            item.Name,
            item.Slug,
            item.Kind,
            0,
            0,
            item.CreatedAt,
            item.UpdatedAt);
    }

    private static TrainingJobResponse ToResponse(TrainingJob j) => new(
        j.Id, j.WorkspaceId, j.ProjectId, j.DatasetId, j.DatasetVersionId, j.ModelRouteId, j.WalletReservationId,
        j.Name, j.Slug, j.Kind, j.Status, j.ConfigJson, j.HyperparametersJson, j.ArtifactsJson, j.MetricsJson,
        j.EstimatedCost, j.ActualCost, j.StartedAt, j.CompletedAt, j.FailureReason, j.CreatedAt, j.UpdatedAt);

    private static ExperimentResponse ToResponse(Experiment e) => new(
        e.Id, e.WorkspaceId, e.ProjectId, e.DatasetVersionId, e.AgentDefinitionId, e.ModelRouteId,
        e.Name, e.Slug, e.Status, e.ConfigJson, e.MetricsJson, e.ArtifactsJson, e.CreatedAt, e.UpdatedAt);

    private static ModelVersionResponse ToResponse(ModelVersion m) => new(
        m.Id, m.WorkspaceId, m.ProjectId, m.TrainingJobId, m.ExperimentId,
        m.Name, m.Slug, m.Kind, m.Status, m.ArtifactUri, m.ConfigJson, m.CapabilitiesJson, m.CreatedAt, m.UpdatedAt);

    private static ModelAliasResponse ToResponse(ModelAlias a) => new(
        a.Id, a.WorkspaceId, a.ProjectId, a.ModelVersionId, a.Alias, a.Description, a.IsActive, a.CreatedAt, a.UpdatedAt);

    private static EvalRunResponse ToResponse(EvalRun r) => new(
        r.Id, r.WorkspaceId, r.ProjectId, r.EvalSuiteId, r.ModelVersionId, r.AgentDefinitionId,
        r.Status, r.Score, r.Threshold, r.Passed, r.ResultsJson, r.FailuresJson, r.CreatedAt, r.UpdatedAt);

    private static HumanReviewRecordResponse ToResponse(HumanReviewRecord r) => new(
        r.Id, r.WorkspaceId, r.ProjectId, r.AgentRunId, r.TraceRecordId, r.Queue, r.Status,
        r.Label, r.Severity, r.Reason, r.FollowUpAction, r.ReviewerId, r.ReviewerName,
        r.ReviewedAt, r.CanReuseForTraining, r.CreatedAt, r.UpdatedAt);

    private static Dictionary<string, string[]> ValidateNameAndSlug(string name, string? slug)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(name)) errors["name"] = ["Name is required."];
        if (slug is not null && string.IsNullOrWhiteSpace(slug)) errors["slug"] = ["Slug cannot be blank."];
        return errors;
    }

    private static IResult ValidationProblem(Dictionary<string, string[]> errors)
    {
        return Results.ValidationProblem(errors, title: "Validation failed");
    }

    private static IResult Problem(int statusCode, string code, string detail)
    {
        return Results.Problem(statusCode: statusCode, title: code, detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}
