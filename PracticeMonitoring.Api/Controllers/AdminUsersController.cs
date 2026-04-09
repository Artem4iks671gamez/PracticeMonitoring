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
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly AuditLogService _auditLogService;

    public AdminUsersController(
        AppDbContext context,
        PasswordService passwordService,
        AuditLogService auditLogService)
    {
        _context = context;
        _passwordService = passwordService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminUserListItemResponse>>> GetAll()
    {
        var users = await _context.Users
            .Include(x => x.Role)
            .Include(x => x.Group)
                .ThenInclude(g => g.Specialty)
            .OrderBy(x => x.Surname)
            .ThenBy(x => x.FirstName)
            .Select(x => new AdminUserListItemResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                Role = x.Role.Name,
                IsActive = x.IsActive,
                AvatarUrl = x.AvatarUrl,
                GroupId = x.GroupId,
                GroupName = x.Group != null ? x.Group.Name : null,
                Course = x.Group != null ? x.Group.Course : null,
                SpecialtyId = x.Group != null ? x.Group.SpecialtyId : null,
                SpecialtyCode = x.Group != null && x.Group.Specialty != null ? x.Group.Specialty.Code : null,
                SpecialtyName = x.Group != null && x.Group.Specialty != null ? x.Group.Specialty.Name : null
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminUserListItemResponse>> Update(int id, AdminUpsertUserRequest request)
    {
        var admin = await GetActorAsync();
        if (admin is null)
            return Unauthorized();

        var user = await _context.Users
            .Include(x => x.Role)
            .Include(x => x.Group)
                .ThenInclude(g => g.Specialty)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
            return NotFound();

        var email = request.Email.Trim().ToLower();
        var emailInUse = await _context.Users.AnyAsync(x => x.Email == email && x.Id != id);
        if (emailInUse)
            return BadRequest(new { message = "Этот email уже используется другим пользователем." });

        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Name == request.Role);
        if (role is null)
            return BadRequest(new { message = "Указанная роль не существует." });

        Group? group = null;
        if (request.Role == "Student")
        {
            if (!request.GroupId.HasValue)
                return BadRequest(new { message = "Для студента необходимо выбрать группу." });

            group = await _context.Groups
                .Include(g => g.Specialty)
                .FirstOrDefaultAsync(g => g.Id == request.GroupId.Value);

            if (group is null)
                return BadRequest(new { message = "Выбранная группа не найдена." });
        }

        var changes = new List<string>();

        TrackChange(changes, "Фамилия", user.Surname, request.Surname.Trim());
        TrackChange(changes, "Имя", user.FirstName, request.FirstName.Trim());
        TrackChange(changes, "Отчество", user.Patronymic, string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim());
        TrackChange(changes, "Email", user.Email, email);
        TrackChange(changes, "Роль", user.Role.Name, role.Name);
        TrackChange(changes, "Активность", user.IsActive ? "Активен" : "Неактивен", request.IsActive ? "Активен" : "Неактивен");
        TrackChange(changes, "Группа", user.Group?.Name, group?.Name);

        user.Surname = request.Surname.Trim();
        user.FirstName = request.FirstName.Trim();
        user.Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim();
        user.Email = email;
        user.RoleId = role.Id;
        user.Role = role;
        user.GroupId = group?.Id;
        user.Group = group;
        user.IsActive = request.IsActive;

        if (request.RemoveAvatar)
        {
            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
                changes.Add("Аватар удалён");

            user.AvatarUrl = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.AvatarUrl) && user.AvatarUrl != request.AvatarUrl)
        {
            changes.Add("Аватар обновлён");
            user.AvatarUrl = request.AvatarUrl.Trim();
        }

        user.FullName = string.IsNullOrWhiteSpace(user.Patronymic)
            ? $"{user.Surname} {user.FirstName}"
            : $"{user.Surname} {user.FirstName} {user.Patronymic}";

        await _context.SaveChangesAsync();

        if (changes.Count > 0)
        {
            await _auditLogService.LogAdminActionAsync(
                actorUserId: admin.Id,
                actorFullName: admin.FullName,
                action: "AdminUpdatedUser",
                description: $"Администратор изменил пользователя {user.FullName}. Изменения: {string.Join("; ", changes)}.",
                targetUserId: user.Id,
                targetUserFullName: user.FullName);
        }

        user = await _context.Users
            .Include(x => x.Role)
            .Include(x => x.Group)
                .ThenInclude(g => g.Specialty)
            .FirstAsync(x => x.Id == id);

        return Ok(MapUser(user));
    }

    [HttpPost("create-admin")]
    public Task<ActionResult<AdminUserListItemResponse>> CreateAdmin(AdminUpsertUserRequest request)
        => CreateByRole(request, "Admin", "AdminCreated");

    [HttpPost("create-supervisor")]
    public Task<ActionResult<AdminUserListItemResponse>> CreateSupervisor(AdminUpsertUserRequest request)
        => CreateByRole(request, "Supervisor", "SupervisorCreated");

    [HttpPost("create-department-staff")]
    public Task<ActionResult<AdminUserListItemResponse>> CreateDepartmentStaff(AdminUpsertUserRequest request)
        => CreateByRole(request, "DepartmentStaff", "DepartmentStaffCreated");

    private async Task<ActionResult<AdminUserListItemResponse>> CreateByRole(AdminUpsertUserRequest request, string roleName, string actionName)
    {
        var admin = await GetActorAsync();
        if (admin is null)
            return Unauthorized();

        var email = request.Email.Trim().ToLower();
        var emailInUse = await _context.Users.AnyAsync(x => x.Email == email);
        if (emailInUse)
            return BadRequest(new { message = "Этот email уже используется другим пользователем." });

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Для нового пользователя необходимо указать пароль." });

        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Name == roleName);
        if (role is null)
            return BadRequest(new { message = $"Роль {roleName} не найдена." });

        var user = new User
        {
            Surname = request.Surname.Trim(),
            FirstName = request.FirstName.Trim(),
            Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim(),
            Email = email,
            RoleId = role.Id,
            Role = role,
            GroupId = null,
            Group = null,
            AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            Theme = "light",
            IsActive = request.IsActive
        };

        user.FullName = string.IsNullOrWhiteSpace(user.Patronymic)
            ? $"{user.Surname} {user.FirstName}"
            : $"{user.Surname} {user.FirstName} {user.Patronymic}";

        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAdminActionAsync(
            actorUserId: admin.Id,
            actorFullName: admin.FullName,
            action: actionName,
            description: $"Администратор создал пользователя {user.FullName} с ролью {role.Name}.",
            targetUserId: user.Id,
            targetUserFullName: user.FullName);

        return Ok(MapUser(user));
    }

    private async Task<User?> GetActorAsync()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim is null || !int.TryParse(claim, out var actorId))
            return null;

        return await _context.Users.FirstOrDefaultAsync(x => x.Id == actorId);
    }

    private static AdminUserListItemResponse MapUser(User x)
    {
        return new AdminUserListItemResponse
        {
            Id = x.Id,
            FullName = x.FullName,
            Email = x.Email,
            Role = x.Role.Name,
            IsActive = x.IsActive,
            AvatarUrl = x.AvatarUrl,
            GroupId = x.GroupId,
            GroupName = x.Group?.Name,
            Course = x.Group?.Course,
            SpecialtyId = x.Group?.SpecialtyId,
            SpecialtyCode = x.Group?.Specialty?.Code,
            SpecialtyName = x.Group?.Specialty?.Name
        };
    }

    private static void TrackChange(List<string> changes, string field, string? oldValue, string? newValue)
    {
        if ((oldValue ?? string.Empty) != (newValue ?? string.Empty))
        {
            changes.Add($"{field}: \"{oldValue ?? "—"}\" → \"{newValue ?? "—"}\"");
        }
    }
}