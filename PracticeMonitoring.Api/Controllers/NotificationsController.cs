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
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationItemResponse>>> GetAll()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var items = await _context.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .Select(x => new NotificationItemResponse
            {
                Id = x.Id,
                Category = x.Category,
                Title = x.Title,
                Message = x.Message,
                LinkUrl = x.LinkUrl,
                IsRead = x.IsRead,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var count = await _context.Notifications
            .CountAsync(x => x.UserId == currentUserId.Value && !x.IsRead);

        return Ok(new { count });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == currentUserId.Value);

        if (notification is null)
            return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Уведомление отмечено как прочитанное." });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized();

        await _context.Notifications
            .Where(x => x.UserId == currentUserId.Value && !x.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRead, true));

        return Ok(new { message = "Уведомления отмечены как прочитанные." });
    }

    private int? GetCurrentUserId()
    {
        var rawValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawValue, out var userId) ? userId : null;
    }
}
