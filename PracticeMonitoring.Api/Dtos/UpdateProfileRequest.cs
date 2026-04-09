using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Api.Dtos;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Введите фамилию.")]
    public string Surname { get; set; } = null!;

    [Required(ErrorMessage = "Введите имя.")]
    public string FirstName { get; set; } = null!;

    public string? Patronymic { get; set; }

    [Required(ErrorMessage = "Введите email.")]
    [EmailAddress(ErrorMessage = "Некорректный формат email.")]
    public string Email { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    [Required(ErrorMessage = "Укажите тему.")]
    [RegularExpression("^(light|dark)$", ErrorMessage = "Допустимые значения темы: light или dark.")]
    public string Theme { get; set; } = "light";
}