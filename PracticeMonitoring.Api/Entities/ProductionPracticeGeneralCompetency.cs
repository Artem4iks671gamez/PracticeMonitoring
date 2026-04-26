namespace PracticeMonitoring.Api.Entities;

public class ProductionPracticeGeneralCompetency
{
    public int Id { get; set; }

    public int ProductionPracticeId { get; set; }

    public ProductionPractice ProductionPractice { get; set; } = null!;

    public string CompetencyCode { get; set; } = string.Empty;

    public string CompetencyDescription { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
