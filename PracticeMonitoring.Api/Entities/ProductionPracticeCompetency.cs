namespace PracticeMonitoring.Api.Entities;

public class ProductionPracticeCompetency
{
    public int Id { get; set; }

    public int ProductionPracticeId { get; set; }

    public ProductionPractice ProductionPractice { get; set; } = null!;

    public string CompetencyCode { get; set; } = null!;

    public string CompetencyDescription { get; set; } = null!;

    public string WorkTypes { get; set; } = null!;

    public int Hours { get; set; }
}