using System.Net;
using System.Net.Http.Json;
using AgentPort.PlatformApi.Data;
using AgentPort.PlatformApi.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AgentPort.PlatformApi.IntegrationTests;

/// <summary>
/// Exercises the API-key security floor against a real Postgres (pgvector) instance through the full
/// HTTP pipeline: 401 for missing keys, 403 for missing scope / workspace mismatch, 404 for the
/// gated dev endpoints when AGENTPORT_ALLOW_DEV_ENDPOINTS is unset, 2xx for a valid scoped key, and
/// the random + idempotent bootstrap-key contract.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AuthFloorTests : IDisposable
{
    private const string ApiKeyHeader = "x-agentport-api-key";

    private readonly PostgresFixture _postgres;

    // Factory with the dev endpoints gated OFF (no AGENTPORT_ALLOW_DEV_ENDPOINTS): used for the 404
    // gating assertions and the seeded-key scope/workspace assertions.
    private readonly AuthFloorWebFactory _gatedFactory;

    // Factory with the dev endpoints enabled: used to drive bootstrap and the valid-scoped-key path.
    private readonly AuthFloorWebFactory _devFactory;

    public AuthFloorTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        _gatedFactory = new AuthFloorWebFactory(postgres.ConnectionString, allowDevEndpoints: false);
        _devFactory = new AuthFloorWebFactory(postgres.ConnectionString, allowDevEndpoints: true);
    }

    public void Dispose()
    {
        _gatedFactory.Dispose();
        _devFactory.Dispose();
    }

    [Fact]
    public async Task Mutation_Without_Key_Returns_401()
    {
        _gatedFactory.EnsureSchemaCreated();
        var client = _gatedFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/datasets", new
        {
            workspaceId = Guid.NewGuid(),
            projectId = Guid.NewGuid(),
            name = "no-key"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Wrong_Scope_Returns_403()
    {
        _gatedFactory.EnsureSchemaCreated();
        // Key carries only datasets:read, never datasets:write, and no admin super-scope.
        var (workspaceId, rawKey) = await SeedScopedKeyAsync(_gatedFactory, "datasets:read");

        var client = _gatedFactory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyHeader, rawKey);

        var response = await client.PostAsJsonAsync("/api/v1/datasets", new
        {
            workspaceId,
            projectId = Guid.NewGuid(),
            name = "wrong-scope"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Workspace_Mismatch_Returns_403()
    {
        _gatedFactory.EnsureSchemaCreated();
        // Key is scoped to workspace A with datasets:write but no admin override; the request body
        // targets a different workspace B, so the workspace-match rule must reject it.
        var (_, rawKey) = await SeedScopedKeyAsync(_gatedFactory, "datasets:write");
        var otherWorkspaceId = Guid.NewGuid();

        var client = _gatedFactory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyHeader, rawKey);

        var response = await client.PostAsJsonAsync("/api/v1/datasets", new
        {
            workspaceId = otherWorkspaceId,
            projectId = Guid.NewGuid(),
            name = "workspace-mismatch"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DevReset_Returns_404_When_Flag_Unset()
    {
        _gatedFactory.EnsureSchemaCreated();
        var client = _gatedFactory.CreateClient();

        var response = await client.PostAsync("/api/v1/dev/reset", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BootstrapLocal_Returns_404_When_Flag_Unset()
    {
        _gatedFactory.EnsureSchemaCreated();
        var client = _gatedFactory.CreateClient();

        var response = await client.PostAsync("/api/v1/bootstrap/local", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Valid_Scoped_Key_Returns_2xx()
    {
        _devFactory.EnsureSchemaCreated();
        var client = _devFactory.CreateClient();

        var bootstrap = await client.PostAsync("/api/v1/bootstrap/local", content: null);
        bootstrap.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await bootstrap.Content.ReadFromJsonAsync<BootstrapPayload>();
        payload.Should().NotBeNull();
        payload!.ApiKey.Should().NotBeNullOrWhiteSpace();

        // The bootstrap key carries datasets:write (+ workspace:admin) and belongs to the bootstrapped
        // workspace, so a scoped read of that workspace's datasets must succeed.
        var scoped = _devFactory.CreateClient();
        scoped.DefaultRequestHeaders.Add(ApiKeyHeader, payload.ApiKey);

        var datasets = await scoped.GetAsync($"/api/v1/datasets?workspaceId={payload.WorkspaceId}");
        datasets.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Bootstrap_Key_Is_Random_And_Idempotent()
    {
        _devFactory.EnsureSchemaCreated();
        var client = _devFactory.CreateClient();

        var first = await client.PostAsync("/api/v1/bootstrap/local", content: null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstPayload = await first.Content.ReadFromJsonAsync<BootstrapPayload>();

        var second = await client.PostAsync("/api/v1/bootstrap/local", content: null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondPayload = await second.Content.ReadFromJsonAsync<BootstrapPayload>();

        firstPayload.Should().NotBeNull();
        secondPayload.Should().NotBeNull();

        // The raw key is cryptographically random per bootstrap: not a known constant, and the two
        // runs must not collide on the same value.
        firstPayload!.ApiKey.Should().NotBeNullOrWhiteSpace();
        secondPayload!.ApiKey.Should().NotBeNullOrWhiteSpace();
        firstPayload.ApiKey.Should().NotBe("ap_local_dev");
        firstPayload.ApiKey.Should().NotBe(secondPayload.ApiKey);

        // Idempotent identity: the same workspace is reused and exactly one local-bootstrap key row
        // exists after two runs (the key is rotated on the existing row, not duplicated).
        secondPayload.WorkspaceId.Should().Be(firstPayload.WorkspaceId);

        using var scope = _devFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgentPortDbContext>();
        var bootstrapKeyCount = await db.ApiKeys
            .CountAsync(item => item.WorkspaceId == firstPayload.WorkspaceId && item.Name == "local-bootstrap");
        bootstrapKeyCount.Should().Be(1);
    }

    /// <summary>
    /// Seeds a workspace + service account + a single API key carrying exactly the given scopes (no
    /// admin override), returning the workspace id and the raw key value to present on requests.
    /// </summary>
    private static async Task<(Guid WorkspaceId, string RawKey)> SeedScopedKeyAsync(
        AuthFloorWebFactory factory,
        params string[] scopes)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgentPortDbContext>();

        var workspace = new Workspace { Name = "Scoped Workspace", Slug = $"scoped-{Guid.NewGuid():N}" };
        db.Workspaces.Add(workspace);

        var rawKey = ApiKeyHasher.GenerateRawKey();
        db.ApiKeys.Add(new ApiKey
        {
            WorkspaceId = workspace.Id,
            Name = $"scoped-{Guid.NewGuid():N}",
            Prefix = ApiKeyHasher.GetPrefix(rawKey),
            KeyHash = ApiKeyHasher.Hash(rawKey),
            Scopes = [.. scopes]
        });

        await db.SaveChangesAsync();
        return (workspace.Id, rawKey);
    }

    private sealed record BootstrapPayload(Guid WorkspaceId, string? ApiKey);
}
