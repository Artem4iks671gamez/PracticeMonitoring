using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Services;

public class NotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public void Add(int userId, string category, string title, string message, string? linkUrl = null)
    {
        if (userId <= 0)
            return;

        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Category = category.Trim(),
            Title = title.Trim(),
            Message = message.Trim(),
            LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
