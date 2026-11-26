using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzelDers.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonFeeDecisionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeeDecision",
                table: "Lessons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FeeDecisionAtUtc",
                table: "Lessons",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeeDecisionReason",
                table: "Lessons",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeeDecision",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "FeeDecisionAtUtc",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "FeeDecisionReason",
                table: "Lessons");
        }
    }
}
