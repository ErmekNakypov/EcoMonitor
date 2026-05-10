using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAirQualityStations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_air_quality_readings_station_id_measured_at",
                table: "air_quality_readings");

            migrationBuilder.CreateTable(
                name: "air_quality_stations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    locality = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    provider_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_reading_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_air_quality_stations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_air_quality_readings_station_id_measured_at",
                table: "air_quality_readings",
                columns: new[] { "station_id", "measured_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_air_quality_stations_external_id_source",
                table: "air_quality_stations",
                columns: new[] { "external_id", "source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_air_quality_stations_is_active",
                table: "air_quality_stations",
                column: "is_active");

            migrationBuilder.AddForeignKey(
                name: "fk_air_quality_readings_air_quality_stations_station_id",
                table: "air_quality_readings",
                column: "station_id",
                principalTable: "air_quality_stations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_air_quality_readings_air_quality_stations_station_id",
                table: "air_quality_readings");

            migrationBuilder.DropTable(
                name: "air_quality_stations");

            migrationBuilder.DropIndex(
                name: "ix_air_quality_readings_station_id_measured_at",
                table: "air_quality_readings");

            migrationBuilder.CreateIndex(
                name: "ix_air_quality_readings_station_id_measured_at",
                table: "air_quality_readings",
                columns: new[] { "station_id", "measured_at" });
        }
    }
}
