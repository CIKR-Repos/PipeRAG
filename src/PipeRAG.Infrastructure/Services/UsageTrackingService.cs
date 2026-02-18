using Microsoft.EntityFrameworkCore;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;

namespace PipeRAG.Infrastructure.Services;

public class UsageTrackingService : IUsageTrackingService
{
    private readonly PipeRagDbContext _db;

    public UsageTrackingService(PipeRagDbContext db) => _db = db;

    public async Task IncrementQueryCountAsync(Guid userId)
    {
        var record = await GetOrCreateTodayRecord(userId);
        record.QueryCount++;
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<UsageDto> GetUsageAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");
        var limits = TierLimits.GetLimits(user.Tier);
        var today = await GetOrCreateTodayRecord(userId);

        var totalDocs = await _db.Documents
            .CountAsync(d => d.Project.OwnerId == userId);
        var totalProjects = await _db.Projects
            .CountAsync(p => p.OwnerId == userId);
        var totalStorage = await _db.Documents
            .Where(d => d.Project.OwnerId == userId)
            .SumAsync(d => (long)d.FileSizeBytes);

        return new UsageDto(
            today.QueryCount, limits.QueriesPerDay,
            totalDocs, limits.MaxDocuments,
            totalProjects, limits.MaxProjects,
            totalStorage, limits.MaxStorageBytes,
            user.Tier);
    }

    public async Task<bool> CanPerformQueryAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;
        if (user.Tier == UserTier.Enterprise) return true;
        var limits = TierLimits.GetLimits(user.Tier);
        var today = await GetOrCreateTodayRecord(userId);
        return today.QueryCount < limits.QueriesPerDay;
    }

    public async Task<bool> CanCreateDocumentAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;
        if (user.Tier == UserTier.Enterprise) return true;
        var limits = TierLimits.GetLimits(user.Tier);
        var count = await _db.Documents.CountAsync(d => d.Project.OwnerId == userId);
        return count < limits.MaxDocuments;
    }

    public async Task<bool> CanCreateProjectAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;
        if (user.Tier == UserTier.Enterprise) return true;
        var limits = TierLimits.GetLimits(user.Tier);
        var count = await _db.Projects.CountAsync(p => p.OwnerId == userId);
        return count < limits.MaxProjects;
    }

    public async Task<bool> CanUploadStorageAsync(Guid userId, long additionalBytes)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;
        if (user.Tier == UserTier.Enterprise) return true;
        var limits = TierLimits.GetLimits(user.Tier);
        var used = await _db.Documents
            .Where(d => d.Project.OwnerId == userId)
            .SumAsync(d => (long)d.FileSizeBytes);
        return (used + additionalBytes) <= limits.MaxStorageBytes;
    }

    public async Task RecalculateDocumentCountAsync(Guid userId)
    {
        var record = await GetOrCreateTodayRecord(userId);
        record.DocumentCount = await _db.Documents.CountAsync(d => d.Project.OwnerId == userId);
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RecalculateProjectCountAsync(Guid userId)
    {
        var record = await GetOrCreateTodayRecord(userId);
        record.ProjectCount = await _db.Projects.CountAsync(p => p.OwnerId == userId);
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task RecalculateStorageAsync(Guid userId)
    {
        var record = await GetOrCreateTodayRecord(userId);
        record.StorageBytes = await _db.Documents
            .Where(d => d.Project.OwnerId == userId)
            .SumAsync(d => (long)d.FileSizeBytes);
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task<UsageRecord> GetOrCreateTodayRecord(Guid userId)
    {
        var today = DateTime.UtcNow.Date;
        var record = await _db.UsageRecords
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Date == today);
        if (record == null)
        {
            record = new UsageRecord { UserId = userId, Date = today };
            _db.UsageRecords.Add(record);
            await _db.SaveChangesAsync();
        }
        return record;
    }
}
