using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos.Supervisor;
using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/supervisor")]
[Authorize(Roles = "Supervisor")]
public class SupervisorController : ControllerBase
{
    private static readonly HashSet<string> AllowedSectionCommentKeys = new(StringComparer.Ordinal)
    {
        "organization",
        "introduction",
        "sources"
    };

    private readonly AppDbContext _context;
    private readonly NotificationService _notificationService;

    public SupervisorController(AppDbContext context, NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<SupervisorDashboardResponse>> GetDashboard()
    {
        var supervisorId = GetCurrentUserId();
        if (supervisorId is null)
            return Unauthorized();

        var assignments = await LoadAssignments(supervisorId.Value).ToListAsync();
        var students = assignments.Select(MapStudentProgress).ToList();

        var totalStudents = students.Count;
        var practices = students
            .GroupBy(x => x.PracticeId)
            .Select(group =>
            {
                var first = group.First();
                return new SupervisorPracticeProgressResponse
                {
                    PracticeId = first.PracticeId,
                    PracticeIndex = first.PracticeIndex,
                    PracticeName = first.PracticeName,
                    SpecialtyCode = assignments.First(x => x.ProductionPracticeId == first.PracticeId).ProductionPractice.Specialty.Code,
                    SpecialtyName = assignments.First(x => x.ProductionPracticeId == first.PracticeId).ProductionPractice.Specialty.Name,
                    StartDate = first.StartDate,
                    EndDate = first.EndDate,
                    StudentsCount = group.Count(),
                    AverageProgress = RoundPercent(group.Average(x => x.ProgressPercent)),
                    CriticalCount = group.Count(x => x.RiskLevel == "critical"),
                    AttentionCount = group.Count(x => x.RiskLevel == "attention"),
                    ReportReadyCount = group.Count(x => x.IsReportReady)
                };
            })
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.PracticeIndex)
            .ToList();

        var risks = students
            .Where(x => x.RiskLevel is "critical" or "attention")
            .OrderBy(x => x.RiskLevel == "critical" ? 0 : 1)
            .ThenBy(x => x.ProgressPercent)
            .Select(x => new SupervisorRiskItemResponse
            {
                AssignmentId = x.AssignmentId,
                StudentFullName = x.StudentFullName,
                GroupName = x.GroupName,
                PracticeIndex = x.PracticeIndex,
                PracticeName = x.PracticeName,
                RiskLevel = x.RiskLevel,
                RiskLabel = x.RiskLabel,
                Message = x.MainIssue,
                ProgressPercent = x.ProgressPercent
            })
            .ToList();

        var response = new SupervisorDashboardResponse
        {
            Students = students
                .OrderByDescending(x => x.RiskLevel == "critical")
                .ThenByDescending(x => x.RiskLevel == "attention")
                .ThenBy(x => x.GroupName)
                .ThenBy(x => x.StudentFullName)
                .ToList(),
            Practices = practices,
            Risks = risks,
            Summary = new SupervisorSummaryResponse
            {
                TotalStudents = totalStudents,
                ActiveStudents = students.Count(x => !x.IsCompleted),
                CompletedStudents = students.Count(x => x.IsCompleted),
                TotalPractices = practices.Count,
                AverageProgress = totalStudents == 0 ? 0 : RoundPercent(students.Average(x => x.ProgressPercent)),
                CriticalCount = students.Count(x => x.RiskLevel == "critical"),
                AttentionCount = students.Count(x => x.RiskLevel == "attention"),
                ReportReadyCount = students.Count(x => x.IsReportReady),
                OrganizationsMissingCount = students.Count(x => !x.HasOrganization),
                DiaryLaggingCount = students.Count(x => x.MissingDiaryEntriesCount > 0)
            },
            ProgressBuckets = BuildProgressBuckets(students),
            GroupProgress = BuildGroupProgress(students),
            RiskDistribution = BuildRiskDistribution(students)
        };

        return Ok(response);
    }

