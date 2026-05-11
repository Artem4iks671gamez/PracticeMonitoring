namespace PracticeMonitoring.Api.Entities;

public class StudentPracticeDiaryEntry
{
    public int Id { get; set; }

    public int ProductionPracticeStudentAssignmentId { get; set; }

    public ProductionPracticeStudentAssignment Assignment { get; set; } = null!;

    public DateTime WorkDate { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string DetailedReport { get; set; } = string.Empty;

    public bool IsReviewed { get; set; }

    public int? SupervisorGrade { get; set; }

    public string? SupervisorComment { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public int? ReviewedBySupervisorId { get; set; }

    public User? ReviewedBySupervisor { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<StudentPracticeDiaryAttachment> Attachments { get; set; } = new();
}
