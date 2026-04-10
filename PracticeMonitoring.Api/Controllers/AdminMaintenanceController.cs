using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/admin/maintenance")]
[Authorize(Roles = "Admin")]
public class AdminMaintenanceController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLogService;
    private readonly DatabaseBackupService _databaseBackupService;

    public AdminMaintenanceController(
        AppDbContext context,
        AuditLogService auditLogService,
        DatabaseBackupService databaseBackupService)
    {
        _context = context;
        _auditLogService = auditLogService;
        _databaseBackupService = databaseBackupService;
    }

    [HttpGet("logs/export/{category}")]
    public async Task<IActionResult> ExportLogs(string category)
    {
        var normalizedCategory = category switch
        {
            "registered-users" => "RegisteredUser",
            "admin-actions" => "AdminAction",
            "user-profile-changes" => "UserProfileChange",
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
            sb.AppendLine($"{log.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm:ss} | {log.Action} | {log.Description}");
        }

        var fileName = category switch
        {
            "registered-users" => "registered-users-log.txt",
            "admin-actions" => "admin-actions-log.txt",
            "user-profile-changes" => "user-profile-changes-log.txt",
            _ => "logs.txt"
        };

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/plain; charset=utf-8", fileName);
    }

    [HttpPost("database/backup")]
    public async Task<IActionResult> BackupDatabase()
    {
        var admin = await GetCurrentAdminAsync();
        if (admin is null)
            return Unauthorized();

        var result = await _databaseBackupService.CreateBackupAsync();

        await _auditLogService.LogAdminActionAsync(
            actorUserId: admin.Id,
            actorFullName: admin.FullName,
            action: "DatabaseBackupCreated",
            description: "Администратор создал резервную копию базы данных.");

        return File(result.Content, "application/octet-stream", result.FileName);
    }

    [HttpPost("database/restore")]
    [RequestSizeLimit(500_000_000)]
    public async Task<IActionResult> RestoreDatabase(IFormFile backupFile)
    {
        if (backupFile is null || backupFile.Length == 0)
            return BadRequest(new { message = "Файл резервной копии не выбран." });

        var admin = await GetCurrentAdminAsync();
        if (admin is null)
            return Unauthorized();

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{Path.GetExtension(backupFile.FileName)}");

        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await backupFile.CopyToAsync(stream);
            }

            await _databaseBackupService.RestoreBackupAsync(tempPath);

            await _auditLogService.LogAdminActionAsync(
                actorUserId: admin.Id,
                actorFullName: admin.FullName,
                action: "DatabaseBackupRestored",
                description: $"Администратор восстановил базу данных из файла {backupFile.FileName}.");

            return Ok(new { message = "База данных успешно восстановлена." });
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    private async Task<PracticeMonitoring.Api.Entities.User?> GetCurrentAdminAsync()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim is null || !int.TryParse(claim, out var adminId))
            return null;

        return await _context.Users.FirstOrDefaultAsync(x => x.Id == adminId);
    }
}