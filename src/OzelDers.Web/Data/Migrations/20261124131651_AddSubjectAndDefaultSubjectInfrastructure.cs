using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzelDers.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectAndDefaultSubjectInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subjects_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_TeacherId_IsArchived",
                table: "Subjects",
                columns: new[] { "TeacherId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_TeacherId_IsDefault",
                table: "Subjects",
                column: "TeacherId",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsArchived] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_TeacherId_Name",
                table: "Subjects",
                columns: new[] { "TeacherId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
