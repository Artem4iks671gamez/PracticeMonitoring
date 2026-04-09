namespace PracticeMonitoring.Web.Models.Auth;

public class CurrentUserViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? Surname { get; set; }

    public string? FirstName { get; set; }

    public string? Patronymic { get; set; }

    public int? GroupId { get; set; }

    public string? GroupName { get; set; }

    public string? SpecialtyCode { get; set; }

    public string? SpecialtyName { get; set; }

    public string? AvatarUrl { get; set; }
}