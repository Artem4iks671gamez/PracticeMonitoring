using PracticeMonitoring.Web.Models.Auth;
using PracticeMonitoring.Web.Models.Messaging;
using PracticeMonitoring.Web.Models.Notifications;

namespace PracticeMonitoring.Web.Models.Student;

public class StudentPageViewModel
{
    public CurrentUserViewModel CurrentUser { get; set; } = new();

    public List<StudentPracticeListItemViewModel> Practices { get; set; } = new();

    public MessagingWorkspaceViewModel Messaging { get; set; } = new();

    public NotificationsPanelViewModel Notifications { get; set; } = new();

    public int UnreadChatsCount => Messaging.Threads.Sum(x => x.UnreadCount);
}
