using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models.Auth;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class AccountController : Controller
{
    private readonly AuthApiService _authApiService;

    public AccountController(AuthApiService authApiService)
    {
        _authApiService = authApiService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authApiService.LoginAsync(model);
        
        if (result is null)
        {
            ViewBag.Error = "Неверный логин или пароль";
            return View(model);
        }

        HttpContext.Session.SetString("Token", result.Token);
        HttpContext.Session.SetString("FullName", result.FullName);
        HttpContext.Session.SetString("Role", result.Role);

        if (result.Role == "Admin")
            return RedirectToAction("Index", "Admin");

        if (result.Role == "Student")
            return RedirectToAction("Index", "Student");

        return RedirectToAction("Profile");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authApiService.RegisterAsync(model);

        if (result is null)
        {
            ViewBag.Error = "Ошибка регистрации";
            return View(model);
        }

        HttpContext.Session.SetString("Token", result.Token);
        HttpContext.Session.SetString("FullName", result.FullName);
        HttpContext.Session.SetString("Role", result.Role);

        return RedirectToAction("Profile");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var token = HttpContext.Session.GetString("Token");

        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        var user = await _authApiService.GetCurrentUserAsync(token);

        if (user is null)
            return RedirectToAction("Login");

        return View(user);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}