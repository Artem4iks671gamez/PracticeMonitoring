using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PracticeMonitoring.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentDiaryAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DetailedReport",
                table: "student_practice_diary_entries",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8000)",
                oldMaxLength: 8000);

            migrationBuilder.CreateTable(
                name: "student_practice_diary_attachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentPracticeDiaryEntryId = table.Column<int>(type: "integer", nullable: false),
                    Caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_practice_diary_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_practice_diary_attachments_student_practice_diary_e~",
                        column: x => x.StudentPracticeDiaryEntryId,
                        principalTable: "student_practice_diary_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_student_practice_diary_attachments_StudentPracticeDiaryEntr~",
                table: "student_practice_diary_attachments",
                columns: new[] { "StudentPracticeDiaryEntryId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_practice_diary_attachments");

            migrationBuilder.AlterColumn<string>(
                name: "DetailedReport",
                table: "student_practice_diary_entries",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20000)",
                oldMaxLength: 20000);
        }
    }
}
