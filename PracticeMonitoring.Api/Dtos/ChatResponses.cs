namespace PracticeMonitoring.Api.Dtos;

public class ChatThreadListItemResponse
{
    public int Id { get; set; }

    public ChatUserShortResponse OtherUser { get; set; } = new();

    public string LastMessagePreview { get; set; } = string.Empty;

    public DateTime? LastMessageAtUtc { get; set; }

    public int UnreadCount { get; set; }
}

public class ChatUserShortResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string Subtitle { get; set; } = string.Empty;
}

public class ChatThreadDetailsResponse
{
    public int Id { get; set; }

    public ChatUserShortResponse OtherUser { get; set; } = new();

    public List<ChatMessageResponse> Messages { get; set; } = new();
}

public class ChatMessageResponse
{
    public int Id { get; set; }

    public int ThreadId { get; set; }

    public int SenderUserId { get; set; }

    public string SenderFullName { get; set; } = string.Empty;

    public string? Text { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<ChatAttachmentResponse> Attachments { get; set; } = new();
}

public class ChatAttachmentResponse
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}