    [HttpGet("assignments/{assignmentId:int}")]
    public async Task<ActionResult<SupervisorAssignmentDetailsResponse>> GetAssignment(int assignmentId)
    {
        var supervisorId = GetCurrentUserId();
        if (supervisorId is null)
            return Unauthorized();

        var assignment = await LoadAssignments(supervisorId.Value)
            .FirstOrDefaultAsync(x => x.Id == assignmentId);

        if (assignment is null)
            return NotFound();

        var progress = MapStudentProgress(assignment);
        return Ok(MapAssignmentDetails(assignment, progress));
    }

    [HttpPost("diary-entries/{entryId:int}/review")]
    public async Task<ActionResult<SupervisorAssignmentDetailsResponse>> ReviewDiaryEntry(
        int entryId,
        SupervisorDiaryReviewRequest request)
    {
        var supervisorId = GetCurrentUserId();
        if (supervisorId is null)
            return Unauthorized();

        var errors = ValidateDiaryReview(request);
        if (errors.Count > 0)
            return BadRequest(new { message = "Проверьте оценку и комментарий.", errors });

        var entry = await _context.StudentPracticeDiaryEntries
            .Include(x => x.Assignment)
                .ThenInclude(x => x.Student)
            .Include(x => x.Assignment)
                .ThenInclude(x => x.ProductionPractice)
            .FirstOrDefaultAsync(x => x.Id == entryId && x.Assignment.SupervisorId == supervisorId.Value);

        if (entry is null)
            return NotFound();

        entry.IsReviewed = true;
        entry.SupervisorGrade = request.Grade!.Value;
        entry.SupervisorComment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        entry.ReviewedAtUtc = DateTime.UtcNow;
        entry.ReviewedBySupervisorId = supervisorId.Value;

        var reviewMessage = $"Проверен подробный отчёт за {entry.WorkDate:dd.MM.yyyy} по практике {entry.Assignment.ProductionPractice.PracticeIndex} \"{entry.Assignment.ProductionPractice.Name}\". Оценка: {entry.SupervisorGrade}.";
        if (!string.IsNullOrWhiteSpace(entry.SupervisorComment))
            reviewMessage += $" Комментарий: {TrimForNotification(entry.SupervisorComment)}";

        _notificationService.Add(
            entry.Assignment.StudentId,
            "DiaryReview",
            string.IsNullOrWhiteSpace(entry.SupervisorComment)
                ? "Руководитель поставил оценку за день"
                : "Руководитель поставил оценку и оставил комментарий",
            reviewMessage,
            $"/Student/Index?practiceId={entry.Assignment.Id}");

        await _context.SaveChangesAsync();

        var assignment = await LoadAssignments(supervisorId.Value)
            .FirstOrDefaultAsync(x => x.Id == entry.ProductionPracticeStudentAssignmentId);

        if (assignment is null)
            return NotFound();

        var progress = MapStudentProgress(assignment);
        return Ok(MapAssignmentDetails(assignment, progress));
    }

    [HttpPost("assignments/{assignmentId:int}/section-comments/{sectionKey}")]
    public async Task<ActionResult<SupervisorAssignmentDetailsResponse>> SaveSectionComment(
        int assignmentId,
        string sectionKey,
        SupervisorSectionCommentRequest request)
    {
        var supervisorId = GetCurrentUserId();
        if (supervisorId is null)
            return Unauthorized();

        var normalizedSectionKey = sectionKey.Trim();
        var errors = ValidateSectionComment(normalizedSectionKey, request);
        if (errors.Count > 0)
            return BadRequest(new { message = "Проверьте комментарий к разделу.", errors });

        var assignment = await _context.ProductionPracticeStudentAssignments
            .Include(x => x.Student)
            .Include(x => x.ProductionPractice)
            .Include(x => x.SectionComments)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.SupervisorId == supervisorId.Value);

        if (assignment is null)
            return NotFound();

        var comment = assignment.SectionComments.FirstOrDefault(x => x.SectionKey == normalizedSectionKey);
        var now = DateTime.UtcNow;
        if (comment is null)
        {
            comment = new StudentPracticeSectionComment
            {
                ProductionPracticeStudentAssignmentId = assignment.Id,
                SectionKey = normalizedSectionKey,
                CreatedAtUtc = now
            };
            assignment.SectionComments.Add(comment);
        }

