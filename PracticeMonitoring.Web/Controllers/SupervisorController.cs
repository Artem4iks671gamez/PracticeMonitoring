using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models.Messaging;
using PracticeMonitoring.Web.Models.Supervisor;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class SupervisorController : Controller
{
    private readonly AuthApiService _authApiService;
    private readonly ChatApiService _chatApiService;
    private readonly NotificationApiService _notificationApiService;
    private readonly SupervisorApiService _supervisorApiService;

    public SupervisorController(
        AuthApiService authApiService,
        ChatApiService chatApiService,
        NotificationApiService notificationApiService,
        SupervisorApiService supervisorApiService)
    {
        _authApiService = authApiService;
        _chatApiService = chatApiService;
        _notificationApiService = notificationApiService;
        _supervisorApiService = supervisorApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Supervisor")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        var userTask = _authApiService.GetCurrentUserAsync(token);
        var threadsTask = _chatApiService.GetThreadsAsync(token);
        var notificationsTask = _notificationApiService.GetNotificationsAsync(token);
        var dashboardTask = _supervisorApiService.GetDashboardAsync(token);

        await Task.WhenAll(userTask, threadsTask, notificationsTask, dashboardTask);

        var user = userTask.Result;
        if (user is null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Theme", user.Theme ?? "light");

        return View(new SupervisorPageViewModel
        {
            CurrentUser = user,
            Dashboard = dashboardTask.Result,
            Messaging = new MessagingWorkspaceViewModel
            {
                CurrentUserId = user.Id,
                CurrentUserRole = user.Role,
                CurrentUserFullName = user.FullName,
                CurrentUserAvatarUrl = user.AvatarUrl,
                Threads = threadsTask.Result
            },
            Notifications = notificationsTask.Result
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAssignmentDetails(int assignmentId)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var details = await _supervisorApiService.GetAssignmentDetailsAsync(token, assignmentId);
        if (details is null)
            return NotFound();

        return Json(details);
    }

    [HttpPost]
    public async Task<IActionResult> ReviewDiaryEntry(
        int entryId,
        [FromBody] SupervisorDiaryReviewRequestViewModel? model)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        if (model is null)
            return BadRequest(new { message = "Не удалось прочитать оценку." });

        var details = await _supervisorApiService.ReviewDiaryEntryAsync(token, entryId, model);
        if (details is null)
            return BadRequest(new { message = "Не удалось сохранить проверку дня." });

        return Json(details);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSectionComment(
        int assignmentId,
        string sectionKey,
        [FromBody] SupervisorSectionCommentRequestViewModel? model)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        if (model is null)
            return BadRequest(new { message = "Не удалось прочитать комментарий." });

        var details = await _supervisorApiService.SaveSectionCommentAsync(token, assignmentId, sectionKey, model);
        if (details is null)
            return BadRequest(new { message = "Не удалось сохранить комментарий к разделу." });

        return Json(details);
    }

    [HttpGet]
    public async Task<IActionResult> OpenAppendix(int appendixId)
    {
        var file = await GetAppendixFile(appendixId);
        if (file is null)
            return NotFound();

        return File(file.Content, file.ContentType);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAppendix(int appendixId)
    {
        var file = await GetAppendixFile(appendixId);
        if (file is null)
            return NotFound();

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> OpenDiaryAttachment(int attachmentId)
    {
        var file = await GetDiaryAttachmentFile(attachmentId);
        if (file is null)
            return NotFound();

        return File(file.Content, file.ContentType);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDiaryAttachment(int attachmentId)
    {
        var file = await GetDiaryAttachmentFile(attachmentId);
        if (file is null)
            return NotFound();

        return File(file.Content, file.ContentType, file.FileName);
    }

    private async Task<SupervisorFileResult?> GetAppendixFile(int appendixId)
    {
        var token = HttpContext.Session.GetString("Token");
        return string.IsNullOrWhiteSpace(token)
            ? null
            : await _supervisorApiService.DownloadAppendixAsync(token, appendixId);
    }

    private async Task<SupervisorFileResult?> GetDiaryAttachmentFile(int attachmentId)
    {
        var token = HttpContext.Session.GetString("Token");
        return string.IsNullOrWhiteSpace(token)
            ? null
            : await _supervisorApiService.DownloadDiaryAttachmentAsync(token, attachmentId);
    }
}
