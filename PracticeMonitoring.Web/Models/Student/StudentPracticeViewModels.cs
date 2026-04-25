namespace PracticeMonitoring.Web.Models.Student;

public class StudentPracticeListItemViewModel
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

public class StudentPracticeDetailsViewModel : StudentPracticeListItemViewModel
{
    public DateTime AssignedAtUtc { get; set; }

    public string? OrganizationSupervisorFullName { get; set; }

    public string? OrganizationSupervisorPosition { get; set; }

    public string? OrganizationSupervisorPhone { get; set; }

    public string? OrganizationSupervisorEmail { get; set; }

    public string? PracticeTaskContent { get; set; }

    public List<StudentPracticeCompetencyViewModel> Competencies { get; set; } = new();

    public List<StudentPracticeDiaryEntryViewModel> DiaryEntries { get; set; } = new();

    public List<StudentPracticeReportItemViewModel> ReportItems { get; set; } = new();

    public List<StudentPracticeSourceViewModel> Sources { get; set; } = new();

    public List<StudentPracticeAppendixViewModel> Appendices { get; set; } = new();
}

public class StudentPracticeCompetencyViewModel
{
    public string CompetencyCode { get; set; } = string.Empty;

    public string CompetencyDescription { get; set; } = string.Empty;

    public string WorkTypes { get; set; } = string.Empty;

    public int Hours { get; set; }
}

public class StudentPracticeDiaryEntryViewModel
{
    public int Id { get; set; }

    public DateTime WorkDate { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string DetailedReport { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public List<StudentPracticeDiaryAttachmentViewModel> Attachments { get; set; } = new();
}

public class StudentPracticeDiaryAttachmentViewModel
{
    public int Id { get; set; }

    public string Caption { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int SortOrder { get; set; }
}

public class StudentPracticeReportItemViewModel
{
    public int Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }
}

public class StudentPracticeSourceViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }
}

public class StudentPracticeAppendixViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public class StudentPracticeOrganizationRequestViewModel
{
    public string? OrganizationName { get; set; }

    public string? OrganizationSupervisorFullName { get; set; }

    public string? OrganizationSupervisorPosition { get; set; }

    public string? OrganizationSupervisorPhone { get; set; }

    public string? OrganizationSupervisorEmail { get; set; }

    public string? PracticeTaskContent { get; set; }
}

public class StudentPracticeDiaryEntryRequestViewModel
{
    public DateTime WorkDate { get; set; }

    public string? ShortDescription { get; set; }

    public string? DetailedReport { get; set; }

    public List<StudentPracticeDiaryFigureRequestViewModel> Figures { get; set; } = new();
}

public class StudentPracticeDiaryFigureRequestViewModel
{
    public string? Caption { get; set; }

    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    public string? Base64Content { get; set; }

    public int SortOrder { get; set; }
}

public class StudentPracticeReportItemsRequestViewModel
{
    public List<StudentPracticeReportItemRequestViewModel> Items { get; set; } = new();
}

public class StudentPracticeReportItemRequestViewModel
{
    public string? Category { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }
}

public class StudentPracticeSourcesRequestViewModel
{
    public List<StudentPracticeSourceRequestViewModel> Sources { get; set; } = new();
}

public class StudentPracticeSourceRequestViewModel
{
    public string? Title { get; set; }

    public string? Url { get; set; }

    public string? Description { get; set; }
}

public class StudentApiResult<T>
{
    public bool Success { get; set; }

    public int StatusCode { get; set; }

    public string? ErrorMessage { get; set; }

    public T? Data { get; set; }

    public Dictionary<string, string[]> ValidationErrors { get; set; } = new();
}

public class StudentFileResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public string ContentType { get; set; } = "application/octet-stream";

    public string FileName { get; set; } = "file.bin";
}
