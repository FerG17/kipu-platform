using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierPaymentPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reordered by hand from what `dotnet ef migrations add` generated:
            // the new composite index must exist BEFORE the old single-column
            // one is dropped, or MySQL refuses the drop ("needed in a foreign
            // key constraint") — at every point in time, some index has to
            // support fk_alerts_purchase_order_purchase_order_id, and the
            // composite index (purchase_order_id, type, status) qualifies via
            // its leftmost column the moment it's created.
            migrationBuilder.CreateIndex(
                name: "ix_alerts_purchase_order_id_type_status",
                table: "alerts",
                columns: new[] { "purchase_order_id", "type", "status" });

            migrationBuilder.DropIndex(
                name: "ix_alerts_purchase_order_id",
                table: "alerts");

            migrationBuilder.AlterColumn<string>(
                name: "alert_type",
                table: "alert_rules",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateTable(
                name: "supplier_payment_plans",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    purchase_order_id = table.Column<int>(type: "int", nullable: false),
                    business_id = table.Column<int>(type: "int", nullable: false),
                    total_installments = table.Column<int>(type: "int", nullable: false),
                    paid_installments = table.Column<int>(type: "int", nullable: false),
                    is_cancelled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_payment_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_payment_plans_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_payment_plans_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "supplier_payment_installments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    supplier_payment_plan_id = table.Column<int>(type: "int", nullable: false),
                    number = table.Column<int>(type: "int", nullable: false),
                    due_date = table.Column<DateTime>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    is_paid = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_payment_installments", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_payment_installments_plan_id",
                        column: x => x.supplier_payment_plan_id,
                        principalTable: "supplier_payment_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "supplier_installment_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    supplier_payment_plan_id = table.Column<int>(type: "int", nullable: false),
                    supplier_payment_installment_id = table.Column<int>(type: "int", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    paid_by_user_id = table.Column<int>(type: "int", nullable: false),
                    is_reversed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    reversed_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    reversed_by_user_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_installment_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplier_installment_payments_installment_id",
                        column: x => x.supplier_payment_installment_id,
                        principalTable: "supplier_payment_installments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_supplier_installment_payments_plan_id",
                        column: x => x.supplier_payment_plan_id,
                        principalTable: "supplier_payment_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_installment_payments_supplier_payment_installment_id",
                table: "supplier_installment_payments",
                column: "supplier_payment_installment_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_installment_payments_supplier_payment_plan_id",
                table: "supplier_installment_payments",
                column: "supplier_payment_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payment_installments_supplier_payment_plan_id",
                table: "supplier_payment_installments",
                column: "supplier_payment_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payment_plans_business_id",
                table: "supplier_payment_plans",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payment_plans_purchase_order_id",
                table: "supplier_payment_plans",
                column: "purchase_order_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_installment_payments");

            migrationBuilder.DropTable(
                name: "supplier_payment_installments");

            migrationBuilder.DropTable(
                name: "supplier_payment_plans");

            // Same reordering as Up() and for the same reason: the plain
            // index must exist before the composite one that's currently
            // supporting the FK gets dropped.
            migrationBuilder.CreateIndex(
                name: "ix_alerts_purchase_order_id",
                table: "alerts",
                column: "purchase_order_id");

            migrationBuilder.DropIndex(
                name: "ix_alerts_purchase_order_id_type_status",
                table: "alerts");

            migrationBuilder.AlterColumn<string>(
                name: "alert_type",
                table: "alert_rules",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30);
        }
    }
}
