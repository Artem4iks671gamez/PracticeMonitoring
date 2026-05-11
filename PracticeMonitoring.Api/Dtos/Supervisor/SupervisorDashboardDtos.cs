namespace PracticeMonitoring.Api.Dtos.Supervisor;

public class SupervisorDashboardResponse
{
    public SupervisorSummaryResponse Summary { get; set; } = new();

    public List<SupervisorPracticeProgressResponse> Practices { get; set; } = new();

    public List<SupervisorStudentProgressResponse> Students { get; set; } = new();

    public List<SupervisorRiskItemResponse> Risks { get; set; } = new();

    public List<SupervisorChartPointResponse> ProgressBuckets { get; set; } = new();

    public List<SupervisorChartPointResponse> GroupProgress { get; set; } = new();

    public List<SupervisorChartPointResponse> RiskDistribution { get; set; } = new();
}

public class SupervisorSummaryResponse
{
    public int TotalStudents { get; set; }

    public int ActiveStudents { get; set; }

    public int CompletedStudents { get; set; }

    public int TotalPractices { get; set; }

    public int AverageProgress { get; set; }

    public int CriticalCount { get; set; }

    public int AttentionCount { get; set; }

    public int ReportReadyCount { get; set; }

    public int OrganizationsMissingCount { get; set; }

    public int DiaryLaggingCount { get; set; }
}

public class SupervisorStudentProgressResponse
{
    public int AssignmentId { get; set; }

    public int StudentId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string? StudentAvatarUrl { get; set; }

    public string? GroupName { get; set; }

    public int? Course { get; set; }

    public int PracticeId { get; set; }

    public string PracticeIndex { get; set; } = string.Empty;

    public string PracticeName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsCompleted { get; set; }

    public string? OrganizationName { get; set; }

    public int ProgressPercent { get; set; }

    public int DiaryEntriesCount { get; set; }

    public int ExpectedDiaryEntriesCount { get; set; }

    public int WorkDaysCount { get; set; }

    public int DetailedReportsCount { get; set; }

    public int ReviewedDiaryEntriesCount { get; set; }

    public int GradedDiaryEntriesCount { get; set; }

    public bool HasOrganization { get; set; }

    public bool HasIntroduction { get; set; }

    public bool HasTechnicalTools { get; set; }

    public bool HasSources { get; set; }

    public bool HasAppendices { get; set; }

    public bool IsReportReady { get; set; }

    public int MissingDiaryEntriesCount { get; set; }

    public int MissingReviewCount { get; set; }

    public string RiskLevel { get; set; } = "ok";

    public string RiskLabel { get; set; } = "В норме";

    public string MainIssue { get; set; } = string.Empty;

    public DateTime? LastActivityAtUtc { get; set; }
}

public class SupervisorPracticeProgressResponse
{
    public int PracticeId { get; set; }

    public string PracticeIndex { get; set; } = string.Empty;

    public string PracticeName { get; set; } = string.Empty;

    public string SpecialtyCode { get; set; } = string.Empty;

    public string SpecialtyName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int StudentsCount { get; set; }

    public int AverageProgress { get; set; }

    public int CriticalCount { get; set; }

    public int AttentionCount { get; set; }

    public int ReportReadyCount { get; set; }
}

public class SupervisorRiskItemResponse
{
    public int AssignmentId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string? GroupName { get; set; }

    public string PracticeIndex { get; set; } = string.Empty;

    public string PracticeName { get; set; } = string.Empty;

    public string RiskLevel { get; set; } = "ok";

    public string RiskLabel { get; set; } = "В норме";

    public string Message { get; set; } = string.Empty;

    public int ProgressPercent { get; set; }
}

public class SupervisorChartPointResponse
{
    public string Label { get; set; } = string.Empty;

    public int Value { get; set; }

    public int Percent { get; set; }
}

public class SupervisorAssignmentDetailsResponse : SupervisorStudentProgressResponse
{
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

    public List<SupervisorDiaryEntryResponse> DiaryEntries { get; set; } = new();

    public List<SupervisorReportSectionResponse> ReportSections { get; set; } = new();

    public List<SupervisorSourceResponse> Sources { get; set; } = new();

    public List<SupervisorAppendixResponse> Appendices { get; set; } = new();

    public List<SupervisorSectionCommentResponse> SectionComments { get; set; } = new();
}

public class SupervisorDiaryEntryResponse
{
    public int Id { get; set; }

    public DateTime WorkDate { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string DetailedReport { get; set; } = string.Empty;

    public bool HasDetailedReport { get; set; }

    public int AttachmentsCount { get; set; }

    public bool IsReviewed { get; set; }

    public int? SupervisorGrade { get; set; }

    public string? SupervisorComment { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewedBySupervisorFullName { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<SupervisorDiaryAttachmentResponse> Attachments { get; set; } = new();
}

public class SupervisorDiaryAttachmentResponse
{
    public int Id { get; set; }

    public string Caption { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int SortOrder { get; set; }
}

public class SupervisorReportSectionResponse
{
    public string SectionKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsReady { get; set; }

    public string Description { get; set; } = string.Empty;
}

public class SupervisorSourceResponse
{
    public string Title { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? Description { get; set; }
}

public class SupervisorAppendixResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public class SupervisorSectionCommentResponse
{
    public string SectionKey { get; set; } = string.Empty;

    public string SectionTitle { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public string SupervisorFullName { get; set; } = string.Empty;
}

public class SupervisorDiaryReviewRequest
{
    public int? Grade { get; set; }

    public string? Comment { get; set; }
}

public class SupervisorSectionCommentRequest
{
    public string? Comment { get; set; }
}
