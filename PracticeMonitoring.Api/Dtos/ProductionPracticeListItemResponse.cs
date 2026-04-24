namespace PracticeMonitoring.Api.Dtos.DepartmentStaff;

public class ProductionPracticeListItemResponse
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

    public int AssignedStudentsCount { get; set; }

    public bool IsCompleted { get; set; }
}
