using PracticeMonitoring.Web.Models.Auth;
using PracticeMonitoring.Web.Models.Messaging;

namespace PracticeMonitoring.Web.Models.Student;

public class StudentPageViewModel
{
    public CurrentUserViewModel CurrentUser { get; set; } = new();

    public MessagingWorkspaceViewModel Messaging { get; set; } = new();
}
