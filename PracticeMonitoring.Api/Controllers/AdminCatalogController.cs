using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos;
using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/admin/catalog")]
[Authorize(Roles = "Admin")]
public class AdminCatalogController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLogService;

    public AdminCatalogController(AppDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    [HttpGet("specialties")]
    public async Task<ActionResult<List<AdminSpecialtyCatalogResponse>>> GetSpecialties([FromQuery] bool includeArchived = true)
    {
        var query = _context.Specialties.AsQueryable();

        if (!includeArchived)
        {
            query = query.Where(x => !x.IsArchived);
        }

        var specialties = await query
            .Include(x => x.Groups)
                .ThenInclude(x => x.Users)
            .OrderBy(x => x.IsArchived)
            .ThenBy(x => x.Code)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(specialties.Select(MapSpecialty).ToList());
    }

    [HttpGet("groups")]
    public async Task<ActionResult<List<AdminGroupCatalogResponse>>> GetGroups([FromQuery] int specialtyId, [FromQuery] bool includeArchived = false)
    {
        if (specialtyId <= 0)
            return Ok(new List<AdminGroupCatalogResponse>());

        var query = _context.Groups
            .Where(x => x.SpecialtyId == specialtyId)
            .AsQueryable();

        if (!includeArchived)
        {
            query = query.Where(x => !x.IsArchived && !x.Specialty.IsArchived);
        }

        var groups = await query
            .Include(x => x.Specialty)
            .Include(x => x.Users)
            .OrderBy(x => x.IsArchived)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(groups.Select(g => MapGroup(g)).ToList());
    }

    [HttpPost("specialties")]
    public async Task<ActionResult<AdminSpecialtyCatalogResponse>> CreateSpecialty(AdminSpecialtyUpsertRequest request)
    {
        var actor = await GetActorAsync();
        if (actor is null)
            return Unauthorized();

        var code = request.Code.Trim();
        var name = request.Name.Trim();

        var duplicate = await _context.Specialties.AnyAsync(x =>
            !x.IsArchived &&
            x.Code.ToLower() == code.ToLower() &&
            x.Name.ToLower() == name.ToLower());

        if (duplicate)
            return BadRequest(new { message = "Такая специальность уже есть в активном справочнике." });

        var specialty = new Specialty
        {
            Code = code,
            Name = name,
            IsArchived = false
        };

        _context.Specialties.Add(specialty);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAdminActionAsync(
            actor.Id,
            actor.FullName,
            "SpecialtyCreated",
            $"Администратор создал специальность {specialty.Code} {specialty.Name}.");

        return Ok(MapSpecialty(specialty));
    }

    [HttpPut("specialties/{id:int}")]
    public async Task<ActionResult<AdminSpecialtyCatalogResponse>> UpdateSpecialty(int id, AdminSpecialtyUpsertRequest request)
    {
        var actor = await GetActorAsync();
        if (actor is null)
            return Unauthorized();

        var specialty = await _context.Specialties
            .Include(x => x.Groups)
                .ThenInclude(x => x.Users)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (specialty is null)
            return NotFound();

        var code = request.Code.Trim();
        var name = request.Name.Trim();

        var duplicate = await _context.Specialties.AnyAsync(x =>
            x.Id != id &&
            !x.IsArchived &&
            x.Code.ToLower() == code.ToLower() &&
            x.Name.ToLower() == name.ToLower());

        if (duplicate)
            return BadRequest(new { message = "Такая специальность уже есть в активном справочнике." });

        var changes = new List<string>();
        TrackChange(changes, "Код", specialty.Code, code);
        TrackChange(changes, "Название", specialty.Name, name);

        specialty.Code = code;
        specialty.Name = name;

        await _context.SaveChangesAsync();

        if (changes.Count > 0)
        {
            await _auditLogService.LogAdminActionAsync(
                actor.Id,
                actor.FullName,
                "SpecialtyUpdated",
                $"Администратор изменил специальность {specialty.Code} {specialty.Name}. Изменения: {string.Join("; ", changes)}.");
        }

        return Ok(MapSpecialty(specialty));
    }

    [HttpPost("specialties/{id:int}/archive")]
    public async Task<IActionResult> ArchiveSpecialty(int id)
    {
        var actor = await GetActorAsync();
        if (actor is null)
            return Unauthorized();

        var specialty = await _context.Specialties
            .Include(x => x.Groups)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (specialty is null)
            return NotFound();

        if (!specialty.IsArchived)
        {
            specialty.IsArchived = true;
            foreach (var group in specialty.Groups)
            {
                group.IsArchived = true;
            }

            await _context.SaveChangesAsync();

            await _auditLogService.LogAdminActionAsync(
                actor.Id,
                actor.FullName,
                "SpecialtyArchived",
                $"Администратор отправил в архив специальность {specialty.Code} {specialty.Name} и группы внутри нее.");
        }

        return Ok(new { message = "Специальность отправлена в архив." });
    }

    [HttpPost("specialties/{id:int}/restore")]
    public async Task<IActionResult> RestoreSpecialty(int id)
    {
        var actor = await GetActorAsync();
        if (actor is null)
            return Unauthorized();

        var specialty = await _context.Specialties.FirstOrDefaultAsync(x => x.Id == id);
        if (specialty is null)
            return NotFound();

        var duplicate = await _context.Specialties.AnyAsync(x =>
            x.Id != id &&
            !x.IsArchived &&
            x.Code.ToLower() == specialty.Code.ToLower() &&
            x.Name.ToLower() == specialty.Name.ToLower());

        if (duplicate)
            return BadRequest(new { message = "Нельзя восстановить специальность: такая активная запись уже есть." });

        if (specialty.IsArchived)
        {
            specialty.IsArchived = false;
            await _context.SaveChangesAsync();

            await _auditLogService.LogAdminActionAsync(
                actor.Id,
                actor.FullName,
                "SpecialtyRestored",
                $"Администратор восстановил специальность {specialty.Code} {specialty.Name} из архива.");
        }

        return Ok(new { message = "Специальность восстановлена." });
    }

    [HttpPost("groups")]
    public async Task<ActionResult<AdminGroupCatalogResponse>> CreateGroup(AdminGroupUpsertRequest request)
    {
        var actor = await GetActorAsync();
        if (actor is null)
            return Unauthorized();

        var specialty = await _context.Specialties.FirstOrDefaultAsync(x => x.Id == request.SpecialtyId);
        if (specialty is null)
            return BadRequest(new { message = "Выбранная специальность не найдена." });

        if (specialty.IsArchived)
            return BadRequest(new { message = "Нельзя создать группу внутри архивной специальности." });

        var name = request.Name.Trim();
        var duplicate = await _context.Groups.AnyAsync(x =>
            x.SpecialtyId == request.SpecialtyId &&
            !x.IsArchived &&
            x.Name.ToLower() == name.ToLower());

        if (duplicate)
            return BadRequest(new { message = "Такая группа уже есть в этой специальности." });

        var group = new Group
        {
            SpecialtyId = specialty.Id,
            Specialty = specialty,
            Name = name,
            Course = request.Course,
            IsArchived = false
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAdminActionAsync(
            actor.Id,
            actor.FullName,
            "GroupCreated",
            $"Администратор создал группу {group.Name} для специальности {specialty.Code} {specialty.Name}.");

        return Ok(MapGroup(group));
    }

    [HttpPut("groups/{id:int}")]
    public async Task<ActionResult<AdminGroupCatalogResponse>> UpdateGroup(int id, AdminGroupUpsertRequest request)
    {
        var actor = await GetActorAsync();
        if (actor is null)
            return Unauthorized();

        var group = await _context.Groups
            .Include(x => x.Specialty)
            .Include(x => x.Users)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (group is null)
            return NotFound();

        var specialty = await _context.Specialties.FirstOrDefaultAsync(x => x.Id == request.SpecialtyId);
        if (specialty is null)
            return BadRequest(new { message = "Выбранная специальность не найдена." });

        if (specialty.IsArchived)
            return BadRequest(new { message = "Нельзя привязать группу к архивной специальности." });

        var name = request.Name.Trim();
        var duplicate = await _context.Groups.AnyAsync(x =>
            x.Id != id &&
            x.SpecialtyId == specialty.Id &&
            !x.IsArchived &&
            x.Name.ToLower() == name.ToLower());

        if (duplicate)
            return BadRequest(new { message = "Такая группа уже есть в этой специальности." });

        var changes = new List<string>();
        TrackChange(changes, "Специальность", $"{group.Specialty.Code} {group.Specialty.Name}", $"{specialty.Code} {specialty.Name}");
        TrackChange(changes, "Группа", group.Name, name);
        TrackChange(changes, "Курс", group.Course.ToString(), request.Course.ToString());

        group.SpecialtyId = specialty.Id;
        group.Specialty = specialty;
        group.Name = name;
        group.Course = request.Course;

        await _context.SaveChangesAsync();

        if (changes.Count > 0)
        {
            await _auditLogService.LogAdminActionAsync(
                actor.Id,
                actor.FullName,
                "GroupUpdated",
                $"Администратор изменил группу {group.Name}. Изменения: {string.Join("; ", changes)}.");
        }

        return Ok(MapGroup(group));
    }

    [HttpPost("groups/{id:int}/archive")]
    public async Task<IActionResult> ArchiveGroup(int id)
    {
        var actor = await GetActorAsync();
        if (actor is null)
            return Unauthorized();

        var group = await _context.Groups
            .Include(x => x.Specialty)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (group is null)
            return NotFound();

        if (!group.IsArchived)
        {
            group.IsArchived = true;
            await _context.SaveChangesAsync();

            await _auditLogService.LogAdminActionAsync(
                actor.Id,
                actor.FullName,
                "GroupArchived",
                $"Администратор отправил в архив группу {group.Name} ({group.Specialty.Code} {group.Specialty.Name}).");
        }

        return Ok(new { message = "Группа отправлена в архив." });
    }

    [HttpPost("groups/{id:int}/restore")]
    public async Task<IActionResult> RestoreGroup(int id)
    {
        var actor = await GetActorAsync();
        if (actor is null)
            return Unauthorized();

        var group = await _context.Groups
            .Include(x => x.Specialty)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (group is null)
            return NotFound();

        if (group.Specialty.IsArchived)
            return BadRequest(new { message = "Сначала восстановите специальность, к которой относится группа." });

        var duplicate = await _context.Groups.AnyAsync(x =>
            x.Id != id &&
            x.SpecialtyId == group.SpecialtyId &&
            !x.IsArchived &&
            x.Name.ToLower() == group.Name.ToLower());

        if (duplicate)
            return BadRequest(new { message = "Нельзя восстановить группу: такая активная группа уже есть." });

        if (group.IsArchived)
        {
            group.IsArchived = false;
            await _context.SaveChangesAsync();

            await _auditLogService.LogAdminActionAsync(
                actor.Id,
                actor.FullName,
                "GroupRestored",
                $"Администратор восстановил группу {group.Name} ({group.Specialty.Code} {group.Specialty.Name}) из архива.");
        }

        return Ok(new { message = "Группа восстановлена." });
    }

    private async Task<User?> GetActorAsync()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim is null || !int.TryParse(claim, out var actorId))
            return null;

        return await _context.Users.FirstOrDefaultAsync(x => x.Id == actorId);
    }

    private static AdminSpecialtyCatalogResponse MapSpecialty(Specialty x)
    {
        return new AdminSpecialtyCatalogResponse
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            IsArchived = x.IsArchived,
            ActiveGroupsCount = x.Groups.Count(g => !g.IsArchived),
            ArchivedGroupsCount = x.Groups.Count(g => g.IsArchived),
            Groups = x.Groups
                .OrderBy(g => g.IsArchived)
                .ThenBy(g => g.Name)
                .Select(g => MapGroup(g, x))
                .ToList()
        };
    }

    private static AdminGroupCatalogResponse MapGroup(Group x, Specialty? specialtyOverride = null)
    {
        var specialty = specialtyOverride ?? x.Specialty;

        return new AdminGroupCatalogResponse
        {
            Id = x.Id,
            SpecialtyId = x.SpecialtyId,
            SpecialtyCode = specialty.Code,
            SpecialtyName = specialty.Name,
            Name = x.Name,
            Course = x.Course,
            IsArchived = x.IsArchived,
            StudentCount = x.Users.Count
        };
    }

    private static void TrackChange(List<string> changes, string field, string? oldValue, string? newValue)
    {
        if ((oldValue ?? string.Empty) != (newValue ?? string.Empty))
        {
            changes.Add($"{field}: \"{oldValue ?? "—"}\" -> \"{newValue ?? "—"}\"");
        }
    }
}