        comment.Comment = request.Comment!.Trim();
        comment.SupervisorId = supervisorId.Value;
        comment.UpdatedAtUtc = now;

        var sectionTitle = GetSectionTitle(normalizedSectionKey);
        _notificationService.Add(
            assignment.StudentId,
            "SectionComment",
            $"Руководитель прокомментировал раздел: {sectionTitle}",
            $"По практике {assignment.ProductionPractice.PracticeIndex} \"{assignment.ProductionPractice.Name}\" оставлен комментарий к разделу \"{sectionTitle}\": {TrimForNotification(comment.Comment)}",
            $"/Student/Index?practiceId={assignment.Id}");

        await _context.SaveChangesAsync();

        var reloaded = await LoadAssignments(supervisorId.Value)
            .FirstOrDefaultAsync(x => x.Id == assignmentId);

        if (reloaded is null)
            return NotFound();

        var progress = MapStudentProgress(reloaded);
        return Ok(MapAssignmentDetails(reloaded, progress));
    }

    [HttpGet("appendices/{appendixId:int}/download")]
    public async Task<IActionResult> DownloadAppendix(int appendixId)
    {
        var supervisorId = GetCurrentUserId();
        if (supervisorId is null)
            return Unauthorized();

        var appendix = await _context.StudentPracticeAppendices
            .AsNoTracking()
            .Include(x => x.Assignment)
            .FirstOrDefaultAsync(x => x.Id == appendixId && x.Assignment.SupervisorId == supervisorId.Value);

        if (appendix is null)
            return NotFound();

        return File(appendix.Content, appendix.ContentType, appendix.FileName);
    }

    [HttpGet("diary-attachments/{attachmentId:int}/download")]
    public async Task<IActionResult> DownloadDiaryAttachment(int attachmentId)
    {
        var supervisorId = GetCurrentUserId();
        if (supervisorId is null)
            return Unauthorized();

        var attachment = await _context.StudentPracticeDiaryAttachments
            .AsNoTracking()
            .Include(x => x.DiaryEntry)
                .ThenInclude(x => x.Assignment)
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.DiaryEntry.Assignment.SupervisorId == supervisorId.Value);

        if (attachment is null)
            return NotFound();

        return File(attachment.Content, attachment.ContentType, attachment.FileName);
    }

    private IQueryable<ProductionPracticeStudentAssignment> LoadAssignments(int supervisorId)
    {
        return _context.ProductionPracticeStudentAssignments
            .AsNoTracking()
            .Include(x => x.Student)
                .ThenInclude(x => x.Group)
            .Include(x => x.ProductionPractice)
                .ThenInclude(x => x.Specialty)
            .Include(x => x.DiaryEntries)
                .ThenInclude(x => x.Attachments)
            .Include(x => x.DiaryEntries)
                .ThenInclude(x => x.ReviewedBySupervisor)
            .Include(x => x.ReportItems)
            .Include(x => x.Sources)
            .Include(x => x.Appendices)
            .Include(x => x.SectionComments)
                .ThenInclude(x => x.Supervisor)
            .Where(x => x.SupervisorId == supervisorId);
    }

    private static SupervisorAssignmentDetailsResponse MapAssignmentDetails(
        ProductionPracticeStudentAssignment assignment,
        SupervisorStudentProgressResponse progress)
    {
        return new SupervisorAssignmentDetailsResponse
        {
            AssignmentId = progress.AssignmentId,
            StudentId = progress.StudentId,
            StudentFullName = progress.StudentFullName,
            StudentAvatarUrl = progress.StudentAvatarUrl,
            GroupName = progress.GroupName,
            Course = progress.Course,
            PracticeId = progress.PracticeId,
            PracticeIndex = progress.PracticeIndex,
            PracticeName = progress.PracticeName,
            StartDate = progress.StartDate,
            EndDate = progress.EndDate,
            IsCompleted = progress.IsCompleted,
            OrganizationName = progress.OrganizationName,
            ProgressPercent = progress.ProgressPercent,
            DiaryEntriesCount = progress.DiaryEntriesCount,
            ExpectedDiaryEntriesCount = progress.ExpectedDiaryEntriesCount,
            WorkDaysCount = progress.WorkDaysCount,
            DetailedReportsCount = progress.DetailedReportsCount,
            ReviewedDiaryEntriesCount = progress.ReviewedDiaryEntriesCount,
            GradedDiaryEntriesCount = progress.GradedDiaryEntriesCount,
            HasOrganization = progress.HasOrganization,
            HasIntroduction = progress.HasIntroduction,
            HasTechnicalTools = progress.HasTechnicalTools,
            HasSources = progress.HasSources,
            HasAppendices = progress.HasAppendices,
            IsReportReady = progress.IsReportReady,
            MissingDiaryEntriesCount = progress.MissingDiaryEntriesCount,
            MissingReviewCount = progress.MissingReviewCount,
            RiskLevel = progress.RiskLevel,
            RiskLabel = progress.RiskLabel,
            MainIssue = progress.MainIssue,
            LastActivityAtUtc = progress.LastActivityAtUtc,
            OrganizationFullName = assignment.OrganizationFullName,
            OrganizationShortName = assignment.OrganizationShortName,
            OrganizationAddress = assignment.OrganizationAddress,
            OrganizationSupervisorFullName = assignment.OrganizationSupervisorFullName,
            OrganizationSupervisorPosition = assignment.OrganizationSupervisorPosition,
            OrganizationSupervisorPhone = assignment.OrganizationSupervisorPhone,
            OrganizationSupervisorEmail = assignment.OrganizationSupervisorEmail,
            PracticeTaskContent = assignment.PracticeTaskContent,
            StudentDuties = assignment.StudentDuties,
            ProvidedMaterialsDescription = assignment.ProvidedMaterialsDescription,
            WorkScheduleDescription = assignment.WorkScheduleDescription,
            IntroductionMainGoal = assignment.IntroductionMainGoal,
            DiaryEntries = assignment.DiaryEntries
                .OrderBy(x => x.WorkDate)
                .Select(x => new SupervisorDiaryEntryResponse
                {
                    Id = x.Id,
                    WorkDate = x.WorkDate,
                    ShortDescription = x.ShortDescription,
                    DetailedReport = x.DetailedReport,
                    HasDetailedReport = !string.IsNullOrWhiteSpace(x.DetailedReport),
                    AttachmentsCount = x.Attachments.Count,
                    IsReviewed = x.IsReviewed,
                    SupervisorGrade = x.SupervisorGrade,
                    SupervisorComment = x.SupervisorComment,
                    ReviewedAtUtc = x.ReviewedAtUtc,
                    ReviewedBySupervisorFullName = x.ReviewedBySupervisor?.FullName,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    Attachments = x.Attachments
                        .OrderBy(a => a.SortOrder)
                        .Select(a => new SupervisorDiaryAttachmentResponse
                        {
                            Id = a.Id,
                            Caption = a.Caption,
                            FileName = a.FileName,
                            ContentType = a.ContentType,
                            SizeBytes = a.SizeBytes,
                            SortOrder = a.SortOrder
                        })
                        .ToList()
                })
                .ToList(),
            ReportSections = BuildReportSections(assignment),
            Sources = assignment.Sources
                .OrderBy(x => x.SortOrder)
                .Select(x => new SupervisorSourceResponse
                {
                    Title = x.Title,
                    Url = x.Url,
                    Description = x.Description
                })
                .ToList(),
            Appendices = assignment.Appendices
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new SupervisorAppendixResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    FileName = x.FileName,
                    ContentType = x.ContentType,
                    SizeBytes = x.SizeBytes,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToList(),
            SectionComments = assignment.SectionComments
                .OrderBy(x => x.SectionKey)
                .Select(x => new SupervisorSectionCommentResponse
                {
                    SectionKey = x.SectionKey,
                    SectionTitle = GetSectionTitle(x.SectionKey),
                    Comment = x.Comment,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    SupervisorFullName = x.Supervisor.FullName
                })
                .ToList()
        };
    }

    private static SupervisorStudentProgressResponse MapStudentProgress(ProductionPracticeStudentAssignment assignment)
    {
        var practice = assignment.ProductionPractice;
        var today = DateTime.UtcNow.Date;
        var workDaysCount = CountWorkDays(practice.StartDate, practice.EndDate);
        var expectedDiaryEntries = CountExpectedWorkDays(practice.StartDate, practice.EndDate, today);
        var filledDiaryEntries = assignment.DiaryEntries.Count(x => !string.IsNullOrWhiteSpace(x.ShortDescription));
        var detailedReports = assignment.DiaryEntries.Count(x => !string.IsNullOrWhiteSpace(x.DetailedReport));
        var reviewedEntries = assignment.DiaryEntries.Count(x => x.IsReviewed);
        var gradedEntries = assignment.DiaryEntries.Count(x => x.IsReviewed && x.SupervisorGrade.HasValue);
        var hasOrganization = HasRequiredDetails(assignment);
        var hasIntroduction = HasIntroduction(assignment);
        var hasTechnicalTools = assignment.ReportItems.Any(x => x.Category == "TechnicalTool" && !string.IsNullOrWhiteSpace(x.Name));
        var hasSources = assignment.Sources.Count > 0;
        var hasAppendices = assignment.Appendices.Count > 0;
        var missingDiary = Math.Max(0, expectedDiaryEntries - filledDiaryEntries);
        var missingReview = Math.Max(0, workDaysCount - gradedEntries);
        var isReportReady = hasOrganization &&
                            hasIntroduction &&
                            hasTechnicalTools &&
                            filledDiaryEntries >= workDaysCount &&
                            detailedReports >= workDaysCount &&
                            gradedEntries >= workDaysCount;
        var progress = CalculateProgress(
            hasOrganization,
            hasIntroduction,
            hasTechnicalTools,
            hasSources,
            hasAppendices,
            filledDiaryEntries,
            expectedDiaryEntries,
            detailedReports,
            reviewedEntries,
            isReportReady);
        var risk = CalculateRisk(assignment, progress, hasOrganization, hasIntroduction, hasTechnicalTools, missingDiary, missingReview, isReportReady);

        return new SupervisorStudentProgressResponse
        {
            AssignmentId = assignment.Id,
            StudentId = assignment.StudentId,
            StudentFullName = assignment.Student.FullName,
            StudentAvatarUrl = assignment.Student.AvatarUrl,
            GroupName = assignment.Student.Group?.Name,
            Course = assignment.Student.Group?.Course,
            PracticeId = practice.Id,
            PracticeIndex = practice.PracticeIndex,
            PracticeName = practice.Name,
            StartDate = practice.StartDate,
            EndDate = practice.EndDate,
            IsCompleted = practice.EndDate.Date < today,
            OrganizationName = assignment.OrganizationFullName ?? assignment.OrganizationName,
            ProgressPercent = progress,
            DiaryEntriesCount = filledDiaryEntries,
            ExpectedDiaryEntriesCount = expectedDiaryEntries,
            WorkDaysCount = workDaysCount,
            DetailedReportsCount = detailedReports,
            ReviewedDiaryEntriesCount = reviewedEntries,
            GradedDiaryEntriesCount = gradedEntries,
            HasOrganization = hasOrganization,
            HasIntroduction = hasIntroduction,
            HasTechnicalTools = hasTechnicalTools,
            HasSources = hasSources,
            HasAppendices = hasAppendices,
            IsReportReady = isReportReady,
            MissingDiaryEntriesCount = missingDiary,
            MissingReviewCount = missingReview,
            RiskLevel = risk.Level,
            RiskLabel = risk.Label,
            MainIssue = risk.Message,
            LastActivityAtUtc = assignment.DiaryEntries
                .Select(x => (DateTime?)x.UpdatedAtUtc)
                .Concat(new[] { assignment.StudentDetailsUpdatedAtUtc })
                .Max()
        };
    }

    private static List<SupervisorReportSectionResponse> BuildReportSections(ProductionPracticeStudentAssignment assignment)
    {
        var filledDiaryEntries = assignment.DiaryEntries.Count(x => !string.IsNullOrWhiteSpace(x.ShortDescription));
        var detailedReports = assignment.DiaryEntries.Count(x => !string.IsNullOrWhiteSpace(x.DetailedReport));
        var gradedEntries = assignment.DiaryEntries.Count(x => x.IsReviewed && x.SupervisorGrade.HasValue);
        var workDaysCount = CountWorkDays(assignment.ProductionPractice.StartDate, assignment.ProductionPractice.EndDate);
        var hasIntroWork = assignment.ReportItems.Any(x => x.Category == "IntroductionWorkType" && !string.IsNullOrWhiteSpace(x.Name));
        var hasIntroSoftware = assignment.ReportItems.Any(x => x.Category == "IntroductionSoftwareTechnology" && !string.IsNullOrWhiteSpace(x.Name));

        return new List<SupervisorReportSectionResponse>
        {
            new() { Name = "Организация", IsReady = HasRequiredDetails(assignment), Description = "Организация, адрес и руководитель от организации" },
            new() { Name = "Введение", IsReady = HasIntroduction(assignment), Description = "Цель, обязанности, материалы, график, виды работ и технологии" },
            new() { Name = "Виды работ", IsReady = hasIntroWork, Description = "Таблица видов работ во введении" },
            new() { Name = "Программные средства", IsReady = hasIntroSoftware, Description = "Программные средства и технологии во введении" },
            new() { Name = "Технические средства", IsReady = assignment.ReportItems.Any(x => x.Category == "TechnicalTool" && !string.IsNullOrWhiteSpace(x.Name)), Description = "Компьютер и характеристики" },
            new() { Name = "Дневник", IsReady = filledDiaryEntries >= workDaysCount && detailedReports >= workDaysCount, Description = $"Заполнено дней: {filledDiaryEntries}/{workDaysCount}, подробных отчётов: {detailedReports}/{workDaysCount}" },
            new() { Name = "Проверка руководителем", IsReady = gradedEntries >= workDaysCount, Description = $"Проверено и оценено дней: {gradedEntries}/{workDaysCount}" },
            new() { Name = "Источники", IsReady = assignment.Sources.Count > 0, Description = $"Источников: {assignment.Sources.Count}" },
            new() { Name = "Приложения", IsReady = assignment.Appendices.Count > 0, Description = $"Файлов: {assignment.Appendices.Count}" }
        };
    }

    private static Dictionary<string, string[]> ValidateDiaryReview(SupervisorDiaryReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!request.Grade.HasValue)
            errors[nameof(request.Grade)] = new[] { "Выберите оценку." };
        else if (request.Grade.Value is < 2 or > 5)
            errors[nameof(request.Grade)] = new[] { "Оценка должна быть от 2 до 5." };

        if (!string.IsNullOrWhiteSpace(request.Comment) && request.Comment.Trim().Length > 2000)
            errors[nameof(request.Comment)] = new[] { "Комментарий не должен превышать 2000 символов." };

        return errors;
    }

    private static Dictionary<string, string[]> ValidateSectionComment(string sectionKey, SupervisorSectionCommentRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!AllowedSectionCommentKeys.Contains(sectionKey))
            errors[nameof(sectionKey)] = new[] { "Выберите корректный раздел." };

        if (string.IsNullOrWhiteSpace(request.Comment))
            errors[nameof(request.Comment)] = new[] { "Введите комментарий." };
        else if (request.Comment.Trim().Length > 2000)
            errors[nameof(request.Comment)] = new[] { "Комментарий не должен превышать 2000 символов." };

        return errors;
    }

    private static string GetSectionTitle(string sectionKey)
    {
        return sectionKey switch
        {
            "organization" => "Данные об организации",
            "introduction" => "Введение",
            "sources" => "Источники",
            _ => sectionKey
        };
    }

    private static string TrimForNotification(string value)
    {
        var normalized = value.Trim();
        return normalized.Length <= 220 ? normalized : $"{normalized[..217]}...";
    }

    private static bool HasIntroduction(ProductionPracticeStudentAssignment assignment)
    {
        return !string.IsNullOrWhiteSpace(assignment.IntroductionMainGoal) &&
               !string.IsNullOrWhiteSpace(assignment.StudentDuties) &&
               !string.IsNullOrWhiteSpace(assignment.ProvidedMaterialsDescription) &&
               !string.IsNullOrWhiteSpace(assignment.WorkScheduleDescription) &&
               assignment.ReportItems.Any(x => x.Category == "IntroductionWorkType" && !string.IsNullOrWhiteSpace(x.Name)) &&
               assignment.ReportItems.Any(x => x.Category == "IntroductionSoftwareTechnology" && !string.IsNullOrWhiteSpace(x.Name));
    }

    private static bool HasRequiredDetails(ProductionPracticeStudentAssignment assignment)
    {
        return !string.IsNullOrWhiteSpace(assignment.OrganizationFullName ?? assignment.OrganizationName) &&
               !string.IsNullOrWhiteSpace(assignment.OrganizationShortName) &&
               !string.IsNullOrWhiteSpace(assignment.OrganizationAddress) &&
               !string.IsNullOrWhiteSpace(assignment.OrganizationSupervisorFullName) &&
               !string.IsNullOrWhiteSpace(assignment.OrganizationSupervisorPosition) &&
               (!string.IsNullOrWhiteSpace(assignment.OrganizationSupervisorPhone) ||
                !string.IsNullOrWhiteSpace(assignment.OrganizationSupervisorEmail)) &&
               !string.IsNullOrWhiteSpace(assignment.PracticeTaskContent);
    }

    private static int CalculateProgress(
        bool hasOrganization,
        bool hasIntroduction,
        bool hasTechnicalTools,
        bool hasSources,
        bool hasAppendices,
        int diaryEntriesCount,
        int expectedDiaryEntriesCount,
        int detailedReportsCount,
        int reviewedEntriesCount,
        bool isReportReady)
    {
        var progress = 0;
        if (hasOrganization)
            progress += 15;
        if (hasIntroduction)
            progress += 15;
        if (hasTechnicalTools)
            progress += 10;
        if (hasSources)
            progress += 5;
        if (hasAppendices)
            progress += 5;

        var diaryTarget = Math.Max(1, expectedDiaryEntriesCount);
        progress += Math.Min(25, RoundPercent(25d * Math.Min(diaryEntriesCount, diaryTarget) / diaryTarget));

        var detailsTarget = Math.Max(1, diaryEntriesCount);
        progress += Math.Min(10, RoundPercent(10d * Math.Min(detailedReportsCount, detailsTarget) / detailsTarget));

        var reviewTarget = Math.Max(1, Math.Min(diaryEntriesCount, Math.Max(1, expectedDiaryEntriesCount)));
        progress += Math.Min(5, RoundPercent(5d * Math.Min(reviewedEntriesCount, reviewTarget) / reviewTarget));

        if (isReportReady)
            progress += 10;

        return Math.Clamp(progress, 0, 100);
    }

    private static RiskState CalculateRisk(
        ProductionPracticeStudentAssignment assignment,
        int progress,
        bool hasOrganization,
        bool hasIntroduction,
        bool hasTechnicalTools,
        int missingDiaryEntries,
        int missingReviewEntries,
        bool isReportReady)
    {
        var today = DateTime.UtcNow.Date;
        var practice = assignment.ProductionPractice;
        var detailsDueDate = practice.StartDate.Date.AddDays(2);

        if (practice.EndDate.Date < today && isReportReady)
            return new RiskState("done", "Завершено", "Практика завершена, отчёт выглядит готовым.");

        if (!hasOrganization && today > detailsDueDate)
            return new RiskState("critical", "Критично", "Не заполнены сведения об организации после контрольного срока.");

        if (missingDiaryEntries >= 3)
            return new RiskState("critical", "Критично", $"Дневник отстаёт на {missingDiaryEntries} рабочих дня.");

        if (practice.EndDate.Date < today && !isReportReady)
            return new RiskState("critical", "Критично", "Практика завершена, но отчёт не готов.");

        if (missingDiaryEntries > 0)
            return new RiskState("attention", "Требует внимания", $"Нужно дозаполнить дневник: {missingDiaryEntries} дн.");

        if (missingReviewEntries > 0 && practice.EndDate.Date <= today)
            return new RiskState("attention", "Требует проверки", $"Нужно проверить и оценить дней: {missingReviewEntries}.");

        if (!hasIntroduction)
            return new RiskState("attention", "Требует внимания", "Не завершён раздел введения и видов работ.");

        if (!hasTechnicalTools)
            return new RiskState("attention", "Требует внимания", "Не заполнены технические средства.");

        if (practice.EndDate.Date.AddDays(-3) <= today && progress < 70)
            return new RiskState("attention", "Требует внимания", "До конца практики мало времени, прогресс ниже ожидаемого.");

        return new RiskState("ok", "В норме", "Заполнение идёт без явных рисков.");
    }

    private static List<SupervisorChartPointResponse> BuildProgressBuckets(List<SupervisorStudentProgressResponse> students)
    {
        var buckets = new[]
        {
            ("0-25", students.Count(x => x.ProgressPercent <= 25)),
            ("26-50", students.Count(x => x.ProgressPercent is > 25 and <= 50)),
            ("51-75", students.Count(x => x.ProgressPercent is > 50 and <= 75)),
            ("76-100", students.Count(x => x.ProgressPercent > 75))
        };

        return buckets.Select(x => CreateChartPoint(x.Item1, x.Item2, students.Count)).ToList();
    }

    private static List<SupervisorChartPointResponse> BuildGroupProgress(List<SupervisorStudentProgressResponse> students)
    {
        return students
            .GroupBy(x => string.IsNullOrWhiteSpace(x.GroupName) ? "Без группы" : x.GroupName)
            .OrderBy(x => x.Key)
            .Select(x => new SupervisorChartPointResponse
            {
                Label = x.Key,
                Value = RoundPercent(x.Average(item => item.ProgressPercent)),
                Percent = RoundPercent(x.Average(item => item.ProgressPercent))
            })
            .ToList();
    }

    private static List<SupervisorChartPointResponse> BuildRiskDistribution(List<SupervisorStudentProgressResponse> students)
    {
        var total = students.Count;
        return new List<SupervisorChartPointResponse>
        {
            CreateChartPoint("Критично", students.Count(x => x.RiskLevel == "critical"), total),
            CreateChartPoint("Внимание", students.Count(x => x.RiskLevel == "attention"), total),
            CreateChartPoint("В норме", students.Count(x => x.RiskLevel == "ok"), total),
            CreateChartPoint("Завершено", students.Count(x => x.RiskLevel == "done"), total)
        };
    }

    private static SupervisorChartPointResponse CreateChartPoint(string label, int value, int total)
    {
        return new SupervisorChartPointResponse
        {
            Label = label,
            Value = value,
            Percent = total == 0 ? 0 : RoundPercent(value * 100d / total)
        };
    }

    private static int CountWorkDays(DateTime startDate, DateTime endDate)
    {
        var count = 0;
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                count++;
        }

        return count;
    }

    private static int CountExpectedWorkDays(DateTime startDate, DateTime endDate, DateTime today)
    {
        if (today < startDate.Date)
            return 0;

        return CountWorkDays(startDate, today > endDate.Date ? endDate.Date : today);
    }

    private static int RoundPercent(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private int? GetCurrentUserId()
    {
        var rawValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawValue, out var userId) ? userId : null;
    }

    private sealed record RiskState(string Level, string Label, string Message);
}
