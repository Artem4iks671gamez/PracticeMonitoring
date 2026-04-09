namespace PracticeMonitoring.Web.Models.Admin;

public class AdminLogsPageViewModel
{
    public string AdminFullName { get; set; } = string.Empty;

    public List<AdminLogItemViewModel> RegisteredUsersLogs { get; set; } = new();

    public List<AdminLogItemViewModel> AdminActionsLogs { get; set; } = new();

    public List<AdminLogItemViewModel> UserProfileChangesLogs { get; set; } = new();
}