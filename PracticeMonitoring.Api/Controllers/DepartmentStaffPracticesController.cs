using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos.DepartmentStaff;
using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/department-staff/practices")]
[Authorize(Roles = "DepartmentStaff,Admin")]
public class DepartmentStaffPracticesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLogService;

    public DepartmentStaffPracticesController(AppDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
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
                AssignedStudentsCount = x.StudentAssignments.Count,
                IsCompleted = IsCompleted(x.EndDate)
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

        await _auditLogService.LogProductionPracticeChangeAsync(
            GetActorUserId(),
            GetActorFullName(),
            "PracticeCreated",
            TruncateDescription(
                $"Создана производственная практика {BuildPracticeDisplayName(practice.PracticeIndex, practice.Name)}. " +
                $"Специальность: {specialty.Code} — {specialty.Name}. " +
                $"Сроки: {practice.StartDate:dd.MM.yyyy} - {practice.EndDate:dd.MM.yyyy}. " +
                $"Часы: {practice.Hours}. Компетенций: {practice.Competencies.Count}."));

        practice = await LoadPracticeDetailsQuery()
            .FirstAsync(x => x.Id == practice.Id);

        return Ok(MapDetails(practice));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductionPracticeDetailsResponse>> Update(int id, ProductionPracticeUpsertRequest request)
    {
        var practice = await _context.ProductionPractices
            .Include(x => x.Specialty)
            .Include(x => x.Competencies)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Student)
                    .ThenInclude(x => x!.Group)
                        .ThenInclude(x => x!.Specialty)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Supervisor)
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
        var specialtyChanged = practice.SpecialtyId != specialty.Id;
        var previousSnapshot = CreatePracticeSnapshot(practice);
        var removedAssignmentsBySpecialtyChange = practice.StudentAssignments
            .Select(CreateAssignmentSnapshot)
            .ToList();

        if (specialtyChanged && removedAssignmentsBySpecialtyChange.Count > 0 && !request.ConfirmSpecialtyChangeStudentReset)
        {
            return BadRequest(new
            {
                message = "В случае изменения специальности у производственной практики все назначенные студенты будут удалены. Подтвердите действие.",
                errors = new Dictionary<string, string[]>
                {
                    [nameof(request.SpecialtyId)] = new[]
                    {
                        "В случае изменения специальности у производственной практики все назначенные студенты будут удалены. Подтвердите действие."
                    }
                }
            });
        }

        if (specialtyChanged && removedAssignmentsBySpecialtyChange.Count > 0)
        {
            _context.ProductionPracticeStudentAssignments.RemoveRange(practice.StudentAssignments);
            practice.StudentAssignments = new List<ProductionPracticeStudentAssignment>();
        }

        practice.PracticeIndex = request.PracticeIndex.Trim();
        practice.Name = request.Name.Trim();
        practice.SpecialtyId = specialty.Id;
        practice.Specialty = specialty;
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

        var changedFields = BuildPracticeChangedFields(previousSnapshot, request, specialty);
        if (changedFields.Count > 0)
        {
            await _auditLogService.LogProductionPracticeChangeAsync(
                GetActorUserId(),
                GetActorFullName(),
                "PracticeUpdated",
                TruncateDescription(
                    $"Изменена производственная практика {BuildPracticeDisplayName(practice.PracticeIndex, practice.Name)}. " +
                    $"Изменено: {string.Join("; ", changedFields)}."));
        }

        if (specialtyChanged && removedAssignmentsBySpecialtyChange.Count > 0)
        {
            await _auditLogService.LogProductionPracticeAssignmentChangeAsync(
                GetActorUserId(),
                GetActorFullName(),
                "AssignmentsClearedBySpecialtyChange",
                TruncateDescription(
                    $"При смене специальности у практики {BuildPracticeDisplayName(practice.PracticeIndex, practice.Name)} " +
                    $"автоматически удалены назначения студентов ({removedAssignmentsBySpecialtyChange.Count}): " +
                    $"{JoinAssignmentNames(removedAssignmentsBySpecialtyChange)}."));
        }

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
                .ThenInclude(x => x.Student)
                    .ThenInclude(x => x!.Group)
                        .ThenInclude(x => x!.Specialty)
            .Include(x => x.StudentAssignments)
                .ThenInclude(x => x.Supervisor)
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

        var previousAssignments = practice.StudentAssignments
            .Select(CreateAssignmentSnapshot)
            .ToList();

        _context.ProductionPracticeStudentAssignments.RemoveRange(practice.StudentAssignments);
        practice.StudentAssignments = await BuildAssignmentsAsync(request.StudentAssignments, practice.Id);

        await _context.SaveChangesAsync();

        var currentAssignments = practice.StudentAssignments
            .Select(CreateAssignmentSnapshot)
            .ToList();

        await LogAssignmentsDiffAsync(practice, previousAssignments, currentAssignments);

        practice = await LoadPracticeDetailsQuery()
            .FirstAsync(x => x.Id == id);

        return Ok(MapDetails(practice));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
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

        var removedAssignments = practice.StudentAssignments
            .Select(CreateAssignmentSnapshot)
            .ToList();
        var competenciesCount = practice.Competencies.Count;
        var practiceLabel = BuildPracticeDisplayName(practice.PracticeIndex, practice.Name);
        var specialtyLabel = $"{practice.Specialty.Code} — {practice.Specialty.Name}";

        _context.ProductionPracticeCompetencies.RemoveRange(practice.Competencies);
        _context.ProductionPracticeStudentAssignments.RemoveRange(practice.StudentAssignments);
        _context.ProductionPractices.Remove(practice);

        await _context.SaveChangesAsync();

        await _auditLogService.LogProductionPracticeChangeAsync(
            GetActorUserId(),
            GetActorFullName(),
            "PracticeDeleted",
            TruncateDescription(
                $"Удалена производственная практика {practiceLabel}. " +
                $"Специальность: {specialtyLabel}. " +
                $"Компетенций: {competenciesCount}. Назначенных студентов: {removedAssignments.Count}."));

        if (removedAssignments.Count > 0)
        {
            await _auditLogService.LogProductionPracticeAssignmentChangeAsync(
                GetActorUserId(),
                GetActorFullName(),
                "AssignmentsDeletedWithPractice",
                TruncateDescription(
                    $"При удалении практики {practiceLabel} сняты назначения студентов ({removedAssignments.Count}): " +
                    $"{JoinAssignmentNames(removedAssignments)}."));
        }

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
            var student = await _context.Users
                .Include(x => x.Group)
                    .ThenInclude(x => x!.Specialty)
                .FirstAsync(x => x.Id == assignment.StudentId);

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

    private async Task LogAssignmentsDiffAsync(
        ProductionPractice practice,
        List<AssignmentSnapshot> previousAssignments,
        List<AssignmentSnapshot> currentAssignments)
    {
        var previousByStudent = previousAssignments.ToDictionary(x => x.StudentId);
        var currentByStudent = currentAssignments.ToDictionary(x => x.StudentId);

        var added = currentAssignments
            .Where(x => !previousByStudent.ContainsKey(x.StudentId))
            .ToList();
        var removed = previousAssignments
            .Where(x => !currentByStudent.ContainsKey(x.StudentId))
            .ToList();
        var supervisorChanged = currentAssignments
            .Where(x => previousByStudent.TryGetValue(x.StudentId, out var previous) && previous.SupervisorId != x.SupervisorId)
            .Select(x => new SupervisorChangeSnapshot(
                x.StudentFullName,
                previousByStudent[x.StudentId].SupervisorFullName,
                x.SupervisorFullName))
            .ToList();

        if (added.Count == 0 && removed.Count == 0 && supervisorChanged.Count == 0)
            return;

        var parts = new List<string>();

        if (added.Count > 0)
            parts.Add($"добавлены {added.Count}: {JoinAssignmentNames(added)}");

        if (removed.Count > 0)
            parts.Add($"удалены {removed.Count}: {JoinAssignmentNames(removed)}");

        if (supervisorChanged.Count > 0)
            parts.Add($"изменён руководитель у {supervisorChanged.Count}: {JoinSupervisorChanges(supervisorChanged)}");

        await _auditLogService.LogProductionPracticeAssignmentChangeAsync(
            GetActorUserId(),
            GetActorFullName(),
            "AssignmentsUpdated",
            TruncateDescription(
                $"Обновлены назначения студентов для практики {BuildPracticeDisplayName(practice.PracticeIndex, practice.Name)}. " +
                $"{string.Join("; ", parts)}."));
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
            IsCompleted = IsCompleted(x.EndDate),
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

    private static List<string> BuildPracticeChangedFields(
        PracticeSnapshot previous,
        ProductionPracticeUpsertRequest request,
        Specialty specialty)
    {
        var changedFields = new List<string>();

        var practiceIndex = request.PracticeIndex.Trim();
        var name = request.Name.Trim();
        var professionalModuleCode = request.ProfessionalModuleCode.Trim();
        var professionalModuleName = request.ProfessionalModuleName.Trim();
        var startDate = EnsureUtc(request.StartDate);
        var endDate = EnsureUtc(request.EndDate);
        var specialtyLabel = $"{specialty.Code} — {specialty.Name}";
        var competenciesSignature = BuildCompetenciesSignature(request.Competencies);

        if (!string.Equals(previous.PracticeIndex, practiceIndex, StringComparison.Ordinal))
            changedFields.Add($"индекс ПП: {previous.PracticeIndex} -> {practiceIndex}");

        if (!string.Equals(previous.Name, name, StringComparison.Ordinal))
            changedFields.Add($"название: {previous.Name} -> {name}");

        if (previous.SpecialtyId != specialty.Id)
            changedFields.Add($"специальность: {previous.SpecialtyLabel} -> {specialtyLabel}");

        if (!string.Equals(previous.ProfessionalModuleCode, professionalModuleCode, StringComparison.Ordinal))
            changedFields.Add($"код ПМ: {previous.ProfessionalModuleCode} -> {professionalModuleCode}");

        if (!string.Equals(previous.ProfessionalModuleName, professionalModuleName, StringComparison.Ordinal))
            changedFields.Add($"название ПМ: {previous.ProfessionalModuleName} -> {professionalModuleName}");

        if (previous.Hours != request.Hours)
            changedFields.Add($"часы: {previous.Hours} -> {request.Hours}");

        if (previous.StartDate.Date != startDate.Date)
            changedFields.Add($"дата начала: {previous.StartDate:dd.MM.yyyy} -> {startDate:dd.MM.yyyy}");

        if (previous.EndDate.Date != endDate.Date)
            changedFields.Add($"дата окончания: {previous.EndDate:dd.MM.yyyy} -> {endDate:dd.MM.yyyy}");

        if (!string.Equals(previous.CompetenciesSignature, competenciesSignature, StringComparison.Ordinal))
            changedFields.Add($"компетенции: {previous.CompetenciesCount} -> {request.Competencies.Count}");

        return changedFields;
    }

    private static PracticeSnapshot CreatePracticeSnapshot(ProductionPractice practice)
    {
        return new PracticeSnapshot(
            practice.PracticeIndex,
            practice.Name,
            practice.SpecialtyId,
            $"{practice.Specialty.Code} — {practice.Specialty.Name}",
            practice.ProfessionalModuleCode,
            practice.ProfessionalModuleName,
            practice.Hours,
            practice.StartDate,
            practice.EndDate,
            practice.Competencies.Count,
            BuildCompetenciesSignature(practice.Competencies));
    }

    private static AssignmentSnapshot CreateAssignmentSnapshot(ProductionPracticeStudentAssignment assignment)
    {
        return new AssignmentSnapshot(
            assignment.StudentId,
            assignment.Student.FullName,
            assignment.SupervisorId,
            assignment.Supervisor?.FullName);
    }

    private static string BuildCompetenciesSignature(IEnumerable<ProductionPracticeCompetencyRequest> competencies)
    {
        return string.Join(" | ", competencies
            .Select(x => $"{x.CompetencyCode.Trim()}::{x.CompetencyDescription.Trim()}::{x.WorkTypes.Trim()}::{x.Hours}")
            .OrderBy(x => x, StringComparer.Ordinal));
    }

    private static string BuildCompetenciesSignature(IEnumerable<ProductionPracticeCompetency> competencies)
    {
        return string.Join(" | ", competencies
            .Select(x => $"{x.CompetencyCode.Trim()}::{x.CompetencyDescription.Trim()}::{x.WorkTypes.Trim()}::{x.Hours}")
            .OrderBy(x => x, StringComparer.Ordinal));
    }

    private int GetActorUserId()
    {
        var rawValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawValue, out var value) ? value : 0;
    }

    private string GetActorFullName()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? "Работник отдела";
    }

    private static string BuildPracticeDisplayName(string practiceIndex, string name)
    {
        return $"{practiceIndex} \"{name}\"";
    }

    private static string JoinAssignmentNames(IEnumerable<AssignmentSnapshot> assignments)
    {
        return JoinLimited(assignments.Select(x => x.StudentFullName));
    }

    private static string JoinSupervisorChanges(IEnumerable<SupervisorChangeSnapshot> changes)
    {
        return JoinLimited(changes.Select(x =>
            $"{x.StudentFullName} ({(string.IsNullOrWhiteSpace(x.PreviousSupervisorFullName) ? "без руководителя" : x.PreviousSupervisorFullName)} -> {(string.IsNullOrWhiteSpace(x.CurrentSupervisorFullName) ? "без руководителя" : x.CurrentSupervisorFullName)})"));
    }

    private static string JoinLimited(IEnumerable<string> items, int take = 6)
    {
        var materialized = items
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (materialized.Count == 0)
            return "нет данных";

        if (materialized.Count <= take)
            return string.Join(", ", materialized);

        return $"{string.Join(", ", materialized.Take(take))} и ещё {materialized.Count - take}";
    }

    private static string TruncateDescription(string value)
    {
        const int maxLength = 1000;
        if (value.Length <= maxLength)
            return value;

        return $"{value[..(maxLength - 3)]}...";
    }

    private static bool IsCompleted(DateTime endDate)
    {
        return endDate.Date < DateTime.UtcNow.Date;
    }

    private static DateTime EnsureUtc(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
            return dt;

        if (dt.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        return dt.ToUniversalTime();
    }

    private sealed record PracticeSnapshot(
        string PracticeIndex,
        string Name,
        int SpecialtyId,
        string SpecialtyLabel,
        string ProfessionalModuleCode,
        string ProfessionalModuleName,
        int Hours,
        DateTime StartDate,
        DateTime EndDate,
        int CompetenciesCount,
        string CompetenciesSignature);

    private sealed record AssignmentSnapshot(
        int StudentId,
        string StudentFullName,
        int? SupervisorId,
        string? SupervisorFullName);

    private sealed record SupervisorChangeSnapshot(
        string StudentFullName,
        string? PreviousSupervisorFullName,
        string? CurrentSupervisorFullName);
}
