using Microsoft.AspNetCore.Mvc.Rendering;

namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffPracticeUpsertViewModel
{
    public int? Id { get; set; }

    public string PracticeIndex { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SpecialtyId { get; set; }

    public string ProfessionalModuleCode { get; set; } = string.Empty;

    public string ProfessionalModuleName { get; set; } = string.Empty;

    public int Hours { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public List<DepartmentStaffPracticeCompetencyEditViewModel> Competencies { get; set; } = new();

    public List<DepartmentStaffPracticeStudentAssignmentEditViewModel> StudentAssignments { get; set; } = new();
}

public class DepartmentStaffPracticeCompetencyEditViewModel
{
    public string CompetencyCode { get; set; } = string.Empty;

    public string CompetencyDescription { get; set; } = string.Empty;

    public string WorkTypes { get; set; } = string.Empty;

    public int Hours { get; set; }
}

public class DepartmentStaffPracticeStudentAssignmentEditViewModel
{
    public int StudentId { get; set; }

    public int? SupervisorId { get; set; }
}