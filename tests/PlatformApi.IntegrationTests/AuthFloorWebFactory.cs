using AgentPort.PlatformApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgentPort.PlatformApi.IntegrationTests;

/// <summary>
/// Hosts the real platform-api against the Testcontainers Postgres instance. The connection string
/// is overridden to the container; the environment is Development. The AGENTPORT_ALLOW_DEV_ENDPOINTS
/// flag is set explicitly per factory so the dev-endpoint gating matrix can be exercised both ways.
///
/// The production model has no EF migrations checked in, so the startup MigrateAsync only creates the
/// history table. This factory calls EnsureCreated once to materialise the schema (including the
/// pgvector extension and the vector(768) column) before tests issue requests.
/// </summary>
public sealed class AuthFloorWebFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly bool _allowDevEndpoints;
    private bool _schemaReady;
    private readonly object _schemaLock = new();

    public AuthFloorWebFactory(string connectionString, bool allowDevEndpoints)
    {
        _connectionString = connectionString;
        _allowDevEndpoints = allowDevEndpoints;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        // The Redis ready-check is irrelevant to the auth floor; point it at the same host so the
        // app starts without a live Redis (health endpoints are not exercised by these tests).
        builder.UseSetting("ConnectionStrings:Redis", "localhost:6379");

        // DevEndpointGate reads either configuration[AGENTPORT_ALLOW_DEV_ENDPOINTS] or the process env
        // var. Drive it through configuration so each factory instance is independent and no global
        // process state leaks between tests.
        builder.UseSetting(
            Infrastructure.DevEndpointGate.AllowDevEndpointsEnvVar,
            _allowDevEndpoints ? "true" : "false");
    }

    /// <summary>
    /// Ensures the EF schema exists on the container DB. Idempotent and safe to call from every
    /// test; the first caller creates the schema, later callers are no-ops.
    /// </summary>
    public void EnsureSchemaCreated()
    {
        if (_schemaReady)
        {
            return;
        }

        lock (_schemaLock)
        {
            if (_schemaReady)
            {
                return;
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgentPortDbContext>();
            // The startup MigrateAsync only creates __EFMigrationsHistory (no migrations are checked
            // in), which would make EnsureCreated short-circuit. Drop everything first so the model is
            // materialised from scratch (pgvector extension + vector(768) column included).
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            _schemaReady = true;
        }
    }
}
