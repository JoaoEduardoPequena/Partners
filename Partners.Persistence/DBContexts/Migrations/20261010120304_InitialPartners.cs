using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partners.Persistence.DBContexts.Migrations
{
    /// <inheritdoc />
    public partial class InitialPartners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "partner",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    tax_number = table.Column<string>(type: "varchar(9)", unicode: false, maxLength: 9, nullable: false),
                    type = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    credit_limit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner", x => x.id);
                    table.CheckConstraint("ck_partner_credit_limit", "[credit_limit] >= 0 AND [credit_limit] <= 1000000");
                    table.CheckConstraint("ck_partner_tax_number", "LEN([tax_number]) = 9 AND [tax_number] NOT LIKE '%[^0-9]%'");
                    table.CheckConstraint("ck_partner_type", "[type] IN ('CUSTOMER', 'SUPPLIER', 'BOTH')");
                });

            migrationBuilder.CreateIndex(
                name: "idx_partner_name_id",
                table: "partner",
                columns: new[] { "name", "id" });

            migrationBuilder.CreateIndex(
                name: "idx_partner_tax_number",
                table: "partner",
                column: "tax_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partner");
        }
    }
}
