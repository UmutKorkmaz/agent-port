namespace AgentPort.PlatformApi.Infrastructure;

/// <summary>
/// Guards destructive local-only endpoints (dev reset, local bootstrap). They are reachable only
/// when BOTH the host is in the Development environment AND the explicit opt-in env flag
/// AGENTPORT_ALLOW_DEV_ENDPOINTS=true is set. Otherwise the endpoint behaves as if it does not
/// exist (404), so production deployments never expose them even if Development is misconfigured.
/// </summary>
public static class DevEndpointGate
{
    public const string AllowDevEndpointsEnvVar = "AGENTPORT_ALLOW_DEV_ENDPOINTS";

    public static bool IsEnabled(IHostEnvironment environment, IConfiguration configuration)
    {
        if (!environment.IsDevelopment())
        {
            return false;
        }

        var flag = configuration[AllowDevEndpointsEnvVar]
            ?? Environment.GetEnvironmentVariable(AllowDevEndpointsEnvVar);
        return string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static IResult Disabled() =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "dev_endpoint_disabled",
            detail: $"This endpoint requires the Development environment and {AllowDevEndpointsEnvVar}=true.",
            extensions: new Dictionary<string, object?> { ["code"] = "dev_endpoint_disabled" });
}
