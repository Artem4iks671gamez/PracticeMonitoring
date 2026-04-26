using Microsoft.AspNetCore.Mvc;
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

    public StudentController(
        AuthApiService authApiService,
        ChatApiService chatApiService,
        StudentApiService studentApiService,
        NotificationApiService notificationApiService,
        PracticeReportDocumentService practiceReportDocumentService)
    {
        _authApiService = authApiService;
        _chatApiService = chatApiService;
        _studentApiService = studentApiService;
        _notificationApiService = notificationApiService;
        _practiceReportDocumentService = practiceReportDocumentService;
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

        var practice = await _studentApiService.GetPracticeAsync(token, assignmentId);
        if (practice is null)
            return NotFound();

        return Json(practice);
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
        return ToJsonResult(result);
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
            attachmentId => _studentApiService.DownloadDiaryAttachmentAsync(token, attachmentId));

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

    private string? GetToken()
    {
        return HttpContext.Session.GetString("Token");
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
