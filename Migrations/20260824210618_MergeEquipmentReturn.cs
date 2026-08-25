using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EquipmentDechargeManager.Migrations
{
    /// <inheritdoc />
    public partial class MergeEquipmentReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment_returns");

            migrationBuilder.AddColumn<string>(
                name: "return_condition",
                table: "decharge_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "return_date",
                table: "decharge_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_notes",
                table: "decharge_items",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "return_condition",
                table: "decharge_items");

            migrationBuilder.DropColumn(
                name: "return_date",
                table: "decharge_items");

            migrationBuilder.DropColumn(
                name: "return_notes",
                table: "decharge_items");

            migrationBuilder.CreateTable(
                name: "equipment_returns",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    decharge_item_id = table.Column<int>(type: "integer", nullable: false),
                    condition = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    return_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_equipment_returns_decharge_items_decharge_item_id",
                        column: x => x.decharge_item_id,
                        principalTable: "decharge_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_returns_decharge_item_id",
                table: "equipment_returns",
                column: "decharge_item_id",
                unique: true);
        }
    }
}
