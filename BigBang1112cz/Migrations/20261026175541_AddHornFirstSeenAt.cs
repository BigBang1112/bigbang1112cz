using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112cz.Migrations
{
    /// <inheritdoc />
    public partial class AddHornFirstSeenAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstSeenAt",
                table: "Horns",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstSeenAt",
                table: "Horns");
        }
    }
}
