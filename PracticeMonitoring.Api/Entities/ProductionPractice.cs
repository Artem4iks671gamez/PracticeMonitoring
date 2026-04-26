namespace PracticeMonitoring.Api.Entities;

public class ProductionPractice
{
    public int Id { get; set; }

    public string PracticeIndex { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int SpecialtyId { get; set; }

    public Specialty Specialty { get; set; } = null!;

    public string ProfessionalModuleCode { get; set; } = null!;

    public string ProfessionalModuleName { get; set; } = null!;

    public int Hours { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public List<ProductionPracticeCompetency> Competencies { get; set; } = new();

    public List<ProductionPracticeGeneralCompetency> GeneralCompetencies { get; set; } = new();

    public List<ProductionPracticeStudentAssignment> StudentAssignments { get; set; } = new();
}
