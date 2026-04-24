using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
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

    [HttpGet("export/{category}")]
    public async Task<IActionResult> ExportLogs(string category)
    {
        var normalizedCategory = category switch
        {
            "practice-changes" => "ProductionPracticeChange",
            "assignment-changes" => "ProductionPracticeAssignmentChange",
            _ => null
        };

        if (normalizedCategory is null)
            return BadRequest(new { message = "Неизвестная категория логов." });

        var logs = await _context.AuditLogs
            .Where(x => x.Category == normalizedCategory)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        var sb = new StringBuilder();

        foreach (var log in logs)
        {
            var actor = string.IsNullOrWhiteSpace(log.ActorFullName) ? "Работник отдела" : log.ActorFullName;
            sb.AppendLine($"{log.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm:ss} | {actor} | {log.Action} | {log.Description}");
        }

        var fileName = category switch
        {
            "practice-changes" => "production-practice-changes-log.txt",
            "assignment-changes" => "production-practice-assignments-log.txt",
            _ => "department-staff-logs.txt"
        };

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/plain; charset=utf-8", fileName);
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
