namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffPracticeListItemViewModel
{
    public int Id { get; set; }

    public string PracticeIndex { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SpecialtyId { get; set; }

    public string SpecialtyCode { get; set; } = string.Empty;

    public string SpecialtyName { get; set; } = string.Empty;

    public string ProfessionalModuleCode { get; set; } = string.Empty;

    public string ProfessionalModuleName { get; set; } = string.Empty;

    public int Hours { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int AssignedStudentsCount { get; set; }

    public bool IsCompleted { get; set; }
}
