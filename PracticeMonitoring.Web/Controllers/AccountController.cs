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
        ViewBag.Info = TempData["LoginInfo"];
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
        HttpContext.Session.SetString("MustChangePassword", result.Data.MustChangePassword.ToString());

        if (result.Data.MustChangePassword)
            return RedirectToAction(nameof(ChangePassword));

        return RedirectByRole(result.Data.Role);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> SendRegistrationCode(RegisterViewModel model)
    {
        ModelState.Remove(nameof(RegisterViewModel.Code));

        if (!ModelState.IsValid)
            return View("Register", model);

        var result = await _authApiService.SendRegistrationCodeAsync(model);

        if (!result.Success)
        {
            ApplyApiErrorsToModelState(result.ValidationErrors);
            ViewBag.Error = result.ErrorMessage ?? "Не удалось отправить код.";
            return View("Register", model);
        }

        ViewBag.Success = "Код подтверждения отправлен на указанную почту.";
        ViewBag.CodeSent = true;
        return View("Register", model);
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code))
            ModelState.AddModelError(nameof(model.Code), "Введите код подтверждения");

        if (!ModelState.IsValid)
        {
            ViewBag.CodeSent = true;
            return View(model);
        }

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
            ViewBag.CodeSent = true;

            return View(model);
        }

        HttpContext.Session.SetString("Token", result.Data.Token);
        HttpContext.Session.SetString("FullName", result.Data.FullName);
        HttpContext.Session.SetString("Role", result.Data.Role);
        HttpContext.Session.SetString("MustChangePassword", result.Data.MustChangePassword.ToString());

        return RedirectByRole(result.Data.Role);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authApiService.ForgotPasswordAsync(model);
        if (!result.Success)
        {
            ViewBag.Error = result.ErrorMessage ?? "Не удалось отправить код восстановления.";
            return View(model);
        }

        TempData["ResetInfo"] = "Код восстановления отправлен на почту.";
        return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email)
    {
        ViewBag.Info = TempData["ResetInfo"];
        return View(new ResetPasswordViewModel { Email = email ?? string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authApiService.ResetPasswordAsync(model);
        if (!result.Success)
        {
            ApplyApiErrorsToModelState(result.ValidationErrors);
            ViewBag.Error = result.ErrorMessage ?? "Не удалось сменить пароль.";
            return View(model);
        }

        TempData["LoginInfo"] = "Пароль изменён. Теперь можно войти.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        var mustChange = bool.TryParse(HttpContext.Session.GetString("MustChangePassword"), out var value) && value;
        return View(new ChangePasswordViewModel { MustChangePassword = mustChange });
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        if (!model.MustChangePassword && string.IsNullOrWhiteSpace(model.CurrentPassword))
            ModelState.AddModelError(nameof(model.CurrentPassword), "Введите текущий пароль");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _authApiService.ChangePasswordAsync(token, model);
        if (!result.Success)
        {
            ApplyApiErrorsToModelState(result.ValidationErrors);
            ViewBag.Error = result.ErrorMessage ?? "Не удалось сменить пароль.";
            return View(model);
        }

        HttpContext.Session.SetString("MustChangePassword", "False");
        return RedirectByRole(HttpContext.Session.GetString("Role") ?? string.Empty);
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

        if (user.MustChangePassword)
            return RedirectToAction(nameof(ChangePassword));

        return View(user);
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    private IActionResult RedirectByRole(string role)
    {
        return role switch
        {
            "Admin" => RedirectToAction("Index", "Admin"),
            "Student" => RedirectToAction("Index", "Student"),
            "DepartmentStaff" => RedirectToAction("Index", "DepartmentStaff"),
            "Supervisor" => RedirectToAction("Index", "Supervisor"),
            _ => RedirectToAction("Profile")
        };
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
