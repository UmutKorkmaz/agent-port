using AgentPort.PlatformApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgentPort.PlatformApi.IntegrationTests;

/// <summary>
/// Hosts the real platform-api against the Testcontainers Postgres instance. The connection string
/// is overridden to the container; the environment is Development. The AGENTPORT_ALLOW_DEV_ENDPOINTS
/// flag is set explicitly per factory so the dev-endpoint gating matrix can be exercised both ways.
///
/// Startup auto-migration is disabled (<see cref="SkipStartupMigrationSetting"/>); schema is applied
/// once per container via <see cref="EnsureSchemaCreated"/> using the checked-in EF migrations.
/// </summary>
public sealed class AuthFloorWebFactory : WebApplicationFactory<Program>
{
    internal const string SkipStartupMigrationSetting = "AGENTPORT_SKIP_STARTUP_MIGRATION";

    private readonly string _connectionString;
    private readonly bool _allowDevEndpoints;
    private bool _schemaReady;
    private readonly object _schemaLock = new();

    // Both factory instances in AuthFloorTests share one Postgres container; coordinate schema setup
    // globally so Program startup and EnsureSchemaCreated do not fight over migration history.
    private static readonly object GlobalSchemaLock = new();
    private static bool GlobalSchemaReady;

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
        builder.UseSetting(SkipStartupMigrationSetting, "true");

        // DevEndpointGate reads either configuration[AGENTPORT_ALLOW_DEV_ENDPOINTS] or the process env
        // var. Drive it through configuration so each factory instance is independent and no global
        // process state leaks between tests.
        builder.UseSetting(
            Infrastructure.DevEndpointGate.AllowDevEndpointsEnvVar,
            _allowDevEndpoints ? "true" : "false");
    }

    /// <summary>
    /// Ensures the EF migration schema exists on the container DB. Idempotent and safe to call from
    /// every test; the first caller drops and re-migrates, later callers are no-ops.
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

            lock (GlobalSchemaLock)
            {
                if (!GlobalSchemaReady)
                {
                    using var scope = Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AgentPortDbContext>();
                    db.Database.EnsureDeleted();
                    db.Database.Migrate();
                    GlobalSchemaReady = true;
                }
            }

            _schemaReady = true;
        }
    }
}