using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Entities;
using PracticeMonitoring.Api.Services;

namespace PracticeMonitoring.Tests.Services;

public class DemoDataSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesLogicalDemoDatasetAndCanRunTwice()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        context.Roles.AddRange(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Student" },
            new Role { Id = 3, Name = "Supervisor" },
            new Role { Id = 4, Name = "DepartmentStaff" });
        await context.SaveChangesAsync();

        var seeder = new DemoDataSeeder(context, new PasswordService());

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var demoUsers = await context.Users
            .Include(x => x.Role)
            .Where(x => x.Email.EndsWith(".demo@example.com") || x.Email == "demo.admin@example.com")
            .ToListAsync();

        Assert.Equal(4, demoUsers.Count);
        Assert.All(demoUsers, user =>
        {
            Assert.True(user.IsActive);
            Assert.False(user.MustChangePassword);
            Assert.False(string.IsNullOrWhiteSpace(user.AvatarUrl));
            Assert.True(new PasswordService().VerifyPassword(user, DemoDataSeeder.DemoPassword));
        });

        var practice = await context.ProductionPractices
            .Include(x => x.Competencies)
            .Include(x => x.GeneralCompetencies)
            .SingleAsync(x => x.PracticeIndex == "04.01" && x.Name == "Разработка модулей информационной системы");

        Assert.Equal(144, practice.Hours);
        Assert.Equal(4, practice.Competencies.Count);
        Assert.Equal(4, practice.GeneralCompetencies.Count);
        Assert.Equal(practice.Hours, practice.Competencies.Sum(x => x.Hours));

        var assignment = await context.ProductionPracticeStudentAssignments
            .Include(x => x.DiaryEntries)
            .Include(x => x.ReportItems)
            .Include(x => x.Sources)
            .Include(x => x.Appendices)
            .SingleAsync();

        Assert.Equal(20, assignment.DiaryEntries.Count);
        Assert.All(assignment.DiaryEntries, entry =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.ShortDescription));
            Assert.False(string.IsNullOrWhiteSpace(entry.DetailedReport));
            Assert.True(entry.IsReviewed);
            Assert.NotNull(entry.SupervisorGrade);
        });
        Assert.Contains(assignment.ReportItems, x => x.Category == "TechnicalTool" && x.Name == "Компьютер");
        Assert.NotEmpty(assignment.Sources);
        Assert.Single(assignment.Appendices);
    }
}
