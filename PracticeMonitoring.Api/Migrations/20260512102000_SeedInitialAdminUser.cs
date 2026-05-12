using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PracticeMonitoring.Api.Data;

#nullable disable

namespace PracticeMonitoring.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260512102000_SeedInitialAdminUser")]
    public partial class SeedInitialAdminUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO users
                    ("FullName", "Surname", "FirstName", "Patronymic", "Email", "PasswordHash", "RoleId", "GroupId", "AvatarUrl", "Theme", "IsActive", "MustChangePassword")
                SELECT
                    'Admin Admin',
                    'Admin',
                    'Admin',
                    NULL,
                    'admin@example.com',
                    'AQAAAAIAAYagAAAAEAKOqq4tiEGRZY8eAEqLU7EDjEJsdhGrX/AoRfuknnH717tHsZ3QQb/j9wDs7a3UcQ==',
                    r."Id",
                    NULL,
                    NULL,
                    'light',
                    true,
                    true
                FROM roles r
                WHERE r."Name" = 'Admin'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM users u
                      WHERE u."Email" = 'admin@example.com'
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM users
                WHERE "Email" = 'admin@example.com'
                  AND "RoleId" = (
                      SELECT "Id"
                      FROM roles
                      WHERE "Name" = 'Admin'
                  );
                """);
        }
    }
}
