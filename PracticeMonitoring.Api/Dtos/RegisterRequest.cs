using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Api.Dtos;

public class RegisterRequest
{
    [Required(ErrorMessage = "Фамилия обязательна.")]
    public string Surname { get; set; } = null!;

    [Required(ErrorMessage = "Имя обязательно.")]
    public string Name { get; set; } = null!;

    public string? Patronymic { get; set; }

    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Некорректный формат email.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Пароль обязателен.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,32}$",
        ErrorMessage = "Пароль должен содержать 8–32 символа, минимум одну заглавную букву, одну строчную букву и одну цифру. Разрешены только латинские буквы и цифры."
    )]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Роль обязательна.")]
    public string Role { get; set; } = null!;

    public int? GroupId { get; set; }
}