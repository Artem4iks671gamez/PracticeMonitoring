using Microsoft.AspNetCore.Mvc;

namespace PracticeMonitoring.Web.Controllers;

public class StudentController : Controller
{
    public IActionResult Index()
    {
        var role = HttpContext.Session.GetString("Role");

        if (role != "Student")
            return RedirectToAction("Login", "Account");

        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Role = role;

        return View();
    }
}