namespace PracticeMonitoring.Api.Dtos.Student;

public class StudentPracticeListItemResponse
{
    public int AssignmentId { get; set; }

    public int PracticeId { get; set; }

    public string PracticeIndex { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string SpecialtyCode { get; set; } = string.Empty;

    public string SpecialtyName { get; set; } = string.Empty;

    public string ProfessionalModuleCode { get; set; } = string.Empty;

    public string ProfessionalModuleName { get; set; } = string.Empty;

    public int Hours { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsCompleted { get; set; }

    public string? SupervisorFullName { get; set; }

    public string? OrganizationName { get; set; }

    public bool HasRequiredDetails { get; set; }

    public DateTime DetailsDueDate { get; set; }

    public bool IsDetailsOverdue { get; set; }

    public int DiaryEntriesCount { get; set; }

    public int WorkDaysCount { get; set; }
}

public class StudentPracticeDetailsResponse : StudentPracticeListItemResponse
{
    public DateTime AssignedAtUtc { get; set; }

    public string? OrganizationSupervisorFullName { get; set; }

    public string? OrganizationSupervisorPosition { get; set; }

    public string? OrganizationSupervisorPhone { get; set; }

    public string? OrganizationSupervisorEmail { get; set; }

    public string? PracticeTaskContent { get; set; }

    public List<StudentPracticeCompetencyResponse> Competencies { get; set; } = new();

    public List<StudentPracticeDiaryEntryResponse> DiaryEntries { get; set; } = new();

    public List<StudentPracticeReportItemResponse> ReportItems { get; set; } = new();

    public List<StudentPracticeSourceResponse> Sources { get; set; } = new();

    public List<StudentPracticeAppendixResponse> Appendices { get; set; } = new();
}

public class StudentPracticeCompetencyResponse
{
    public string CompetencyCode { get; set; } = string.Empty;

    public string CompetencyDescription { get; set; } = string.Empty;

    public string WorkTypes { get; set; } = string.Empty;

    public int Hours { get; set; }
}

public class StudentPracticeDiaryEntryResponse
{
    public int Id { get; set; }

    public DateTime WorkDate { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string DetailedReport { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public List<StudentPracticeDiaryAttachmentResponse> Attachments { get; set; } = new();
}

public class StudentPracticeDiaryAttachmentResponse
{
    public int Id { get; set; }

    public string Caption { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int SortOrder { get; set; }
}

public class StudentPracticeReportItemResponse
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }
}

public class StudentPracticeSourceResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }
}

public class StudentPracticeAppendixResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public class StudentPracticeOrganizationRequest
{
    public string? OrganizationName { get; set; }

    public string? OrganizationSupervisorFullName { get; set; }

    public string? OrganizationSupervisorPosition { get; set; }

    public string? OrganizationSupervisorPhone { get; set; }

    public string? OrganizationSupervisorEmail { get; set; }

    public string? PracticeTaskContent { get; set; }
}

public class StudentPracticeDiaryEntryRequest
{
    public DateTime WorkDate { get; set; }

    public string? ShortDescription { get; set; }

    public string? DetailedReport { get; set; }

    public List<StudentPracticeDiaryFigureRequest> Figures { get; set; } = new();

    public List<int> KeptAttachmentIds { get; set; } = new();
}

public class StudentPracticeDiaryFigureRequest
{
    public string? ClientId { get; set; }

    public string? Caption { get; set; }

    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    public string? Base64Content { get; set; }

    public int SortOrder { get; set; }
}

public class StudentPracticeReportItemsRequest
{
    public List<StudentPracticeReportItemRequest> Items { get; set; } = new();
}

public class StudentPracticeReportItemRequest
{
    public string? Category { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}

public class StudentPracticeSourcesRequest
{
    public List<StudentPracticeSourceRequest> Sources { get; set; } = new();
}

public class StudentPracticeSourceRequest
{
    public string? Title { get; set; }

    public string? Url { get; set; }

    public string? Description { get; set; }
}
