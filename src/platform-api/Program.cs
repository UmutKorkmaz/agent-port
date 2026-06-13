using AgentPort.PlatformApi.Data;
using AgentPort.PlatformApi.Endpoints;
using AgentPort.PlatformApi.Infrastructure;
using AgentPort.PlatformApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=agentport;Username=agentport;Password=agentport";
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

builder.Services.AddProblemDetails();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<AgentPortDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly("AgentPort.PlatformApi")));
builder.Services.AddScoped<LocalBootstrapService>();

builder.Services
    .AddAuthentication(ApiKeyAuthorization.AuthenticationScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthorization.AuthenticationScheme,
        _ => { });
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"])
    .AddDbContextCheck<AgentPortDbContext>("postgres", tags: ["ready"])
    .AddRedis(redisConnectionString, name: "redis", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
}

app.MapOpenApi();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new
{
    Status = "live",
    Timestamp = DateTime.UtcNow
}));
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthReport
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthReport
});

app.MapPhase0Api();
app.MapPhase11Api();
app.MapPhase12Api();
app.MapPhase2Api();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgentPortDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

static Task WriteHealthReport(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var payload = new
    {
        Status = report.Status.ToString().ToLowerInvariant(),
        Timestamp = DateTime.UtcNow,
        Checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new
            {
                Status = entry.Value.Status.ToString().ToLowerInvariant(),
                entry.Value.Description,
                DurationMs = entry.Value.Duration.TotalMilliseconds
            })
    };

    return context.Response.WriteAsJsonAsync(payload);
}
