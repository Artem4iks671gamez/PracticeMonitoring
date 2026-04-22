namespace PracticeMonitoring.Web.Models;

public class DepartmentStaffPracticeLogViewModel
{
    public DateTime CreatedAtUtc { get; set; }
    public string? ActorFullName { get; set; }
    public string Description { get; set; } = string.Empty;
}