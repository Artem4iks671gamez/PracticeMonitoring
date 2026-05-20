using System.Text.Json.Serialization;

namespace PracticeMonitoring.Mobile.Models;

public sealed class ApiResult<T>
{
    public bool Success { get; init; }

    public int StatusCode { get; init; }

    public T? Data { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public Dictionary<string, string[]> ValidationErrors { get; init; } = new();
}

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime TokenExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
}

public sealed class PushSettings
{
    public bool IsConfigured { get; set; }
    public bool HasRegisteredDevice { get; set; }
}

public sealed class CurrentUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Surname { get; set; }
    public string? FirstName { get; set; }
    public string? Patronymic { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? SpecialtyCode { get; set; }
    public string? SpecialtyName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Theme { get; set; } = "light";
    public bool MustChangePassword { get; set; }
}

public sealed class SpecialtyOption
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public override string ToString() => $"{Code} {Name}".Trim();
}

public sealed class GroupOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
    public int SpecialtyId { get; set; }
    public override string ToString() => $"{Name}, {Course} курс";
}

public sealed class RegisterRequest
{
    public string Surname { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; } = "Student";
    public int? GroupId { get; set; }
    public string? Code { get; set; }
}

public class PracticeListItem
{
    public int AssignmentId { get; set; }
    public int PracticeId { get; set; }
    public string PracticeIndex { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SpecialtyCode { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string QualificationName { get; set; } = string.Empty;
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentGroup { get; set; } = string.Empty;
    public int? StudentCourse { get; set; }
    public string ProfessionalModuleCode { get; set; } = string.Empty;
    public string ProfessionalModuleName { get; set; } = string.Empty;
    public int Hours { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCompleted { get; set; }
    public string? SupervisorFullName { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationFullName { get; set; }
    public string? OrganizationShortName { get; set; }
    public string? OrganizationAddress { get; set; }
    public bool HasRequiredDetails { get; set; }
    public DateTime DetailsDueDate { get; set; }
    public bool IsDetailsOverdue { get; set; }
    public int DiaryEntriesCount { get; set; }
    public int WorkDaysCount { get; set; }
}

public sealed class PracticeDetails : PracticeListItem
{
    public DateTime AssignedAtUtc { get; set; }
    public string? OrganizationSupervisorFullName { get; set; }
    public string? OrganizationSupervisorPosition { get; set; }
    public string? OrganizationSupervisorPhone { get; set; }
    public string? OrganizationSupervisorEmail { get; set; }
    public string? PracticeTaskContent { get; set; }
    public string? StudentDuties { get; set; }
    public string? ProvidedMaterialsDescription { get; set; }
    public string? WorkScheduleDescription { get; set; }
    public string? IntroductionMainGoal { get; set; }
    public List<GeneralCompetency> GeneralCompetencies { get; set; } = new();
    public List<PracticeCompetency> Competencies { get; set; } = new();
    public List<DiaryEntry> DiaryEntries { get; set; } = new();
    public List<ReportItem> ReportItems { get; set; } = new();
    public List<PracticeSource> Sources { get; set; } = new();
    public List<PracticeAppendix> Appendices { get; set; } = new();
    public List<SectionComment> SectionComments { get; set; } = new();
}

public sealed class PracticeCompetency
{
    public string CompetencyCode { get; set; } = string.Empty;
    public string CompetencyDescription { get; set; } = string.Empty;
    public string WorkTypes { get; set; } = string.Empty;
    public int Hours { get; set; }
}

public sealed class GeneralCompetency
{
    public string CompetencyCode { get; set; } = string.Empty;
    public string CompetencyDescription { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class DiaryEntry
{
    public int Id { get; set; }
    public DateTime WorkDate { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string DetailedReport { get; set; } = string.Empty;
    public bool IsReviewed { get; set; }
    public int? SupervisorGrade { get; set; }
    public string? SupervisorComment { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBySupervisorFullName { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<DiaryAttachment> Attachments { get; set; } = new();
}

public sealed class DiaryAttachment
{
    public int Id { get; set; }
    public string Caption { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int SortOrder { get; set; }
}

public sealed class DiaryFigureUpload
{
    public string? ClientId { get; set; }
    public string? Caption { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? Base64Content { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ReportItem
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PracticeSource
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PracticeAppendix
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class SectionComment
{
    public string SectionKey { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string SupervisorFullName { get; set; } = string.Empty;
}

public sealed class NotificationItem
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ChatThreadItem
{
    public int Id { get; set; }
    public ChatUser OtherUser { get; set; } = new();
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime? LastMessageAtUtc { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class ChatUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Subtitle { get; set; } = string.Empty;
    public override string ToString() => $"{FullName} - {Subtitle}".Trim(' ', '-');
}

public sealed class ChatThreadDetails
{
    public int Id { get; set; }
    public ChatUser OtherUser { get; set; } = new();
    public List<ChatMessage> Messages { get; set; } = new();
}

public sealed class ChatMessage
{
    public int Id { get; set; }
    public int ThreadId { get; set; }
    public int SenderUserId { get; set; }
    public string SenderFullName { get; set; } = string.Empty;
    public string? Text { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<ChatAttachment> Attachments { get; set; } = new();
}

public sealed class ChatAttachment
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public sealed class FileDownload
{
    public byte[] Content { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
}
