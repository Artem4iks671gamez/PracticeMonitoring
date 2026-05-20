namespace PracticeMonitoring.Api.Entities;

public class PushDevice
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public string TokenEncrypted { get; set; } = null!;

    public string Platform { get; set; } = "android";

    public string? DeviceName { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? LastSeenAtUtc { get; set; }
}
