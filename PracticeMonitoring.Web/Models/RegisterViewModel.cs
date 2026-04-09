using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Web.Models.Auth;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введите фамилию")]
    public string Surname { get; set; } = string.Empty;
    [Required(ErrorMessage = "Введите Имя")]
    public string Name { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    [Required(ErrorMessage = "Введите email")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Введите пароль")]
    public string Password { get; set; } = string.Empty;
    
    public string Role { get; set; } = "Student";

    [Required(ErrorMessage = "Выберите специальность")]
    public int? SpecialtyId { get; set; }
    [Required(ErrorMessage = "Выберите группу")]
    public int? GroupId { get; set; }
}