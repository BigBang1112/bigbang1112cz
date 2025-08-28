using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112cz.Migrations
{
    /// <inheritdoc />
    public partial class FixStringLengthIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "HornUsers",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Nickname",
                table: "HornUsers",
                type: "varchar(75)",
                maxLength: 75,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Login",
                table: "HornUsers",
                type: "varchar(75)",
                maxLength: 75,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "HornUserHistory",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Nickname",
                table: "HornUserHistory",
                type: "varchar(75)",
                maxLength: 75,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_HornUsers_Login",
                table: "HornUsers",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Horns_FileName",
                table: "Horns",
                column: "FileName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HornUsers_Login",
                table: "HornUsers");

            migrationBuilder.DropIndex(
                name: "IX_Horns_FileName",
                table: "Horns");

            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "HornUsers",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Nickname",
                table: "HornUsers",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(75)",
                oldMaxLength: 75,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Login",
                table: "HornUsers",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(75)",
                oldMaxLength: 75)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "HornUserHistory",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Nickname",
                table: "HornUserHistory",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(75)",
                oldMaxLength: 75,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
