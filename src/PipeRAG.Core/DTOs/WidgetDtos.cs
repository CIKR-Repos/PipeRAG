namespace PipeRAG.Core.DTOs;

/// <summary>
/// Request to create or update a widget configuration.
/// </summary>
public record WidgetConfigRequest(
    string? PrimaryColor = null,
    string? BackgroundColor = null,
    string? TextColor = null,
    string? Position = null,
    string? AvatarUrl = null,
    string? Title = null,
    string? Subtitle = null,
    string? PlaceholderText = null,
    string? AllowedOrigins = null,
    bool? IsActive = null);

/// <summary>
/// Response with widget configuration details.
/// </summary>
public record WidgetConfigResponse(
    Guid Id,
    Guid ProjectId,
    string PrimaryColor,
    string BackgroundColor,
    string TextColor,
    string Position,
    string? AvatarUrl,
    string Title,
    string Subtitle,
    string PlaceholderText,
    string AllowedOrigins,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
