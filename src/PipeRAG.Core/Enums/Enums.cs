namespace PipeRAG.Core.Enums;

public enum UserTier
{
    Free,
    Pro,
    Enterprise
}

public enum OrgRole
{
    Member,
    Admin,
    Owner
}

public enum DocumentStatus
{
    Uploaded,
    Processing,
    Chunked,
    Embedded,
    Failed
}

public enum PipelineStatus
{
    Draft,
    Active,
    Paused,
    Archived
}

public enum PipelineRunStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum ChatMessageRole
{
    User,
    Assistant,
    System
}

public enum AuditAction
{
    Create,
    Update,
    Delete,
    Login,
    Logout,
    Upload,
    Query
}
