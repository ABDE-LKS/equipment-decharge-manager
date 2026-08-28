using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentDechargeManager.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDechargeUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_decharge_items_equipments_equipment_id",
                table: "decharge_items");

            migrationBuilder.DropForeignKey(
                name: "fk_decharges_employees_employee_id",
                table: "decharges");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "decharges");

            migrationBuilder.AlterColumn<int>(
                name: "employee_id",
                table: "decharges",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "equipment_id",
                table: "decharge_items",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "fk_decharge_items_equipments_equipment_id",
                table: "decharge_items",
                column: "equipment_id",
                principalTable: "equipments",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_decharges_employees_employee_id",
                table: "decharges",
                column: "employee_id",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_decharge_items_equipments_equipment_id",
                table: "decharge_items");

            migrationBuilder.DropForeignKey(
                name: "fk_decharges_employees_employee_id",
                table: "decharges");

            migrationBuilder.AlterColumn<int>(
                name: "employee_id",
                table: "decharges",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "decharges",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "equipment_id",
                table: "decharge_items",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_decharge_items_equipments_equipment_id",
                table: "decharge_items",
                column: "equipment_id",
                principalTable: "equipments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_decharges_employees_employee_id",
                table: "decharges",
                column: "employee_id",
                principalTable: "employees",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
