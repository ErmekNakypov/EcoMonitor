using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DomainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "air_quality_readings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    pm25 = table.Column<double>(type: "double precision", nullable: true),
                    pm10 = table.Column<double>(type: "double precision", nullable: true),
                    temperature = table.Column<double>(type: "double precision", nullable: true),
                    humidity = table.Column<double>(type: "double precision", nullable: true),
                    pressure = table.Column<double>(type: "double precision", nullable: true),
                    measured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_air_quality_readings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dumpsite_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    photo_paths = table.Column<string>(type: "jsonb", nullable: false),
                    assigned_inspector_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dumpsite_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "waste_containers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    installed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waste_containers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_air_quality_readings_measured_at",
                table: "air_quality_readings",
                column: "measured_at");

            migrationBuilder.CreateIndex(
                name: "ix_air_quality_readings_station_id_measured_at",
                table: "air_quality_readings",
                columns: new[] { "station_id", "measured_at" });

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_reports_assigned_inspector_id",
                table: "dumpsite_reports",
                column: "assigned_inspector_id");

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_reports_reporter_id",
                table: "dumpsite_reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_reports_status",
                table: "dumpsite_reports",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_waste_containers_code",
                table: "waste_containers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waste_containers_status",
                table: "waste_containers",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "air_quality_readings");

            migrationBuilder.DropTable(
                name: "dumpsite_reports");

            migrationBuilder.DropTable(
                name: "waste_containers");
        }
    }
}
