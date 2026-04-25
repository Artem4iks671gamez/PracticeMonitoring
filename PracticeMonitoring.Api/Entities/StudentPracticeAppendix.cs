namespace PracticeMonitoring.Api.Entities;

public class StudentPracticeAppendix
{
    public int Id { get; set; }

    public int ProductionPracticeStudentAssignmentId { get; set; }

    public ProductionPracticeStudentAssignment Assignment { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public byte[] Content { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
