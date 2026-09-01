using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Kipu.Platform.Suppliers.Domain.Model.Aggregates;
using Kipu.Platform.Suppliers.Domain.Model.Entities;

namespace Kipu.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySuppliersConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Supplier>(entity =>
        {
            entity.HasKey(supplier => supplier.Id);
            entity.Property(supplier => supplier.Id).ValueGeneratedOnAdd();
            entity.Property(supplier => supplier.Name).IsRequired().HasMaxLength(150);
            entity.Property(supplier => supplier.LastName).HasMaxLength(150);
            entity.Property(supplier => supplier.Ruc).HasMaxLength(20);
            entity.Property(supplier => supplier.Email).HasMaxLength(150);
            entity.Property(supplier => supplier.Phone).HasMaxLength(20);
            entity.Property(supplier => supplier.Address).HasMaxLength(255);
            entity.Property(supplier => supplier.ContactPerson).HasMaxLength(150);
            // Reuses Shared.ProductCategory's vocabulary — plain string, not
            // a separate SupplierCategory enum (see architecture doc §6.6).
            entity.Property(supplier => supplier.Category).IsRequired().HasMaxLength(50);
            entity.Property(supplier => supplier.Status).IsRequired().HasMaxLength(20);
            entity.Property(supplier => supplier.Since).HasDateOnlyConversion();

            entity.HasOne<Business>().WithMany().HasForeignKey(supplier => supplier.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Id).ValueGeneratedOnAdd();
            entity.Property(order => order.Status).IsRequired().HasMaxLength(20);
            entity.Property(order => order.Currency).IsRequired().HasMaxLength(10);
            entity.Property(order => order.Description).HasMaxLength(500);
            entity.Property(order => order.Date).HasDateOnlyConversion();
            entity.Property(order => order.ExpectedDate).HasDateOnlyConversion();
            entity.Property(order => order.ReceivedDate).HasDateOnlyConversion();

            // Makes the move to RECEIVED conditional on the status the request
            // read, so a double submit books the delivery into inventory once
            // rather than once per request. See IVersionedEntity.
            entity.Property(order => order.Version).IsConcurrencyToken();

            entity.HasOne<Business>().WithMany().HasForeignKey(order => order.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Supplier>().WithMany().HasForeignKey(order => order.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(order => order.Details).WithOne().HasForeignKey(detail => detail.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(PurchaseOrder.Details))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<PurchaseOrderDetail>(entity =>
        {
            entity.HasKey(detail => detail.Id);
            entity.Property(detail => detail.Id).ValueGeneratedOnAdd();
            entity.Property(detail => detail.UnitPrice).HasColumnType("decimal(10,2)");
            entity.Property(detail => detail.Discount).HasColumnType("decimal(5,4)");
            entity.Property(detail => detail.DeliveryStatus).HasMaxLength(20);
            entity.Property(detail => detail.DeliveryTrackingNum).HasMaxLength(50);
            entity.Property(detail => detail.BatchLabel).HasMaxLength(60);

            entity.HasOne<Product>().WithMany().HasForeignKey(detail => detail.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Credit purchase orders (X6 #12) — mirrors Sales' PaymentPlan/InstallmentPayment
        // configuration (X6 #7) exactly.
        builder.Entity<SupplierPaymentPlan>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Id).ValueGeneratedOnAdd();

            // Keeps two payments registered at the same instant from both
            // counting against the same installment. See IVersionedEntity.
            entity.Property(plan => plan.Version).IsConcurrencyToken();

            entity.HasOne<Business>().WithMany().HasForeignKey(plan => plan.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            // One plan per purchase order — mirrors "at most one plan per order" in the domain.
            entity.HasOne<PurchaseOrder>().WithOne().HasForeignKey<SupplierPaymentPlan>(plan => plan.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(plan => plan.PurchaseOrderId).IsUnique();

            // Payments/Installments are private-field-backed collections, same
            // treatment as Sales.PaymentPlan.Payments/Installments. Explicit
            // short constraint names — the auto-generated ones for this shape
            // ("supplier_payment_installments"/"supplier_installment_payments"
            // + "supplier_payment_plan" twice) came out over MySQL's 64-char
            // identifier limit, the same class of bug the installment_id FK
            // below already works around.
            entity.HasMany(plan => plan.Payments).WithOne().HasForeignKey(payment => payment.SupplierPaymentPlanId)
                .HasConstraintName("fk_supplier_installment_payments_plan_id")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(SupplierPaymentPlan.Payments))!.SetPropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(plan => plan.Installments).WithOne().HasForeignKey(installment => installment.SupplierPaymentPlanId)
                .HasConstraintName("fk_supplier_payment_installments_plan_id")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(SupplierPaymentPlan.Installments))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<SupplierInstallmentPayment>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Id).ValueGeneratedOnAdd();
            entity.Property(payment => payment.Amount).HasColumnType("decimal(10,2)");

            // Explicit short name — see Sales.InstallmentPayment's own FK
            // config comment: the auto-generated name for this exact shape
            // came out over MySQL's 64-char identifier limit and silently
            // broke Migrate() partway through.
            entity.HasOne<SupplierPaymentInstallment>().WithMany().HasForeignKey(payment => payment.SupplierPaymentInstallmentId)
                .HasConstraintName("fk_supplier_installment_payments_installment_id")
                .OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        builder.Entity<SupplierPaymentInstallment>(entity =>
        {
            entity.HasKey(installment => installment.Id);
            entity.Property(installment => installment.Id).ValueGeneratedOnAdd();
            entity.Property(installment => installment.Amount).HasColumnType("decimal(10,2)");
            entity.Property(installment => installment.DueDate).HasDateOnlyConversion();
        });
    }
}
