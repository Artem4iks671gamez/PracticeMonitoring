namespace PracticeMonitoring.Api.Dtos;

public class AuditLogItemResponse
{
    public int Id { get; set; }

    public string Category { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public int? ActorUserId { get; set; }

    public string? ActorFullName { get; set; }

    public int? TargetUserId { get; set; }

    public string? TargetUserFullName { get; set; }
}