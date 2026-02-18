using Microsoft.EntityFrameworkCore;
using PipeRAG.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Database
builder.Services.AddDbContext<PipeRagDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.UseVector()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", async (PipeRagDbContext db) =>
{
    var dbHealthy = false;
    try
    {
        dbHealthy = await db.Database.CanConnectAsync();
    }
    catch { }

    return Results.Ok(new
    {
        status = dbHealthy ? "healthy" : "degraded",
        database = dbHealthy ? "connected" : "unavailable",
        timestamp = DateTime.UtcNow
    });
});

app.Run();
