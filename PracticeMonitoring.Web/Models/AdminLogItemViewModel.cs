namespace PracticeMonitoring.Web.Models.Admin;

public class AdminLogItemViewModel
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public int? ActorUserId { get; set; }

    public string? ActorFullName { get; set; }

    public int? TargetUserId { get; set; }

    public string? TargetUserFullName { get; set; }
}