using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PipeRAG.Core.DTOs;
using PipeRAG.Infrastructure.Data;

namespace PipeRAG.Api.Controllers;

/// <summary>
/// User profile management endpoints.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly PipeRagDbContext _db;

    public UserController(PipeRagDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get current user's profile.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetProfile()
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new UserProfileResponse(
            user.Id, user.Email, user.DisplayName, user.Tier, user.IsActive, user.CreatedAt));
    }

    /// <summary>
    /// Update current user's profile.
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (request.DisplayName != null)
            user.DisplayName = request.DisplayName;

        if (request.Email != null)
        {
            if (await _db.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId))
                return BadRequest(new { error = "Email already in use." });
            user.Email = request.Email;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new UserProfileResponse(
            user.Id, user.Email, user.DisplayName, user.Tier, user.IsActive, user.CreatedAt));
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("UserId")?.Value
            ?? throw new InvalidOperationException("UserId claim not found.");
        return Guid.Parse(claim);
    }
}
