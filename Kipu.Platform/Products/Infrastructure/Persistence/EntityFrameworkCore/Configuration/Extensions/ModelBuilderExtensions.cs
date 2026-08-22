using Microsoft.EntityFrameworkCore;
using Kipu.Platform.Iam.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Aggregates;
using Kipu.Platform.Products.Domain.Model.Entities;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

namespace Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyProductConfiguration(this ModelBuilder builder)
    {
        // BusinessId is a real FK to IAM's Business table on every entity
        // below — same pattern as Iam's User.BusinessId (see IAM's
        // ModelBuilderExtensions) — strong tenant-isolation guarantee at the
        // database level, even though Product and IAM are separate bounded
        // contexts (acceptable here because it's one shared physical
        // database — a modular monolith, not separate services).

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).ValueGeneratedOnAdd();
            entity.Property(product => product.Name).IsRequired().HasMaxLength(150);
            entity.Property(product => product.Description).HasMaxLength(500);
            entity.Property(product => product.Category).IsRequired().HasMaxLength(50);
            entity.Property(product => product.BasePrice).HasColumnType("decimal(10,2)");
            entity.Property(product => product.Status).IsRequired().HasMaxLength(20);
            entity.Property(product => product.Barcode).HasMaxLength(64);
            entity.Property(product => product.UnitOfSale).IsRequired().HasMaxLength(20);

            entity.HasOne<Business>().WithMany().HasForeignKey(product => product.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(warehouse => warehouse.Id);
            entity.Property(warehouse => warehouse.Id).ValueGeneratedOnAdd();
            entity.Property(warehouse => warehouse.Name).IsRequired().HasMaxLength(150);
            entity.Property(warehouse => warehouse.Code).HasMaxLength(30);
            entity.Property(warehouse => warehouse.Address).HasMaxLength(255);
            entity.Property(warehouse => warehouse.Status).IsRequired().HasMaxLength(20);
            entity.Property(warehouse => warehouse.Capacity).IsRequired().HasMaxLength(20);

            entity.HasOne<Business>().WithMany().HasForeignKey(warehouse => warehouse.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).ValueGeneratedOnAdd();

            // Two tills selling the same shelf land on this row at the same
            // time; the token makes each UPDATE conditional on the value the
            // request actually read, so the loser is rejected instead of
            // overwriting the winner's decrement. See IVersionedEntity.
            entity.Property(item => item.Version).IsConcurrencyToken();

            // X5 Bloque D: decimal so a "se vende por peso" product can carry
            // a fractional stock (e.g. 2.35 kg on the shelf).
            entity.Property(item => item.StockUnit).HasColumnType("decimal(10,2)");
            entity.Property(item => item.MinimumStock).HasColumnType("decimal(10,2)");

            // Real N:M — one product can have independent stock per
            // warehouse (architecture doc §8.1).
            entity.HasIndex(item => new { item.ProductId, item.WarehouseId }).IsUnique();

            entity.HasOne<Product>().WithMany().HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(item => item.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Business>().WithMany().HasForeignKey(item => item.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Batch>(entity =>
        {
            entity.HasKey(batch => batch.Id);
            entity.Property(batch => batch.Id).ValueGeneratedOnAdd();
            entity.Property(batch => batch.PurchasePrice).HasColumnType("decimal(10,2)");
            entity.Property(batch => batch.Status).IsRequired().HasMaxLength(20);
            entity.Property(batch => batch.Quantity).HasColumnType("decimal(10,2)");
            entity.Property(batch => batch.RemainingQuantity).HasColumnType("decimal(10,2)");

            entity.Property(batch => batch.Expiration).HasDateOnlyConversion();

            entity.HasOne<Product>().WithMany().HasForeignKey(batch => batch.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Business>().WithMany().HasForeignKey(batch => batch.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            // X5 Bloque C: a real FK via the InventoryItem navigation (not a
            // plain `int` copied by hand) — every batch is now always
            // created together with a real InventoryItem in the same
            // SaveChanges call, so EF Core's own key fixup resolves the id
            // correctly even before the InventoryItem is persisted. This
            // also fixes the InventoryId=0 bug (X5 #9) that the old
            // hand-copied `int?` had. The shadow column is named
            // InventoryItemId (not InventoryId) to avoid colliding with the
            // entity's read-only InventoryId convenience property, which is
            // unmapped below.
            entity.Ignore(batch => batch.InventoryId);
            entity.HasOne(batch => batch.InventoryItem).WithMany()
                .HasForeignKey("InventoryItemId").IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(movement => movement.Id);
            entity.Property(movement => movement.Id).ValueGeneratedOnAdd();
            entity.Property(movement => movement.Quantity).HasColumnType("decimal(10,2)");
            entity.Property(movement => movement.Type).IsRequired().HasMaxLength(20);
            entity.Property(movement => movement.Supplier).HasMaxLength(150);
            entity.Property(movement => movement.Note).HasMaxLength(500);

            entity.HasOne<Product>().WithMany().HasForeignKey(movement => movement.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(movement => movement.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Business>().WithMany().HasForeignKey(movement => movement.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            // X5 Bloque C: which lot this movement touched, nullable (old
            // rows, and Adjustment movements, have none). Same "assign via
            // navigation, not a hand-copied int" reasoning as Batch.InventoryItem
            // above — an intake's StockMovement and its new Batch commit in
            // the same SaveChanges call.
            entity.HasOne(movement => movement.Batch).WithMany()
                .HasForeignKey("BatchId").IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ProductSupplier>(entity =>
        {
            entity.HasKey(link => link.Id);
            entity.Property(link => link.Id).ValueGeneratedOnAdd();

            // A product tags the same supplier at most once.
            entity.HasIndex(link => new { link.ProductId, link.SupplierId }).IsUnique();

            entity.HasOne<Product>().WithMany().HasForeignKey(link => link.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Business>().WithMany().HasForeignKey(link => link.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);
            // SupplierId intentionally has no FK constraint — same
            // cross-bounded-context soft reference as StockMovement.SupplierId;
            // Suppliers is a separate bounded context, and existence/tenant
            // ownership is validated in ProductCommandService before a link
            // is ever created, via ISupplierContextFacade.
        });
    }
}
