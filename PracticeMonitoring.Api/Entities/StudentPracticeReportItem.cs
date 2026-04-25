namespace PracticeMonitoring.Api.Entities;

public class StudentPracticeReportItem
{
    public int Id { get; set; }

    public int ProductionPracticeStudentAssignmentId { get; set; }

    public ProductionPracticeStudentAssignment Assignment { get; set; } = null!;

    public string Category { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }
}
