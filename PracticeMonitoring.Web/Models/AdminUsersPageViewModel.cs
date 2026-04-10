using System.Collections.Generic;

namespace PracticeMonitoring.Web.Models.Admin;

public class AdminUsersPageViewModel
{
    public string AdminFullName { get; set; } = string.Empty;

    public List<AdminLogItemViewModel> RegisteredUsersLogs { get; set; } = new();

    public List<AdminLogItemViewModel> AdminActionsLogs { get; set; } = new();

    public List<AdminLogItemViewModel> UserProfileChangesLogs { get; set; } = new();

    public List<AdminUserItemViewModel> Users { get; set; } = new();
}