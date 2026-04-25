using PracticeMonitoring.Web.Models.Auth;
using PracticeMonitoring.Web.Models.Messaging;

namespace PracticeMonitoring.Web.Models.Supervisor;

public class SupervisorPageViewModel
{
    public CurrentUserViewModel CurrentUser { get; set; } = new();

    public MessagingWorkspaceViewModel Messaging { get; set; } = new();
}
