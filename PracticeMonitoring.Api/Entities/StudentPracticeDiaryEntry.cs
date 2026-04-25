namespace PracticeMonitoring.Api.Entities;

public class StudentPracticeDiaryEntry
{
    public int Id { get; set; }

    public int ProductionPracticeStudentAssignmentId { get; set; }

    public ProductionPracticeStudentAssignment Assignment { get; set; } = null!;

    public DateTime WorkDate { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string DetailedReport { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<StudentPracticeDiaryAttachment> Attachments { get; set; } = new();
}
