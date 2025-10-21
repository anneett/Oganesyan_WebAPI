using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oganesyan_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDataBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserSQL",
                table: "Solutions");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Users",
                newName: "IsAdmin");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "Difficulty",
                table: "Exercises",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "IsAdmin",
                table: "Users",
                newName: "Role");

            migrationBuilder.AddColumn<string>(
                name: "UserSQL",
                table: "Solutions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Difficulty",
                table: "Exercises",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }
    }
}
