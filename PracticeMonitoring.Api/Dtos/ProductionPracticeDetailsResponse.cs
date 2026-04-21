namespace PracticeMonitoring.Api.Dtos.DepartmentStaff;

public class ProductionPracticeDetailsResponse
{
    public int Id { get; set; }

    public string PracticeIndex { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int SpecialtyId { get; set; }

    public string SpecialtyCode { get; set; } = null!;

    public string SpecialtyName { get; set; } = null!;

    public string ProfessionalModuleCode { get; set; } = null!;

    public string ProfessionalModuleName { get; set; } = null!;

    public int Hours { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public List<ProductionPracticeCompetencyItemResponse> Competencies { get; set; } = new();

    public List<ProductionPracticeStudentAssignmentItemResponse> StudentAssignments { get; set; } = new();
}

public class ProductionPracticeCompetencyItemResponse
{
    public int Id { get; set; }

    public string CompetencyCode { get; set; } = null!;

    public string CompetencyDescription { get; set; } = null!;

    public string WorkTypes { get; set; } = null!;

    public int Hours { get; set; }
}

public class ProductionPracticeStudentAssignmentItemResponse
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public string StudentFullName { get; set; } = null!;

    public int? SupervisorId { get; set; }

    public string? SupervisorFullName { get; set; }
}