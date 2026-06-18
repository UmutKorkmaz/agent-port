using AgentPort.PlatformApi.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace AgentPort.PlatformApi.Tests.Auth;

// The dev/reset and bootstrap/local endpoints must require BOTH the Development environment AND an
// explicit AGENTPORT_ALLOW_DEV_ENDPOINTS=true opt-in. These tests pin that AND semantics.
public sealed class DevEndpointGateTests
{
    [Fact]
    public void Disabled_WhenNotDevelopment_EvenIfFlagSet()
    {
        var env = new FakeEnvironment("Production");
        var config = ConfigWith(("AGENTPORT_ALLOW_DEV_ENDPOINTS", "true"));

        DevEndpointGate.IsEnabled(env, config).Should().BeFalse();
    }

    [Fact]
    public void Disabled_WhenDevelopment_ButFlagMissing()
    {
        var env = new FakeEnvironment("Development");
        var config = ConfigWith();

        DevEndpointGate.IsEnabled(env, config).Should().BeFalse();
    }

    [Fact]
    public void Disabled_WhenDevelopment_ButFlagFalse()
    {
        var env = new FakeEnvironment("Development");
        var config = ConfigWith(("AGENTPORT_ALLOW_DEV_ENDPOINTS", "false"));

        DevEndpointGate.IsEnabled(env, config).Should().BeFalse();
    }

    [Fact]
    public void Enabled_WhenDevelopment_AndFlagTrue()
    {
        var env = new FakeEnvironment("Development");
        var config = ConfigWith(("AGENTPORT_ALLOW_DEV_ENDPOINTS", "true"));

        DevEndpointGate.IsEnabled(env, config).Should().BeTrue();
    }

    [Fact]
    public void Enabled_IsCaseInsensitiveOnFlag()
    {
        var env = new FakeEnvironment("Development");
        var config = ConfigWith(("AGENTPORT_ALLOW_DEV_ENDPOINTS", "TRUE"));

        DevEndpointGate.IsEnabled(env, config).Should().BeTrue();
    }

    private static IConfiguration ConfigWith(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public FakeEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "AgentPort.PlatformApi";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
