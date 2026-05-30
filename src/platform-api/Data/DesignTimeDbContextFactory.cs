using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgentPort.PlatformApi.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AgentPortDbContext>
{
    public AgentPortDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AgentPortDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=agentport;Username=agentport;Password=agentport");
        return new AgentPortDbContext(optionsBuilder.Options);
    }
}
