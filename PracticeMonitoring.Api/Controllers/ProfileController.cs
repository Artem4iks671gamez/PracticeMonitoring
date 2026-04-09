using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLogService;

    public ProfileController(AppDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
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

        var changedFields = new List<string>();

        if (user.Surname != request.Surname.Trim())
            changedFields.Add("Фамилия");

        if (user.FirstName != request.FirstName.Trim())
            changedFields.Add("Имя");

        var newPatronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim();
        if (user.Patronymic != newPatronymic)
            changedFields.Add("Отчество");

        if (user.Email != normalizedEmail)
            changedFields.Add("Email");

        user.Surname = request.Surname.Trim();
        user.FirstName = request.FirstName.Trim();
        user.Patronymic = newPatronymic;
        user.Email = normalizedEmail;
        user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        user.Theme = request.Theme.Trim().ToLower();
        user.FullName = string.IsNullOrWhiteSpace(user.Patronymic)
            ? $"{user.Surname} {user.FirstName}"
            : $"{user.Surname} {user.FirstName} {user.Patronymic}";

        await _context.SaveChangesAsync();

        if (changedFields.Count > 0)
        {
            await _auditLogService.LogUserProfileChangeAsync(
                actorUserId: user.Id,
                actorFullName: user.FullName,
                targetUserId: user.Id,
                targetUserFullName: user.FullName,
                changedFields: changedFields);
        }

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