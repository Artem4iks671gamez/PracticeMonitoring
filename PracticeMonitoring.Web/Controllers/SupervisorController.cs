using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models.Messaging;
using PracticeMonitoring.Web.Models.Supervisor;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class SupervisorController : Controller
{
    private readonly AuthApiService _authApiService;
    private readonly ChatApiService _chatApiService;

    public SupervisorController(AuthApiService authApiService, ChatApiService chatApiService)
    {
        _authApiService = authApiService;
        _chatApiService = chatApiService;
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

        await Task.WhenAll(userTask, threadsTask);

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
}
