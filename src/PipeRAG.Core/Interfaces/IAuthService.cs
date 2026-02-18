using PipeRAG.Core.DTOs;

namespace PipeRAG.Core.Interfaces;

/// <summary>
/// Authentication service interface.
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
}
