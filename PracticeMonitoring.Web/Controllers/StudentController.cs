using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class StudentController : Controller
{
    private readonly AuthApiService _authApiService;

    public StudentController(AuthApiService authApiService)
    {
        _authApiService = authApiService;
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

        var user = await _authApiService.GetCurrentUserAsync(token);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        return View(user);
    }
}