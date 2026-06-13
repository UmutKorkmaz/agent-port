using System.Security.Claims;

namespace AgentPort.PlatformApi.Infrastructure;

/// <summary>
/// Pure authorization predicates for API keys. Kept free of EF/HTTP dependencies so the
/// security-critical scope and workspace decisions can be unit-tested without a host.
/// </summary>
public static class ApiKeyAuthorization
{
    public const string WorkspaceAdminScope = "workspace:admin";

    public const string AuthenticationScheme = "ApiKey";

    public const string WorkspaceClaimType = "agentport:workspace_id";

    public const string ScopeClaimType = "agentport:scope";

    public const string ApiKeyIdClaimType = "agentport:api_key_id";

    public const string KeyTypeClaimType = "agentport:key_type";

    public const string PrefixClaimType = "agentport:prefix";

    /// <summary>
    /// A key satisfies a required scope when it explicitly carries that scope (case-insensitive)
    /// or carries the workspace:admin super-scope.
    /// </summary>
    public static bool HasRequiredScope(IReadOnlyCollection<string> scopes, string requiredScope) =>
        scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase)
        || scopes.Contains(WorkspaceAdminScope, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A key may act on a target workspace when it belongs to that workspace, or when it holds the
    /// workspace:admin super-scope (the existing cross-workspace override).
    /// </summary>
    public static bool CanAccessWorkspace(
        Guid keyWorkspaceId,
        IReadOnlyCollection<string> scopes,
        Guid targetWorkspaceId) =>
        keyWorkspaceId == targetWorkspaceId
        || scopes.Contains(WorkspaceAdminScope, StringComparer.OrdinalIgnoreCase);

    public static Guid? GetWorkspaceId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(WorkspaceClaimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static IReadOnlyList<string> GetScopes(ClaimsPrincipal principal) =>
        principal.FindAll(ScopeClaimType).Select(claim => claim.Value).ToArray();

    public static Guid? GetApiKeyId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ApiKeyIdClaimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
