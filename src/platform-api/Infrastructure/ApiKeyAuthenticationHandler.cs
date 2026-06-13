using System.Security.Claims;
using System.Text.Encodings.Web;
using AgentPort.PlatformApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgentPort.PlatformApi.Infrastructure;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// Resolves the AgentPort API key from the x-agentport-api-key header or an
/// Authorization: Bearer header, hashes it via <see cref="ApiKeyHasher"/>, looks it up, and
/// builds a <see cref="ClaimsPrincipal"/> carrying the key's workspace id and scopes.
///
/// The handler authenticates only. Per-endpoint scope and workspace checks are enforced by
/// <see cref="ApiKeyScopeFilter"/> so anonymous endpoints (health) are never blocked.
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly AgentPortDbContext _db;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AgentPortDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rawApiKey = ResolveRawApiKey(Context);
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            // No credentials presented: leave unauthenticated. The endpoint filter decides whether
            // that is allowed (anonymous endpoints) or a 401 (protected endpoints).
            return AuthenticateResult.NoResult();
        }

        var keyHash = ApiKeyHasher.Hash(rawApiKey);
        var apiKey = await _db.ApiKeys.FirstOrDefaultAsync(item => item.KeyHash == keyHash, Context.RequestAborted);
        if (apiKey is null)
        {
            return AuthenticateResult.Fail("API key is invalid.");
        }

        if (apiKey.RevokedAt.HasValue || (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value <= DateTime.UtcNow))
        {
            return AuthenticateResult.Fail("API key is inactive.");
        }

        apiKey.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(Context.RequestAborted);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.Id.ToString()),
            new(ApiKeyAuthorization.ApiKeyIdClaimType, apiKey.Id.ToString()),
            new(ApiKeyAuthorization.WorkspaceClaimType, apiKey.WorkspaceId.ToString()),
            new(ApiKeyAuthorization.KeyTypeClaimType, apiKey.KeyType),
            new(ApiKeyAuthorization.PrefixClaimType, apiKey.Prefix)
        };
        claims.AddRange(apiKey.Scopes.Select(scope => new Claim(ApiKeyAuthorization.ScopeClaimType, scope)));

        var identity = new ClaimsIdentity(claims, ApiKeyAuthorization.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthorization.AuthenticationScheme);
        return AuthenticateResult.Success(ticket);
    }

    public static string? ResolveRawApiKey(HttpContext httpContext)
    {
        var explicitHeader = httpContext.Request.Headers["x-agentport-api-key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(explicitHeader))
        {
            return explicitHeader.Trim();
        }

        var authorization = httpContext.Request.Headers.Authorization.FirstOrDefault();
        const string bearerPrefix = "Bearer ";
        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[bearerPrefix.Length..].Trim();
        }

        return null;
    }
}
