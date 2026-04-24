using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Web.Models.DepartmentStaff;

public class DepartmentStaffPracticeUpsertViewModel : IValidatableObject
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Индекс ПП обязателен.")]
    [RegularExpression(@"^\d+(\.\d+)*$", ErrorMessage = "Индекс должен содержать только цифры и точки, напр. 12.3")]
    public string PracticeIndex { get; set; } = string.Empty;

    [Required(ErrorMessage = "Название практики обязательно.")]
    [MinLength(3, ErrorMessage = "Название слишком короткое.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Выберите специальность.")]
    public int SpecialtyId { get; set; }

    [Required(ErrorMessage = "Код профессионального модуля обязателен.")]
    [RegularExpression(@"^\d+(\.\d+)*$", ErrorMessage = "Код ПМ должен содержать только цифры и точки.")]
    public string ProfessionalModuleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Название профессионального модуля обязательно.")]
    public string ProfessionalModuleName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Количество часов должно быть больше нуля.")]
    public int Hours { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool ConfirmSpecialtyChangeStudentReset { get; set; }

    public List<DepartmentStaffPracticeCompetencyEditViewModel> Competencies { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!StartDate.HasValue)
            yield return new ValidationResult("Укажите дату начала.", new[] { nameof(StartDate) });

        if (!EndDate.HasValue)
            yield return new ValidationResult("Укажите дату окончания.", new[] { nameof(EndDate) });

        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value < StartDate.Value)
            yield return new ValidationResult("Дата окончания не может быть раньше даты начала.", new[] { nameof(EndDate) });

        if (Competencies == null || Competencies.Count == 0)
        {
            yield return new ValidationResult("Добавьте хотя бы одну профессиональную компетенцию.", new[] { nameof(Competencies) });
        }
        else
        {
            var sum = 0;

            for (var i = 0; i < Competencies.Count; i++)
            {
                var c = Competencies[i];
                var prefix = $"Competencies[{i}]";

                if (string.IsNullOrWhiteSpace(c.CompetencyCode))
                    yield return new ValidationResult("Код компетенции обязателен.", new[] { $"{prefix}.CompetencyCode" });

                if (string.IsNullOrWhiteSpace(c.CompetencyDescription))
                    yield return new ValidationResult("Описание компетенции обязательно.", new[] { $"{prefix}.CompetencyDescription" });

                if (string.IsNullOrWhiteSpace(c.WorkTypes))
                    yield return new ValidationResult("Укажите виды работ.", new[] { $"{prefix}.WorkTypes" });

                if (c.Hours <= 0)
                    yield return new ValidationResult("Часы по компетенции должны быть больше нуля.", new[] { $"{prefix}.Hours" });
                else
                    sum += c.Hours;
            }

            if (Hours > 0 && sum != Hours)
                yield return new ValidationResult(
                    $"Сумма часов по компетенциям ({sum}) должна быть равна общему количеству часов ({Hours}).",
                    new[] { nameof(Competencies) });
        }

    }
}

public class DepartmentStaffPracticeCompetencyEditViewModel
{
    [Required(ErrorMessage = "Код компетенции обязателен.")]
    public string CompetencyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Описание компетенции обязательно.")]
    public string CompetencyDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "Виды работ обязательны.")]
    public string WorkTypes { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Часы должны быть больше нуля.")]
    public int Hours { get; set; }
}

public class DepartmentStaffPracticeStudentAssignmentEditViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Выберите студента.")]
    public int StudentId { get; set; }

    public int? SupervisorId { get; set; }
}
