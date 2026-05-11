using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Web.Models.Auth;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Введите корректный email")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Введите корректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите код")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Код должен состоять из 6 цифр")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите новый пароль")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,32}$",
        ErrorMessage = "Пароль должен содержать 8–32 символа, минимум одну заглавную букву, одну строчную букву и одну цифру. Разрешены только латинские буквы и цифры."
    )]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "Введите новый пароль")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,32}$",
        ErrorMessage = "Пароль должен содержать 8–32 символа, минимум одну заглавную букву, одну строчную букву и одну цифру. Разрешены только латинские буквы и цифры."
    )]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool MustChangePassword { get; set; }
}
