using Microsoft.EntityFrameworkCore;
using Bodega.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Dashboard.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Bodega.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
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
public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new AuditableEntityInterceptor());
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
    }
}
