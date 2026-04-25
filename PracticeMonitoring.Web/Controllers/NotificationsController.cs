using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class NotificationsController : Controller
{
    private readonly NotificationApiService _notificationApiService;

    public NotificationsController(NotificationApiService notificationApiService)
    {
        _notificationApiService = notificationApiService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrWhiteSpace(token))
            await _notificationApiService.MarkAsReadAsync(token, id);

        return RedirectBack();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var token = HttpContext.Session.GetString("Token");
        if (!string.IsNullOrWhiteSpace(token))
            await _notificationApiService.MarkAllAsReadAsync(token);

        return RedirectBack();
    }

    private IActionResult RedirectBack()
    {
        var referer = Request.Headers.Referer.ToString();
        return string.IsNullOrWhiteSpace(referer)
            ? RedirectToAction("Login", "Account")
            : Redirect(referer);
    }
}
