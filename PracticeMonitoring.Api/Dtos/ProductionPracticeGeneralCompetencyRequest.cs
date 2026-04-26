using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Api.Dtos.DepartmentStaff;

public class ProductionPracticeGeneralCompetencyRequest
{
    [Required(ErrorMessage = "Код общей компетенции обязателен.")]
    public string CompetencyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Описание общей компетенции обязательно.")]
    public string CompetencyDescription { get; set; } = string.Empty;
}
