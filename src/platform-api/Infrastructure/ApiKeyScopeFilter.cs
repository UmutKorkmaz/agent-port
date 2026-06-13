using System.Reflection;

namespace AgentPort.PlatformApi.Infrastructure;

/// <summary>
/// Shared endpoint filter enforcing the API-key security floor on protected endpoints:
///   1. A valid authenticated key must be present (else 401).
///   2. The key must carry the required scope or workspace:admin (else 403).
///   3. Any workspaceId supplied in the route, query string, or request body must match the
///      key's workspace, unless the key holds the workspace:admin super-scope (else 403).
///
/// Centralizing the rule here keeps the per-endpoint handlers untouched and avoids copy-paste
/// auth logic across the four endpoint modules.
/// </summary>
public sealed class ApiKeyScopeFilter : IEndpointFilter
{
    private readonly string _requiredScope;

    public ApiKeyScopeFilter(string requiredScope)
    {
        _requiredScope = requiredScope;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var principal = httpContext.User;

        if (principal.Identity?.IsAuthenticated != true)
        {
            // Distinguish "no credentials" (401) from "bad credentials" (also 401): either way the
            // caller must present a valid key. The authentication handler already rejected invalid
            // or inactive keys; here we just require an authenticated identity.
            return Problem(StatusCodes.Status401Unauthorized, "api_key_missing", "A valid API key is required.");
        }

        var scopes = ApiKeyAuthorization.GetScopes(principal);
        if (!ApiKeyAuthorization.HasRequiredScope(scopes, _requiredScope))
        {
            return Problem(StatusCodes.Status403Forbidden, "api_key_scope_missing", $"API key requires '{_requiredScope}'.");
        }

        var keyWorkspaceId = ApiKeyAuthorization.GetWorkspaceId(principal);
        if (keyWorkspaceId is null)
        {
            return Problem(StatusCodes.Status401Unauthorized, "api_key_invalid", "API key is invalid.");
        }

        var targetWorkspaceId = ResolveTargetWorkspaceId(context);
        if (targetWorkspaceId is { } target
            && target != Guid.Empty
            && !ApiKeyAuthorization.CanAccessWorkspace(keyWorkspaceId.Value, scopes, target))
        {
            return Problem(StatusCodes.Status403Forbidden, "api_key_workspace_mismatch", "API key cannot access this workspace.");
        }

        return await next(context);
    }

    private static Guid? ResolveTargetWorkspaceId(EndpointFilterInvocationContext context)
    {
        var httpContext = context.HttpContext;

        // Route value (e.g. /workspaces/{workspaceId}).
        if (httpContext.Request.RouteValues.TryGetValue("workspaceId", out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeWorkspaceId))
        {
            return routeWorkspaceId;
        }

        // Query string (?workspaceId=...).
        var queryValue = httpContext.Request.Query["workspaceId"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(queryValue) && Guid.TryParse(queryValue, out var queryWorkspaceId))
        {
            return queryWorkspaceId;
        }

        // Body DTO: inspect bound arguments for a WorkspaceId property without re-reading the stream.
        foreach (var argument in context.Arguments)
        {
            if (argument is null)
            {
                continue;
            }

            var property = argument.GetType().GetProperty("WorkspaceId", BindingFlags.Public | BindingFlags.Instance);
            if (property is null)
            {
                continue;
            }

            // Boxed Guid? with a value unboxes to Guid; null stays null.
            if (property.GetValue(argument) is Guid guid)
            {
                return guid;
            }
        }

        return null;
    }

    private static IResult Problem(int statusCode, string code, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: code,
            detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}

/// <summary>
/// Extension helpers for attaching the API-key security floor to endpoints.
/// </summary>
public static class ApiKeyEndpointExtensions
{
    public static TBuilder RequireApiKey<TBuilder>(this TBuilder builder, string requiredScope)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(new ApiKeyScopeFilter(requiredScope));
        return builder;
    }
}
