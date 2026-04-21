using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos.DepartmentStaff;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/department-staff/practices")]
[Authorize(Roles = "DepartmentStaff,Admin")]
public class DepartmentStaffPracticesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DepartmentStaffPracticesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductionPracticeListItemResponse>>> GetAll()
    {
        var items = await _context.ProductionPractices
            .Include(x => x.Specialty)
            .Include(x => x.StudentAssignments)
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.PracticeIndex)
            .Select(x => new ProductionPracticeListItemResponse
            {
                Id = x.Id,
                PracticeIndex = x.PracticeIndex,
                Name = x.Name,
                SpecialtyId = x.SpecialtyId,
                SpecialtyCode = x.Specialty.Code,
                SpecialtyName = x.Specialty.Name,
                Hours = x.Hours,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                AssignedStudentsCount = x.StudentAssignments.Count
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductionPracticeDetailsResponse>> GetById(int id)
    {
        var practice = await _context.ProductionPractices
            .Include(x => x.Specialty)
            .Include(x => x.Competencies)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Student)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Supervisor)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (practice is null)
            return NotFound();

        return Ok(MapDetails(practice));
    }

    [HttpPost]
    public async Task<ActionResult<ProductionPracticeDetailsResponse>> Create(ProductionPracticeUpsertRequest request)
    {
        var specialty = await _context.Specialties.FirstOrDefaultAsync(x => x.Id == request.SpecialtyId);
        if (specialty is null)
            return BadRequest(new { message = "Выбранная специальность не найдена." });

        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "Дата окончания не может быть раньше даты начала." });

        var validationError = await ValidateAssignmentsAsync(request.StudentAssignments, specialty.Id);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var practice = new ProductionPractice
        {
            PracticeIndex = request.PracticeIndex.Trim(),
            Name = request.Name.Trim(),
            SpecialtyId = specialty.Id,
            Specialty = specialty,
            ProfessionalModuleCode = request.ProfessionalModuleCode.Trim(),
            ProfessionalModuleName = request.ProfessionalModuleName.Trim(),
            Hours = request.Hours,
            StartDate = EnsureUtc(request.StartDate),
            EndDate = EnsureUtc(request.EndDate)
        };

        practice.Competencies = request.Competencies.Select(x => new ProductionPracticeCompetency
        {
            CompetencyCode = x.CompetencyCode.Trim(),
            CompetencyDescription = x.CompetencyDescription.Trim(),
            WorkTypes = x.WorkTypes.Trim(),
            Hours = x.Hours
        }).ToList();

        practice.StudentAssignments = await BuildAssignmentsAsync(request.StudentAssignments);

        _context.ProductionPractices.Add(practice);
        await _context.SaveChangesAsync();

        practice = await _context.ProductionPractices
            .Include(x => x.Specialty)
            .Include(x => x.Competencies)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Student)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Supervisor)
            .FirstAsync(x => x.Id == practice.Id);

        return Ok(MapDetails(practice));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductionPracticeDetailsResponse>> Update(int id, ProductionPracticeUpsertRequest request)
    {
        var practice = await _context.ProductionPractices
            .Include(x => x.Competencies)
            .Include(x => x.StudentAssignments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (practice is null)
            return NotFound();

        var specialty = await _context.Specialties.FirstOrDefaultAsync(x => x.Id == request.SpecialtyId);
        if (specialty is null)
            return BadRequest(new { message = "Выбранная специальность не найдена." });

        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "Дата окончания не может быть раньше даты начала." });

        var validationError = await ValidateAssignmentsAsync(request.StudentAssignments, specialty.Id);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        practice.PracticeIndex = request.PracticeIndex.Trim();
        practice.Name = request.Name.Trim();
        practice.SpecialtyId = specialty.Id;
        practice.ProfessionalModuleCode = request.ProfessionalModuleCode.Trim();
        practice.ProfessionalModuleName = request.ProfessionalModuleName.Trim();
        practice.Hours = request.Hours;
        practice.StartDate = EnsureUtc(request.StartDate);
        practice.EndDate = EnsureUtc(request.EndDate);

        _context.ProductionPracticeCompetencies.RemoveRange(practice.Competencies);
        _context.ProductionPracticeStudentAssignments.RemoveRange(practice.StudentAssignments);

        practice.Competencies = request.Competencies.Select(x => new ProductionPracticeCompetency
        {
            ProductionPracticeId = practice.Id,
            CompetencyCode = x.CompetencyCode.Trim(),
            CompetencyDescription = x.CompetencyDescription.Trim(),
            WorkTypes = x.WorkTypes.Trim(),
            Hours = x.Hours
        }).ToList();

        practice.StudentAssignments = await BuildAssignmentsAsync(request.StudentAssignments, practice.Id);

        await _context.SaveChangesAsync();

        practice = await _context.ProductionPractices
            .Include(x => x.Specialty)
            .Include(x => x.Competencies)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Student)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Supervisor)
            .FirstAsync(x => x.Id == id);

        return Ok(MapDetails(practice));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var practice = await _context.ProductionPractices
            .Include(x => x.Competencies)
            .Include(x => x.StudentAssignments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (practice is null)
            return NotFound();

        _context.ProductionPracticeCompetencies.RemoveRange(practice.Competencies);
        _context.ProductionPracticeStudentAssignments.RemoveRange(practice.StudentAssignments);
        _context.ProductionPractices.Remove(practice);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Производственная практика удалена." });
    }

    private async Task<string?> ValidateAssignmentsAsync(
        List<ProductionPracticeStudentAssignmentRequest> assignments,
        int specialtyId)
    {
        foreach (var assignment in assignments)
        {
            var student = await _context.Users
                .Include(x => x.Role)
                .Include(x => x.Group)
                .FirstOrDefaultAsync(x => x.Id == assignment.StudentId);

            if (student is null || student.Role.Name != "Student")
                return "Один из выбранных пользователей не является студентом.";

            if (student.Group is null || student.Group.SpecialtyId != specialtyId)
                return $"Студент {student.FullName} не относится к выбранной специальности.";

            if (assignment.SupervisorId.HasValue)
            {
                var supervisor = await _context.Users
                    .Include(x => x.Role)
                    .FirstOrDefaultAsync(x => x.Id == assignment.SupervisorId.Value);

                if (supervisor is null || supervisor.Role.Name != "Supervisor")
                    return "Один из выбранных руководителей не имеет роль Supervisor.";
            }
        }

        return null;
    }

    private async Task<List<ProductionPracticeStudentAssignment>> BuildAssignmentsAsync(
        List<ProductionPracticeStudentAssignmentRequest> assignments,
        int? practiceId = null)
    {
        var result = new List<ProductionPracticeStudentAssignment>();

        foreach (var assignment in assignments)
        {
            var student = await _context.Users.FirstAsync(x => x.Id == assignment.StudentId);
            User? supervisor = null;

            if (assignment.SupervisorId.HasValue)
                supervisor = await _context.Users.FirstAsync(x => x.Id == assignment.SupervisorId.Value);

            result.Add(new ProductionPracticeStudentAssignment
            {
                ProductionPracticeId = practiceId ?? 0,
                StudentId = student.Id,
                Student = student,
                SupervisorId = supervisor?.Id,
                Supervisor = supervisor
            });
        }

        return result;
    }

    private static ProductionPracticeDetailsResponse MapDetails(ProductionPractice x)
    {
        return new ProductionPracticeDetailsResponse
        {
            Id = x.Id,
            PracticeIndex = x.PracticeIndex,
            Name = x.Name,
            SpecialtyId = x.SpecialtyId,
            SpecialtyCode = x.Specialty.Code,
            SpecialtyName = x.Specialty.Name,
            ProfessionalModuleCode = x.ProfessionalModuleCode,
            ProfessionalModuleName = x.ProfessionalModuleName,
            Hours = x.Hours,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            Competencies = x.Competencies
                .OrderBy(c => c.Id)
                .Select(c => new ProductionPracticeCompetencyItemResponse
                {
                    Id = c.Id,
                    CompetencyCode = c.CompetencyCode,
                    CompetencyDescription = c.CompetencyDescription,
                    WorkTypes = c.WorkTypes,
                    Hours = c.Hours
                })
                .ToList(),
            StudentAssignments = x.StudentAssignments
                .OrderBy(a => a.Student.FullName)
                .Select(a => new ProductionPracticeStudentAssignmentItemResponse
                {
                    Id = a.Id,
                    StudentId = a.StudentId,
                    StudentFullName = a.Student.FullName,
                    SupervisorId = a.SupervisorId,
                    SupervisorFullName = a.Supervisor != null ? a.Supervisor.FullName : null
                })
                .ToList()
        };
    }

    private static DateTime EnsureUtc(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
            return dt;

        if (dt.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        return dt.ToUniversalTime();
    }
}