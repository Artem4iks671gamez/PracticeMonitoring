namespace PracticeMonitoring.Web.Models.Admin;

public class AdminUserItemViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string? AvatarUrl { get; set; }

    public int? GroupId { get; set; }

    public string? GroupName { get; set; }

    public int? Course { get; set; }

    public int? SpecialtyId { get; set; }

    public string? SpecialtyCode { get; set; }

    public string? SpecialtyName { get; set; }
}