using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerDistrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "district_id",
                table: "waste_containers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_waste_containers_district_id",
                table: "waste_containers",
                column: "district_id");

            migrationBuilder.AddForeignKey(
                name: "fk_waste_containers_districts_district_id",
                table: "waste_containers",
                column: "district_id",
                principalTable: "districts",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_waste_containers_districts_district_id",
                table: "waste_containers");

            migrationBuilder.DropIndex(
                name: "ix_waste_containers_district_id",
                table: "waste_containers");

            migrationBuilder.DropColumn(
                name: "district_id",
                table: "waste_containers");
        }
    }
}
