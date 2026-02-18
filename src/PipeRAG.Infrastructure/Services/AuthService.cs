using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PipeRAG.Core.DTOs;
using PipeRAG.Core.Entities;
using PipeRAG.Core.Enums;
using PipeRAG.Core.Interfaces;
using PipeRAG.Infrastructure.Data;

namespace PipeRAG.Infrastructure.Services;

/// <summary>
/// JWT-based authentication service.
/// </summary>
public class AuthService : IAuthService
{
    private readonly PipeRagDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(PipeRagDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            DisplayName = request.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Tier = UserTier.Free,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await GenerateAuthResponse(user);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email)
            ?? throw new InvalidOperationException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials.");

        if (!user.IsActive)
            throw new InvalidOperationException("Account is deactivated.");

        return await GenerateAuthResponse(user);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var stored = await _db.Set<RefreshToken>()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken)
            ?? throw new InvalidOperationException("Invalid refresh token.");

        if (!stored.IsActive)
            throw new InvalidOperationException("Refresh token expired or revoked.");

        // Revoke old token
        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GenerateAuthResponse(stored.User);
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user)
    {
        var jwtSection = _config.GetSection("Jwt");
        var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured.");
        var issuer = jwtSection["Issuer"] ?? "PipeRAG";
        var audience = jwtSection["Audience"] ?? "PipeRAG";
        var accessMinutes = int.Parse(jwtSection["AccessTokenExpiryMinutes"] ?? "15");
        var refreshDays = int.Parse(jwtSection["RefreshTokenExpiryDays"] ?? "7");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(accessMinutes);

        var claims = new[]
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("Tier", user.Tier.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        // Generate refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            CreatedAt = DateTime.UtcNow
        };

        _db.Set<RefreshToken>().Add(refreshTokenEntity);
        await _db.SaveChangesAsync();

        var profile = new UserProfileResponse(
            user.Id, user.Email, user.DisplayName, user.Tier, user.IsActive, user.CreatedAt);

        return new AuthResponse(accessToken, refreshTokenEntity.Token, expiresAt, profile);
    }
}
