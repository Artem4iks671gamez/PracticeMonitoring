using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PracticeMonitoring.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorDiaryReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReviewed",
                table: "student_practice_diary_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "student_practice_diary_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedBySupervisorId",
                table: "student_practice_diary_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorComment",
                table: "student_practice_diary_entries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorGrade",
                table: "student_practice_diary_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                DELETE FROM student_practice_diary_attachments;

                UPDATE student_practice_diary_entries
                SET
                    "DetailedReport" = '',
                    "IsReviewed" = FALSE,
                    "SupervisorGrade" = NULL,
                    "SupervisorComment" = NULL,
                    "ReviewedAtUtc" = NULL,
                    "ReviewedBySupervisorId" = NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_student_practice_diary_entries_ReviewedBySupervisorId",
                table: "student_practice_diary_entries",
                column: "ReviewedBySupervisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_student_practice_diary_entries_users_ReviewedBySupervisorId",
                table: "student_practice_diary_entries",
                column: "ReviewedBySupervisorId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_student_practice_diary_entries_users_ReviewedBySupervisorId",
                table: "student_practice_diary_entries");

            migrationBuilder.DropIndex(
                name: "IX_student_practice_diary_entries_ReviewedBySupervisorId",
                table: "student_practice_diary_entries");

            migrationBuilder.DropColumn(
                name: "IsReviewed",
                table: "student_practice_diary_entries");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "student_practice_diary_entries");

            migrationBuilder.DropColumn(
                name: "ReviewedBySupervisorId",
                table: "student_practice_diary_entries");

            migrationBuilder.DropColumn(
                name: "SupervisorComment",
                table: "student_practice_diary_entries");

            migrationBuilder.DropColumn(
                name: "SupervisorGrade",
                table: "student_practice_diary_entries");
        }
    }
}
