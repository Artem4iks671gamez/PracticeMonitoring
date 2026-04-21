using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Api.Dtos.DepartmentStaff;

public class ProductionPracticeCompetencyRequest
{
    [Required(ErrorMessage = "Укажите код компетенции.")]
    public string CompetencyCode { get; set; } = null!;

    [Required(ErrorMessage = "Укажите описание компетенции.")]
    public string CompetencyDescription { get; set; } = null!;

    [Required(ErrorMessage = "Укажите виды работ.")]
    public string WorkTypes { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Количество часов должно быть больше нуля.")]
    public int Hours { get; set; }
}