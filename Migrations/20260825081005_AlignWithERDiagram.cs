using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EquipmentDechargeManager.Migrations
{
    /// <inheritdoc />
    public partial class AlignWithERDiagram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_decharge_items_equipment_id",
                table: "decharge_items");

            migrationBuilder.DropColumn(
                name: "is_returned",
                table: "decharge_items");

            migrationBuilder.DropColumn(
                name: "return_condition",
                table: "decharge_items");

            migrationBuilder.DropColumn(
                name: "return_date",
                table: "decharge_items");

            migrationBuilder.DropColumn(
                name: "return_notes",
                table: "decharge_items");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "decharges",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "equipment_returns",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    decharge_item_id = table.Column<int>(type: "integer", nullable: false),
                    return_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    condition_returned = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
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
                name: "ix_decharge_items_equipment_id",
                table: "decharge_items",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_returns_decharge_item_id",
                table: "equipment_returns",
                column: "decharge_item_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment_returns");

            migrationBuilder.DropIndex(
                name: "ix_decharge_items_equipment_id",
                table: "decharge_items");

            migrationBuilder.DropColumn(
                name: "status",
                table: "decharges");

            migrationBuilder.AddColumn<bool>(
                name: "is_returned",
                table: "decharge_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.CreateIndex(
                name: "ix_decharge_items_equipment_id",
                table: "decharge_items",
                column: "equipment_id",
                unique: true,
                filter: "is_returned = false");
        }
    }
}
