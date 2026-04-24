using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos.DepartmentStaff;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/department-staff/supervisors")]
[Authorize(Roles = "DepartmentStaff,Admin")]
public class DepartmentStaffSupervisorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DepartmentStaffSupervisorsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<DepartmentStaffSupervisorListItemResponse>>> GetAll()
    {
        var items = await _context.Users
            .Include(x => x.Role)
            .Where(x => x.Role.Name == "Supervisor" && x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => new DepartmentStaffSupervisorListItemResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                AvatarUrl = x.AvatarUrl,
                AssignedStudentsCount = _context.ProductionPracticeStudentAssignments.Count(a => a.SupervisorId == x.Id),
                PracticesCount = _context.ProductionPracticeStudentAssignments
                    .Where(a => a.SupervisorId == x.Id)
                    .Select(a => a.ProductionPracticeId)
                    .Distinct()
                    .Count()
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmentStaffSupervisorDetailsResponse>> GetById(int id)
    {
        var supervisor = await _context.Users
            .Include(x => x.Role)
            .Where(x => x.Id == id && x.Role.Name == "Supervisor" && x.IsActive)
            .Select(x => new DepartmentStaffSupervisorDetailsResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                AvatarUrl = x.AvatarUrl,
                AssignedStudentsCount = _context.ProductionPracticeStudentAssignments.Count(a => a.SupervisorId == x.Id),
                PracticesCount = _context.ProductionPracticeStudentAssignments
                    .Where(a => a.SupervisorId == x.Id)
                    .Select(a => a.ProductionPracticeId)
                    .Distinct()
                    .Count()
            })
            .FirstOrDefaultAsync();

        if (supervisor is null)
            return NotFound();

        supervisor.Students = await _context.ProductionPracticeStudentAssignments
            .Where(x => x.SupervisorId == id)
            .Include(x => x.Student)
                .ThenInclude(x => x.Group)
            .Include(x => x.ProductionPractice)
            .OrderBy(x => x.Student.Group != null ? x.Student.Group.Course : 99)
            .ThenBy(x => x.Student.Group != null ? x.Student.Group.Name : "")
            .ThenBy(x => x.Student.FullName)
            .Select(x => new DepartmentStaffSupervisorStudentItemResponse
            {
                StudentId = x.StudentId,
                StudentFullName = x.Student.FullName,
                GroupName = x.Student.Group != null ? x.Student.Group.Name : null,
                Course = x.Student.Group != null ? x.Student.Group.Course : null,
                PracticeId = x.ProductionPracticeId,
                PracticeIndex = x.ProductionPractice.PracticeIndex,
                PracticeName = x.ProductionPractice.Name
            })
            .ToListAsync();

        supervisor.Practices = await _context.ProductionPracticeStudentAssignments
            .Where(x => x.SupervisorId == id)
            .Include(x => x.ProductionPractice)
                .ThenInclude(x => x.Specialty)
            .GroupBy(x => new
            {
                x.ProductionPracticeId,
                x.ProductionPractice.PracticeIndex,
                PracticeName = x.ProductionPractice.Name,
                SpecialtyCode = x.ProductionPractice.Specialty.Code,
                SpecialtyName = x.ProductionPractice.Specialty.Name
            })
            .OrderBy(x => x.Key.PracticeIndex)
            .Select(x => new DepartmentStaffSupervisorPracticeItemResponse
            {
                PracticeId = x.Key.ProductionPracticeId,
                PracticeIndex = x.Key.PracticeIndex,
                PracticeName = x.Key.PracticeName,
                SpecialtyCode = x.Key.SpecialtyCode,
                SpecialtyName = x.Key.SpecialtyName,
                StudentsCount = x.Count()
            })
            .ToListAsync();

        return Ok(supervisor);
    }
}
