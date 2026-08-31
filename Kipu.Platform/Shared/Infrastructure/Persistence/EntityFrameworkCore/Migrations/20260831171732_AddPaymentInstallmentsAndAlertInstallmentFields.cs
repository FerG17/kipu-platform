using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentInstallmentsAndAlertInstallmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "payment_installment_id",
                table: "installment_payments",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "alerts",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "product_name",
                table: "alerts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "alerts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "min_stock",
                table: "alerts",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "current_stock",
                table: "alerts",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "alerts",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_or_supplier_name",
                table: "alerts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "days_remaining",
                table: "alerts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "purchase_order_id",
                table: "alerts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sale_id",
                table: "alerts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_installments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    payment_plan_id = table.Column<int>(type: "int", nullable: false),
                    number = table.Column<int>(type: "int", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    is_paid = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_installments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_installments_payment_plan_payment_plan_id",
                        column: x => x.payment_plan_id,
                        principalTable: "payment_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_installment_payments_payment_installment_id",
                table: "installment_payments",
                column: "payment_installment_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_purchase_order_id",
                table: "alerts",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_sale_id_type_status",
                table: "alerts",
                columns: new[] { "sale_id", "type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_installments_payment_plan_id",
                table: "payment_installments",
                column: "payment_plan_id");

            migrationBuilder.AddForeignKey(
                name: "fk_alerts_purchase_order_purchase_order_id",
                table: "alerts",
                column: "purchase_order_id",
                principalTable: "purchase_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_alerts_sale_sale_id",
                table: "alerts",
                column: "sale_id",
                principalTable: "sales",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_installment_payments_installment_id",
                table: "installment_payments",
                column: "payment_installment_id",
                principalTable: "payment_installments",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_alerts_purchase_order_purchase_order_id",
                table: "alerts");

            migrationBuilder.DropForeignKey(
                name: "fk_alerts_sale_sale_id",
                table: "alerts");

            migrationBuilder.DropForeignKey(
                name: "fk_installment_payments_installment_id",
                table: "installment_payments");

            migrationBuilder.DropTable(
                name: "payment_installments");

            migrationBuilder.DropIndex(
                name: "ix_installment_payments_payment_installment_id",
                table: "installment_payments");

            migrationBuilder.DropIndex(
                name: "ix_alerts_purchase_order_id",
                table: "alerts");

            migrationBuilder.DropIndex(
                name: "ix_alerts_sale_id_type_status",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "payment_installment_id",
                table: "installment_payments");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "customer_or_supplier_name",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "days_remaining",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "purchase_order_id",
                table: "alerts");

            migrationBuilder.DropColumn(
                name: "sale_id",
                table: "alerts");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "alerts",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "product_name",
                table: "alerts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "alerts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "min_stock",
                table: "alerts",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "current_stock",
                table: "alerts",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);
        }
    }
}
