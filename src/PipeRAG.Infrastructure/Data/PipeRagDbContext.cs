using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;

namespace PipeRAG.Infrastructure.Data;

/// <summary>
/// Main database context for PipeRAG.
/// </summary>
public class PipeRagDbContext : DbContext
{
    /// <summary>
    /// Vector embedding dimension. Maps to the embedding model:
    /// text-embedding-3-small = 1536 dimensions (default)
    /// text-embedding-3-large = 3072 dimensions
    /// </summary>
    public static int EmbeddingDimension { get; set; } = 1536;

    public PipeRagDbContext(DbContextOptions<PipeRagDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrgMembership> OrgMemberships => Set<OrgMembership>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension (skip for InMemory provider)
        if (!Database.IsInMemory())
            modelBuilder.HasPostgresExtension("vector");

        // Store enums as strings
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Tier).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Organization>(e =>
        {
            e.HasIndex(o => o.Slug).IsUnique();
        });

        modelBuilder.Entity<OrgMembership>(e =>
        {
            e.HasIndex(m => new { m.UserId, m.OrganizationId }).IsUnique();
            e.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
            e.HasOne(m => m.User).WithMany(u => u.OrgMemberships).HasForeignKey(m => m.UserId);
            e.HasOne(m => m.Organization).WithMany(o => o.Members).HasForeignKey(m => m.OrganizationId);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.HasIndex(p => p.OwnerId);
            e.HasOne(p => p.Owner).WithMany(u => u.Projects).HasForeignKey(p => p.OwnerId);
            e.HasOne(p => p.Organization).WithMany(o => o.Projects).HasForeignKey(p => p.OrganizationId);
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.HasIndex(d => d.ProjectId);
            e.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(d => d.Project).WithMany(p => p.Documents).HasForeignKey(d => d.ProjectId);
        });

        modelBuilder.Entity<DocumentChunk>(e =>
        {
            e.HasIndex(c => c.DocumentId);
            if (Database.IsInMemory())
                e.Ignore(c => c.Embedding);
            else
                e.Property(c => c.Embedding).HasColumnType($"vector({EmbeddingDimension})");
            e.HasOne(c => c.Document).WithMany(d => d.Chunks).HasForeignKey(c => c.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pipeline>(e =>
        {
            e.HasIndex(p => p.ProjectId);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(p => p.Project).WithMany(p => p.Pipelines).HasForeignKey(p => p.ProjectId);
        });

        modelBuilder.Entity<PipelineRun>(e =>
        {
            e.HasIndex(r => r.PipelineId);
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(r => r.Pipeline).WithMany(p => p.Runs).HasForeignKey(r => r.PipelineId);
        });

        modelBuilder.Entity<ChatSession>(e =>
        {
            e.HasIndex(s => s.ProjectId);
            e.HasIndex(s => s.UserId);
            e.HasOne(s => s.Project).WithMany(p => p.ChatSessions).HasForeignKey(s => s.ProjectId);
            e.HasOne(s => s.User).WithMany(u => u.ChatSessions).HasForeignKey(s => s.UserId);
        });

        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasIndex(m => m.SessionId);
            e.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
            e.HasOne(m => m.Session).WithMany(s => s.Messages).HasForeignKey(m => m.SessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiKey>(e =>
        {
            e.HasIndex(k => k.KeyHash).IsUnique();
            e.HasIndex(k => k.UserId);
            e.HasOne(k => k.User).WithMany(u => u.ApiKeys).HasForeignKey(k => k.UserId);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.CreatedAt);
            e.Property(a => a.Action).HasConversion<string>().HasMaxLength(20);
            e.HasOne(a => a.User).WithMany(u => u.AuditLogs).HasForeignKey(a => a.UserId);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(r => r.Token).IsUnique();
            e.HasIndex(r => r.UserId);
            e.HasOne(r => r.User).WithMany(u => u.RefreshTokens).HasForeignKey(r => r.UserId);
        });
    }
}
