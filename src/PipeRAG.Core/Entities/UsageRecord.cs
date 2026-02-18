namespace PipeRAG.Core.Entities;

public class UsageRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public int QueryCount { get; set; }
    public int DocumentCount { get; set; }
    public int ProjectCount { get; set; }
    public long StorageBytes { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
