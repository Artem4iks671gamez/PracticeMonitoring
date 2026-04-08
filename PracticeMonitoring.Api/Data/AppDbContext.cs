using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

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

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(200);
            entity.Property(x => x.PasswordHash).IsRequired();

            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasOne(x => x.Role)
                  .WithMany(x => x.Users)
                  .HasForeignKey(x => x.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}