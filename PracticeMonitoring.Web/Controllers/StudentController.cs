using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using PracticeMonitoring.Web.Models.Messaging;
using PracticeMonitoring.Web.Models.Student;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class StudentController : Controller
{
    private readonly AuthApiService _authApiService;
    private readonly ChatApiService _chatApiService;
    private readonly StudentApiService _studentApiService;
    private readonly NotificationApiService _notificationApiService;
    private readonly PracticeReportDocumentService _practiceReportDocumentService;
    private readonly PracticeDiaryDocumentService _practiceDiaryDocumentService;
    private readonly AttestationSheetService _attestationSheetService;
    private readonly DocxPdfConversionService _docxPdfConversionService;

    public StudentController(
        AuthApiService authApiService,
        ChatApiService chatApiService,
        StudentApiService studentApiService,
        NotificationApiService notificationApiService,
        PracticeReportDocumentService practiceReportDocumentService,
        PracticeDiaryDocumentService practiceDiaryDocumentService,
        AttestationSheetService attestationSheetService,
        DocxPdfConversionService docxPdfConversionService)
    {
        _authApiService = authApiService;
        _chatApiService = chatApiService;
        _studentApiService = studentApiService;
        _notificationApiService = notificationApiService;
        _practiceReportDocumentService = practiceReportDocumentService;
        _practiceDiaryDocumentService = practiceDiaryDocumentService;
        _attestationSheetService = attestationSheetService;
        _docxPdfConversionService = docxPdfConversionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Student")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        var userTask = _authApiService.GetCurrentUserAsync(token);
        var threadsTask = _chatApiService.GetThreadsAsync(token);
        var practicesTask = _studentApiService.GetPracticesAsync(token);
        var notificationsTask = _notificationApiService.GetNotificationsAsync(token);

        await Task.WhenAll(userTask, threadsTask, practicesTask, notificationsTask);

        var user = userTask.Result;
        if (user is null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Theme", user.Theme ?? "light");

        return View(new StudentPageViewModel
        {
            CurrentUser = user,
            Practices = practicesTask.Result,
            Notifications = notificationsTask.Result,
            Messaging = new MessagingWorkspaceViewModel
            {
                CurrentUserId = user.Id,
                CurrentUserRole = user.Role,
                CurrentUserFullName = user.FullName,
                CurrentUserAvatarUrl = user.AvatarUrl,
                Threads = threadsTask.Result
            }
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetPractice(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var result = await _studentApiService.GetPracticeResultAsync(token, assignmentId);
        if (!result.Success || result.Data is null)
        {
            var statusCode = result.StatusCode is >= 400 and <= 599
                ? result.StatusCode
                : StatusCodes.Status502BadGateway;

            return StatusCode(statusCode, new
            {
                message = result.ErrorMessage ?? "Не удалось загрузить практику."
            });
        }

        return Json(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> SaveOrganization(
        int assignmentId,
        [FromBody] StudentPracticeOrganizationRequestViewModel? model)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        if (model is null)
            return BadRequest(new { message = "Не удалось прочитать сведения об организации." });

        var result = await _studentApiService.SaveOrganizationAsync(token, assignmentId, model);
        return ToJsonResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> SaveDiaryEntry(
        int assignmentId,
        [FromBody] StudentPracticeDiaryEntryRequestViewModel? model)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        if (model is null)
            return BadRequest(new { message = "Не удалось прочитать запись дневника." });

        var result = await _studentApiService.SaveDiaryEntryAsync(token, assignmentId, model);
        return ToJsonResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> UploadDiaryAttachment(
        int assignmentId,
        DateTime workDate,
        string? title,
        IFormFile? file)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var result = await _studentApiService.UploadDiaryAttachmentAsync(token, assignmentId, workDate, title, file);
        if (result.Success && result.Data is not null)
        {
            result.Data.DownloadUrl = Url.Action(
                nameof(DownloadDiaryAttachment),
                "Student",
                new { attachmentId = result.Data.AttachmentId }) ?? result.Data.DownloadUrl;

            return Json(result.Data);
        }

        return BadRequest(new
        {
            message = result.ErrorMessage ?? "Не удалось загрузить изображение.",
            errors = result.ValidationErrors
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveReportItems(
        int assignmentId,
        [FromBody] StudentPracticeReportItemsRequestViewModel? model)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        if (model is null)
            return BadRequest(new { message = "Не удалось прочитать таблицы отчёта." });

        var result = await _studentApiService.SaveReportItemsAsync(token, assignmentId, model);
        return ToJsonResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSources(
        int assignmentId,
        [FromBody] StudentPracticeSourcesRequestViewModel? model)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        if (model is null)
            return BadRequest(new { message = "Не удалось прочитать источники." });

        var result = await _studentApiService.SaveSourcesAsync(token, assignmentId, model);
        return ToJsonResult(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25 * 1024 * 1024)]
    public async Task<IActionResult> UploadAppendix(
        int assignmentId,
        string? title,
        string? description,
        IFormFile? file)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var result = await _studentApiService.UploadAppendixAsync(token, assignmentId, title, description, file);
        if (result.Success)
        {
            var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
            if (practice is null)
                return Json(result.Data);

            return Json(new
            {
                details = practice,
                appendix = practice.Appendices
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefault()
            });
        }

        return BadRequest(new
        {
            message = result.ErrorMessage ?? "Не удалось загрузить приложение.",
            errors = result.ValidationErrors
        });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAppendix(int appendixId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var result = await _studentApiService.DeleteAppendixAsync(token, appendixId);
        if (result.Success)
            return Ok(new { message = "Приложение удалено." });

        return BadRequest(new { message = result.ErrorMessage ?? "Не удалось удалить приложение." });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAppendix(int appendixId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var file = await _studentApiService.DownloadAppendixAsync(token, appendixId);
        if (file is null)
            return NotFound();

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDiaryAttachment(int attachmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var file = await _studentApiService.DownloadDiaryAttachmentAsync(token, attachmentId);
        if (file is null)
            return NotFound();

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> GetPracticeReportPreview(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        return Json(new
        {
            missing = _practiceReportDocumentService.Validate(practice)
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAttestationPreview(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var missing = _attestationSheetService.Validate(practice);
        if (missing.Count > 0)
        {
            return Json(new
            {
                missing
            });
        }

        return Json(new
        {
            fileName = _attestationSheetService.BuildFileName(practice),
            pdfFileName = _attestationSheetService.BuildPdfFileName(practice),
            archiveFileName = _attestationSheetService.BuildArchiveFileName(practice),
            previewUrl = Url.Action(nameof(PreviewAttestationPdf), "Student", new { assignmentId }),
            docxUrl = Url.Action(nameof(DownloadAttestation), "Student", new { assignmentId }),
            pdfUrl = Url.Action(nameof(DownloadAttestationPdf), "Student", new { assignmentId }),
            archiveUrl = Url.Action(nameof(DownloadAttestationArchive), "Student", new { assignmentId }),
            missing
        });
    }

    [HttpGet]
    public async Task<IActionResult> PreviewAttestationPdf(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var missing = _attestationSheetService.Validate(practice);
        if (missing.Count > 0)
            return BadRequest(new { message = "Аттестационный лист нельзя показать: заполнены не все обязательные реквизиты.", missing });

        try
        {
            var docxFileName = _attestationSheetService.BuildFileName(practice);
            var pdf = await ConvertDocxToPdfAsync(_attestationSheetService.BuildDocx(practice), docxFileName);
            return File(pdf, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            return PdfConversionFailed(ex);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttestation(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var missing = _attestationSheetService.Validate(practice);
        if (missing.Count > 0)
        {
            return BadRequest(new
            {
                message = "Аттестационный лист нельзя сформировать: заполнены не все обязательные реквизиты.",
                missing
            });
        }

        return File(
            _attestationSheetService.BuildDocx(practice),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _attestationSheetService.BuildFileName(practice));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttestationPdf(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var missing = _attestationSheetService.Validate(practice);
        if (missing.Count > 0)
        {
            return BadRequest(new
            {
                message = "PDF аттестационного листа нельзя сформировать: заполнены не все обязательные реквизиты.",
                missing
            });
        }

        try
        {
            var docxFileName = _attestationSheetService.BuildFileName(practice);
            var pdf = await ConvertDocxToPdfAsync(_attestationSheetService.BuildDocx(practice), docxFileName);
            return File(pdf, "application/pdf", _attestationSheetService.BuildPdfFileName(practice));
        }
        catch (InvalidOperationException ex)
        {
            return PdfConversionFailed(ex);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttestationArchive(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var missing = _attestationSheetService.Validate(practice);
        if (missing.Count > 0)
        {
            return BadRequest(new
            {
                message = "Архив аттестационного листа нельзя сформировать: заполнены не все обязательные реквизиты.",
                missing
            });
        }

        using var archiveStream = new MemoryStream();
        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var docxFileName = _attestationSheetService.BuildFileName(practice);
            var docx = _attestationSheetService.BuildDocx(practice);
            byte[] pdf;
            try
            {
                pdf = await ConvertDocxToPdfAsync(docx, docxFileName);
            }
            catch (InvalidOperationException ex)
            {
                return PdfConversionFailed(ex);
            }

            AddZipEntry(archive, docxFileName, docx);
            AddZipEntry(archive, _attestationSheetService.BuildPdfFileName(practice), pdf);
        }

        return File(
            archiveStream.ToArray(),
            "application/zip",
            _attestationSheetService.BuildArchiveFileName(practice));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPracticeReport(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var result = await _practiceReportDocumentService.BuildDocxAsync(
            practice,
            attachmentId => _studentApiService.DownloadDiaryAttachmentAsync(token, attachmentId),
            appendixId => _studentApiService.DownloadAppendixAsync(token, appendixId));

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = "Отчет нельзя сформировать: заполнены не все обязательные разделы.",
                missing = result.Missing
            });
        }

        return File(
            result.Content,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            result.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPracticeReportPdf(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var result = await _practiceReportDocumentService.BuildDocxAsync(
            practice,
            attachmentId => _studentApiService.DownloadDiaryAttachmentAsync(token, attachmentId),
            appendixId => _studentApiService.DownloadAppendixAsync(token, appendixId));

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = "PDF отчёта нельзя сформировать: заполнены не все обязательные разделы.",
                missing = result.Missing
            });
        }

        try
        {
            var pdf = await ConvertDocxToPdfAsync(result.Content, result.FileName);
            return File(pdf, "application/pdf", Path.ChangeExtension(result.FileName, ".pdf"));
        }
        catch (InvalidOperationException ex)
        {
            return PdfConversionFailed(ex);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPracticeDiaryPreview(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var missing = _practiceDiaryDocumentService.Validate(practice);
        if (missing.Count > 0)
        {
            return Json(new
            {
                missing
            });
        }

        return Json(new
        {
            fileName = _practiceDiaryDocumentService.BuildFileName(practice),
            pdfFileName = _practiceDiaryDocumentService.BuildPdfFileName(practice),
            previewUrl = Url.Action(nameof(PreviewPracticeDiaryPdf), "Student", new { assignmentId }),
            docxUrl = Url.Action(nameof(DownloadPracticeDiary), "Student", new { assignmentId }),
            pdfUrl = Url.Action(nameof(DownloadPracticeDiaryPdf), "Student", new { assignmentId }),
            missing
        });
    }

    [HttpGet]
    public async Task<IActionResult> PreviewPracticeDiaryPdf(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var missing = _practiceDiaryDocumentService.Validate(practice);
        if (missing.Count > 0)
            return BadRequest(new { message = "Дневник практики нельзя показать: заполнены не все обязательные данные.", missing });

        var docxResult = await _practiceDiaryDocumentService.BuildDocxAsync(practice);
        if (!docxResult.Success)
            return BadRequest(new { message = "Дневник практики нельзя показать: заполнены не все обязательные данные.", missing = docxResult.Missing });

        try
        {
            var pdf = await ConvertDocxToPdfAsync(docxResult.Content, docxResult.FileName);
            return File(pdf, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            return PdfConversionFailed(ex);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPracticeDiary(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var result = await _practiceDiaryDocumentService.BuildDocxAsync(practice);
        if (!result.Success)
        {
            return BadRequest(new
            {
                message = "Дневник практики нельзя сформировать: заполнены не все обязательные данные.",
                missing = result.Missing
            });
        }

        return File(
            result.Content,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            result.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPracticeDiaryPdf(int assignmentId)
    {
        var token = GetToken();
        if (token is null)
            return Unauthorized();

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        var missing = _practiceDiaryDocumentService.Validate(practice);
        if (missing.Count > 0)
        {
            return BadRequest(new
            {
                message = "PDF дневника практики нельзя сформировать: заполнены не все обязательные данные.",
                missing
            });
        }

        var docxResult = await _practiceDiaryDocumentService.BuildDocxAsync(practice);
        if (!docxResult.Success)
        {
            return BadRequest(new
            {
                message = "PDF дневника практики нельзя сформировать: заполнены не все обязательные данные.",
                missing = docxResult.Missing
            });
        }

        try
        {
            var pdf = await ConvertDocxToPdfAsync(docxResult.Content, docxResult.FileName);
            return File(pdf, "application/pdf", _practiceDiaryDocumentService.BuildPdfFileName(practice));
        }
        catch (InvalidOperationException ex)
        {
            return PdfConversionFailed(ex);
        }
    }

    private string? GetToken()
    {
        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrWhiteSpace(token))
            return token;

        var authorization = HttpContext.Request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";
        return authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[bearerPrefix.Length..].Trim()
            : null;
    }

    private Task<byte[]> ConvertDocxToPdfAsync(byte[] docxContent, string docxFileName)
    {
        return _docxPdfConversionService.ConvertDocxToPdfAsync(
            docxContent,
            docxFileName,
            HttpContext.RequestAborted);
    }

    private ObjectResult PdfConversionFailed(InvalidOperationException exception)
    {
        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            message = exception.Message
        });
    }

    private static void AddZipEntry(ZipArchive archive, string fileName, byte[] content)
    {
        var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
        using var entryStream = entry.Open();
        entryStream.Write(content, 0, content.Length);
    }

    private IActionResult ToJsonResult<T>(StudentApiResult<T> result)
    {
        if (result.Success && result.Data is not null)
            return Json(result.Data);

        return BadRequest(new
        {
            message = result.ErrorMessage ?? "Не удалось выполнить действие.",
            errors = result.ValidationErrors
        });
    }
}
