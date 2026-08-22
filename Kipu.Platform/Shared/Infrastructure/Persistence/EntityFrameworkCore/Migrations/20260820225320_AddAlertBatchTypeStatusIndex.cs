using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertBatchTypeStatusIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create-then-drop, not drop-then-create: MySQL ties ix_alerts_batch_id
            // to the BatchId foreign key constraint and refuses to drop it while
            // it's the only index covering that column. The new composite index
            // also starts with batch_id, so it can stand in for the FK's backing
            // index — but only once it actually exists.
            migrationBuilder.CreateIndex(
                name: "ix_alerts_batch_id_type_status",
                table: "alerts",
                columns: new[] { "batch_id", "type", "status" });

            migrationBuilder.DropIndex(
                name: "ix_alerts_batch_id",
                table: "alerts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_alerts_batch_id",
                table: "alerts",
                column: "batch_id");

            migrationBuilder.DropIndex(
                name: "ix_alerts_batch_id_type_status",
                table: "alerts");
        }
    }
}
