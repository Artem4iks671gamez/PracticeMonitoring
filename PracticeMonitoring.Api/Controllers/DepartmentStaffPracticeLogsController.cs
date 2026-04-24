using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/department-staff/practice-logs")]
[Authorize(Roles = "DepartmentStaff,Admin")]
public class DepartmentStaffPracticeLogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DepartmentStaffPracticeLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("practice-changes")]
    public Task<ActionResult<List<AuditLogItemResponse>>> GetPracticeChangesLogs()
    {
        return GetLogsByCategoryAsync("ProductionPracticeChange");
    }

    [HttpGet("assignment-changes")]
    public Task<ActionResult<List<AuditLogItemResponse>>> GetAssignmentChangesLogs()
    {
        return GetLogsByCategoryAsync("ProductionPracticeAssignmentChange");
    }

    private async Task<ActionResult<List<AuditLogItemResponse>>> GetLogsByCategoryAsync(string category)
    {
        var logs = await _context.AuditLogs
            .Where(x => x.Category == category)
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
