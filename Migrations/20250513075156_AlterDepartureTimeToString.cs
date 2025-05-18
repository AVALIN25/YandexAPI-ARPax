using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightValidationService.Migrations
{
    /// <inheritdoc />
    public partial class AlterDepartureTimeToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DepartureTime",
                table: "flights",
                type: "varchar(5)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DepartureTime",
                table: "flights",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(5)");
        }
    }
}
