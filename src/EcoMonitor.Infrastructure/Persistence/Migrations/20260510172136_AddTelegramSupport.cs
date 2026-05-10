using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "reporter_id",
                table: "dumpsite_reports",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "source",
                table: "dumpsite_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "telegram_user_id",
                table: "dumpsite_reports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telegram_user_name",
                table: "dumpsite_reports",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "telegram_user_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    telegram_user_id = table.Column<long>(type: "bigint", nullable: false),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    draft_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    draft_photo_file_ids = table.Column<string>(type: "text", nullable: false),
                    draft_latitude = table.Column<double>(type: "double precision", nullable: true),
                    draft_longitude = table.Column<double>(type: "double precision", nullable: true),
                    last_interaction_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_user_sessions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_reports_telegram_user_id",
                table: "dumpsite_reports",
                column: "telegram_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_user_sessions_telegram_user_id",
                table: "telegram_user_sessions",
                column: "telegram_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_user_sessions");

            migrationBuilder.DropIndex(
                name: "ix_dumpsite_reports_telegram_user_id",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "source",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "telegram_user_id",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "telegram_user_name",
                table: "dumpsite_reports");

            migrationBuilder.AlterColumn<Guid>(
                name: "reporter_id",
                table: "dumpsite_reports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
