using Microsoft.EntityFrameworkCore;
using PipeRAG.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Database
// Connection string should come from environment variables or user secrets, not appsettings.json.
// Example: dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=piperag;Username=piperag;Password=secret"
// Or set the environment variable: ConnectionStrings__DefaultConnection
builder.Services.AddDbContext<PipeRagDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.UseVector()));

// Embedding dimension configuration (used by DbContext for vector column sizing)
var embeddingDimension = builder.Configuration.GetValue("Embedding:Dimension", 1536);
builder.Services.AddSingleton(new EmbeddingOptions(embeddingDimension));
PipeRagDbContext.EmbeddingDimension = embeddingDimension;

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", async (PipeRagDbContext db, ILogger<Program> logger) =>
{
    var dbHealthy = false;
    try
    {
        dbHealthy = await db.Database.CanConnectAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Health check: database connection failed");
        return Results.Json(new
        {
            status = "unhealthy",
            database = false,
            error = ex.Message,
            timestamp = DateTime.UtcNow
        }, statusCode: 503);
    }

    if (!dbHealthy)
    {
        return Results.Json(new
        {
            status = "unhealthy",
            database = false,
            error = "Cannot connect to database",
            timestamp = DateTime.UtcNow
        }, statusCode: 503);
    }

    return Results.Ok(new
    {
        status = "healthy",
        database = true,
        timestamp = DateTime.UtcNow
    });
});

app.Run();

/// <summary>
/// Configuration for embedding vector dimensions.
/// text-embedding-3-small = 1536 dimensions (default)
/// text-embedding-3-large = 3072 dimensions
/// </summary>
public record EmbeddingOptions(int Dimension = 1536);
