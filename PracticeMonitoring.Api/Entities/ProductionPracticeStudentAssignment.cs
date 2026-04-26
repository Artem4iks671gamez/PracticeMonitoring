namespace PracticeMonitoring.Api.Entities;

public class ProductionPracticeStudentAssignment
{
    public int Id { get; set; }

    public int ProductionPracticeId { get; set; }

    public ProductionPractice ProductionPractice { get; set; } = null!;

    public int StudentId { get; set; }

    public User Student { get; set; } = null!;

    public int? SupervisorId { get; set; }

    public User? Supervisor { get; set; }

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public string? OrganizationName { get; set; }

    public string? OrganizationFullName { get; set; }

    public string? OrganizationShortName { get; set; }

    public string? OrganizationAddress { get; set; }

    public string? OrganizationSupervisorFullName { get; set; }

    public string? OrganizationSupervisorPosition { get; set; }

    public string? OrganizationSupervisorPhone { get; set; }

    public string? OrganizationSupervisorEmail { get; set; }

    public string? PracticeTaskContent { get; set; }

    public string? StudentDuties { get; set; }

    public string? ProvidedMaterialsDescription { get; set; }

    public string? WorkScheduleDescription { get; set; }

    public string? IntroductionMainGoal { get; set; }

    public DateTime? StudentDetailsUpdatedAtUtc { get; set; }

    public List<StudentPracticeDiaryEntry> DiaryEntries { get; set; } = new();

    public List<StudentPracticeReportItem> ReportItems { get; set; } = new();

    public List<StudentPracticeSource> Sources { get; set; } = new();

    public List<StudentPracticeAppendix> Appendices { get; set; } = new();
}
