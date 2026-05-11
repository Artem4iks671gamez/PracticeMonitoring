using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Api.Dtos;

public class SendRegistrationCodeRequest : RegisterRequest
{
}

public class ConfirmRegistrationRequest : RegisterRequest
{
    [Required(ErrorMessage = "Введите код подтверждения.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Код должен состоять из 6 цифр.")]
    public string Code { get; set; } = null!;
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Некорректный формат email.")]
    public string Email { get; set; } = null!;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Некорректный формат email.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Введите код подтверждения.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Код должен состоять из 6 цифр.")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Пароль обязателен.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,32}$",
        ErrorMessage = "Пароль должен содержать 8–32 символа, минимум одну заглавную букву, одну строчную букву и одну цифру. Разрешены только латинские буквы и цифры."
    )]
    public string NewPassword { get; set; } = null!;
}

public class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "Новый пароль обязателен.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,32}$",
        ErrorMessage = "Пароль должен содержать 8–32 символа, минимум одну заглавную букву, одну строчную букву и одну цифру. Разрешены только латинские буквы и цифры."
    )]
    public string NewPassword { get; set; } = null!;
}
