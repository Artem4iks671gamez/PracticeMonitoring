using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Api.Dtos.DepartmentStaff;

public class ProductionPracticeUpsertRequest
{
    [Required(ErrorMessage = "Укажите индекс ПП.")]
    public string PracticeIndex { get; set; } = null!;

    [Required(ErrorMessage = "Укажите название ПП.")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Выберите специальность.")]
    public int SpecialtyId { get; set; }

    [Required(ErrorMessage = "Укажите код профессионального модуля.")]
    public string ProfessionalModuleCode { get; set; } = null!;

    [Required(ErrorMessage = "Укажите название профессионального модуля.")]
    public string ProfessionalModuleName { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Количество часов должно быть больше нуля.")]
    public int Hours { get; set; }

    [Required(ErrorMessage = "Укажите дату начала.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Укажите дату окончания.")]
    public DateTime EndDate { get; set; }

    public bool ConfirmSpecialtyChangeStudentReset { get; set; }

    public List<ProductionPracticeCompetencyRequest> Competencies { get; set; } = new();

    public List<ProductionPracticeStudentAssignmentRequest> StudentAssignments { get; set; } = new();
}
