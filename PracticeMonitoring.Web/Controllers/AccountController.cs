using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models;
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
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authApiService.LoginAsync(model);

        if (!result.Success || result.Data is null)
        {
            ApplyApiErrorsToModelState(result.ValidationErrors);

            ViewBag.Error = result.StatusCode switch
            {
                400 => result.ErrorMessage ?? "Проверьте корректность введённых данных.",
                401 => result.ErrorMessage ?? "Неверный email или пароль.",
                403 => result.ErrorMessage ?? "Доступ запрещён.",
                _ => result.ErrorMessage ?? "Не удалось выполнить вход."
            };

            return View(model);
        }

        HttpContext.Session.SetString("Token", result.Data.Token);
        HttpContext.Session.SetString("FullName", result.Data.FullName);
        HttpContext.Session.SetString("Role", result.Data.Role);

        if (result.Data.Role == "Admin")
            return RedirectToAction("Index", "Admin");

        if (result.Data.Role == "Student")
            return RedirectToAction("Index", "Student");

        return RedirectToAction("Profile");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authApiService.RegisterAsync(model);

        if (!result.Success || result.Data is null)
        {
            ApplyApiErrorsToModelState(result.ValidationErrors);

            ViewBag.Error = result.StatusCode switch
            {
                400 => result.ErrorMessage ?? "Проверьте данные формы регистрации.",
                401 => result.ErrorMessage ?? "Недостаточно прав для выполнения операции.",
                409 => result.ErrorMessage ?? "Пользователь с такими данными уже существует.",
                _ => result.ErrorMessage ?? "Не удалось выполнить регистрацию."
            };

            return View(model);
        }

        HttpContext.Session.SetString("Token", result.Data.Token);
        HttpContext.Session.SetString("FullName", result.Data.FullName);
        HttpContext.Session.SetString("Role", result.Data.Role);

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
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        return View(user);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    private void ApplyApiErrorsToModelState(Dictionary<string, string[]> validationErrors)
    {
        foreach (var pair in validationErrors)
        {
            foreach (var error in pair.Value)
            {
                ModelState.AddModelError(pair.Key, error);
            }
        }
    }
}