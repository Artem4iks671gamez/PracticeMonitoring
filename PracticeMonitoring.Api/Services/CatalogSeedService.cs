using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Services;

public class CatalogSeedService
{
    private const string DefaultSpecialtyCode = "09.02.07";
    private const string DefaultSpecialtyName = "Разработчик веб и мультимедийных приложений";
    private const string DefaultGroupName = "ВД50-1-22";
    private const int DefaultGroupCourse = 4;

    private readonly AppDbContext _context;

    public CatalogSeedService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var hasActiveSpecialties = await _context.Specialties
            .AnyAsync(x => !x.IsArchived, cancellationToken);

        if (hasActiveSpecialties)
            return;

        var specialty = await _context.Specialties
            .FirstOrDefaultAsync(x => x.Code == DefaultSpecialtyCode, cancellationToken);

        if (specialty is null)
        {
            specialty = new Specialty
            {
                Code = DefaultSpecialtyCode,
                Name = DefaultSpecialtyName,
                IsArchived = false
            };

            _context.Specialties.Add(specialty);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else if (specialty.IsArchived)
        {
            specialty.IsArchived = false;
            await _context.SaveChangesAsync(cancellationToken);
        }

        var hasActiveGroups = await _context.Groups
            .AnyAsync(x => x.SpecialtyId == specialty.Id && !x.IsArchived, cancellationToken);

        if (hasActiveGroups)
            return;

        var group = await _context.Groups
            .FirstOrDefaultAsync(x => x.SpecialtyId == specialty.Id && x.Name == DefaultGroupName, cancellationToken);

        if (group is null)
        {
            _context.Groups.Add(new Group
            {
                SpecialtyId = specialty.Id,
                Name = DefaultGroupName,
                Course = DefaultGroupCourse,
                IsArchived = false
            });
        }
        else
        {
            group.Course = DefaultGroupCourse;
            group.IsArchived = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
