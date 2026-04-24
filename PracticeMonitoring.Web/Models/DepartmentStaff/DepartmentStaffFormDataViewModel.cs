namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffFormDataViewModel
{
    public List<DepartmentStaffSelectOptionViewModel> Specialties { get; set; } = new();

    public List<DepartmentStaffStudentOptionViewModel> Students { get; set; } = new();

    public List<DepartmentStaffSupervisorOptionViewModel> Supervisors { get; set; } = new();
}

public class DepartmentStaffSelectOptionViewModel
{
    public int Id { get; set; }

    public string Label { get; set; } = string.Empty;
}

public class DepartmentStaffStudentOptionViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int? SpecialtyId { get; set; }

    public string? SpecialtyCode { get; set; }

    public string? SpecialtyName { get; set; }

    public string? GroupName { get; set; }

    public int? Course { get; set; }
}

public class DepartmentStaffSupervisorOptionViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;
}
