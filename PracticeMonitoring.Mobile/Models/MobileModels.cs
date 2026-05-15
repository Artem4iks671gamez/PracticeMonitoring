using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Mobile.Models;

public sealed class ApiResult<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public T? Data { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, string[]> ValidationErrors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
}

public sealed class CurrentUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? SpecialtyCode { get; set; }
    public string? SpecialtyName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Theme { get; set; } = "light";
    public bool MustChangePassword { get; set; }
}

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Введите email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Введите фамилию")]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите имя")]
    public string Name { get; set; } = string.Empty;

    public string? Patronymic { get; set; }

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Введите корректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,32}$",
        ErrorMessage = "Пароль должен содержать 8-32 символа, минимум одну заглавную букву, одну строчную букву и одну цифру. Разрешены только латинские буквы и цифры."
    )]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? Role { get; set; } = "Student";

    [Required(ErrorMessage = "Выберите специальность")]
    public int? SpecialtyId { get; set; }

    [Required(ErrorMessage = "Выберите группу")]
    public int? GroupId { get; set; }

    [RegularExpression(@"^\d{6}$", ErrorMessage = "Код должен состоять из 6 цифр")]
    public string Code { get; set; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Введите корректный email")]
    public string Email { get; set; } = string.Empty;
}

public sealed class PasswordResetRequest
{
    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Введите корректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите код")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Код должен состоять из 6 цифр")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите новый пароль")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,32}$",
        ErrorMessage = "Пароль должен содержать 8-32 символа, минимум одну заглавную букву, одну строчную букву и одну цифру. Разрешены только латинские буквы и цифры."
    )]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "Введите новый пароль")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[A-Za-z\d]{8,32}$",
        ErrorMessage = "Пароль должен содержать 8-32 символа, минимум одну заглавную букву, одну строчную букву и одну цифру. Разрешены только латинские буквы и цифры."
    )]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class UpdateProfileRequest
{
    public string Surname { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Theme { get; set; } = "light";
}

public sealed class SpecialtyItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class GroupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Course { get; set; }
    public int SpecialtyId { get; set; }
}

public class StudentPracticeListItem
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

public sealed class StudentPracticeDetails : StudentPracticeListItem
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
    public List<StudentPracticeGeneralCompetency> GeneralCompetencies { get; set; } = new();
    public List<StudentPracticeCompetency> Competencies { get; set; } = new();
    public List<StudentPracticeDiaryEntry> DiaryEntries { get; set; } = new();
    public List<StudentPracticeReportItem> ReportItems { get; set; } = new();
    public List<StudentPracticeSource> Sources { get; set; } = new();
    public List<StudentPracticeAppendix> Appendices { get; set; } = new();
    public List<StudentPracticeSectionComment> SectionComments { get; set; } = new();
}

public sealed class StudentPracticeCompetency
{
    public string CompetencyCode { get; set; } = string.Empty;
    public string CompetencyDescription { get; set; } = string.Empty;
    public string WorkTypes { get; set; } = string.Empty;
    public int Hours { get; set; }
}

public sealed class StudentPracticeGeneralCompetency
{
    public string CompetencyCode { get; set; } = string.Empty;
    public string CompetencyDescription { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class StudentPracticeDiaryEntry
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
    public List<StudentPracticeDiaryAttachment> Attachments { get; set; } = new();
}

public sealed class StudentPracticeDiaryAttachment
{
    public int Id { get; set; }
    public string Caption { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int SortOrder { get; set; }
}

public sealed class StudentPracticeReportItem
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public sealed class StudentPracticeSource
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public sealed class StudentPracticeAppendix
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class StudentPracticeSectionComment
{
    public string SectionKey { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string SupervisorFullName { get; set; } = string.Empty;
}

public sealed class StudentPracticeOrganizationRequest
{
    public string? OrganizationName { get; set; }
    public string? OrganizationFullName { get; set; }
    public string? OrganizationShortName { get; set; }
    public string? OrganizationAddress { get; set; }
    public string? OrganizationSupervisorFullName { get; set; }
    public string? OrganizationSupervisorPosition { get; set; }
    public string? OrganizationSupervisorPhone { get; set; }
    public string? OrganizationSupervisorEmail { get; set; }
    public string? PracticeTaskContent { get; set; }
    public string? StudentDuties { get; set; }
    public string? ProvidedMaterialsDescription { get; set; }
    public string? WorkScheduleDescription { get; set; }
    public string? IntroductionMainGoal { get; set; }
}

public sealed class StudentPracticeDiaryEntryRequest
{
    public DateTime WorkDate { get; set; }
    public string? ShortDescription { get; set; }
    public string? DetailedReport { get; set; }
    public List<StudentPracticeDiaryFigureRequest> Figures { get; set; } = new();
    public List<int> KeptAttachmentIds { get; set; } = new();
}

public sealed class StudentPracticeDiaryFigureRequest
{
    public string? ClientId { get; set; }
    public string? Caption { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? Base64Content { get; set; }
    public int SortOrder { get; set; }
}

public sealed class StudentPracticeReportItemsRequest
{
    public List<StudentPracticeReportItemRequest> Items { get; set; } = new();
}

public sealed class StudentPracticeReportItemRequest
{
    public string? Category { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public sealed class StudentPracticeSourcesRequest
{
    public List<StudentPracticeSourceRequest> Sources { get; set; } = new();
}

public sealed class StudentPracticeSourceRequest
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
}

public sealed class StudentPracticeAppendixUploadResponse
{
    public StudentPracticeDetails Details { get; set; } = new();
    public StudentPracticeAppendix Appendix { get; set; } = new();
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

public sealed class ChatThreadDetails
{
    public int Id { get; set; }
    public ChatUser OtherUser { get; set; } = new();
    public List<ChatMessage> Messages { get; set; } = new();
}

public sealed class ChatUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Subtitle { get; set; } = string.Empty;
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
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = "file.bin";
    public string ContentType { get; set; } = "application/octet-stream";
}
