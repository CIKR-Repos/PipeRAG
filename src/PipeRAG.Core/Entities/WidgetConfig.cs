namespace PipeRAG.Core.Entities;

/// <summary>
/// Widget configuration for an embeddable chat widget on a project.
/// </summary>
public class WidgetConfig
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }

    // Theme
    public string PrimaryColor { get; set; } = "#6366f1";
    public string BackgroundColor { get; set; } = "#1e1e2e";
    public string TextColor { get; set; } = "#ffffff";

    // Position: bottom-right, bottom-left
    public string Position { get; set; } = "bottom-right";

    // Avatar
    public string? AvatarUrl { get; set; }

    // Display
    public string Title { get; set; } = "Chat with us";
    public string Subtitle { get; set; } = "Ask anything about our docs";
    public string PlaceholderText { get; set; } = "Type a message...";

    // Security
    public string AllowedOrigins { get; set; } = "*";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
}
