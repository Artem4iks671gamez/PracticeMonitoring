using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPut("me")]
    public async Task<ActionResult<CurrentUserResponse>> UpdateProfile(UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _context.Users
            .Include(x => x.Role)
            .Include(x => x.Group)
                .ThenInclude(g => g.Specialty)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return NotFound();

        var normalizedEmail = request.Email.Trim().ToLower();
        var emailInUse = await _context.Users.AnyAsync(x => x.Email == normalizedEmail && x.Id != userId);
        if (emailInUse)
            return BadRequest(new { message = "Этот email уже используется другим пользователем." });

        user.Surname = request.Surname.Trim();
        user.FirstName = request.FirstName.Trim();
        user.Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim();
        user.Email = normalizedEmail;
        user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        user.Theme = request.Theme.Trim().ToLower();
        user.FullName = string.IsNullOrWhiteSpace(user.Patronymic)
            ? $"{user.Surname} {user.FirstName}"
            : $"{user.Surname} {user.FirstName} {user.Patronymic}";

        await _context.SaveChangesAsync();

        return Ok(new CurrentUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.Name,
            Surname = user.Surname,
            FirstName = user.FirstName,
            Patronymic = user.Patronymic,
            GroupId = user.GroupId,
            GroupName = user.Group?.Name,
            SpecialtyCode = user.Group?.Specialty?.Code,
            SpecialtyName = user.Group?.Specialty?.Name,
            AvatarUrl = user.AvatarUrl,
            Theme = user.Theme
        });
    }
}