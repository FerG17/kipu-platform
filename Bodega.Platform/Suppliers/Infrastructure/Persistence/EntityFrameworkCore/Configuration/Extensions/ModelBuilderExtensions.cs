using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Products.Domain.Model.Aggregates;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Suppliers.Domain.Model.Aggregates;
using Bodega.Platform.Suppliers.Domain.Model.Entities;

namespace Bodega.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

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

            entity.HasOne<Product>().WithMany().HasForeignKey(detail => detail.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
