namespace PracticeMonitoring.Api.Entities;

public class ChatThread
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<ChatParticipant> Participants { get; set; } = new();

    public List<ChatMessage> Messages { get; set; } = new();
}
