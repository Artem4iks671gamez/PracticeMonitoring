using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PracticeMonitoring.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogArchivingAndSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "specialties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                INSERT INTO specialties ("Code", "Name", "IsArchived")
                SELECT '09.02.07', 'Разработчик веб и мультимедийных приложений', FALSE
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM specialties
                    WHERE "Code" = '09.02.07'
                      AND "Name" = 'Разработчик веб и мультимедийных приложений'
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO groups ("Name", "Course", "SpecialtyId", "IsArchived")
                SELECT 'ВД50-1-22', 4, s."Id", FALSE
                FROM specialties s
                WHERE s."Code" = '09.02.07'
                  AND s."Name" = 'Разработчик веб и мультимедийных приложений'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM groups g
                      WHERE g."Name" = 'ВД50-1-22'
                        AND g."SpecialtyId" = s."Id"
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "specialties");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "groups");
        }
    }
}
