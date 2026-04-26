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
    private readonly NotificationService _notificationService;

    public DepartmentStaffPracticesController(
        AppDbContext context,
        AuditLogService auditLogService,
        NotificationService notificationService)
    {
        _context = context;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
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
                Label = $"{x.Code} вЂ” {x.Name}"
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
                message = "РСЃРїСЂР°РІСЊС‚Рµ РѕС€РёР±РєРё С„РѕСЂРјС‹.",
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

        practice.GeneralCompetencies = request.GeneralCompetencies
            .Select((x, index) => new ProductionPracticeGeneralCompetency
            {
                CompetencyCode = x.CompetencyCode.Trim(),
                CompetencyDescription = x.CompetencyDescription.Trim(),
                SortOrder = index + 1
            })
            .ToList();

        _context.ProductionPractices.Add(practice);
        await _context.SaveChangesAsync();

        await _auditLogService.LogProductionPracticeChangeAsync(
            GetActorUserId(),
            GetActorFullName(),
            "PracticeCreated",
            TruncateDescription(
                $"РЎРѕР·РґР°РЅР° РїСЂРѕРёР·РІРѕРґСЃС‚РІРµРЅРЅР°СЏ РїСЂР°РєС‚РёРєР° {BuildPracticeDisplayName(practice.PracticeIndex, practice.Name)}. " +
                $"РЎРїРµС†РёР°Р»СЊРЅРѕСЃС‚СЊ: {specialty.Code} вЂ” {specialty.Name}. " +
                $"РЎСЂРѕРєРё: {practice.StartDate:dd.MM.yyyy} - {practice.EndDate:dd.MM.yyyy}. " +
                $"Р§Р°СЃС‹: {practice.Hours}. РљРѕРјРїРµС‚РµРЅС†РёР№: {practice.Competencies.Count}."));

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
            .Include(x => x.GeneralCompetencies)
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
                message = "РСЃРїСЂР°РІСЊС‚Рµ РѕС€РёР±РєРё С„РѕСЂРјС‹.",
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
                message = "Р’ СЃР»СѓС‡Р°Рµ РёР·РјРµРЅРµРЅРёСЏ СЃРїРµС†РёР°Р»СЊРЅРѕСЃС‚Рё Сѓ РїСЂРѕРёР·РІРѕРґСЃС‚РІРµРЅРЅРѕР№ РїСЂР°РєС‚РёРєРё РІСЃРµ РЅР°Р·РЅР°С‡РµРЅРЅС‹Рµ СЃС‚СѓРґРµРЅС‚С‹ Р±СѓРґСѓС‚ СѓРґР°Р»РµРЅС‹. РџРѕРґС‚РІРµСЂРґРёС‚Рµ РґРµР№СЃС‚РІРёРµ.",
                errors = new Dictionary<string, string[]>
                {
                    [nameof(request.SpecialtyId)] = new[]
                    {
                        "Р’ СЃР»СѓС‡Р°Рµ РёР·РјРµРЅРµРЅРёСЏ СЃРїРµС†РёР°Р»СЊРЅРѕСЃС‚Рё Сѓ РїСЂРѕРёР·РІРѕРґСЃС‚РІРµРЅРЅРѕР№ РїСЂР°РєС‚РёРєРё РІСЃРµ РЅР°Р·РЅР°С‡РµРЅРЅС‹Рµ СЃС‚СѓРґРµРЅС‚С‹ Р±СѓРґСѓС‚ СѓРґР°Р»РµРЅС‹. РџРѕРґС‚РІРµСЂРґРёС‚Рµ РґРµР№СЃС‚РІРёРµ."
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
        _context.ProductionPracticeGeneralCompetencies.RemoveRange(practice.GeneralCompetencies);

        practice.Competencies = request.Competencies.Select(x => new ProductionPracticeCompetency
        {
            ProductionPracticeId = practice.Id,
            CompetencyCode = x.CompetencyCode.Trim(),
            CompetencyDescription = x.CompetencyDescription.Trim(),
            WorkTypes = x.WorkTypes.Trim(),
            Hours = x.Hours
        }).ToList();

        practice.GeneralCompetencies = request.GeneralCompetencies
            .Select((x, index) => new ProductionPracticeGeneralCompetency
            {
                ProductionPracticeId = practice.Id,
                CompetencyCode = x.CompetencyCode.Trim(),
                CompetencyDescription = x.CompetencyDescription.Trim(),
                SortOrder = index + 1
            })
            .ToList();

        await _context.SaveChangesAsync();

        var changedFields = BuildPracticeChangedFields(previousSnapshot, request, specialty);
        if (changedFields.Count > 0)
        {
            await _auditLogService.LogProductionPracticeChangeAsync(
                GetActorUserId(),
                GetActorFullName(),
                "PracticeUpdated",
                TruncateDescription(
                    $"РР·РјРµРЅРµРЅР° РїСЂРѕРёР·РІРѕРґСЃС‚РІРµРЅРЅР°СЏ РїСЂР°РєС‚РёРєР° {BuildPracticeDisplayName(practice.PracticeIndex, practice.Name)}. " +
                    $"РР·РјРµРЅРµРЅРѕ: {string.Join("; ", changedFields)}."));
        }

        if (specialtyChanged && removedAssignmentsBySpecialtyChange.Count > 0)
        {
            await _auditLogService.LogProductionPracticeAssignmentChangeAsync(
                GetActorUserId(),
                GetActorFullName(),
                "AssignmentsClearedBySpecialtyChange",
                TruncateDescription(
                    $"РџСЂРё СЃРјРµРЅРµ СЃРїРµС†РёР°Р»СЊРЅРѕСЃС‚Рё Сѓ РїСЂР°РєС‚РёРєРё {BuildPracticeDisplayName(practice.PracticeIndex, practice.Name)} " +
                    $"Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё СѓРґР°Р»РµРЅС‹ РЅР°Р·РЅР°С‡РµРЅРёСЏ СЃС‚СѓРґРµРЅС‚РѕРІ ({removedAssignmentsBySpecialtyChange.Count}): " +
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
                message = "РСЃРїСЂР°РІСЊС‚Рµ РѕС€РёР±РєРё РЅР°Р·РЅР°С‡РµРЅРёСЏ СЃС‚СѓРґРµРЅС‚РѕРІ.",
                errors = validationErrors
            });
        }

        var previousAssignments = practice.StudentAssignments
            .Select(CreateAssignmentSnapshot)
            .ToList();

        ApplyAssignments(practice, request.StudentAssignments);
        await _context.SaveChangesAsync();

        practice = await LoadPracticeDetailsQuery()
            .FirstAsync(x => x.Id == id);

        var currentAssignments = practice.StudentAssignments
            .Select(CreateAssignmentSnapshot)
            .ToList();

        await LogAssignmentsDiffAsync(practice, previousAssignments, currentAssignments);
        await CreateAssignmentNotificationsAsync(practice, previousAssignments, currentAssignments);

        return Ok(MapDetails(practice));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var practice = await _context.ProductionPractices
            .Include(x => x.Specialty)
            .Include(x => x.Competencies)
            .Include(x => x.GeneralCompetencies)
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
        var specialtyLabel = $"{practice.Specialty.Code} вЂ” {practice.Specialty.Name}";

        _context.ProductionPracticeCompetencies.RemoveRange(practice.Competencies);
        _context.ProductionPracticeStudentAssignments.RemoveRange(practice.StudentAssignments);
        _context.ProductionPractices.Remove(practice);

        await _context.SaveChangesAsync();

        await _auditLogService.LogProductionPracticeChangeAsync(
            GetActorUserId(),
            GetActorFullName(),
            "PracticeDeleted",
            TruncateDescription(
                $"РЈРґР°Р»РµРЅР° РїСЂРѕРёР·РІРѕРґСЃС‚РІРµРЅРЅР°СЏ РїСЂР°РєС‚РёРєР° {practiceLabel}. " +
                $"РЎРїРµС†РёР°Р»СЊРЅРѕСЃС‚СЊ: {specialtyLabel}. " +
                $"РљРѕРјРїРµС‚РµРЅС†РёР№: {competenciesCount}. РќР°Р·РЅР°С‡РµРЅРЅС‹С… СЃС‚СѓРґРµРЅС‚РѕРІ: {removedAssignments.Count}."));

        if (removedAssignments.Count > 0)
        {
            await _auditLogService.LogProductionPracticeAssignmentChangeAsync(
                GetActorUserId(),
                GetActorFullName(),
                "AssignmentsDeletedWithPractice",
                TruncateDescription(
                    $"РџСЂРё СѓРґР°Р»РµРЅРёРё РїСЂР°РєС‚РёРєРё {practiceLabel} СЃРЅСЏС‚С‹ РЅР°Р·РЅР°С‡РµРЅРёСЏ СЃС‚СѓРґРµРЅС‚РѕРІ ({removedAssignments.Count}): " +
                    $"{JoinAssignmentNames(removedAssignments)}."));
        }

        return Ok(new { message = "РџСЂРѕРёР·РІРѕРґСЃС‚РІРµРЅРЅР°СЏ РїСЂР°РєС‚РёРєР° СѓРґР°Р»РµРЅР°." });
    }

    private IQueryable<ProductionPractice> LoadPracticeDetailsQuery()
    {
        return _context.ProductionPractices
            .Include(x => x.Specialty)
            .Include(x => x.Competencies)
            .Include(x => x.GeneralCompetencies)
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
            AddError(nameof(request.PracticeIndex), "Р’РІРµРґРёС‚Рµ РёРЅРґРµРєСЃ РїСЂРѕРёР·РІРѕРґСЃС‚РІРµРЅРЅРѕР№ РїСЂР°РєС‚РёРєРё.");

        if (string.IsNullOrWhiteSpace(request.Name))
            AddError(nameof(request.Name), "Р’РІРµРґРёС‚Рµ РЅР°Р·РІР°РЅРёРµ РїСЂРѕРёР·РІРѕРґСЃС‚РІРµРЅРЅРѕР№ РїСЂР°РєС‚РёРєРё.");

        if (request.SpecialtyId <= 0)
        {
            AddError(nameof(request.SpecialtyId), "Р’С‹Р±РµСЂРёС‚Рµ СЃРїРµС†РёР°Р»СЊРЅРѕСЃС‚СЊ.");
        }
        else
        {
            var specialtyExists = await _context.Specialties.AnyAsync(x => x.Id == request.SpecialtyId);
            if (!specialtyExists)
                AddError(nameof(request.SpecialtyId), "Р’С‹Р±СЂР°РЅРЅР°СЏ СЃРїРµС†РёР°Р»СЊРЅРѕСЃС‚СЊ РЅРµ РЅР°Р№РґРµРЅР°.");
        }

        if (string.IsNullOrWhiteSpace(request.ProfessionalModuleCode))
            AddError(nameof(request.ProfessionalModuleCode), "Р’РІРµРґРёС‚Рµ РєРѕРґ РїСЂРѕС„РµСЃСЃРёРѕРЅР°Р»СЊРЅРѕРіРѕ РјРѕРґСѓР»СЏ.");

        if (string.IsNullOrWhiteSpace(request.ProfessionalModuleName))
            AddError(nameof(request.ProfessionalModuleName), "Р’РІРµРґРёС‚Рµ РЅР°Р·РІР°РЅРёРµ РїСЂРѕС„РµСЃСЃРёРѕРЅР°Р»СЊРЅРѕРіРѕ РјРѕРґСѓР»СЏ.");

        if (request.Hours <= 0)
            AddError(nameof(request.Hours), "РљРѕР»РёС‡РµСЃС‚РІРѕ С‡Р°СЃРѕРІ РґРѕР»Р¶РЅРѕ Р±С‹С‚СЊ Р±РѕР»СЊС€Рµ РЅСѓР»СЏ.");

        if (request.StartDate == default)
            AddError(nameof(request.StartDate), "РЈРєР°Р¶РёС‚Рµ РґР°С‚Сѓ РЅР°С‡Р°Р»Р° РїСЂР°РєС‚РёРєРё.");

        if (request.EndDate == default)
            AddError(nameof(request.EndDate), "РЈРєР°Р¶РёС‚Рµ РґР°С‚Сѓ РѕРєРѕРЅС‡Р°РЅРёСЏ РїСЂР°РєС‚РёРєРё.");

        if (request.StartDate != default && request.EndDate != default && request.EndDate < request.StartDate)
            AddError(nameof(request.EndDate), "Р”Р°С‚Р° РѕРєРѕРЅС‡Р°РЅРёСЏ РЅРµ РјРѕР¶РµС‚ Р±С‹С‚СЊ СЂР°РЅСЊС€Рµ РґР°С‚С‹ РЅР°С‡Р°Р»Р°.");

        request.GeneralCompetencies ??= new List<ProductionPracticeGeneralCompetencyRequest>();
        for (var i = 0; i < request.GeneralCompetencies.Count; i++)
        {
            var item = request.GeneralCompetencies[i];

            if (string.IsNullOrWhiteSpace(item.CompetencyCode))
                AddError($"GeneralCompetencies[{i}].CompetencyCode", "Введите код общей компетенции.");

            if (string.IsNullOrWhiteSpace(item.CompetencyDescription))
                AddError($"GeneralCompetencies[{i}].CompetencyDescription", "Введите описание общей компетенции.");
        }
        if (request.Competencies is null || request.Competencies.Count == 0)
        {
            AddError(nameof(request.Competencies), "Р”РѕР±Р°РІСЊС‚Рµ С…РѕС‚СЏ Р±С‹ РѕРґРЅСѓ РїСЂРѕС„РµСЃСЃРёРѕРЅР°Р»СЊРЅСѓСЋ РєРѕРјРїРµС‚РµРЅС†РёСЋ.");
        }
        else
        {
            var competencyHoursSum = 0;

            for (var i = 0; i < request.Competencies.Count; i++)
            {
                var item = request.Competencies[i];

                if (string.IsNullOrWhiteSpace(item.CompetencyCode))
                    AddError($"Competencies[{i}].CompetencyCode", "Р’РІРµРґРёС‚Рµ РєРѕРґ РєРѕРјРїРµС‚РµРЅС†РёРё.");

                if (string.IsNullOrWhiteSpace(item.CompetencyDescription))
                    AddError($"Competencies[{i}].CompetencyDescription", "Р’РІРµРґРёС‚Рµ РѕРїРёСЃР°РЅРёРµ РєРѕРјРїРµС‚РµРЅС†РёРё.");

                if (string.IsNullOrWhiteSpace(item.WorkTypes))
                    AddError($"Competencies[{i}].WorkTypes", "Р’РІРµРґРёС‚Рµ РІРёРґС‹ СЂР°Р±РѕС‚.");

                if (item.Hours <= 0)
                    AddError($"Competencies[{i}].Hours", "РљРѕР»РёС‡РµСЃС‚РІРѕ С‡Р°СЃРѕРІ РїРѕ РєРѕРјРїРµС‚РµРЅС†РёРё РґРѕР»Р¶РЅРѕ Р±С‹С‚СЊ Р±РѕР»СЊС€Рµ РЅСѓР»СЏ.");
                else
                    competencyHoursSum += item.Hours;
            }

            if (request.Hours > 0 && competencyHoursSum != request.Hours)
                AddError(nameof(request.Competencies), $"РЎСѓРјРјР° С‡Р°СЃРѕРІ РїРѕ РєРѕРјРїРµС‚РµРЅС†РёСЏРј ({competencyHoursSum}) РґРѕР»Р¶РЅР° Р±С‹С‚СЊ СЂР°РІРЅР° РѕР±С‰РµРјСѓ РєРѕР»РёС‡РµСЃС‚РІСѓ С‡Р°СЃРѕРІ ({request.Hours}).");
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
                AddError($"StudentAssignments[{i}].StudentId", "Р’С‹Р±РµСЂРёС‚Рµ СЃС‚СѓРґРµРЅС‚Р°.");
                continue;
            }

            if (duplicateStudentIds.Contains(assignment.StudentId))
                AddError($"StudentAssignments[{i}].StudentId", "Р­С‚РѕС‚ СЃС‚СѓРґРµРЅС‚ РІС‹Р±СЂР°РЅ РЅРµСЃРєРѕР»СЊРєРѕ СЂР°Р·.");

            var student = await _context.Users
                .Include(x => x.Role)
                .Include(x => x.Group)
                .FirstOrDefaultAsync(x => x.Id == assignment.StudentId);

            if (student is null || student.Role.Name != "Student")
            {
                AddError($"StudentAssignments[{i}].StudentId", "Р’С‹Р±СЂР°РЅРЅС‹Р№ РїРѕР»СЊР·РѕРІР°С‚РµР»СЊ РЅРµ СЏРІР»СЏРµС‚СЃСЏ СЃС‚СѓРґРµРЅС‚РѕРј.");
            }
            else if (student.Group is null || student.Group.SpecialtyId != specialtyId)
            {
                AddError($"StudentAssignments[{i}].StudentId", $"РЎС‚СѓРґРµРЅС‚ {student.FullName} РЅРµ РѕС‚РЅРѕСЃРёС‚СЃСЏ Рє РІС‹Р±СЂР°РЅРЅРѕР№ СЃРїРµС†РёР°Р»СЊРЅРѕСЃС‚Рё.");
            }

            if (assignment.SupervisorId.HasValue)
            {
                var supervisor = await _context.Users
                    .Include(x => x.Role)
                    .FirstOrDefaultAsync(x => x.Id == assignment.SupervisorId.Value);

                if (supervisor is null || supervisor.Role.Name != "Supervisor")
                    AddError($"StudentAssignments[{i}].SupervisorId", "Р’С‹Р±СЂР°РЅРЅС‹Р№ СЂСѓРєРѕРІРѕРґРёС‚РµР»СЊ РЅРµ РёРјРµРµС‚ СЂРѕР»СЊ Supervisor.");
            }
        }

        return errors.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }

    private void ApplyAssignments(
        ProductionPractice practice,
        List<ProductionPracticeStudentAssignmentRequest> assignments)
    {
        var requestedByStudent = assignments
            .Where(x => x.StudentId > 0)
            .GroupBy(x => x.StudentId)
            .ToDictionary(x => x.Key, x => x.First());

        var existingByStudent = practice.StudentAssignments.ToDictionary(x => x.StudentId);

        var removedAssignments = practice.StudentAssignments
            .Where(x => !requestedByStudent.ContainsKey(x.StudentId))
            .ToList();

        if (removedAssignments.Count > 0)
            _context.ProductionPracticeStudentAssignments.RemoveRange(removedAssignments);

        foreach (var request in requestedByStudent.Values)
        {
            if (existingByStudent.TryGetValue(request.StudentId, out var existingAssignment))
            {
                existingAssignment.SupervisorId = request.SupervisorId;
                continue;
            }

            practice.StudentAssignments.Add(new ProductionPracticeStudentAssignment
            {
                ProductionPracticeId = practice.Id,
                StudentId = request.StudentId,
                SupervisorId = request.SupervisorId,
                AssignedAtUtc = DateTime.UtcNow
            });
        }
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
            parts.Add($"РґРѕР±Р°РІР»РµРЅС‹ {added.Count}: {JoinAssignmentNames(added)}");

        if (removed.Count > 0)
            parts.Add($"СѓРґР°Р»РµРЅС‹ {removed.Count}: {JoinAssignmentNames(removed)}");

        if (supervisorChanged.Count > 0)
            parts.Add($"РёР·РјРµРЅС‘РЅ СЂСѓРєРѕРІРѕРґРёС‚РµР»СЊ Сѓ {supervisorChanged.Count}: {JoinSupervisorChanges(supervisorChanged)}");

        await _auditLogService.LogProductionPracticeAssignmentChangeAsync(
            GetActorUserId(),
            GetActorFullName(),
            "AssignmentsUpdated",
            TruncateDescription(
                $"РћР±РЅРѕРІР»РµРЅС‹ РЅР°Р·РЅР°С‡РµРЅРёСЏ СЃС‚СѓРґРµРЅС‚РѕРІ РґР»СЏ РїСЂР°РєС‚РёРєРё {BuildPracticeDisplayName(practice.PracticeIndex, practice.Name)}. " +
                $"{string.Join("; ", parts)}."));
    }

    private async Task CreateAssignmentNotificationsAsync(
        ProductionPractice practice,
        List<AssignmentSnapshot> previousAssignments,
        List<AssignmentSnapshot> currentAssignments)
    {
        var previousByStudent = previousAssignments.ToDictionary(x => x.StudentId);
        var practiceLabel = BuildPracticeDisplayName(practice.PracticeIndex, practice.Name);
        var datesLabel = $"{practice.StartDate:dd.MM.yyyy} - {practice.EndDate:dd.MM.yyyy}";

        foreach (var assignment in currentAssignments)
        {
            if (!previousByStudent.TryGetValue(assignment.StudentId, out var previous))
            {
                var supervisorLabel = string.IsNullOrWhiteSpace(assignment.SupervisorFullName)
                    ? "СЂСѓРєРѕРІРѕРґРёС‚РµР»СЊ РїСЂР°РєС‚РёРєРё РїРѕРєР° РЅРµ РЅР°Р·РЅР°С‡РµРЅ"
                    : $"СЂСѓРєРѕРІРѕРґРёС‚РµР»СЊ: {assignment.SupervisorFullName}";

                _notificationService.Add(
                    assignment.StudentId,
                    "PracticeAssignment",
                    "РќР°Р·РЅР°С‡РµРЅР° РїСЂРѕРёР·РІРѕРґСЃС‚РІРµРЅРЅР°СЏ РїСЂР°РєС‚РёРєР°",
                    $"Р’С‹ РЅР°Р·РЅР°С‡РµРЅС‹ РЅР° РїСЂРѕРёР·РІРѕРґСЃС‚РІРµРЅРЅСѓСЋ РїСЂР°РєС‚РёРєСѓ {practiceLabel}. РЎСЂРѕРєРё: {datesLabel}, {practice.Hours} С‡.; {supervisorLabel}. Р’ РїРµСЂРІС‹Рµ 2 РґРЅСЏ РїСЂР°РєС‚РёРєРё Р·Р°РїРѕР»РЅРёС‚Рµ РѕСЂРіР°РЅРёР·Р°С†РёСЋ, СЂСѓРєРѕРІРѕРґРёС‚РµР»СЏ РѕС‚ РѕСЂРіР°РЅРёР·Р°С†РёРё Рё СЃРѕРґРµСЂР¶Р°РЅРёРµ Р·Р°РґР°РЅРёСЏ.",
                    $"/Student?practiceId={assignment.AssignmentId}");

                continue;
            }

            if (previous.SupervisorId != assignment.SupervisorId)
            {
                var supervisorLabel = string.IsNullOrWhiteSpace(assignment.SupervisorFullName)
                    ? "СЂСѓРєРѕРІРѕРґРёС‚РµР»СЊ РїСЂР°РєС‚РёРєРё СЃРЅСЏС‚"
                    : $"РЅРѕРІС‹Р№ СЂСѓРєРѕРІРѕРґРёС‚РµР»СЊ: {assignment.SupervisorFullName}";

                _notificationService.Add(
                    assignment.StudentId,
                    "PracticeSupervisorChanged",
                    "РР·РјРµРЅС‘РЅ СЂСѓРєРѕРІРѕРґРёС‚РµР»СЊ РїСЂР°РєС‚РёРєРё",
                    $"Р”Р»СЏ РїСЂР°РєС‚РёРєРё {practiceLabel} РёР·РјРµРЅС‘РЅ СЂСѓРєРѕРІРѕРґРёС‚РµР»СЊ: {supervisorLabel}.",
                    $"/Student?practiceId={assignment.AssignmentId}");
            }
        }

        await _context.SaveChangesAsync();
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
            GeneralCompetencies = x.GeneralCompetencies
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Id)
                .Select(c => new ProductionPracticeGeneralCompetencyItemResponse
                {
                    Id = c.Id,
                    CompetencyCode = c.CompetencyCode,
                    CompetencyDescription = c.CompetencyDescription,
                    SortOrder = c.SortOrder
                })
                .ToList(),            StudentAssignments = x.StudentAssignments
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
        var specialtyLabel = $"{specialty.Code} вЂ” {specialty.Name}";
        var competenciesSignature = BuildCompetenciesSignature(request.Competencies);

        if (!string.Equals(previous.PracticeIndex, practiceIndex, StringComparison.Ordinal))
            changedFields.Add($"РёРЅРґРµРєСЃ РџРџ: {previous.PracticeIndex} -> {practiceIndex}");

        if (!string.Equals(previous.Name, name, StringComparison.Ordinal))
            changedFields.Add($"РЅР°Р·РІР°РЅРёРµ: {previous.Name} -> {name}");

        if (previous.SpecialtyId != specialty.Id)
            changedFields.Add($"СЃРїРµС†РёР°Р»СЊРЅРѕСЃС‚СЊ: {previous.SpecialtyLabel} -> {specialtyLabel}");

        if (!string.Equals(previous.ProfessionalModuleCode, professionalModuleCode, StringComparison.Ordinal))
            changedFields.Add($"РєРѕРґ РџРњ: {previous.ProfessionalModuleCode} -> {professionalModuleCode}");

        if (!string.Equals(previous.ProfessionalModuleName, professionalModuleName, StringComparison.Ordinal))
            changedFields.Add($"РЅР°Р·РІР°РЅРёРµ РџРњ: {previous.ProfessionalModuleName} -> {professionalModuleName}");

        if (previous.Hours != request.Hours)
            changedFields.Add($"С‡Р°СЃС‹: {previous.Hours} -> {request.Hours}");

        if (previous.StartDate.Date != startDate.Date)
            changedFields.Add($"РґР°С‚Р° РЅР°С‡Р°Р»Р°: {previous.StartDate:dd.MM.yyyy} -> {startDate:dd.MM.yyyy}");

        if (previous.EndDate.Date != endDate.Date)
            changedFields.Add($"РґР°С‚Р° РѕРєРѕРЅС‡Р°РЅРёСЏ: {previous.EndDate:dd.MM.yyyy} -> {endDate:dd.MM.yyyy}");

        if (!string.Equals(previous.CompetenciesSignature, competenciesSignature, StringComparison.Ordinal))
            changedFields.Add($"РєРѕРјРїРµС‚РµРЅС†РёРё: {previous.CompetenciesCount} -> {request.Competencies.Count}");

        return changedFields;
    }

    private static PracticeSnapshot CreatePracticeSnapshot(ProductionPractice practice)
    {
        return new PracticeSnapshot(
            practice.PracticeIndex,
            practice.Name,
            practice.SpecialtyId,
            $"{practice.Specialty.Code} вЂ” {practice.Specialty.Name}",
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
            assignment.Id,
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
        return User.FindFirstValue(ClaimTypes.Name) ?? "Р Р°Р±РѕС‚РЅРёРє РѕС‚РґРµР»Р°";
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
            $"{x.StudentFullName} ({(string.IsNullOrWhiteSpace(x.PreviousSupervisorFullName) ? "Р±РµР· СЂСѓРєРѕРІРѕРґРёС‚РµР»СЏ" : x.PreviousSupervisorFullName)} -> {(string.IsNullOrWhiteSpace(x.CurrentSupervisorFullName) ? "Р±РµР· СЂСѓРєРѕРІРѕРґРёС‚РµР»СЏ" : x.CurrentSupervisorFullName)})"));
    }

    private static string JoinLimited(IEnumerable<string> items, int take = 6)
    {
        var materialized = items
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (materialized.Count == 0)
            return "РЅРµС‚ РґР°РЅРЅС‹С…";

        if (materialized.Count <= take)
            return string.Join(", ", materialized);

        return $"{string.Join(", ", materialized.Take(take))} Рё РµС‰С‘ {materialized.Count - take}";
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
        int AssignmentId,
        int StudentId,
        string StudentFullName,
        int? SupervisorId,
        string? SupervisorFullName);

    private sealed record SupervisorChangeSnapshot(
        string StudentFullName,
        string? PreviousSupervisorFullName,
        string? CurrentSupervisorFullName);
}




