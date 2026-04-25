using PracticeMonitoring.Web.Models.Auth;
using PracticeMonitoring.Web.Models.Messaging;
using PracticeMonitoring.Web.Models.Notifications;

namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffPageViewModel
{
    public string FullName { get; set; } = string.Empty;

    public CurrentUserViewModel CurrentUser { get; set; } = new();

    public List<DepartmentStaffPracticeListItemViewModel> Practices { get; set; } = new();

    public List<DepartmentStaffSupervisorListItemViewModel> Supervisors { get; set; } = new();

    public List<DepartmentStaffAuditLogItemViewModel> PracticeChangeLogs { get; set; } = new();

    public List<DepartmentStaffAuditLogItemViewModel> AssignmentChangeLogs { get; set; } = new();

    public MessagingWorkspaceViewModel Messaging { get; set; } = new();

    public NotificationsPanelViewModel Notifications { get; set; } = new();

    public int UnreadChatsCount => Messaging.Threads.Sum(x => x.UnreadCount);
}
