using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Aggregates;
using Kipu.Platform.Sales.Domain.Model.Entities;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

namespace Kipu.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySalesConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Sale>(entity =>
        {
            entity.HasKey(sale => sale.Id);
            entity.Property(sale => sale.Id).ValueGeneratedOnAdd();
            entity.Property(sale => sale.Status).IsRequired().HasMaxLength(20);
            entity.Property(sale => sale.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(sale => sale.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(sale => sale.Description).HasMaxLength(500);
            entity.Property(sale => sale.Currency).IsRequired().HasMaxLength(10);
            entity.Property(sale => sale.IdempotencyKey).HasMaxLength(64);

            // MySQL treats every NULL as distinct in a unique index, so this
            // only ever rejects a genuine repeat key for the same business —
            // sales without one (IdempotencyKey null) never collide with
            // each other.
            entity.HasIndex(sale => new { sale.BusinessId, sale.IdempotencyKey }).IsUnique();

            // Makes a cancellation conditional on the sale still being in the
            // state the request read, so simultaneous cancellations can't each
            // return the same units to stock. See IVersionedEntity.
            entity.Property(sale => sale.Version).IsConcurrencyToken();

            entity.HasOne<Business>().WithMany().HasForeignKey(sale => sale.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            // Anonymous sales are valid (customerId: null) — see §6.4.
            entity.HasOne<Customer>().WithMany().HasForeignKey(sale => sale.CustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // SaleDetails is a private-field-backed collection — the aggregate
            // boundary is enforced in code (Sale.AddLine), EF just needs to
            // know where to materialize rows into.
            entity.HasMany(sale => sale.SaleDetails).WithOne().HasForeignKey(detail => detail.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(Sale.SaleDetails))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<SaleDetail>(entity =>
        {
            entity.HasKey(detail => detail.Id);
            entity.Property(detail => detail.Id).ValueGeneratedOnAdd();
            entity.Property(detail => detail.Quantity).HasColumnType("decimal(10,2)");
            entity.Property(detail => detail.UnitPrice).HasColumnType("decimal(10,2)");
            entity.Property(detail => detail.Discount).HasColumnType("decimal(5,4)");

            entity.HasOne<Product>().WithMany().HasForeignKey(detail => detail.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Customer>(entity =>
        {
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Id).ValueGeneratedOnAdd();
            entity.Property(customer => customer.FullName).IsRequired().HasMaxLength(150);
            entity.Property(customer => customer.DocumentNumber).HasMaxLength(20);
            entity.Property(customer => customer.PhoneNumber).HasMaxLength(20);
            entity.Property(customer => customer.Email).HasMaxLength(150);
            entity.Property(customer => customer.Status).IsRequired().HasMaxLength(20);

            entity.HasOne<Business>().WithMany().HasForeignKey(customer => customer.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentPlan>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Id).ValueGeneratedOnAdd();

            // Keeps two payments registered at the same instant from both
            // counting against the same installment. See IVersionedEntity.
            entity.Property(plan => plan.Version).IsConcurrencyToken();

            entity.HasOne<Business>().WithMany().HasForeignKey(plan => plan.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            // One plan per sale — mirrors "at most one plan per sale" in the
            // domain (PaymentPlanCommandService rejects a second one).
            entity.HasOne<Sale>().WithOne().HasForeignKey<PaymentPlan>(plan => plan.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(plan => plan.SaleId).IsUnique();

            // Payments is a private-field-backed collection, same treatment
            // as Sale.SaleDetails — the aggregate boundary is enforced in
            // code (PaymentPlan.RegisterPayment/RevertLastPayment), EF just
            // needs to know where to materialize rows into.
            entity.HasMany(plan => plan.Payments).WithOne().HasForeignKey(payment => payment.PaymentPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(PaymentPlan.Payments))!.SetPropertyAccessMode(PropertyAccessMode.Field);

            // Installments is the cuota calendar (X6 #7) — same private-field-backed
            // treatment as Payments above.
            entity.HasMany(plan => plan.Installments).WithOne().HasForeignKey(installment => installment.PaymentPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Metadata.FindNavigation(nameof(PaymentPlan.Installments))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<InstallmentPayment>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Id).ValueGeneratedOnAdd();
            entity.Property(payment => payment.Amount).HasColumnType("decimal(10,2)");

            // Which scheduled cuota this payment fulfilled — SetNull rather than
            // Restrict/Cascade: a PaymentInstallment is never deleted on its own
            // (only cascades away with its whole PaymentPlan), but keeping this
            // nullable/SetNull matches InstallmentPayment's own role as an
            // append-only audit trail that must never block on FK issues.
            // Explicit short name — the auto-generated one (table + principal +
            // column, all "payment_installment"-heavy) came out 65 characters,
            // one over MySQL's 64-char identifier limit, and silently broke
            // Migrate() partway through (the DDL before it stays applied,
            // non-transactional in MySQL, while the migration is never
            // recorded as done).
            entity.HasOne<PaymentInstallment>().WithMany().HasForeignKey(payment => payment.PaymentInstallmentId)
                .HasConstraintName("fk_installment_payments_installment_id")
                .OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        builder.Entity<PaymentInstallment>(entity =>
        {
            entity.HasKey(installment => installment.Id);
            entity.Property(installment => installment.Id).ValueGeneratedOnAdd();
            entity.Property(installment => installment.Amount).HasColumnType("decimal(10,2)");
            entity.Property(installment => installment.DueDate).HasDateOnlyConversion();
        });
    }
}
