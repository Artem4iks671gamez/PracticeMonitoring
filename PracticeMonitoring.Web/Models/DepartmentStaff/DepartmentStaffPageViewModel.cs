namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffPageViewModel
{
    public string FullName { get; set; } = string.Empty;

    public List<DepartmentStaffPracticeListItemViewModel> Practices { get; set; } = new();

    public List<DepartmentStaffAuditLogItemViewModel> PracticeChangeLogs { get; set; } = new();

    public List<DepartmentStaffAuditLogItemViewModel> AssignmentChangeLogs { get; set; } = new();
}
