namespace PracticeMonitoring.Api.Entities;

public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? Patronymic { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public int? GroupId { get; set; }

    public Group? Group { get; set; }

    public string? AvatarUrl { get; set; }

    public string Theme { get; set; } = "light";

    public bool IsActive { get; set; } = true;
}