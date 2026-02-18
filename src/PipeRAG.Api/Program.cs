using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PipeRAG.Api.Middleware;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;
using PipeRAG.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddMemoryCache();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Database
builder.Services.AddDbContext<PipeRagDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.UseVector()));

// Embedding dimension configuration
var embeddingDimension = builder.Configuration.GetValue("Embedding:Dimension", 1536);
builder.Services.AddSingleton(new EmbeddingOptions(embeddingDimension));
PipeRagDbContext.EmbeddingDimension = embeddingDimension;

// Auth service
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (builder.Environment.IsDevelopment())
        jwtSecret = "PipeRAG-Dev-Secret-Key-Change-In-Production-Min32Chars!";
    else
        throw new InvalidOperationException("JWT Secret must be configured in production.");
}
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"] ?? "PipeRAG",
        ValidAudience = jwtSection["Audience"] ?? "PipeRAG",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RateLimitingMiddleware>();
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
/// </summary>
public record EmbeddingOptions(int Dimension = 1536);

/// <summary>
/// Marker class for test access to Program.
/// </summary>
public partial class Program { }
