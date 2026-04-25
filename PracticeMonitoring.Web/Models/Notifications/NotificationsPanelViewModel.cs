namespace PracticeMonitoring.Web.Models.Notifications;

public class NotificationsPanelViewModel
{
    public List<NotificationItemViewModel> Items { get; set; } = new();

    public int UnreadCount => Items.Count(x => !x.IsRead);
}

public class NotificationItemViewModel
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
