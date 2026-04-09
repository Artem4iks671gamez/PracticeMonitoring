using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Web.Models.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите email")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Введите пароль")]
    public string Password { get; set; } = string.Empty;
}