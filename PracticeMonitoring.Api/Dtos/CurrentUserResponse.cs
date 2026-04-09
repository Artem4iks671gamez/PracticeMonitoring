namespace PracticeMonitoring.Api.Dtos;

public class CurrentUserResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? Surname { get; set; }

    public string? FirstName { get; set; }

    public string? Patronymic { get; set; }

    public int? GroupId { get; set; }

    public string? GroupName { get; set; }

    public string? SpecialtyCode { get; set; }

    public string? SpecialtyName { get; set; }

    public string? AvatarUrl { get; set; }
}