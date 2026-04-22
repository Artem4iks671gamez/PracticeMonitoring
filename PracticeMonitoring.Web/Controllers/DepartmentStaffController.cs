using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models.DepartmentStaff;
using PracticeMonitoring.Web.Services;
using PracticeMonitoring.Web.Models;

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

    [HttpGet]
    public async Task<IActionResult> GetPracticeDetails(int id)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var item = await _departmentStaffApiService.GetPracticeByIdAsync(token, id);
        if (item is null)
            return NotFound();

        return Json(item);
    }

    [HttpGet]
    public async Task<IActionResult> GetFormData(int specialtyId = 0)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var model = new DepartmentStaffFormDataViewModel
        {
            Specialties = await _departmentStaffApiService.GetSpecialtiesAsync(token),
            Supervisors = await _departmentStaffApiService.GetSupervisorsAsync(token),
            Students = specialtyId > 0
                ? await _departmentStaffApiService.GetStudentsAsync(token, specialtyId)
                : new List<DepartmentStaffStudentOptionViewModel>()
        };

        return Json(model);
    }

    [HttpPost]
    public async Task<IActionResult> SavePractice([FromBody] DepartmentStaffPracticeUpsertViewModel model)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        if (model == null)
            return BadRequest(new { message = "Тело запроса пустое или JSON некорректен." });

        var result = await _departmentStaffApiService.SavePracticeAsync(token, model);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.ErrorMessage ?? "Не удалось сохранить практику.",
                errors = result.ValidationErrors
            });
        }

        return Ok(new { message = "Практика успешно сохранена." });
    }

    [HttpPost]
    public async Task<IActionResult> DeletePractice([FromBody] int id)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var result = await _departmentStaffApiService.DeletePracticeAsync(token, id);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.ErrorMessage ?? "Не удалось удалить практику.",
                errors = result.ValidationErrors
            });
        }

        return Ok(new { message = "Практика удалена." });
    }
}