using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Services;

public class AuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string category,
        string action,
        string description,
        int? actorUserId = null,
        string? actorFullName = null,
        int? targetUserId = null,
        string? targetUserFullName = null)
    {
        var log = new AuditLog
        {
            Category = category,
            Action = action,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow,
            ActorUserId = actorUserId,
            ActorFullName = actorFullName,
            TargetUserId = targetUserId,
            TargetUserFullName = targetUserFullName
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public Task LogRegisteredUserAsync(User user)
    {
        return LogAsync(
            category: "RegisteredUser",
            action: "UserRegistered",
            description: $"Зарегистрирован новый пользователь: {user.FullName} ({user.Email}), роль: {user.Role?.Name ?? user.RoleId.ToString()}.",
            actorUserId: user.Id,
            actorFullName: user.FullName,
            targetUserId: user.Id,
            targetUserFullName: user.FullName);
    }

    public Task LogUserProfileChangeAsync(
        int actorUserId,
        string actorFullName,
        int targetUserId,
        string targetUserFullName,
        List<string> changedFields)
    {
        var description = $"Пользователь изменил данные профиля. Изменены поля: {string.Join(", ", changedFields)}.";

        return LogAsync(
            category: "UserProfileChange",
            action: "ProfileUpdated",
            description: description,
            actorUserId: actorUserId,
            actorFullName: actorFullName,
            targetUserId: targetUserId,
            targetUserFullName: targetUserFullName);
    }

    public Task LogAdminActionAsync(
        int actorUserId,
        string actorFullName,
        string action,
        string description,
        int? targetUserId = null,
        string? targetUserFullName = null)
    {
        return LogAsync(
            category: "AdminAction",
            action: action,
            description: description,
            actorUserId: actorUserId,
            actorFullName: actorFullName,
            targetUserId: targetUserId,
            targetUserFullName: targetUserFullName);
    }
}