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
                ProfessionalModuleCode = x.ProfessionalModuleCode,
                ProfessionalModuleName = x.ProfessionalModuleName,
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
        var practice = await LoadPracticeDetailsQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (practice is null)
            return NotFound();

        return Ok(MapDetails(practice));
    }

    [HttpGet("metadata/specialties")]
    public async Task<ActionResult<List<PracticeSelectOptionResponse>>> GetSpecialties()
    {
        var items = await _context.Specialties
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .Select(x => new PracticeSelectOptionResponse
            {
                Id = x.Id,
                Label = $"{x.Code} — {x.Name}"
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("metadata/students")]
    public async Task<ActionResult<List<PracticeStudentOptionResponse>>> GetStudents([FromQuery] int? specialtyId = null)
    {
        var query = _context.Users
            .Include(x => x.Role)
            .Include(x => x.Group)
                .ThenInclude(x => x!.Specialty)
            .Where(x =>
                x.Role.Name == "Student" &&
                x.IsActive &&
                x.Group != null &&
                x.Group.Specialty != null);

        if (specialtyId.HasValue && specialtyId.Value > 0)
        {
            query = query.Where(x => x.Group != null && x.Group.SpecialtyId == specialtyId.Value);
        }

        var items = await query
            .OrderBy(x => x.Group!.Course)
            .ThenBy(x => x.Group!.Name)
            .ThenBy(x => x.FullName)
            .Select(x => new PracticeStudentOptionResponse
            {
                Id = x.Id,
                FullName = x.FullName,
                SpecialtyId = x.Group != null ? x.Group.SpecialtyId : null,
                SpecialtyCode = x.Group != null && x.Group.Specialty != null ? x.Group.Specialty.Code : null,
                SpecialtyName = x.Group != null && x.Group.Specialty != null ? x.Group.Specialty.Name : null,
                GroupName = x.Group != null ? x.Group.Name : null,
                Course = x.Group != null ? x.Group.Course : null
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("metadata/supervisors")]
    public async Task<ActionResult<List<PracticeSupervisorOptionResponse>>> GetSupervisors()
    {
        var items = await _context.Users
            .Include(x => x.Role)
            .Where(x => x.Role.Name == "Supervisor" && x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => new PracticeSupervisorOptionResponse
            {
                Id = x.Id,
                FullName = x.FullName
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<ProductionPracticeDetailsResponse>> Create(ProductionPracticeUpsertRequest request)
    {
        var validationErrors = await ValidatePracticeRequestAsync(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new
            {
                message = "Исправьте ошибки формы.",
                errors = validationErrors
            });
        }

        var specialty = await _context.Specialties.FirstAsync(x => x.Id == request.SpecialtyId);

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

        _context.ProductionPractices.Add(practice);
        await _context.SaveChangesAsync();

        practice = await LoadPracticeDetailsQuery()
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

        var validationErrors = await ValidatePracticeRequestAsync(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new
            {
                message = "Исправьте ошибки формы.",
                errors = validationErrors
            });
        }

        var specialty = await _context.Specialties.FirstAsync(x => x.Id == request.SpecialtyId);

        practice.PracticeIndex = request.PracticeIndex.Trim();
        practice.Name = request.Name.Trim();
        practice.SpecialtyId = specialty.Id;
        practice.ProfessionalModuleCode = request.ProfessionalModuleCode.Trim();
        practice.ProfessionalModuleName = request.ProfessionalModuleName.Trim();
        practice.Hours = request.Hours;
        practice.StartDate = EnsureUtc(request.StartDate);
        practice.EndDate = EnsureUtc(request.EndDate);

        _context.ProductionPracticeCompetencies.RemoveRange(practice.Competencies);

        practice.Competencies = request.Competencies.Select(x => new ProductionPracticeCompetency
        {
            ProductionPracticeId = practice.Id,
            CompetencyCode = x.CompetencyCode.Trim(),
            CompetencyDescription = x.CompetencyDescription.Trim(),
            WorkTypes = x.WorkTypes.Trim(),
            Hours = x.Hours
        }).ToList();

        await _context.SaveChangesAsync();

        practice = await LoadPracticeDetailsQuery()
            .FirstAsync(x => x.Id == id);

        return Ok(MapDetails(practice));
    }

    [HttpPut("{id:int}/assignments")]
    public async Task<ActionResult<ProductionPracticeDetailsResponse>> UpdateAssignments(int id, ProductionPracticeAssignmentsUpsertRequest request)
    {
        var practice = await _context.ProductionPractices
            .Include(x => x.Specialty)
            .Include(x => x.StudentAssignments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (practice is null)
            return NotFound();

        var validationErrors = await ValidateAssignmentsAsync(request.StudentAssignments, practice.SpecialtyId);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new
            {
                message = "Исправьте ошибки назначения студентов.",
                errors = validationErrors
            });
        }

        _context.ProductionPracticeStudentAssignments.RemoveRange(practice.StudentAssignments);
        practice.StudentAssignments = await BuildAssignmentsAsync(request.StudentAssignments, practice.Id);

        await _context.SaveChangesAsync();

        practice = await LoadPracticeDetailsQuery()
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

    private IQueryable<ProductionPractice> LoadPracticeDetailsQuery()
    {
        return _context.ProductionPractices
            .Include(x => x.Specialty)
            .Include(x => x.Competencies)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Student)
                    .ThenInclude(x => x!.Group)
                        .ThenInclude(x => x!.Specialty)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Supervisor);
    }

    private async Task<Dictionary<string, string[]>> ValidatePracticeRequestAsync(ProductionPracticeUpsertRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string key, string message)
        {
            if (!errors.ContainsKey(key))
                errors[key] = new List<string>();

            if (!errors[key].Contains(message))
                errors[key].Add(message);
        }

        if (string.IsNullOrWhiteSpace(request.PracticeIndex))
            AddError(nameof(request.PracticeIndex), "Введите индекс производственной практики.");

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(nameof(request.Name), "Введите название производственной практики.");

        if (request.SpecialtyId <= 0)
        {
            AddError(nameof(request.SpecialtyId), "Выберите специальность.");
        }
        else
        {
            var specialtyExists = await _context.Specialties.AnyAsync(x => x.Id == request.SpecialtyId);
            if (!specialtyExists)
                AddError(nameof(request.SpecialtyId), "Выбранная специальность не найдена.");
        }

        if (string.IsNullOrWhiteSpace(request.ProfessionalModuleCode))
            AddError(nameof(request.ProfessionalModuleCode), "Введите код профессионального модуля.");

        if (string.IsNullOrWhiteSpace(request.ProfessionalModuleName))
            AddError(nameof(request.ProfessionalModuleName), "Введите название профессионального модуля.");

        if (request.Hours <= 0)
            AddError(nameof(request.Hours), "Количество часов должно быть больше нуля.");

        if (request.StartDate == default)
            AddError(nameof(request.StartDate), "Укажите дату начала практики.");

        if (request.EndDate == default)
            AddError(nameof(request.EndDate), "Укажите дату окончания практики.");

        if (request.StartDate != default && request.EndDate != default && request.EndDate < request.StartDate)
            AddError(nameof(request.EndDate), "Дата окончания не может быть раньше даты начала.");

        if (request.Competencies is null || request.Competencies.Count == 0)
        {
            AddError(nameof(request.Competencies), "Добавьте хотя бы одну профессиональную компетенцию.");
        }
        else
        {
            var competencyHoursSum = 0;

            for (var i = 0; i < request.Competencies.Count; i++)
            {
                var item = request.Competencies[i];

                if (string.IsNullOrWhiteSpace(item.CompetencyCode))
                    AddError($"Competencies[{i}].CompetencyCode", "Введите код компетенции.");

                if (string.IsNullOrWhiteSpace(item.CompetencyDescription))
                    AddError($"Competencies[{i}].CompetencyDescription", "Введите описание компетенции.");

                if (string.IsNullOrWhiteSpace(item.WorkTypes))
                    AddError($"Competencies[{i}].WorkTypes", "Введите виды работ.");

                if (item.Hours <= 0)
                    AddError($"Competencies[{i}].Hours", "Количество часов по компетенции должно быть больше нуля.");
                else
                    competencyHoursSum += item.Hours;
            }

            if (request.Hours > 0 && competencyHoursSum != request.Hours)
                AddError(nameof(request.Competencies), $"Сумма часов по компетенциям ({competencyHoursSum}) должна быть равна общему количеству часов ({request.Hours}).");
        }

        return errors.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }

    private async Task<Dictionary<string, string[]>> ValidateAssignmentsAsync(
        List<ProductionPracticeStudentAssignmentRequest> assignments,
        int specialtyId)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string key, string message)
        {
            if (!errors.ContainsKey(key))
                errors[key] = new List<string>();

            if (!errors[key].Contains(message))
                errors[key].Add(message);
        }

        var duplicateStudentIds = assignments
            .Where(x => x.StudentId > 0)
            .GroupBy(x => x.StudentId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToHashSet();

        for (var i = 0; i < assignments.Count; i++)
        {
            var assignment = assignments[i];

            if (assignment.StudentId <= 0)
            {
                AddError($"StudentAssignments[{i}].StudentId", "Выберите студента.");
                continue;
            }

            if (duplicateStudentIds.Contains(assignment.StudentId))
                AddError($"StudentAssignments[{i}].StudentId", "Этот студент выбран несколько раз.");

            var student = await _context.Users
                .Include(x => x.Role)
                .Include(x => x.Group)
                .FirstOrDefaultAsync(x => x.Id == assignment.StudentId);

            if (student is null || student.Role.Name != "Student")
            {
                AddError($"StudentAssignments[{i}].StudentId", "Выбранный пользователь не является студентом.");
            }
            else if (student.Group is null || student.Group.SpecialtyId != specialtyId)
            {
                AddError($"StudentAssignments[{i}].StudentId", $"Студент {student.FullName} не относится к выбранной специальности.");
            }

            if (assignment.SupervisorId.HasValue)
            {
                var supervisor = await _context.Users
                    .Include(x => x.Role)
                    .FirstOrDefaultAsync(x => x.Id == assignment.SupervisorId.Value);

                if (supervisor is null || supervisor.Role.Name != "Supervisor")
                    AddError($"StudentAssignments[{i}].SupervisorId", "Выбранный руководитель не имеет роль Supervisor.");
            }
        }

        return errors.ToDictionary(x => x.Key, x => x.Value.ToArray());
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
                .OrderBy(a => a.Student.Group != null ? a.Student.Group.Course : 99)
                .ThenBy(a => a.Student.Group != null ? a.Student.Group.Name : "")
                .ThenBy(a => a.Student.FullName)
                .Select(a => new ProductionPracticeStudentAssignmentItemResponse
                {
                    Id = a.Id,
                    StudentId = a.StudentId,
                    StudentFullName = a.Student.FullName,
                    StudentSpecialtyId = a.Student.Group != null ? a.Student.Group.SpecialtyId : null,
                    StudentSpecialtyCode = a.Student.Group != null && a.Student.Group.Specialty != null ? a.Student.Group.Specialty.Code : null,
                    StudentSpecialtyName = a.Student.Group != null && a.Student.Group.Specialty != null ? a.Student.Group.Specialty.Name : null,
                    StudentGroupName = a.Student.Group != null ? a.Student.Group.Name : null,
                    StudentCourse = a.Student.Group != null ? a.Student.Group.Course : null,
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

public class PracticeSelectOptionResponse
{
    public int Id { get; set; }

    public string Label { get; set; } = null!;
}

public class PracticeStudentOptionResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public int? SpecialtyId { get; set; }

    public string? SpecialtyCode { get; set; }

    public string? SpecialtyName { get; set; }

    public string? GroupName { get; set; }

    public int? Course { get; set; }
}

public class PracticeSupervisorOptionResponse
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;
}
