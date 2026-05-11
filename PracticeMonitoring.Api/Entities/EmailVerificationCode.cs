namespace PracticeMonitoring.Api.Entities;

public class EmailVerificationCode
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public string CodeHash { get; set; } = null!;

    public string? PayloadJson { get; set; }

    public int Attempts { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }
}
