using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineCourses.Data.Migrations
{
    public partial class AddLessonFileMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Lessons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Lessons",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "Lessons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "Lessons",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "Lessons");
        }
    }
}
