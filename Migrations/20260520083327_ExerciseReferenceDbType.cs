using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oganesyan_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class ExerciseReferenceDbType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceDbType",
                table: "Exercises",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceDbType",
                table: "Exercises");
        }
    }
}
