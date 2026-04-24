namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffSupervisorListItemViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public int AssignedStudentsCount { get; set; }

    public int PracticesCount { get; set; }
}

public class DepartmentStaffSupervisorDetailsViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public int AssignedStudentsCount { get; set; }

    public int PracticesCount { get; set; }

    public List<DepartmentStaffSupervisorStudentItemViewModel> Students { get; set; } = new();

    public List<DepartmentStaffSupervisorPracticeItemViewModel> Practices { get; set; } = new();
}

public class DepartmentStaffSupervisorStudentItemViewModel
{
    public int StudentId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string? GroupName { get; set; }

    public int? Course { get; set; }

    public int PracticeId { get; set; }

    public string PracticeIndex { get; set; } = string.Empty;

    public string PracticeName { get; set; } = string.Empty;
}

public class DepartmentStaffSupervisorPracticeItemViewModel
{
    public int PracticeId { get; set; }

    public string PracticeIndex { get; set; } = string.Empty;

    public string PracticeName { get; set; } = string.Empty;

    public string SpecialtyCode { get; set; } = string.Empty;

    public string SpecialtyName { get; set; } = string.Empty;

    public int StudentsCount { get; set; }
}
