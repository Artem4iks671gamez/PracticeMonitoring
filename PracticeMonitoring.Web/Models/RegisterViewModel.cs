namespace PracticeMonitoring.Web.Models.Auth;

public class RegisterViewModel
{
    public string Surname { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";

    // Для выбора на клиенте
    public int? SpecialtyId { get; set; }
    public int? GroupId { get; set; }
}