using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models.Auth;
using PracticeMonitoring.Web.Models.Messaging;
using PracticeMonitoring.Web.Models.Student;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class StudentController : Controller
{
    private readonly AuthApiService _authApiService;
    private readonly ChatApiService _chatApiService;
    private readonly IWebHostEnvironment _environment;

    public StudentController(
        AuthApiService authApiService,
        ChatApiService chatApiService,
        IWebHostEnvironment environment)
    {
        _authApiService = authApiService;
        _chatApiService = chatApiService;
        _environment = environment;
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

        await Task.WhenAll(userTask, threadsTask);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Student")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return RedirectToAction("Index");

        string? avatarUrl = model.CurrentAvatarUrl;

        if (model.AvatarFile is not null && model.AvatarFile.Length > 0)
        {
            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploadsRoot);

            var extension = Path.GetExtension(model.AvatarFile.FileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await model.AvatarFile.CopyToAsync(stream);

            avatarUrl = $"/uploads/avatars/{fileName}";
        }

        var result = await _authApiService.UpdateProfileAsync(token, new Services.UpdateProfileRequest
        {
            Surname = model.Surname,
            FirstName = model.FirstName,
            Patronymic = model.Patronymic,
            Email = model.Email,
            AvatarUrl = avatarUrl,
            Theme = model.Theme
        });

        if (!result.Success || result.Data is null)
        {
            TempData["ProfileError"] = result.ErrorMessage ?? "Не удалось сохранить изменения профиля.";
            return RedirectToAction("Index");
        }

        HttpContext.Session.SetString("FullName", result.Data.FullName);
        HttpContext.Session.SetString("Theme", result.Data.Theme);

        TempData["ProfileSuccess"] = "Данные профиля успешно обновлены.";
        return RedirectToAction("Index");
    }
}
