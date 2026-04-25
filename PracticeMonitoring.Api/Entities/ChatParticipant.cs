namespace PracticeMonitoring.Api.Entities;

public class ChatParticipant
{
    public int Id { get; set; }

    public int ChatThreadId { get; set; }

    public ChatThread ChatThread { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime JoinedAtUtc { get; set; }

    public DateTime? LastReadAtUtc { get; set; }
}
