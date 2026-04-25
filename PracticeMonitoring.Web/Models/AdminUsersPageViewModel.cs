using System.Collections.Generic;
using PracticeMonitoring.Web.Models.Auth;
using PracticeMonitoring.Web.Models.Messaging;
using PracticeMonitoring.Web.Models.Notifications;

namespace PracticeMonitoring.Web.Models.Admin;

public class AdminUsersPageViewModel
{
    public string AdminFullName { get; set; } = string.Empty;

    public CurrentUserViewModel CurrentUser { get; set; } = new();

    public List<AdminLogItemViewModel> RegisteredUsersLogs { get; set; } = new();

    public List<AdminLogItemViewModel> AdminActionsLogs { get; set; } = new();

    public List<AdminLogItemViewModel> UserProfileChangesLogs { get; set; } = new();

    public List<AdminUserItemViewModel> Users { get; set; } = new();

    public MessagingWorkspaceViewModel Messaging { get; set; } = new();

    public NotificationsPanelViewModel Notifications { get; set; } = new();

    public int UnreadChatsCount => Messaging.Threads.Sum(x => x.UnreadCount);
}
