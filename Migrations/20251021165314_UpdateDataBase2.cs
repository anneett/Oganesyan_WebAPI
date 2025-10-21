using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oganesyan_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDataBase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Users",
                newName: "Salt");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Salt",
                table: "Users",
                newName: "Password");
        }
    }
}
