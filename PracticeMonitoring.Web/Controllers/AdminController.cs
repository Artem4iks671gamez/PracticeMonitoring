using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models.Admin;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class AdminController : Controller
{
    private readonly AdminApiService _adminApiService;
    private readonly IWebHostEnvironment _environment;

    public AdminController(AdminApiService adminApiService, IWebHostEnvironment environment)
    {
        _adminApiService = adminApiService;
        _environment = environment;
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

        var model = new AdminUsersPageViewModel
        {
            AdminFullName = HttpContext.Session.GetString("FullName") ?? "Администратор",
            RegisteredUsersLogs = await _adminApiService.GetRegisteredUsersLogsAsync(token),
            AdminActionsLogs = await _adminApiService.GetAdminActionsLogsAsync(token),
            UserProfileChangesLogs = await _adminApiService.GetUserProfileChangesLogsAsync(token),
            Users = await _adminApiService.GetUsersAsync(token)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUser(AdminSaveUserViewModel model)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

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

        var requestModel = new
        {
            Surname = model.Surname,
            FirstName = model.FirstName,
            Patronymic = model.Patronymic,
            Email = model.Email,
            Role = model.Role,
            GroupId = model.Role == "Student" ? model.GroupId : null,
            AvatarUrl = avatarUrl,
            RemoveAvatar = model.RemoveAvatar,
            IsActive = model.IsActive,
            Password = model.Password
        };

        AdminApiResult<AdminUserItemViewModel> result;

        if (model.IsCreateMode)
        {
            result = model.Role switch
            {
                "Admin" => await _adminApiService.CreateAdminAsync(token, requestModel),
                "Supervisor" => await _adminApiService.CreateSupervisorAsync(token, requestModel),
                "DepartmentStaff" => await _adminApiService.CreateDepartmentStaffAsync(token, requestModel),
                _ => new AdminApiResult<AdminUserItemViewModel>
                {
                    Success = false,
                    ErrorMessage = "Создание пользователей этой роли пока не поддерживается."
                }
            };
        }
        else
        {
            if (!model.Id.HasValue)
            {
                TempData["AdminError"] = "Не указан идентификатор пользователя.";
                return RedirectToAction(nameof(Index));
            }

            result = await _adminApiService.UpdateUserAsync(token, model.Id.Value, requestModel);
        }

        TempData[result.Success ? "AdminSuccess" : "AdminError"] =
            result.Success
                ? (model.IsCreateMode ? "Пользователь успешно создан." : "Пользователь успешно обновлён.")
                : (result.ErrorMessage ?? "Не удалось выполнить операцию.");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadLogs(string category)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        var file = await _adminApiService.DownloadLogsAsync(token, category);
        if (file is null)
        {
            TempData["AdminError"] = "Не удалось выгрузить лог.";
            return RedirectToAction(nameof(Index));
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BackupDatabase()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        var file = await _adminApiService.BackupDatabaseAsync(token);
        if (file is null)
        {
            TempData["AdminError"] = "Не удалось создать резервную копию базы данных.";
            return RedirectToAction(nameof(Index));
        }

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreDatabase(IFormFile backupFile)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        if (backupFile is null || backupFile.Length == 0)
        {
            TempData["AdminError"] = "Выберите файл резервной копии.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _adminApiService.RestoreDatabaseAsync(token, backupFile);

        TempData[result.Success ? "AdminSuccess" : "AdminError"] =
            result.Success
                ? "База данных успешно восстановлена."
                : (result.ErrorMessage ?? "Не удалось восстановить базу данных.");

        return RedirectToAction(nameof(Index));
    }
}