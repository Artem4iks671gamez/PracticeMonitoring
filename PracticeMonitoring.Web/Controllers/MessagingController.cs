using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class MessagingController : Controller
{
    private readonly ChatApiService _chatApiService;

    public MessagingController(ChatApiService chatApiService)
    {
        _chatApiService = chatApiService;
    }

    [HttpGet]
    public async Task<IActionResult> GetThreads()
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var threads = await _chatApiService.GetThreadsAsync(token);
        return Json(threads);
    }

    [HttpGet]
    public async Task<IActionResult> SearchContacts(string? query)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var users = await _chatApiService.SearchContactsAsync(token, query);
        return Json(users);
    }

    [HttpGet]
    public async Task<IActionResult> GetThread(int id)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var thread = await _chatApiService.GetThreadAsync(token, id);
        if (thread is null)
            return NotFound();

        return Json(thread);
    }

    [HttpPost]
    public async Task<IActionResult> StartThread([FromBody] int targetUserId)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var result = await _chatApiService.StartThreadAsync(token, targetUserId);
        if (!result.Success || result.Data is null)
            return BadRequest(new { message = result.ErrorMessage ?? "Не удалось открыть диалог." });

        return Json(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int threadId, int? targetUserId, string? text, List<IFormFile>? attachments)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var result = await _chatApiService.SendMessageAsync(token, threadId, targetUserId, text, attachments);
        if (!result.Success || result.Data is null)
            return BadRequest(new { message = result.ErrorMessage ?? "Не удалось отправить сообщение." });

        return Json(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var file = await _chatApiService.DownloadAttachmentAsync(token, id);
        if (file is null)
            return NotFound();

        return File(file.Content, file.ContentType, file.FileName);
    }
}
