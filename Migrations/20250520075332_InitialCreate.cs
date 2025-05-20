using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FlightValidationService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "flights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlightNumber = table.Column<string>(type: "text", nullable: false),
                    DepartureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepartureTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EditedByAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "check_log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FlightNumber = table.Column<string>(type: "text", nullable: false),
                    Result = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_log", x => x.Id);
                    table.ForeignKey(
                        name: "FK_check_log_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manual_flight_edits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlightId = table.Column<int>(type: "integer", nullable: false),
                    AdminId = table.Column<int>(type: "integer", nullable: false),
                    OldStatus = table.Column<string>(type: "text", nullable: false),
                    NewStatus = table.Column<string>(type: "text", nullable: false),
                    OldDeparture = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NewDeparture = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manual_flight_edits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_manual_flight_edits_flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_manual_flight_edits_users_AdminId",
                        column: x => x.AdminId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_check_log_UserId",
                table: "check_log",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_flights_FlightNumber_DepartureDate",
                table: "flights",
                columns: new[] { "FlightNumber", "DepartureDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_manual_flight_edits_AdminId",
                table: "manual_flight_edits",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_manual_flight_edits_FlightId",
                table: "manual_flight_edits",
                column: "FlightId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "check_log");

            migrationBuilder.DropTable(
                name: "manual_flight_edits");

            migrationBuilder.DropTable(
                name: "flights");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
