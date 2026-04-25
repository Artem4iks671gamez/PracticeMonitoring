using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models.Auth;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class ProfileController : Controller
{
    private readonly AuthApiService _authApiService;
    private readonly IWebHostEnvironment _environment;

    public ProfileController(AuthApiService authApiService, IWebHostEnvironment environment)
    {
        _authApiService = authApiService;
        _environment = environment;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateProfileViewModel model)
    {
        var token = HttpContext.Session.GetString("Token");
        var role = HttpContext.Session.GetString("Role");

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(role))
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            TempData["ProfileError"] = "Проверьте данные профиля.";
            return RedirectToRoleHome(role);
        }

        var avatarUrl = model.CurrentAvatarUrl;

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

        var result = await _authApiService.UpdateProfileAsync(token, new UpdateProfileRequest
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
            return RedirectToRoleHome(role);
        }

        HttpContext.Session.SetString("FullName", result.Data.FullName);
        HttpContext.Session.SetString("Theme", result.Data.Theme);
        TempData["ProfileSuccess"] = "Данные профиля обновлены.";

        return RedirectToRoleHome(result.Data.Role);
    }

    private RedirectToActionResult RedirectToRoleHome(string role)
    {
        return role switch
        {
            "Admin" => RedirectToAction("Index", "Admin"),
            "DepartmentStaff" => RedirectToAction("Index", "DepartmentStaff"),
            "Supervisor" => RedirectToAction("Index", "Supervisor"),
            "Student" => RedirectToAction("Index", "Student"),
            _ => RedirectToAction("Login", "Account")
        };
    }
}
