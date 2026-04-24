namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffPracticeDetailsViewModel
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

    public bool IsCompleted { get; set; }

    public List<DepartmentStaffPracticeCompetencyItemViewModel> Competencies { get; set; } = new();

    public List<DepartmentStaffPracticeStudentAssignmentItemViewModel> StudentAssignments { get; set; } = new();
}

public class DepartmentStaffPracticeCompetencyItemViewModel
{
    public int Id { get; set; }

    public string CompetencyCode { get; set; } = string.Empty;

    public string CompetencyDescription { get; set; } = string.Empty;

    public string WorkTypes { get; set; } = string.Empty;

    public int Hours { get; set; }
}

public class DepartmentStaffPracticeStudentAssignmentItemViewModel
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public int? StudentSpecialtyId { get; set; }

    public string? StudentSpecialtyCode { get; set; }

    public string? StudentSpecialtyName { get; set; }

    public string? StudentGroupName { get; set; }

    public int? StudentCourse { get; set; }

    public int? SupervisorId { get; set; }

    public string? SupervisorFullName { get; set; }
}
