using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppealMechanism : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "appeal_outcome",
                table: "dumpsite_reports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "appeal_reason",
                table: "dumpsite_reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "appeal_resolution_notes",
                table: "dumpsite_reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "appeal_reviewed_at",
                table: "dumpsite_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "appeal_reviewed_by_inspector_id",
                table: "dumpsite_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "appealed_at",
                table: "dumpsite_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "closed_at",
                table: "dumpsite_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dumpsite_appeal_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    uploaded_by_citizen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dumpsite_appeal_photos", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_reports_resolved_at",
                table: "dumpsite_reports",
                column: "resolved_at");

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_appeal_photos_report_id",
                table: "dumpsite_appeal_photos",
                column: "report_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dumpsite_appeal_photos");

            migrationBuilder.DropIndex(
                name: "ix_dumpsite_reports_resolved_at",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "appeal_outcome",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "appeal_reason",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "appeal_resolution_notes",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "appeal_reviewed_at",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "appeal_reviewed_by_inspector_id",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "appealed_at",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "closed_at",
                table: "dumpsite_reports");
        }
    }
}
