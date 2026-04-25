namespace PracticeMonitoring.Api.Entities;

public class ChatMessageAttachment
{
    public int Id { get; set; }

    public int ChatMessageId { get; set; }

    public ChatMessage ChatMessage { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public byte[] Content { get; set; } = null!;
}
