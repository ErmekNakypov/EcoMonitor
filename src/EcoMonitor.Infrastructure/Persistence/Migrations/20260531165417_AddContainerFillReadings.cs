using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerFillReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "height_cm",
                table: "waste_containers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "last_distance_cm",
                table: "waste_containers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "last_fill_percent",
                table: "waste_containers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_measured_at",
                table: "waste_containers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "container_id",
                table: "iot_devices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "container_fill_readings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_guid = table.Column<Guid>(type: "uuid", nullable: false),
                    distance_cm = table.Column<double>(type: "double precision", nullable: false),
                    fill_percent = table.Column<double>(type: "double precision", nullable: false),
                    battery_mv = table.Column<int>(type: "integer", nullable: true),
                    measured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_container_fill_readings", x => x.id);
                    table.ForeignKey(
                        name: "fk_container_fill_readings_waste_containers_container_id",
                        column: x => x.container_id,
                        principalTable: "waste_containers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_iot_devices_container_id",
                table: "iot_devices",
                column: "container_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_fill_readings_container_id_measured_at",
                table: "container_fill_readings",
                columns: new[] { "container_id", "measured_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_container_fill_readings_measured_at",
                table: "container_fill_readings",
                column: "measured_at");

            migrationBuilder.AddForeignKey(
                name: "fk_iot_devices_waste_containers_container_id",
                table: "iot_devices",
                column: "container_id",
                principalTable: "waste_containers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_iot_devices_waste_containers_container_id",
                table: "iot_devices");

            migrationBuilder.DropTable(
                name: "container_fill_readings");

            migrationBuilder.DropIndex(
                name: "ix_iot_devices_container_id",
                table: "iot_devices");

            migrationBuilder.DropColumn(
                name: "height_cm",
                table: "waste_containers");

            migrationBuilder.DropColumn(
                name: "last_distance_cm",
                table: "waste_containers");

            migrationBuilder.DropColumn(
                name: "last_fill_percent",
                table: "waste_containers");

            migrationBuilder.DropColumn(
                name: "last_measured_at",
                table: "waste_containers");

            migrationBuilder.DropColumn(
                name: "container_id",
                table: "iot_devices");
        }
    }
}
