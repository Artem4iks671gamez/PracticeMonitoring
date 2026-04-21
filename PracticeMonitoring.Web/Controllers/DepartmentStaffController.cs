using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models.Admin;
using PracticeMonitoring.Web.Models.DepartmentStaff;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class DepartmentStaffController : Controller
{
    private readonly DepartmentStaffApiService _departmentStaffApiService;

    public DepartmentStaffController(DepartmentStaffApiService departmentStaffApiService)
    {
        _departmentStaffApiService = departmentStaffApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "DepartmentStaff" && role != "Admin")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        var model = new DepartmentStaffPageViewModel
        {
            FullName = HttpContext.Session.GetString("FullName") ?? "Работник отдела",
            Practices = await _departmentStaffApiService.GetPracticesAsync(token)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePractice(DepartmentStaffPracticeUpsertViewModel model)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "DepartmentStaff" && role != "Admin")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        AdminApiResult<DepartmentStaffPracticeDetailsViewModel> result;

        if (model.Id.HasValue && model.Id.Value > 0)
        {
            result = await _departmentStaffApiService.UpdatePracticeAsync(token, model.Id.Value, model);
        }
        else
        {
            result = await _departmentStaffApiService.CreatePracticeAsync(token, model);
        }

        TempData[result.Success ? "DepartmentStaffSuccess" : "DepartmentStaffError"] =
            result.Success
                ? "Производственная практика успешно сохранена."
                : (result.ErrorMessage ?? "Не удалось сохранить производственную практику.");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePractice(int id)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "DepartmentStaff" && role != "Admin")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        var result = await _departmentStaffApiService.DeletePracticeAsync(token, id);

        TempData[result.Success ? "DepartmentStaffSuccess" : "DepartmentStaffError"] =
            result.Success
                ? "Производственная практика удалена."
                : (result.ErrorMessage ?? "Не удалось удалить производственную практику.");

        return RedirectToAction(nameof(Index));
    }
}