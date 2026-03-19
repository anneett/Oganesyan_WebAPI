using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oganesyan_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatabaseMetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LogicalName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ErdImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    CreateScriptTemplate = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseMetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbMetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    dbType = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionString = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbMetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Login = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Salt = table.Column<string>(type: "TEXT", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    InArchive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TelegramChatId = table.Column<long>(type: "INTEGER", nullable: true),
                    TelegramLinkCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    TelegramLinkCodeExpiry = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    DatabaseMetaId = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercises_DatabaseMetas_DatabaseMetaId",
                        column: x => x.DatabaseMetaId,
                        principalTable: "DatabaseMetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseDeployments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DatabaseMetaId = table.Column<int>(type: "INTEGER", nullable: false),
                    DbMetaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PhysicaDatabaseName = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeployed = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatabaseDeployments_DatabaseMetas_DatabaseMetaId",
                        column: x => x.DatabaseMetaId,
                        principalTable: "DatabaseMetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DatabaseDeployments_DbMetas_DbMetaId",
                        column: x => x.DbMetaId,
                        principalTable: "DbMetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Solutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeploymentId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserAnswer = table.Column<string>(type: "TEXT", nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Result = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solutions_DatabaseDeployments_DeploymentId",
                        column: x => x.DeploymentId,
                        principalTable: "DatabaseDeployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Solutions_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseDeployments_DatabaseMetaId_DbMetaId",
                table: "DatabaseDeployments",
                columns: new[] { "DatabaseMetaId", "DbMetaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseDeployments_DbMetaId",
                table: "DatabaseDeployments",
                column: "DbMetaId");

            migrationBuilder.CreateIndex(
                name: "IX_DbMetas_dbType",
                table: "DbMetas",
                column: "dbType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_DatabaseMetaId",
                table: "Exercises",
                column: "DatabaseMetaId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Title",
                table: "Exercises",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solutions_DeploymentId",
                table: "Solutions",
                column: "DeploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Solutions_ExerciseId",
                table: "Solutions",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                table: "Users",
                column: "Login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Solutions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "DatabaseDeployments");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "DbMetas");

            migrationBuilder.DropTable(
                name: "DatabaseMetas");
        }
    }
}
