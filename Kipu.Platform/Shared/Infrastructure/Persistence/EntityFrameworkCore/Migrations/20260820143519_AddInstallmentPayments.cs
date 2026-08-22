using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallmentPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "installment_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    payment_plan_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    paid_by_user_id = table.Column<int>(type: "int", nullable: false),
                    is_reversed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    reversed_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    reversed_by_user_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installment_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_installment_payments_payment_plan_payment_plan_id",
                        column: x => x.payment_plan_id,
                        principalTable: "payment_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_installment_payments_payment_plan_id",
                table: "installment_payments",
                column: "payment_plan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "installment_payments");
        }
    }
}
