namespace PracticeMonitoring.Web.Models.Messaging;

public class MessagingWorkspaceViewModel
{
    public int CurrentUserId { get; set; }

    public string CurrentUserRole { get; set; } = string.Empty;

    public string CurrentUserFullName { get; set; } = string.Empty;

    public string? CurrentUserAvatarUrl { get; set; }

    public List<ChatThreadListItemViewModel> Threads { get; set; } = new();
}

public class ChatThreadListItemViewModel
{
    public int Id { get; set; }

    public ChatUserShortViewModel OtherUser { get; set; } = new();

    public string LastMessagePreview { get; set; } = string.Empty;

    public DateTime? LastMessageAtUtc { get; set; }

    public int UnreadCount { get; set; }
}

public class ChatUserShortViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string Subtitle { get; set; } = string.Empty;
}

public class ChatThreadDetailsViewModel
{
    public int Id { get; set; }

    public ChatUserShortViewModel OtherUser { get; set; } = new();

    public List<ChatMessageViewModel> Messages { get; set; } = new();
}

public class ChatMessageViewModel
{
    public int Id { get; set; }

    public int ThreadId { get; set; }

    public int SenderUserId { get; set; }

    public string SenderFullName { get; set; } = string.Empty;

    public string? Text { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<ChatAttachmentViewModel> Attachments { get; set; } = new();
}

public class ChatAttachmentViewModel
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}
