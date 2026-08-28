using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EquipmentDechargeManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialCleanSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    matricule = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    function = table.Column<string>(type: "text", nullable: false),
                    structure = table.Column<string>(type: "text", nullable: false),
                    region = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "equipments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    inventory_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sh_code = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "decharges",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    decharge_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_decharges", x => x.id);
                    table.ForeignKey(
                        name: "fk_decharges_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "decharge_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    decharge_id = table.Column<int>(type: "integer", nullable: false),
                    equipment_id = table.Column<int>(type: "integer", nullable: false),
                    condition_at_assignment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    assignment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    return_date = table.Column<DateOnly>(type: "date", nullable: true),
                    condition_returned = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_decharge_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_decharge_items_decharges_decharge_id",
                        column: x => x.decharge_id,
                        principalTable: "decharges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_decharge_items_equipments_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "equipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_decharge_items_decharge_id",
                table: "decharge_items",
                column: "decharge_id");

            migrationBuilder.CreateIndex(
                name: "ix_decharge_items_equipment_id",
                table: "decharge_items",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_decharges_decharge_number",
                table: "decharges",
                column: "decharge_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_decharges_employee_id",
                table: "decharges",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_matricule",
                table: "employees",
                column: "matricule",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_equipments_inventory_number",
                table: "equipments",
                column: "inventory_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_equipments_serial_number",
                table: "equipments",
                column: "serial_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "decharge_items");

            migrationBuilder.DropTable(
                name: "decharges");

            migrationBuilder.DropTable(
                name: "equipments");

            migrationBuilder.DropTable(
                name: "employees");
        }
    }
}
