namespace PracticeMonitoring.Api.Entities;

public class ChatMessage
{
    public int Id { get; set; }

    public int ChatThreadId { get; set; }

    public ChatThread ChatThread { get; set; } = null!;

    public int SenderUserId { get; set; }

    public User SenderUser { get; set; } = null!;

    public string? Text { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<ChatMessageAttachment> Attachments { get; set; } = new();
}
