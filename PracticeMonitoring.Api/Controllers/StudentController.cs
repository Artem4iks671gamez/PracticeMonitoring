using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos.Student;
using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student")]
public class StudentController : ControllerBase
{
    private const long MaxAppendixSizeBytes = 15 * 1024 * 1024;
    private const long MaxDiaryFigureSizeBytes = 8 * 1024 * 1024;
    private static readonly HashSet<string> AllowedDiaryImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp"
    };
    private static readonly HashSet<string> AllowedReportCategories = new(StringComparer.Ordinal)
    {
        "TechnicalTool",
        "SoftwareTool"
    };
    private static readonly JsonSerializerOptions DayReportJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowOutOfOrderMetadataProperties = true
    };

    private readonly AppDbContext _context;
    private readonly NotificationService _notificationService;

    public StudentController(AppDbContext context, NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        return Ok(new
        {
            message = "Добро пожаловать, студент.",
            fullName = User.Identity?.Name,
            role = "Student"
        });
    }

    [HttpGet("practices")]
    public async Task<ActionResult<List<StudentPracticeListItemResponse>>> GetPractices()
    {
        var studentId = GetCurrentUserId();
        if (studentId is null)
            return Unauthorized();

        var assignments = await _context.ProductionPracticeStudentAssignments
            .AsNoTracking()
            .Include(x => x.ProductionPractice)
                .ThenInclude(x => x.Specialty)
            .Include(x => x.ProductionPractice)
                .ThenInclude(x => x.Competencies)
            .Include(x => x.ProductionPractice)
                .ThenInclude(x => x.GeneralCompetencies)
            .Include(x => x.Supervisor)
            .Include(x => x.DiaryEntries)
            .Where(x => x.StudentId == studentId.Value)
            .OrderBy(x => x.ProductionPractice.StartDate)
            .ThenBy(x => x.ProductionPractice.PracticeIndex)
            .ToListAsync();

        return Ok(assignments.Select(MapListItem).ToList());
    }

    [HttpGet("practices/{assignmentId:int}")]
    public async Task<ActionResult<StudentPracticeDetailsResponse>> GetPractice(int assignmentId)
    {
        var assignment = await LoadStudentAssignmentAsync(assignmentId, asNoTracking: true);
        if (assignment is null)
            return NotFound();

        return Ok(MapDetails(assignment));
    }

    [HttpPut("practices/{assignmentId:int}/organization")]
    public async Task<ActionResult<StudentPracticeDetailsResponse>> SaveOrganization(
        int assignmentId,
        StudentPracticeOrganizationRequest request)
    {
        var assignment = await LoadStudentAssignmentAsync(assignmentId);
        if (assignment is null)
            return NotFound();

        var errors = ValidateOrganization(request);
        if (errors.Count > 0)
            return BadRequest(new { message = "Заполните обязательные сведения о практике.", errors });

        assignment.OrganizationName = (request.OrganizationFullName ?? request.OrganizationName)!.Trim();
        assignment.OrganizationFullName = (request.OrganizationFullName ?? request.OrganizationName)!.Trim();
        assignment.OrganizationShortName = request.OrganizationShortName!.Trim();
        assignment.OrganizationAddress = request.OrganizationAddress!.Trim();
        assignment.OrganizationSupervisorFullName = request.OrganizationSupervisorFullName!.Trim();
        assignment.OrganizationSupervisorPosition = request.OrganizationSupervisorPosition!.Trim();
        assignment.OrganizationSupervisorPhone = NormalizeOptional(NormalizePhone(request.OrganizationSupervisorPhone));
        assignment.OrganizationSupervisorEmail = NormalizeOptional(request.OrganizationSupervisorEmail);
        assignment.PracticeTaskContent = request.PracticeTaskContent!.Trim();
        assignment.StudentDuties = request.StudentDuties?.Trim();
        assignment.ProvidedMaterialsDescription = request.ProvidedMaterialsDescription?.Trim();
        assignment.WorkScheduleDescription = request.WorkScheduleDescription?.Trim();
        assignment.IntroductionMainGoal = request.IntroductionMainGoal?.Trim();
        assignment.StudentDetailsUpdatedAtUtc = DateTime.UtcNow;

        if (assignment.SupervisorId.HasValue)
        {
            _notificationService.Add(
                assignment.SupervisorId.Value,
                "StudentPracticeDetails",
                "Студент заполнил сведения о практике",
                $"{assignment.Student.FullName} сохранил организацию, руководителя от организации и содержание задания по практике {assignment.ProductionPractice.PracticeIndex} \"{assignment.ProductionPractice.Name}\".",
                "/Supervisor");
        }

        await _context.SaveChangesAsync();

        assignment = await LoadStudentAssignmentAsync(assignmentId, asNoTracking: true);
        return Ok(MapDetails(assignment!));
    }

    [HttpPut("practices/{assignmentId:int}/diary")]
    public async Task<ActionResult<StudentPracticeDetailsResponse>> SaveDiaryEntry(
        int assignmentId,
        StudentPracticeDiaryEntryRequest request)
    {
        request.Figures ??= new List<StudentPracticeDiaryFigureRequest>();
        request.KeptAttachmentIds ??= new List<int>();

        var assignment = await LoadStudentAssignmentAsync(assignmentId);
        if (assignment is null)
            return NotFound();

        var errors = ValidateDiaryEntry(request, assignment.ProductionPractice);
        if (errors.Count > 0)
            return BadRequest(new { message = "Проверьте запись дневника.", errors });

        var workDate = ToUtcDate(request.WorkDate);
        var entry = assignment.DiaryEntries.FirstOrDefault(x => x.WorkDate.Date == workDate.Date);
        var now = DateTime.UtcNow;

        if (entry is null)
        {
            entry = new StudentPracticeDiaryEntry
            {
                ProductionPracticeStudentAssignmentId = assignment.Id,
                WorkDate = workDate,
                CreatedAtUtc = now
            };
            assignment.DiaryEntries.Add(entry);
        }

        try
        {
            var keptAttachmentIds = request.KeptAttachmentIds.ToHashSet();
            var attachmentsToRemove = entry.Attachments
                .Where(x => !keptAttachmentIds.Contains(x.Id))
                .ToList();

            if (attachmentsToRemove.Count > 0)
            {
                _context.StudentPracticeDiaryAttachments.RemoveRange(attachmentsToRemove);
                foreach (var attachment in attachmentsToRemove)
                    entry.Attachments.Remove(attachment);
            }

            var pendingAttachments = BuildDiaryAttachments(request.Figures);
            foreach (var pending in pendingAttachments)
                entry.Attachments.Add(pending.Attachment);

            entry.ShortDescription = request.ShortDescription!.Trim();
            entry.DetailedReport = request.DetailedReport!.Trim();
            entry.UpdatedAtUtc = now;

            await _context.SaveChangesAsync();

            entry.DetailedReport = BindDiaryAttachmentIds(entry.DetailedReport, pendingAttachments);
            var reportErrors = ValidateDetailedReport(entry.DetailedReport, allowPendingImageUploads: false, pendingUploadClientIds: null);
            if (reportErrors.Count > 0)
                return BadRequest(new { message = "Проверьте подробный отчёт за день.", errors = reportErrors });

            ApplyDiaryAttachmentMetadata(entry, entry.DetailedReport);

            await _context.SaveChangesAsync();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        assignment = await LoadStudentAssignmentAsync(assignmentId, asNoTracking: true);
        return Ok(MapDetails(assignment!));
    }

    [HttpPost("practices/{assignmentId:int}/diary-attachments")]
    public async Task<ActionResult<StudentPracticeDiaryAttachmentUploadResponse>> UploadDiaryAttachment(
        int assignmentId,
        [FromForm] DateTime workDate,
        [FromForm] string? title,
        [FromForm] IFormFile? file)
    {
        var assignment = await LoadStudentAssignmentAsync(assignmentId);
        if (assignment is null)
            return NotFound();

        var normalizedWorkDate = ToUtcDate(workDate);
        if (workDate == default ||
            normalizedWorkDate.Date < assignment.ProductionPractice.StartDate.Date ||
            normalizedWorkDate.Date > assignment.ProductionPractice.EndDate.Date ||
            normalizedWorkDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return BadRequest(new { message = "Дата изображения должна быть рабочим днём в период практики." });
        }

        var errors = ValidateDiaryImageFile(file);
        if (errors.Count > 0)
            return BadRequest(new { message = "Не удалось загрузить изображение.", errors = new Dictionary<string, string[]> { ["file"] = errors.ToArray() } });

        var entry = assignment.DiaryEntries.FirstOrDefault(x => x.WorkDate.Date == normalizedWorkDate.Date);
        var now = DateTime.UtcNow;
        if (entry is null)
        {
            entry = new StudentPracticeDiaryEntry
            {
                ProductionPracticeStudentAssignmentId = assignment.Id,
                WorkDate = normalizedWorkDate,
                ShortDescription = string.Empty,
                DetailedReport = "{\"version\":3,\"type\":\"practice-day-report\",\"blocks\":[]}",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            assignment.DiaryEntries.Add(entry);
        }

        await using var stream = file!.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);

        var attachment = new StudentPracticeDiaryAttachment
        {
            Caption = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(file.FileName) : title.Trim(),
            FileName = Path.GetFileName(file.FileName),
            ContentType = NormalizeImageContentType(file.ContentType, file.FileName),
            SizeBytes = file.Length,
            Content = memory.ToArray(),
            SortOrder = entry.Attachments.Count + 1
        };

        entry.Attachments.Add(attachment);
        entry.UpdatedAtUtc = now;
        await _context.SaveChangesAsync();

        return Ok(new StudentPracticeDiaryAttachmentUploadResponse
        {
            AttachmentId = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            SizeBytes = attachment.SizeBytes,
            DownloadUrl = Url.Action(nameof(DownloadDiaryAttachment), "Student", new { attachmentId = attachment.Id }) ?? string.Empty
        });
    }

    [HttpPut("practices/{assignmentId:int}/report-items")]
    public async Task<ActionResult<StudentPracticeDetailsResponse>> SaveReportItems(
        int assignmentId,
        StudentPracticeReportItemsRequest request)
    {
        var assignment = await LoadStudentAssignmentAsync(assignmentId);
        if (assignment is null)
            return NotFound();

        var errors = ValidateReportItems(request);
        if (errors.Count > 0)
            return BadRequest(new { message = "Проверьте таблицы отчёта.", errors });

        _context.StudentPracticeReportItems.RemoveRange(assignment.ReportItems);

        assignment.ReportItems = request.Items
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select((x, index) => new StudentPracticeReportItem
            {
                ProductionPracticeStudentAssignmentId = assignment.Id,
                Category = x.Category!.Trim(),
                Name = x.Name!.Trim(),
                Description = NormalizeOptional(x.Description),
                SortOrder = index + 1
            })
            .ToList();

        await _context.SaveChangesAsync();

        assignment = await LoadStudentAssignmentAsync(assignmentId, asNoTracking: true);
        return Ok(MapDetails(assignment!));
    }

    [HttpPut("practices/{assignmentId:int}/sources")]
    public async Task<ActionResult<StudentPracticeDetailsResponse>> SaveSources(
        int assignmentId,
        StudentPracticeSourcesRequest request)
    {
        var assignment = await LoadStudentAssignmentAsync(assignmentId);
        if (assignment is null)
            return NotFound();

        var errors = ValidateSources(request);
        if (errors.Count > 0)
            return BadRequest(new { message = "Проверьте список источников.", errors });

        _context.StudentPracticeSources.RemoveRange(assignment.Sources);

        assignment.Sources = request.Sources
            .Where(x => !string.IsNullOrWhiteSpace(x.Title))
            .Select((x, index) => new StudentPracticeSource
            {
                ProductionPracticeStudentAssignmentId = assignment.Id,
                Title = x.Title!.Trim(),
                Url = NormalizeOptional(x.Url),
                Description = NormalizeOptional(x.Description),
                SortOrder = index + 1
            })
            .ToList();

        await _context.SaveChangesAsync();

        assignment = await LoadStudentAssignmentAsync(assignmentId, asNoTracking: true);
        return Ok(MapDetails(assignment!));
    }

    [HttpPost("practices/{assignmentId:int}/appendices")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ActionResult<StudentPracticeDetailsResponse>> UploadAppendix(
        int assignmentId,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] IFormFile? file)
    {
        var assignment = await LoadStudentAssignmentAsync(assignmentId);
        if (assignment is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new { message = "Укажите название приложения." });

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Выберите файл приложения." });

        if (file.Length > MaxAppendixSizeBytes)
            return BadRequest(new { message = "Файл приложения не должен превышать 15 МБ." });

        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        assignment.Appendices.Add(new StudentPracticeAppendix
        {
            ProductionPracticeStudentAssignmentId = assignment.Id,
            Title = title.Trim(),
            Description = NormalizeOptional(description),
            FileName = Path.GetFileName(file.FileName),
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            Content = memoryStream.ToArray(),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        assignment = await LoadStudentAssignmentAsync(assignmentId, asNoTracking: true);
        return Ok(MapDetails(assignment!));
    }

    [HttpDelete("appendices/{appendixId:int}")]
    public async Task<IActionResult> DeleteAppendix(int appendixId)
    {
        var studentId = GetCurrentUserId();
        if (studentId is null)
            return Unauthorized();

        var appendix = await _context.StudentPracticeAppendices
            .Include(x => x.Assignment)
            .FirstOrDefaultAsync(x => x.Id == appendixId && x.Assignment.StudentId == studentId.Value);

        if (appendix is null)
            return NotFound();

        _context.StudentPracticeAppendices.Remove(appendix);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Приложение удалено." });
    }

    [HttpGet("appendices/{appendixId:int}/download")]
    public async Task<IActionResult> DownloadAppendix(int appendixId)
    {
        var studentId = GetCurrentUserId();
        if (studentId is null)
            return Unauthorized();

        var appendix = await _context.StudentPracticeAppendices
            .AsNoTracking()
            .Include(x => x.Assignment)
            .FirstOrDefaultAsync(x => x.Id == appendixId && x.Assignment.StudentId == studentId.Value);

        if (appendix is null)
            return NotFound();

        return File(appendix.Content, appendix.ContentType, appendix.FileName);
    }

    [HttpGet("diary-attachments/{attachmentId:int}/download")]
    public async Task<IActionResult> DownloadDiaryAttachment(int attachmentId)
    {
        var studentId = GetCurrentUserId();
        if (studentId is null)
            return Unauthorized();

        var attachment = await _context.StudentPracticeDiaryAttachments
            .AsNoTracking()
            .Include(x => x.DiaryEntry)
                .ThenInclude(x => x.Assignment)
            .FirstOrDefaultAsync(x =>
                x.Id == attachmentId &&
                x.DiaryEntry.Assignment.StudentId == studentId.Value);

        if (attachment is null)
            return NotFound();

        return File(attachment.Content, attachment.ContentType, attachment.FileName);
    }

    private async Task<ProductionPracticeStudentAssignment?> LoadStudentAssignmentAsync(
        int assignmentId,
        bool asNoTracking = false)
    {
        var studentId = GetCurrentUserId();
        if (studentId is null)
            return null;

        var query = _context.ProductionPracticeStudentAssignments
            .Include(x => x.Student)
            .Include(x => x.Supervisor)
            .Include(x => x.ProductionPractice)
                .ThenInclude(x => x.Specialty)
            .Include(x => x.ProductionPractice)
                .ThenInclude(x => x.Competencies)
            .Include(x => x.ProductionPractice)
                .ThenInclude(x => x.GeneralCompetencies)
            .Include(x => x.DiaryEntries)
                .ThenInclude(x => x.Attachments)
            .Include(x => x.ReportItems)
            .Include(x => x.Sources)
            .Include(x => x.Appendices)
            .Where(x => x.Id == assignmentId && x.StudentId == studentId.Value);

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync();
    }

    private Dictionary<string, string[]> ValidateOrganization(StudentPracticeOrganizationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.OrganizationFullName) &&
            string.IsNullOrWhiteSpace(request.OrganizationName))
            errors[nameof(request.OrganizationFullName)] = new[] { "Укажите полное название организации." };

        if (string.IsNullOrWhiteSpace(request.OrganizationShortName))
            errors[nameof(request.OrganizationShortName)] = new[] { "Укажите сокращенное название организации." };

        if (string.IsNullOrWhiteSpace(request.OrganizationAddress))
            errors[nameof(request.OrganizationAddress)] = new[] { "Укажите адрес организации." };

        if (string.IsNullOrWhiteSpace(request.OrganizationSupervisorFullName))
            errors[nameof(request.OrganizationSupervisorFullName)] = new[] { "Укажите ФИО руководителя от организации." };

        if (string.IsNullOrWhiteSpace(request.OrganizationSupervisorPosition))
            errors[nameof(request.OrganizationSupervisorPosition)] = new[] { "Укажите должность руководителя от организации." };

        if (string.IsNullOrWhiteSpace(request.OrganizationSupervisorPhone) &&
            string.IsNullOrWhiteSpace(request.OrganizationSupervisorEmail))
        {
            errors[nameof(request.OrganizationSupervisorPhone)] = new[] { "Укажите телефон или почту руководителя от организации." };
        }

        var normalizedPhone = NormalizePhone(request.OrganizationSupervisorPhone);
        if (!string.IsNullOrWhiteSpace(request.OrganizationSupervisorPhone) &&
            (normalizedPhone.Length != 12 || !normalizedPhone.StartsWith("+7", StringComparison.Ordinal)))
        {
            errors[nameof(request.OrganizationSupervisorPhone)] = new[] { "Телефон руководителя должен быть в формате +7 (999) 999-99-99." };
        }

        if (!string.IsNullOrWhiteSpace(request.OrganizationSupervisorEmail) &&
            !System.Text.RegularExpressions.Regex.IsMatch(request.OrganizationSupervisorEmail.Trim(), "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$"))
        {
            errors[nameof(request.OrganizationSupervisorEmail)] = new[] { "Почта руководителя от организации указана некорректно." };
        }

        if (string.IsNullOrWhiteSpace(request.PracticeTaskContent))
            errors[nameof(request.PracticeTaskContent)] = new[] { "Опишите содержание задания на производственную практику." };

        return errors;
    }
    private static Dictionary<string, string[]> ValidateDiaryEntry(
        StudentPracticeDiaryEntryRequest request,
        ProductionPractice practice)
    {
        var errors = new Dictionary<string, string[]>();
        var workDate = ToUtcDate(request.WorkDate);
        request.Figures ??= new List<StudentPracticeDiaryFigureRequest>();

        if (request.WorkDate == default)
        {
            errors[nameof(request.WorkDate)] = new[] { "Укажите дату рабочего дня." };
        }
        else if (workDate.Date < practice.StartDate.Date || workDate.Date > practice.EndDate.Date)
        {
            errors[nameof(request.WorkDate)] = new[] { "Дата записи должна попадать в период практики." };
        }
        else if (workDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            errors[nameof(request.WorkDate)] = new[] { "Дневник заполняется за рабочий день. Для выходных запись не требуется." };
        }

        if (string.IsNullOrWhiteSpace(request.ShortDescription))
            errors[nameof(request.ShortDescription)] = new[] { "Заполните краткое описание для дневника." };

        if (string.IsNullOrWhiteSpace(request.DetailedReport))
            errors[nameof(request.DetailedReport)] = new[] { "Заполните подробный отчёт за день." };
        else
        {
            var pendingUploadClientIds = request.Figures
                .Where(x => !string.IsNullOrWhiteSpace(x.Base64Content) && !string.IsNullOrWhiteSpace(x.ClientId))
                .Select(x => x.ClientId!.Trim())
                .ToHashSet(StringComparer.Ordinal);
            foreach (var error in ValidateDetailedReport(request.DetailedReport, allowPendingImageUploads: true, pendingUploadClientIds))
                errors[error.Key] = error.Value;
        }

        for (var i = 0; i < request.Figures.Count; i++)
        {
            var figure = request.Figures[i];
            if (string.IsNullOrWhiteSpace(figure.Base64Content))
                continue;

            if (string.IsNullOrWhiteSpace(figure.Caption))
                errors[$"Figures[{i}].Caption"] = new[] { "Укажите название рисунка." };

            if (string.IsNullOrWhiteSpace(figure.ContentType) ||
                !figure.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                errors[$"Figures[{i}].ContentType"] = new[] { "К дневнику можно прикреплять только изображения." };
            }
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateDetailedReport(
        string detailedReport,
        bool allowPendingImageUploads,
        HashSet<string>? pendingUploadClientIds)
    {
        var messages = new List<string>();
        DayReportDto? report;

        try
        {
            report = JsonSerializer.Deserialize<DayReportDto>(detailedReport, DayReportJsonOptions);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string[]>
            {
                [nameof(StudentPracticeDiaryEntryRequest.DetailedReport)] = new[]
                {
                    "Подробный отчёт должен соответствовать схеме DayReportDto. Неизвестные типы блоков запрещены."
                }
            };
        }

        if (report is null)
            messages.Add("Подробный отчёт не удалось прочитать.");
        else
            ValidateDetailedReportModel(report, allowPendingImageUploads, pendingUploadClientIds, messages);

        return messages.Count == 0
            ? new Dictionary<string, string[]>()
            : new Dictionary<string, string[]>
            {
                [nameof(StudentPracticeDiaryEntryRequest.DetailedReport)] = messages.Distinct().ToArray()
            };
    }

    private static void ValidateDetailedReportModel(
        DayReportDto report,
        bool allowPendingImageUploads,
        HashSet<string>? pendingUploadClientIds,
        List<string> messages)
    {
        if (!string.Equals(report.Type, "practice-day-report", StringComparison.Ordinal))
            messages.Add("У подробного отчёта указан неизвестный тип документа.");

        if (report.Blocks.Count == 0)
        {
            messages.Add("Подробный отчёт не должен быть пустым.");
            return;
        }

        var hasMeaningfulBlock = false;
        for (var blockIndex = 0; blockIndex < report.Blocks.Count; blockIndex++)
        {
            var block = report.Blocks[blockIndex];
            switch (block)
            {
                case TextReportBlockDto textBlock:
                    if (string.IsNullOrWhiteSpace(textBlock.Content))
                        messages.Add($"Текстовый блок #{blockIndex + 1} пустой.");
                    else
                        hasMeaningfulBlock = true;
                    break;

                case TableReportBlockDto tableBlock:
                    ValidateTableReportBlock(tableBlock, blockIndex, messages);
                    hasMeaningfulBlock = true;
                    break;

                case ImageReportBlockDto imageBlock:
                    ValidateImageReportBlock(imageBlock, blockIndex, allowPendingImageUploads, pendingUploadClientIds, messages);
                    hasMeaningfulBlock = true;
                    break;

                default:
                    messages.Add($"Блок #{blockIndex + 1} имеет неизвестный тип.");
                    break;
            }
        }

        if (!hasMeaningfulBlock)
            messages.Add("Подробный отчёт должен содержать хотя бы один заполненный блок.");
    }

    private static void ValidateTableReportBlock(TableReportBlockDto tableBlock, int blockIndex, List<string> messages)
    {
        if (string.IsNullOrWhiteSpace(tableBlock.Title))
            messages.Add($"У таблицы #{blockIndex + 1} должно быть название.");

        if (tableBlock.Rows.Count == 0)
            messages.Add($"Таблица #{blockIndex + 1} должна содержать хотя бы одну строку.");

        for (var rowIndex = 0; rowIndex < tableBlock.Rows.Count; rowIndex++)
        {
            var row = tableBlock.Rows[rowIndex];
            if (row.Cells.Count == 0)
                messages.Add($"Строка #{rowIndex + 1} таблицы #{blockIndex + 1} должна содержать ячейки.");

            for (var cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
            {
                var cell = row.Cells[cellIndex];
                if (cell.Colspan < 1)
                    messages.Add($"Colspan ячейки #{cellIndex + 1} строки #{rowIndex + 1} таблицы #{blockIndex + 1} должен быть не меньше 1.");
                if (cell.Rowspan < 1)
                    messages.Add($"Rowspan ячейки #{cellIndex + 1} строки #{rowIndex + 1} таблицы #{blockIndex + 1} должен быть не меньше 1.");
            }
        }
    }

    private static void ValidateImageReportBlock(
        ImageReportBlockDto imageBlock,
        int blockIndex,
        bool allowPendingImageUploads,
        HashSet<string>? pendingUploadClientIds,
        List<string> messages)
    {
        if (string.IsNullOrWhiteSpace(imageBlock.Title))
            messages.Add($"У рисунка #{blockIndex + 1} должно быть название.");

        var hasAttachment = imageBlock.AttachmentId.HasValue && imageBlock.AttachmentId.Value > 0;
        var hasPendingUpload = allowPendingImageUploads &&
                               !string.IsNullOrWhiteSpace(imageBlock.UploadClientId) &&
                               pendingUploadClientIds?.Contains(imageBlock.UploadClientId.Trim()) == true;

        if (!hasAttachment && !hasPendingUpload)
            messages.Add($"У рисунка #{blockIndex + 1} должен быть attachmentId.");
    }

    private static List<string> ValidateDiaryImageFile(IFormFile? file)
    {
        var errors = new List<string>();
        if (file is null || file.Length == 0)
        {
            errors.Add("Выберите изображение.");
            return errors;
        }

        if (file.Length > MaxDiaryFigureSizeBytes)
            errors.Add("Размер изображения не должен превышать 8 МБ.");

        var contentType = NormalizeImageContentType(file.ContentType, file.FileName);
        if (!AllowedDiaryImageContentTypes.Contains(contentType))
            errors.Add("Можно загрузить только PNG, JPG, JPEG или WEBP.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            errors.Add("Расширение файла должно быть PNG, JPG, JPEG или WEBP.");

        return errors;
    }

    private static string NormalizeImageContentType(string? contentType, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            var normalized = contentType.Trim().ToLowerInvariant();
            return normalized == "image/jpg" ? "image/jpeg" : normalized;
        }

        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static List<PendingDiaryAttachment> BuildDiaryAttachments(List<StudentPracticeDiaryFigureRequest> figures)
    {
        var attachments = new List<PendingDiaryAttachment>();

        foreach (var figure in figures.Where(x => !string.IsNullOrWhiteSpace(x.Base64Content)))
        {
            byte[] bytes;

            try
            {
                bytes = Convert.FromBase64String(figure.Base64Content!);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Не удалось прочитать один из рисунков. Загрузите изображение повторно.");
            }

            if (bytes.Length > MaxDiaryFigureSizeBytes)
                throw new InvalidOperationException("Размер рисунка не должен превышать 8 МБ.");

            attachments.Add(new PendingDiaryAttachment(
                figure.ClientId?.Trim(),
                new StudentPracticeDiaryAttachment
                {
                    Caption = figure.Caption?.Trim() ?? "Рисунок",
                    FileName = string.IsNullOrWhiteSpace(figure.FileName) ? "figure.png" : Path.GetFileName(figure.FileName),
                    ContentType = string.IsNullOrWhiteSpace(figure.ContentType) ? "image/png" : figure.ContentType.Trim(),
                    SizeBytes = bytes.Length,
                    Content = bytes,
                    SortOrder = figure.SortOrder
                }));
        }

        return attachments;
    }

    private static string BindDiaryAttachmentIds(string reportJson, List<PendingDiaryAttachment> pendingAttachments)
    {
        if (pendingAttachments.Count == 0)
            return reportJson;

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(reportJson);
        }
        catch
        {
            return reportJson;
        }

        if (root is not JsonObject document)
            return reportJson;

        var content = GetReportBlocks(document);
        if (content is null)
            return reportJson;

        var byClientId = pendingAttachments
            .Where(x => !string.IsNullOrWhiteSpace(x.ClientId))
            .ToDictionary(x => x.ClientId!, x => x.Attachment, StringComparer.Ordinal);

        foreach (var node in content.OfType<JsonObject>())
        {
            if (!IsReportImageNode(node))
                continue;

            var clientId = node["uploadClientId"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(clientId) || !byClientId.TryGetValue(clientId, out var attachment))
                continue;

            node["attachmentId"] = attachment.Id;
            node["fileName"] = attachment.FileName;
            node["mimeType"] = attachment.ContentType;
            node["size"] = attachment.SizeBytes;
            node.Remove("uploadClientId");
            node.Remove("previewUrl");
        }

        RebuildReportAttachmentIndex(document, content);
        return document.ToJsonString();
    }

    private static void ApplyDiaryAttachmentMetadata(StudentPracticeDiaryEntry entry, string reportJson)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(reportJson);
        }
        catch
        {
            return;
        }

        if (root is not JsonObject document)
            return;

        var content = GetReportBlocks(document);
        if (content is null)
            return;

        var order = 1;
        var metadata = new Dictionary<int, (string? Caption, string? Alt, int SortOrder)>();
        foreach (var node in content.OfType<JsonObject>())
        {
            if (!IsReportImageNode(node))
                continue;

            var attachmentId = node["attachmentId"]?.GetValue<int?>();
            if (!attachmentId.HasValue)
                continue;

            metadata[attachmentId.Value] = (
                node["title"]?.GetValue<string>() ?? node["caption"]?.GetValue<string>(),
                node["alt"]?.GetValue<string>(),
                order++);
        }

        foreach (var attachment in entry.Attachments)
        {
            if (!metadata.TryGetValue(attachment.Id, out var item))
                continue;

            attachment.Caption = string.IsNullOrWhiteSpace(item.Caption) ? attachment.Caption : item.Caption.Trim();
            attachment.SortOrder = item.SortOrder;
        }
    }

    private static void RebuildReportAttachmentIndex(JsonObject document, JsonArray content)
    {
        var attachments = new JsonArray();
        foreach (var node in content.OfType<JsonObject>())
        {
            if (!IsReportImageNode(node))
                continue;

            var attachmentId = node["attachmentId"]?.GetValue<int?>();
            if (!attachmentId.HasValue)
                continue;

            attachments.Add(new JsonObject
            {
                ["id"] = attachmentId.Value,
                ["type"] = "image",
                ["filename"] = node["fileName"]?.GetValue<string>() ?? string.Empty,
                ["mimeType"] = node["mimeType"]?.GetValue<string>() ?? string.Empty,
                ["size"] = node["size"]?.GetValue<long?>() ?? 0
            });
        }

        document["attachments"] = attachments;
    }

    private static JsonArray? GetReportBlocks(JsonObject document)
    {
        return document["blocks"] as JsonArray ?? document["content"] as JsonArray;
    }

    private static bool IsReportImageNode(JsonObject node)
    {
        var type = node["type"]?.GetValue<string>();
        return string.Equals(type, "image", StringComparison.Ordinal) ||
               string.Equals(type, "figure", StringComparison.Ordinal);
    }

    private sealed record PendingDiaryAttachment(string? ClientId, StudentPracticeDiaryAttachment Attachment);
    private static Dictionary<string, string[]> ValidateReportItems(StudentPracticeReportItemsRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            if (string.IsNullOrWhiteSpace(item.Name))
                continue;

            if (string.IsNullOrWhiteSpace(item.Category) || !AllowedReportCategories.Contains(item.Category.Trim()))
                errors[$"Items[{i}].Category"] = new[] { "Выберите корректную таблицу отчёта." };
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateSources(StudentPracticeSourcesRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        for (var i = 0; i < request.Sources.Count; i++)
        {
            var source = request.Sources[i];
            if (string.IsNullOrWhiteSpace(source.Title) &&
                (!string.IsNullOrWhiteSpace(source.Url) || !string.IsNullOrWhiteSpace(source.Description)))
            {
                errors[$"Sources[{i}].Title"] = new[] { "Укажите название источника." };
            }
        }

        return errors;
    }

    private static StudentPracticeListItemResponse MapListItem(ProductionPracticeStudentAssignment assignment)
    {
        var practice = assignment.ProductionPractice;
        var dueDate = practice.StartDate.Date.AddDays(2);
        var hasDetails = HasRequiredDetails(assignment);

        return new StudentPracticeListItemResponse
        {
            AssignmentId = assignment.Id,
            PracticeId = practice.Id,
            PracticeIndex = practice.PracticeIndex,
            Name = practice.Name,
            SpecialtyCode = practice.Specialty.Code,
            SpecialtyName = practice.Specialty.Name,
            ProfessionalModuleCode = practice.ProfessionalModuleCode,
            ProfessionalModuleName = practice.ProfessionalModuleName,
            Hours = practice.Hours,
            StartDate = practice.StartDate,
            EndDate = practice.EndDate,
            IsCompleted = IsCompleted(practice.EndDate),
            SupervisorFullName = assignment.Supervisor?.FullName,
            OrganizationName = assignment.OrganizationFullName ?? assignment.OrganizationName,
            OrganizationFullName = assignment.OrganizationFullName ?? assignment.OrganizationName,
            OrganizationShortName = assignment.OrganizationShortName,
            OrganizationAddress = assignment.OrganizationAddress,
            HasRequiredDetails = hasDetails,
            DetailsDueDate = dueDate,
            IsDetailsOverdue = !hasDetails && DateTime.UtcNow.Date > dueDate,
            DiaryEntriesCount = assignment.DiaryEntries.Count,
            WorkDaysCount = CountWorkDays(practice.StartDate, practice.EndDate)
        };
    }

    private static StudentPracticeDetailsResponse MapDetails(ProductionPracticeStudentAssignment assignment)
    {
        var listItem = MapListItem(assignment);
        var practice = assignment.ProductionPractice;

        return new StudentPracticeDetailsResponse
        {
            AssignmentId = listItem.AssignmentId,
            PracticeId = listItem.PracticeId,
            PracticeIndex = listItem.PracticeIndex,
            Name = listItem.Name,
            SpecialtyCode = listItem.SpecialtyCode,
            SpecialtyName = listItem.SpecialtyName,
            ProfessionalModuleCode = listItem.ProfessionalModuleCode,
            ProfessionalModuleName = listItem.ProfessionalModuleName,
            Hours = listItem.Hours,
            StartDate = listItem.StartDate,
            EndDate = listItem.EndDate,
            IsCompleted = listItem.IsCompleted,
            SupervisorFullName = listItem.SupervisorFullName,
            OrganizationName = assignment.OrganizationFullName ?? assignment.OrganizationName,
            OrganizationFullName = assignment.OrganizationFullName ?? assignment.OrganizationName,
            OrganizationShortName = assignment.OrganizationShortName,
            OrganizationAddress = assignment.OrganizationAddress,
            HasRequiredDetails = listItem.HasRequiredDetails,
            DetailsDueDate = listItem.DetailsDueDate,
            IsDetailsOverdue = listItem.IsDetailsOverdue,
            DiaryEntriesCount = listItem.DiaryEntriesCount,
            WorkDaysCount = listItem.WorkDaysCount,
            AssignedAtUtc = assignment.AssignedAtUtc,
            OrganizationSupervisorFullName = assignment.OrganizationSupervisorFullName,
            OrganizationSupervisorPosition = assignment.OrganizationSupervisorPosition,
            OrganizationSupervisorPhone = assignment.OrganizationSupervisorPhone,
            OrganizationSupervisorEmail = assignment.OrganizationSupervisorEmail,
            PracticeTaskContent = assignment.PracticeTaskContent,
            StudentDuties = assignment.StudentDuties,
            ProvidedMaterialsDescription = assignment.ProvidedMaterialsDescription,
            WorkScheduleDescription = assignment.WorkScheduleDescription,
            IntroductionMainGoal = assignment.IntroductionMainGoal,
            GeneralCompetencies = practice.GeneralCompetencies
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .Select(x => new StudentPracticeGeneralCompetencyResponse
                {
                    CompetencyCode = x.CompetencyCode,
                    CompetencyDescription = x.CompetencyDescription,
                    SortOrder = x.SortOrder
                })
                .ToList(),            Competencies = practice.Competencies
                .OrderBy(x => x.Id)
                .Select(x => new StudentPracticeCompetencyResponse
                {
                    CompetencyCode = x.CompetencyCode,
                    CompetencyDescription = x.CompetencyDescription,
                    WorkTypes = x.WorkTypes,
                    Hours = x.Hours
                })
                .ToList(),
            DiaryEntries = assignment.DiaryEntries
                .OrderByDescending(x => x.WorkDate)
                .Select(x => new StudentPracticeDiaryEntryResponse
                {
                    Id = x.Id,
                    WorkDate = x.WorkDate,
                    ShortDescription = x.ShortDescription,
                    DetailedReport = x.DetailedReport,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    Attachments = x.Attachments
                        .OrderBy(a => a.SortOrder)
                        .Select(a => new StudentPracticeDiaryAttachmentResponse
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
            ReportItems = assignment.ReportItems
                .OrderBy(x => x.Category)
                .ThenBy(x => x.SortOrder)
                .Select(x => new StudentPracticeReportItemResponse
                {
                    Id = x.Id,
                    Category = x.Category,
                    Name = x.Name,
                    Description = x.Description,
                    SortOrder = x.SortOrder
                })
                .ToList(),
            Sources = assignment.Sources
                .OrderBy(x => x.SortOrder)
                .Select(x => new StudentPracticeSourceResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Url = x.Url,
                    Description = x.Description,
                    SortOrder = x.SortOrder
                })
                .ToList(),
            Appendices = assignment.Appendices
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => new StudentPracticeAppendixResponse
                {
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    FileName = x.FileName,
                    ContentType = x.ContentType,
                    SizeBytes = x.SizeBytes,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToList()
        };
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

    private static bool IsCompleted(DateTime endDate)
    {
        return endDate.Date < DateTime.UtcNow.Date;
    }

    private static DateTime ToUtcDate(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return string.Empty;

        if (digits.StartsWith("8", StringComparison.Ordinal) && digits.Length == 11)
            digits = $"7{digits[1..]}";

        if (!digits.StartsWith("7", StringComparison.Ordinal))
            digits = $"7{digits}";

        return $"+{digits}";
    }

    private int? GetCurrentUserId()
    {
        var rawValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawValue, out var userId) ? userId : null;
    }
}













