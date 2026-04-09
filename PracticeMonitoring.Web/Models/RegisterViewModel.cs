using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Web.Models.Auth;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введите фамилию")]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите имя")]
    public string Name { get; set; } = string.Empty;

    public string? Patronymic { get; set; }

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Введите корректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,32}$",
        ErrorMessage = "Пароль должен содержать 8–32 символа, минимум одну заглавную букву, одну строчную букву и одну цифру. Разрешены только латинские буквы и цифры."
    )]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string Role { get; set; } = "Student";

    [Required(ErrorMessage = "Выберите специальность")]
    public int? SpecialtyId { get; set; }

    [Required(ErrorMessage = "Выберите группу")]
    public int? GroupId { get; set; }
}