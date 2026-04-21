namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffPageViewModel
{
    public string FullName { get; set; } = string.Empty;

    public List<DepartmentStaffPracticeListItemViewModel> Practices { get; set; } = new();
}