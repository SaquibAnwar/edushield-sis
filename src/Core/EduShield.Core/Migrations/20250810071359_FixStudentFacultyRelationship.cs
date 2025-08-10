using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduShield.Core.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentFacultyRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Faculty_FacultyId",
                table: "Students");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Faculty_FacultyId",
                table: "Students",
                column: "FacultyId",
                principalTable: "Faculty",
                principalColumn: "FacultyId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Faculty_FacultyId",
                table: "Students");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Faculty_FacultyId",
                table: "Students",
                column: "FacultyId",
                principalTable: "Faculty",
                principalColumn: "FacultyId");
        }
    }
}
