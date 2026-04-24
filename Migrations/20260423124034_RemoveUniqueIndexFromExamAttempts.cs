using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oganesyan_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueIndexFromExamAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_UserId_ExamId",
                table: "ExamAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_UserId",
                table: "ExamAttempts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_UserId",
                table: "ExamAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_UserId_ExamId",
                table: "ExamAttempts",
                columns: new[] { "UserId", "ExamId" },
                unique: true);
        }
    }
}
