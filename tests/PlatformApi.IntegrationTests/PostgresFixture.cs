using Testcontainers.PostgreSql;
using Xunit;

namespace AgentPort.PlatformApi.IntegrationTests;

/// <summary>
/// Spins up a single pgvector-enabled Postgres container for the whole integration test run and
/// exposes its connection string. The pgvector image is required because the EF model declares the
/// document-chunk embedding column as vector(768); EnsureCreated emits CREATE EXTENSION vector.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithDatabase("agentport")
        .WithUsername("agentport")
        .WithPassword("agentport")
        .Build();

    /// <summary>The connection string for the running container, valid after <see cref="InitializeAsync"/>.</summary>
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

/// <summary>
/// xUnit collection that shares one <see cref="PostgresFixture"/> across the auth-floor tests so the
/// container is started once and the assertions run against the same real Postgres instance.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
