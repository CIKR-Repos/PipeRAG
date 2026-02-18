using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PipeRAG.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PipeRagDbContext>
{
    public PipeRagDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=piperag;Username=piperag;Password=piperag_dev";

        var optionsBuilder = new DbContextOptionsBuilder<PipeRagDbContext>();
        optionsBuilder.UseNpgsql(connectionString, o => o.UseVector());

        return new PipeRagDbContext(optionsBuilder.Options);
    }
}
