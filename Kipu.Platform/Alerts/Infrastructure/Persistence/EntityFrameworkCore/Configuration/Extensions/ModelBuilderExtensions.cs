using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Alerts.Domain.Model.Aggregates;
using Kipu.Platform.Alerts.Domain.Model.Entities;
using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using ProductAggregate = Kipu.Platform.Products.Domain.Model.Aggregates.Product;
using Warehouse = Kipu.Platform.Products.Domain.Model.Aggregates.Warehouse;

namespace Kipu.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyAlertsConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Alert>(entity =>
        {
            entity.HasKey(alert => alert.Id);
            entity.Property(alert => alert.Id).ValueGeneratedOnAdd();
            entity.Property(alert => alert.ProductName).HasMaxLength(150);
            entity.Property(alert => alert.Type).IsRequired().HasMaxLength(30);
            entity.Property(alert => alert.Severity).IsRequired().HasMaxLength(20);
            entity.Property(alert => alert.Message).HasMaxLength(500);
            entity.Property(alert => alert.Status).IsRequired().HasMaxLength(20);
            entity.Property(alert => alert.CurrentStock).HasColumnType("decimal(10,2)");
            entity.Property(alert => alert.MinStock).HasColumnType("decimal(10,2)");
            entity.Property(alert => alert.CustomerOrSupplierName).HasMaxLength(150);
            entity.Property(alert => alert.Amount).HasColumnType("decimal(10,2)");

            entity.HasOne<Business>().WithMany().HasForeignKey(alert => alert.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional since X6 #7 — INSTALLMENT_DUE/SUPPLIER_INSTALLMENT_DUE
            // alerts have no product at all (see Alert's class doc comment).
            entity.HasOne<ProductAggregate>().WithMany().HasForeignKey(alert => alert.ProductId)
                .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
            entity.HasOne<Batch>().WithMany().HasForeignKey(alert => alert.BatchId)
                .OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(alert => alert.WarehouseId)
                .OnDelete(DeleteBehavior.SetNull).IsRequired(false);

            // INSTALLMENT_DUE (X6 #7) / SUPPLIER_INSTALLMENT_DUE (X6 #12,
            // Bloque G2) — the latter's column exists from this same
            // migration but stays unpopulated until G2 lands.
            entity.HasOne<Sale>().WithMany().HasForeignKey(alert => alert.SaleId)
                .OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            entity.HasOne<PurchaseOrder>().WithMany().HasForeignKey(alert => alert.PurchaseOrderId)
                .OnDelete(DeleteBehavior.SetNull).IsRequired(false);

            // Covers AlertExpirationSweepJob's batched lookup (BatchId IN (...)
            // AND Type IN (EXPIRATION, EXPIRED) AND Status != RESOLVED) — the
            // plain FK index on BatchId alone doesn't help the Type/Status filter.
            entity.HasIndex(alert => new { alert.BatchId, alert.Type, alert.Status });

            // Same reasoning, for InstallmentDueSweepJob's batched lookup
            // (SaleId IN (...) AND Type == INSTALLMENT_DUE AND Status != RESOLVED).
            entity.HasIndex(alert => new { alert.SaleId, alert.Type, alert.Status });
        });

        builder.Entity<AlertRule>(entity =>
        {
            entity.HasKey(rule => rule.Id);
            entity.Property(rule => rule.Id).ValueGeneratedOnAdd();
            entity.Property(rule => rule.AlertType).IsRequired().HasMaxLength(20);

            entity.HasOne<Business>().WithMany().HasForeignKey(rule => rule.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(rule => new { rule.BusinessId, rule.AlertType }).IsUnique();
        });
    }
}
