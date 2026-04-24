using Microsoft.AspNetCore.Mvc;
using PracticeMonitoring.Web.Models.DepartmentStaff;
using PracticeMonitoring.Web.Services;

namespace PracticeMonitoring.Web.Controllers;

public class DepartmentStaffController : Controller
{
    private readonly DepartmentStaffApiService _departmentStaffApiService;
    private readonly AttestationSheetService _attestationSheetService;

    public DepartmentStaffController(
        DepartmentStaffApiService departmentStaffApiService,
        AttestationSheetService attestationSheetService)
    {
        _departmentStaffApiService = departmentStaffApiService;
        _attestationSheetService = attestationSheetService;
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

        var practicesTask = _departmentStaffApiService.GetPracticesAsync(token);
        var practiceLogsTask = _departmentStaffApiService.GetPracticeChangeLogsAsync(token);
        var assignmentLogsTask = _departmentStaffApiService.GetAssignmentChangeLogsAsync(token);

        await Task.WhenAll(practicesTask, practiceLogsTask, assignmentLogsTask);

        var model = new DepartmentStaffPageViewModel
        {
            FullName = HttpContext.Session.GetString("FullName") ?? "Работник отдела",
            Practices = practicesTask.Result,
            PracticeChangeLogs = practiceLogsTask.Result,
            AssignmentChangeLogs = assignmentLogsTask.Result
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
    public async Task<IActionResult> GetFormData(int? specialtyId = null, bool includeAllStudents = false)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var model = new DepartmentStaffFormDataViewModel
        {
            Specialties = await _departmentStaffApiService.GetSpecialtiesAsync(token),
            Supervisors = await _departmentStaffApiService.GetSupervisorsAsync(token),
            Students = includeAllStudents || (specialtyId.HasValue && specialtyId.Value > 0)
                ? await _departmentStaffApiService.GetStudentsAsync(token, specialtyId)
                : new List<DepartmentStaffStudentOptionViewModel>()
        };

        return Json(model);
    }

    [HttpPost]
    public async Task<IActionResult> SavePractice([FromBody] DepartmentStaffPracticeUpsertViewModel? model)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        if (model is null)
        {
            return BadRequest(new
            {
                message = "Исправьте ошибки формы.",
                errors = new Dictionary<string, string[]>
                {
                    ["PracticeIndex"] = new[] { "Введите индекс ПП." },
                    ["Name"] = new[] { "Введите название практики." },
                    ["SpecialtyId"] = new[] { "Выберите специальность." },
                    ["ProfessionalModuleCode"] = new[] { "Введите код профессионального модуля." },
                    ["ProfessionalModuleName"] = new[] { "Введите название профессионального модуля." },
                    ["Hours"] = new[] { "Укажите количество часов." },
                    ["StartDate"] = new[] { "Укажите дату начала." },
                    ["EndDate"] = new[] { "Укажите дату окончания." },
                    ["Competencies"] = new[] { "Добавьте хотя бы одну профессиональную компетенцию." }
                }
            });
        }

        if (!TryValidateModel(model))
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => NormalizeModelStateKey(x.Key),
                    x => x.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Некорректное значение." : e.ErrorMessage)
                        .ToArray());

            return BadRequest(new
            {
                message = "Исправьте ошибки формы.",
                errors
            });
        }

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
    public async Task<IActionResult> SavePracticeAssignments([FromBody] DepartmentStaffPracticeAssignmentsUpsertViewModel? model)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        if (model is null)
        {
            return BadRequest(new
            {
                message = "Исправьте ошибки назначения студентов.",
                errors = new Dictionary<string, string[]>
                {
                    ["StudentAssignments"] = new[] { "Не удалось прочитать назначения студентов." }
                }
            });
        }

        if (!TryValidateModel(model))
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => NormalizeModelStateKey(x.Key),
                    x => x.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Некорректное значение." : e.ErrorMessage)
                        .ToArray());

            return BadRequest(new
            {
                message = "Исправьте ошибки назначения студентов.",
                errors
            });
        }

        var result = await _departmentStaffApiService.SavePracticeAssignmentsAsync(token, model);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = result.ErrorMessage ?? "Не удалось сохранить назначения студентов.",
                errors = result.ValidationErrors
            });
        }

        return Ok(new { message = "Назначения студентов сохранены." });
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

    [HttpGet]
    public async Task<IActionResult> DownloadLogs(string category)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "DepartmentStaff" && role != "Admin")
            return RedirectToAction("Login", "Account");

        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        var file = await _departmentStaffApiService.DownloadLogsAsync(token, category);
        if (file is null)
            return RedirectToAction(nameof(Index));

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> PreviewAttestation(int id)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var practice = await _departmentStaffApiService.GetPracticeByIdAsync(token, id);
        if (practice is null)
            return NotFound();

        var html = _attestationSheetService.BuildPreviewHtml(practice);

        return Json(new
        {
            html,
            fileName = _attestationSheetService.BuildFileName(practice)
        });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttestation(int id)
    {
        var token = HttpContext.Session.GetString("Token");
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized();

        var practice = await _departmentStaffApiService.GetPracticeByIdAsync(token, id);
        if (practice is null)
            return NotFound();

        var bytes = _attestationSheetService.BuildDocx(practice);
        var fileName = _attestationSheetService.BuildFileName(practice);

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }

    private static string NormalizeModelStateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return key;

        const string modelPrefix = "model.";
        return key.StartsWith(modelPrefix, StringComparison.OrdinalIgnoreCase)
            ? key[modelPrefix.Length..]
            : key;
    }
}
