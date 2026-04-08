using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Dtos;

namespace PracticeMonitoring.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelpingController : ControllerBase
{
    private readonly AppDbContext _context;

    public HelpingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("specialties")]
    public async Task<ActionResult<IEnumerable<SpecialtyDto>>> GetSpecialties()
    {
        var list = await _context.Specialties
            .OrderBy(s => s.Name)
            .Select(s => new SpecialtyDto { Id = s.Id, Code = s.Code, Name = s.Name })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("groups")]
    public async Task<ActionResult<IEnumerable<GroupDto>>> GetGroups(int specialtyId)
    {
        var groups = await _context.Groups
            .Where(g => g.SpecialtyId == specialtyId)
            .OrderBy(g => g.Name)
            .Select(g => new GroupDto { Id = g.Id, Name = g.Name, Course = g.Course, SpecialtyId = g.SpecialtyId })
            .ToListAsync();

        return Ok(groups);
    }
}