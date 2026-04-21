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
}