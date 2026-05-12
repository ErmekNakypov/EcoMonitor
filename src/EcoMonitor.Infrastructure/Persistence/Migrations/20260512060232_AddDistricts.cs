using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDistricts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "district_id",
                table: "dumpsite_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name_ru = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    name_en = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    name_ky = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    color_hex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    assigned_inspector_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_districts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "district_boundary_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    district_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_district_boundary_points", x => x.id);
                    table.ForeignKey(
                        name: "fk_district_boundary_points_districts_district_id",
                        column: x => x.district_id,
                        principalTable: "districts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_reports_district_id",
                table: "dumpsite_reports",
                column: "district_id");

            migrationBuilder.CreateIndex(
                name: "ix_district_boundary_points_district_id_sequence_number",
                table: "district_boundary_points",
                columns: new[] { "district_id", "sequence_number" });

            migrationBuilder.CreateIndex(
                name: "ix_districts_code",
                table: "districts",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_dumpsite_reports_districts_district_id",
                table: "dumpsite_reports",
                column: "district_id",
                principalTable: "districts",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_dumpsite_reports_districts_district_id",
                table: "dumpsite_reports");

            migrationBuilder.DropTable(
                name: "district_boundary_points");

            migrationBuilder.DropTable(
                name: "districts");

            migrationBuilder.DropIndex(
                name: "ix_dumpsite_reports_district_id",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "district_id",
                table: "dumpsite_reports");
        }
    }
}
