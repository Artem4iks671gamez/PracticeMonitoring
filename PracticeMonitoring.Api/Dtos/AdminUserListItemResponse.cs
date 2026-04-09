namespace PracticeMonitoring.Api.Dtos;

public class AdminUserListItemResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? AvatarUrl { get; set; }

    public int? GroupId { get; set; }

    public string? GroupName { get; set; }

    public int? Course { get; set; }

    public int? SpecialtyId { get; set; }

    public string? SpecialtyCode { get; set; }

    public string? SpecialtyName { get; set; }
}