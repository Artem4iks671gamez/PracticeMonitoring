using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PracticeMonitoring.Api.Data;

#nullable disable

namespace PracticeMonitoring.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260517165000_SeedHostedTestAdminUser")]
    public partial class SeedHostedTestAdminUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO users
                    ("FullName", "Surname", "FirstName", "Patronymic", "Email", "PasswordHash", "RoleId", "GroupId", "AvatarUrl", "Theme", "IsActive", "MustChangePassword")
                SELECT
                    'Test Admin',
                    'Test',
                    'Admin',
                    NULL,
                    'isip_a.o.kurbatov+test100@mpt.ru',
                    'AQAAAAIAAYagAAAAEJ0ExB/1w8BPc3kRroZDBUTDnKFXfh0neDWJ2QEP1WwGAPbAKC3cglRgssUJYRoMvQ==',
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
                      WHERE u."Email" = 'isip_a.o.kurbatov+test100@mpt.ru'
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM users
                WHERE "Email" = 'isip_a.o.kurbatov+test100@mpt.ru'
                  AND "RoleId" = (
                      SELECT "Id"
                      FROM roles
                      WHERE "Name" = 'Admin'
                  );
                """);
        }
    }
}
