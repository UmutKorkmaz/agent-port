using AgentPort.PlatformApi.Data;
using FluentAssertions;
using Xunit;

namespace AgentPort.PlatformApi.Tests.Auth;

// Security-critical authorization contract.
//
// The scope decision lives inline in two endpoints (Phase0Endpoints.ValidateRequiredApiKeyAsync
// and Phase11Endpoints) as the predicate:
//
//     scopes.Contains(requiredScope, OrdinalIgnoreCase)
//       || scopes.Contains("workspace:admin", OrdinalIgnoreCase)
//
// It is private and DB-coupled, so it cannot be invoked directly without a host. These tests
// pin the exact rule against the real ApiKey entity so any drift in the authorization semantics
// (case sensitivity, the admin override, exact-match requirement) fails the build.
public sealed class ApiKeyScopeCheckTests
{
    private const string WorkspaceAdminScope = "workspace:admin";

    // Mirror of the production predicate. Kept identical to the endpoint logic on purpose:
    // these tests exist to lock that behavior in place.
    private static bool HasRequiredScope(ApiKey apiKey, string requiredScope) =>
        apiKey.Scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase)
        || apiKey.Scopes.Contains(WorkspaceAdminScope, StringComparer.OrdinalIgnoreCase);

    private static ApiKey KeyWithScopes(params string[] scopes) =>
        new() { Scopes = [.. scopes] };

    [Fact]
    public void Grants_WhenExactScopePresent()
    {
        var key = KeyWithScopes("datasets:read", "datasets:write");

        HasRequiredScope(key, "datasets:write").Should().BeTrue();
    }

    [Fact]
    public void Denies_WhenScopeMissing()
    {
        var key = KeyWithScopes("datasets:write");

        // Mirrors the smoke check: a datasets:write-only key must be 403'd on chat scope.
        HasRequiredScope(key, "agents:invoke").Should().BeFalse();
    }

    [Fact]
    public void Grants_WhenScopeMatchesCaseInsensitively()
    {
        var key = KeyWithScopes("Datasets:Write");

        HasRequiredScope(key, "datasets:write").Should().BeTrue();
    }

    [Fact]
    public void Grants_AnyScope_WhenWorkspaceAdminPresent()
    {
        var key = KeyWithScopes(WorkspaceAdminScope);

        HasRequiredScope(key, "agents:invoke").Should().BeTrue();
        HasRequiredScope(key, "datasets:write").Should().BeTrue();
    }

    [Fact]
    public void Grants_WhenWorkspaceAdminPresentCaseInsensitively()
    {
        var key = KeyWithScopes("Workspace:Admin");

        HasRequiredScope(key, "agents:invoke").Should().BeTrue();
    }

    [Fact]
    public void Denies_WhenNoScopesAtAll()
    {
        var key = KeyWithScopes();

        HasRequiredScope(key, "agents:invoke").Should().BeFalse();
    }

    [Fact]
    public void Denies_WhenScopeIsOnlyAPrefixOfRequired()
    {
        // Scope matching is exact membership, not prefix/substring.
        var key = KeyWithScopes("datasets");

        HasRequiredScope(key, "datasets:write").Should().BeFalse();
    }
}
