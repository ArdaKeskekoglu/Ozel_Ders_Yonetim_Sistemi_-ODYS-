using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzelDers.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonAttendanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendanceStatus",
                table: "Lessons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendanceVerifiedAtUtc",
                table: "Lessons",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "AttendanceVerifiedAtUtc",
                table: "Lessons");
        }
    }
}
