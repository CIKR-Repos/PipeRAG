using PipeRAG.Core.Enums;

namespace PipeRAG.Core.Interfaces;

public interface IUsageTrackingService
{
    Task IncrementQueryCountAsync(Guid userId);
    Task<UsageDto> GetUsageAsync(Guid userId);
    Task<bool> CanPerformQueryAsync(Guid userId);
    Task<bool> CanCreateDocumentAsync(Guid userId);
    Task<bool> CanCreateProjectAsync(Guid userId);
    Task<bool> CanUploadStorageAsync(Guid userId, long additionalBytes);
    Task RecalculateDocumentCountAsync(Guid userId);
    Task RecalculateProjectCountAsync(Guid userId);
    Task RecalculateStorageAsync(Guid userId);
}

public record UsageDto(
    int QueriesUsed,
    int QueriesLimit,
    int DocumentsUsed,
    int DocumentsLimit,
    int ProjectsUsed,
    int ProjectsLimit,
    long StorageBytesUsed,
    long StorageBytesLimit,
    UserTier Tier);

public static class TierLimits
{
    public static (int QueriesPerDay, int MaxDocuments, int MaxProjects, long MaxStorageBytes) GetLimits(UserTier tier) => tier switch
    {
        UserTier.Free => (100, 10, 1, 50L * 1024 * 1024),
        UserTier.Pro => (10_000, 1_000, 20, 5L * 1024 * 1024 * 1024),
        UserTier.Enterprise => (int.MaxValue, int.MaxValue, int.MaxValue, long.MaxValue),
        _ => (100, 10, 1, 50L * 1024 * 1024)
    };
}
