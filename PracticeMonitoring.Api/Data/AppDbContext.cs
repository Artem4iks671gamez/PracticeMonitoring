using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ProductionPractice> ProductionPractices => Set<ProductionPractice>();
    public DbSet<ProductionPracticeCompetency> ProductionPracticeCompetencies => Set<ProductionPracticeCompetency>();
    public DbSet<ProductionPracticeStudentAssignment> ProductionPracticeStudentAssignments => Set<ProductionPracticeStudentAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();

            entity.HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Student" },
                new Role { Id = 3, Name = "Supervisor" },
                new Role { Id = 4, Name = "DepartmentStaff" }
            );
        });

        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.ToTable("specialties");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.ToTable("groups");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Course).IsRequired();

            entity.HasOne(x => x.Specialty)
                .WithMany(x => x.Groups)
                .HasForeignKey(x => x.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Surname).IsRequired().HasMaxLength(100);
            entity.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Patronymic).HasMaxLength(100);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(200);
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.AvatarUrl).HasMaxLength(300);
            entity.Property(x => x.Theme).IsRequired().HasMaxLength(20).HasDefaultValue("light");
            entity.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Group)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Category).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Action).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.ActorFullName).HasMaxLength(200);
            entity.Property(x => x.TargetUserFullName).HasMaxLength(200);

            entity.HasIndex(x => x.Category);
            entity.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<ProductionPractice>(entity =>
        {
            entity.ToTable("production_practices");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PracticeIndex).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(300);
            entity.Property(x => x.ProfessionalModuleCode).IsRequired().HasMaxLength(50);
            entity.Property(x => x.ProfessionalModuleName).IsRequired().HasMaxLength(300);
            entity.Property(x => x.Hours).IsRequired();
            entity.Property(x => x.StartDate).IsRequired();
            entity.Property(x => x.EndDate).IsRequired();

            entity.HasOne(x => x.Specialty)
                .WithMany()
                .HasForeignKey(x => x.SpecialtyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductionPracticeCompetency>(entity =>
        {
            entity.ToTable("production_practice_competencies");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CompetencyCode).IsRequired().HasMaxLength(100);
            entity.Property(x => x.CompetencyDescription).IsRequired().HasMaxLength(500);
            entity.Property(x => x.WorkTypes).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.Hours).IsRequired();

            entity.HasOne(x => x.ProductionPractice)
                .WithMany(x => x.Competencies)
                .HasForeignKey(x => x.ProductionPracticeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductionPracticeStudentAssignment>(entity =>
        {
            entity.ToTable("production_practice_student_assignments");
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.ProductionPractice)
                .WithMany(x => x.StudentAssignments)
                .HasForeignKey(x => x.ProductionPracticeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Supervisor)
                .WithMany()
                .HasForeignKey(x => x.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}