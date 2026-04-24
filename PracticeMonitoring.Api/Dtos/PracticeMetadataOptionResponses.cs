namespace PracticeMonitoring.Api.Dtos.DepartmentStaff;

public class PracticeSelectOptionResponse
{
    public int Id { get; set; }

    public string Label { get; set; } = string.Empty;
}

public class PracticeStudentOptionResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int? SpecialtyId { get; set; }

    public string? SpecialtyCode { get; set; }

    public string? SpecialtyName { get; set; }

    public string? GroupName { get; set; }

    public int? Course { get; set; }
}

public class PracticeSupervisorOptionResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;
}
