using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/admin/logs")]
[Authorize(Roles = "Admin")]
public class AdminLogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("registered-users")]
    public async Task<ActionResult<List<AuditLogItemResponse>>> GetRegisteredUsersLogs()
    {
        var logs = await _context.AuditLogs
            .Where(x => x.Category == "RegisteredUser")
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AuditLogItemResponse
            {
                Id = x.Id,
                Category = x.Category,
                Action = x.Action,
                Description = x.Description,
                CreatedAtUtc = x.CreatedAtUtc,
                ActorUserId = x.ActorUserId,
                ActorFullName = x.ActorFullName,
                TargetUserId = x.TargetUserId,
                TargetUserFullName = x.TargetUserFullName
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("admin-actions")]
    public async Task<ActionResult<List<AuditLogItemResponse>>> GetAdminActionsLogs()
    {
        var logs = await _context.AuditLogs
            .Where(x => x.Category == "AdminAction")
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AuditLogItemResponse
            {
                Id = x.Id,
                Category = x.Category,
                Action = x.Action,
                Description = x.Description,
                CreatedAtUtc = x.CreatedAtUtc,
                ActorUserId = x.ActorUserId,
                ActorFullName = x.ActorFullName,
                TargetUserId = x.TargetUserId,
                TargetUserFullName = x.TargetUserFullName
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("user-profile-changes")]
    public async Task<ActionResult<List<AuditLogItemResponse>>> GetUserProfileChangesLogs()
    {
        var logs = await _context.AuditLogs
            .Where(x => x.Category == "UserProfileChange")
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AuditLogItemResponse
            {
                Id = x.Id,
                Category = x.Category,
                Action = x.Action,
                Description = x.Description,
                CreatedAtUtc = x.CreatedAtUtc,
                ActorUserId = x.ActorUserId,
                ActorFullName = x.ActorFullName,
                TargetUserId = x.TargetUserId,
                TargetUserFullName = x.TargetUserFullName
            })
            .ToListAsync();

        return Ok(logs);
    }
}