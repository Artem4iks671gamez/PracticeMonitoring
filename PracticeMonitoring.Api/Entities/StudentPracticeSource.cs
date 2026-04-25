namespace PracticeMonitoring.Api.Entities;

public class StudentPracticeSource
{
    public int Id { get; set; }

    public int ProductionPracticeStudentAssignmentId { get; set; }

    public ProductionPracticeStudentAssignment Assignment { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }
}
