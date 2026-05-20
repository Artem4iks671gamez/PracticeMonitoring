using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos;
using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLogService;
    private readonly IWebHostEnvironment _environment;

    public ProfileController(AppDbContext context, AuditLogService auditLogService, IWebHostEnvironment environment)
    {
        _context = context;
        _auditLogService = auditLogService;
        _environment = environment;
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
                .ThenInclude(g => g!.Specialty)
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

        return Ok(ToCurrentUserResponse(user));
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<CurrentUserResponse>> UploadAvatar(IFormFile? file)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Выберите файл аватара." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Аватар не должен превышать 5 МБ." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
            return BadRequest(new { message = "Можно загрузить только JPG, PNG или WEBP." });

        var user = await _context.Users
            .Include(x => x.Role)
            .Include(x => x.Group)
                .ThenInclude(g => g!.Specialty)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return NotFound();

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

        var uploadsRoot = Path.Combine(webRoot, "uploads", "avatars");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);
        await using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream);

        user.AvatarUrl = $"{Request.Scheme}://{Request.Host}/uploads/avatars/{fileName}";
        await _context.SaveChangesAsync();

        await _auditLogService.LogUserProfileChangeAsync(
            actorUserId: user.Id,
            actorFullName: user.FullName,
            targetUserId: user.Id,
            targetUserFullName: user.FullName,
            changedFields: new List<string> { "Аватар" });

        return Ok(ToCurrentUserResponse(user));
    }

    private static CurrentUserResponse ToCurrentUserResponse(User user)
    {
        return new CurrentUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role!.Name,
            Surname = user.Surname,
            FirstName = user.FirstName,
            Patronymic = user.Patronymic,
            GroupId = user.GroupId,
            GroupName = user.Group?.Name,
            SpecialtyCode = user.Group?.Specialty?.Code,
            SpecialtyName = user.Group?.Specialty?.Name,
            AvatarUrl = user.AvatarUrl,
            Theme = user.Theme
        };
    }
}
