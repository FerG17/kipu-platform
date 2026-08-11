using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Dashboard.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Iam.Domain.Model.Aggregates;
using Bodega.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;

namespace Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Application database context for the Bodega Platform. A single DbContext
///     shared by every bounded context (modular monolith over one physical
///     database) — each context contributes its own ApplyXConfiguration(builder)
///     call here as it's built.
/// </summary>
public class AppDbContext(DbContextOptions options, ICurrentUserAccessor currentUserAccessor) : DbContext(options)
{
    /// <summary>
    ///     Read as a property (not captured into a local at OnModelCreating
    ///     time) so the compiled query filters below re-evaluate it against
    ///     whichever AppDbContext instance actually runs each query — EF Core
    ///     only builds the model once per context type, but re-reads instance
    ///     member accesses like this per execution. Null outside an
    ///     authenticated HTTP request (background jobs, unauthenticated
    ///     sign-in/sign-up lookups by email) — in that case the filters below
    ///     deliberately fail CLOSED (match nothing), not open. The handful of
    ///     call sites that legitimately need cross-tenant access in that
    ///     situation (email lookup during login, the alerts expiration sweep)
    ///     opt out explicitly with .IgnoreQueryFilters() at the repository
    ///     method level instead — so a bug that leaves CurrentBusinessId
    ///     unexpectedly null (e.g. a missing [Authorize]) can only ever
    ///     result in "no data", never "every tenant's data".
    /// </summary>
    private int? CurrentBusinessId => currentUserAccessor.CurrentBusinessId;

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new AuditableEntityInterceptor(), new ConcurrencyVersionInterceptor());
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // IAM Context
        builder.ApplyIamConfiguration();

        // Product & Inventory Management Context
        builder.ApplyProductConfiguration();

        // Sales & POS Management Context
        builder.ApplySalesConfiguration();

        // Supplier & Replenishment Management Context
        builder.ApplySuppliersConfiguration();

        // Alerts & Operational Monitoring Context
        builder.ApplyAlertsConfiguration();

        // Dashboard & Analytics Context
        builder.ApplyDashboardConfiguration();

        // General naming convention for the database objects — must run last,
        // after every bounded context has registered its entities above.
        builder.UseSnakeCaseNamingConvention();

        // Multi-tenant isolation, enforced at the ORM level for every query
        // (not just the ones a controller remembers to filter manually) —
        // see resumen/plan docs, "Fase B0.5". Must run last, after every
        // entity above is registered.
        ApplyBusinessScopedQueryFilters(builder);
    }

    /// <summary>
    ///     Applies a `BusinessId == CurrentBusinessId` query filter to every
    ///     entity that has a BusinessId column, plus one for Business itself
    ///     (a user may only ever see their own business record). Entities
    ///     that live strictly inside another aggregate's boundary and have no
    ///     BusinessId of their own (SaleDetail, PurchaseOrderDetail) are never
    ///     queried independently — no repository/endpoint exposes them by
    ///     their own id — so they're already covered transitively through
    ///     their parent (Sale/PurchaseOrder) and don't need their own filter.
    /// </summary>
    private void ApplyBusinessScopedQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType.GetProperty("BusinessId") is null) continue;

            typeof(AppDbContext)
                .GetMethod(nameof(SetBusinessIdQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(clrType)
                .Invoke(this, [builder]);
        }

        builder.Entity<Business>().HasQueryFilter(business => business.Id == CurrentBusinessId);
    }

    private void SetBusinessIdQueryFilter<TEntity>(ModelBuilder builder) where TEntity : class
    {
        builder.Entity<TEntity>().HasQueryFilter(entity => EF.Property<int>(entity, "BusinessId") == CurrentBusinessId);
    }
}
