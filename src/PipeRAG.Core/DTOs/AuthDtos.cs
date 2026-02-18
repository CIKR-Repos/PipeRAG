using PipeRAG.Core.Enums;

namespace PipeRAG.Core.DTOs;

/// <summary>
/// Request to register a new user.
/// </summary>
public record RegisterRequest(string Email, string Password, string DisplayName);

/// <summary>
/// Request to log in.
/// </summary>
public record LoginRequest(string Email, string Password);

/// <summary>
/// Request to refresh an access token.
/// </summary>
public record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// Authentication response with tokens.
/// </summary>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserProfileResponse User);

/// <summary>
/// User profile response.
/// </summary>
public record UserProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    UserTier Tier,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>
/// Request to update user profile.
/// </summary>
public record UpdateProfileRequest(string? DisplayName, string? Email);
