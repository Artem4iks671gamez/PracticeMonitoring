namespace PracticeMonitoring.Api.Entities;

public class StudentPracticeSectionComment
{
    public int Id { get; set; }

    public int ProductionPracticeStudentAssignmentId { get; set; }

    public ProductionPracticeStudentAssignment Assignment { get; set; } = null!;

    public string SectionKey { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public int SupervisorId { get; set; }

    public User Supervisor { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
