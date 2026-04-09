using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models.Admin;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class AdminController : Controller
{
    private readonly AdminApiService _adminApiService;

    public AdminController(AdminApiService adminApiService)
    {
        _adminApiService = adminApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        var model = new AdminLogsPageViewModel
        {
            AdminFullName = HttpContext.Session.GetString("FullName") ?? "Администратор",
            RegisteredUsersLogs = await _adminApiService.GetRegisteredUsersLogsAsync(token),
            AdminActionsLogs = await _adminApiService.GetAdminActionsLogsAsync(token),
            UserProfileChangesLogs = await _adminApiService.GetUserProfileChangesLogsAsync(token)
        };

        return View(model);
    }
}