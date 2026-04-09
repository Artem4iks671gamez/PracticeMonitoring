using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PracticeMonitoring.Web.Models.Auth;

public class UpdateProfileViewModel
{
    [Required(ErrorMessage = "Введите фамилию")]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите имя")]
    public string FirstName { get; set; } = string.Empty;

    public string? Patronymic { get; set; }

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Введите корректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите тему")]
    [RegularExpression("^(light|dark)$", ErrorMessage = "Допустимые значения темы: light или dark")]
    public string Theme { get; set; } = "light";

    public IFormFile? AvatarFile { get; set; }

    public string? CurrentAvatarUrl { get; set; }
}