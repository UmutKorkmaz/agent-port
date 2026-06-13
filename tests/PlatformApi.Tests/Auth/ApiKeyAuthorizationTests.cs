using AgentPort.PlatformApi.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AgentPort.PlatformApi.Tests.Auth;

// Pins the workspace-access decision used by the shared ApiKeyScopeFilter. The scope predicate is
// covered by ApiKeyScopeCheckTests; these focus on the workspace-match / admin-override rule.
public sealed class ApiKeyAuthorizationTests
{
    private const string Admin = ApiKeyAuthorization.WorkspaceAdminScope;

    [Fact]
    public void HasRequiredScope_Grants_OnExactMatch()
    {
        ApiKeyAuthorization.HasRequiredScope(["datasets:write"], "datasets:write").Should().BeTrue();
    }

    [Fact]
    public void HasRequiredScope_Grants_OnAdminOverride()
    {
        ApiKeyAuthorization.HasRequiredScope([Admin], "training:write").Should().BeTrue();
    }

    [Fact]
    public void HasRequiredScope_Denies_WhenMissing()
    {
        ApiKeyAuthorization.HasRequiredScope(["datasets:read"], "datasets:write").Should().BeFalse();
    }

    [Fact]
    public void CanAccessWorkspace_Grants_WhenWorkspaceMatches()
    {
        var workspace = Guid.NewGuid();

        ApiKeyAuthorization.CanAccessWorkspace(workspace, ["datasets:write"], workspace).Should().BeTrue();
    }

    [Fact]
    public void CanAccessWorkspace_Denies_OnMismatch_WithoutAdmin()
    {
        ApiKeyAuthorization.CanAccessWorkspace(Guid.NewGuid(), ["datasets:write"], Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void CanAccessWorkspace_Grants_OnMismatch_WhenAdmin()
    {
        // The workspace:admin super-scope is the documented cross-workspace override.
        ApiKeyAuthorization.CanAccessWorkspace(Guid.NewGuid(), [Admin], Guid.NewGuid()).Should().BeTrue();
    }

    [Fact]
    public void CanAccessWorkspace_Grants_OnMismatch_WhenAdminCaseInsensitive()
    {
        ApiKeyAuthorization.CanAccessWorkspace(Guid.NewGuid(), ["Workspace:Admin"], Guid.NewGuid()).Should().BeTrue();
    }
}
