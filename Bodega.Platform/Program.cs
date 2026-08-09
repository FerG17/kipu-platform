using Cortex.Mediator.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi;
using Bodega.Platform.Iam.Application.CommandServices;
using Bodega.Platform.Iam.Application.Internal.CommandServices;
using Bodega.Platform.Iam.Application.Internal.OutboundServices;
using Bodega.Platform.Iam.Application.Internal.QueryServices;
using Bodega.Platform.Iam.Application.QueryServices;
using Bodega.Platform.Iam.Domain.Repositories;
using Bodega.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;
using Bodega.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Bodega.Platform.Iam.Infrastructure.Pipeline.Middleware.Extensions;
using Bodega.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using Bodega.Platform.Iam.Infrastructure.Tokens.Jwt.Services;
using Bodega.Platform.Iam.Resources;
using Bodega.Platform.Products.Application.Acl;
using Bodega.Platform.Products.Application.CommandServices;
using Bodega.Platform.Products.Application.Internal.CommandServices;
using Bodega.Platform.Products.Application.Internal.QueryServices;
using Bodega.Platform.Products.Application.QueryServices;
using Bodega.Platform.Products.Domain.Repositories;
using Bodega.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Bodega.Platform.Products.Interfaces.Acl;
using Bodega.Platform.Products.Resources;
using Bodega.Platform.Dashboard.Application.CommandServices;
using Bodega.Platform.Dashboard.Application.Internal.CommandServices;
using Bodega.Platform.Dashboard.Application.Internal.QueryServices;
using Bodega.Platform.Dashboard.Application.QueryServices;
using Bodega.Platform.Dashboard.Domain.Repositories;
using Bodega.Platform.Dashboard.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Bodega.Platform.Dashboard.Resources;
using Bodega.Platform.Alerts.Application.CommandServices;
using Bodega.Platform.Alerts.Application.Internal.CommandServices;
using Bodega.Platform.Alerts.Application.QueryServices;
using Bodega.Platform.Alerts.Application.Internal.QueryServices;
using Bodega.Platform.Alerts.Domain.Repositories;
using Bodega.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Bodega.Platform.Alerts.Infrastructure.Pipeline.BackgroundServices;
using Bodega.Platform.Alerts.Resources;
using Bodega.Platform.Sales.Application.CommandServices;
using Bodega.Platform.Sales.Application.Internal.CommandServices;
using Bodega.Platform.Sales.Application.Internal.QueryServices;
using Bodega.Platform.Sales.Application.QueryServices;
using Bodega.Platform.Sales.Domain.Repositories;
using Bodega.Platform.Sales.Application.Acl;
using Bodega.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Bodega.Platform.Sales.Interfaces.Acl;
using Bodega.Platform.Sales.Resources;
using Bodega.Platform.Shared.Application;
using Bodega.Platform.Suppliers.Application.CommandServices;
using Bodega.Platform.Suppliers.Application.Internal.CommandServices;
using Bodega.Platform.Suppliers.Application.Internal.QueryServices;
using Bodega.Platform.Suppliers.Application.QueryServices;
using Bodega.Platform.Suppliers.Domain.Repositories;
using Bodega.Platform.Suppliers.Application.Acl;
using Bodega.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Bodega.Platform.Suppliers.Interfaces.Acl;
using Bodega.Platform.Suppliers.Resources;
using Bodega.Platform.Shared.Domain.Repositories;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Bodega.Platform.Shared.Infrastructure.Pipeline.Middleware.Extensions;
using Bodega.Platform.Shared.Infrastructure.Security;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Bodega.Platform.Shared.Resources;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers().AddDataAnnotationsLocalization();

builder.Services.AddProblemDetails();

// CORS — allows the Vue frontend (a different origin) to call this API.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("BodegaFrontend",
        policy => policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Database — MySQL via EF Core. Connection string may contain %VAR% placeholders
// (see appsettings.json), expanded from environment variables at startup.
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionStringTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionStringTemplate))
        throw new InvalidOperationException("Database connection string is not set in the configuration.");

    var connectionString = Environment.ExpandEnvironmentVariables(connectionStringTemplate);

    options.UseMySQL(connectionString)
        .UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>())
        .EnableDetailedErrors();

    if (builder.Environment.IsDevelopment())
        options.EnableSensitiveDataLogging();
});

// Localization — no ResourcesPath override: each bounded context keeps its
// own Resources/ folder mirroring its namespace (e.g. Iam/Resources/IamMessages.resx),
// not a single root-level Resources/ folder, so the default convention
// (resx path = type's namespace, relative to the project root) is what we want.
builder.Services.AddLocalization();
builder.Services.AddSingleton<IStringLocalizer<CommonMessages>, StringLocalizer<CommonMessages>>();
builder.Services.AddSingleton<IStringLocalizer<IamMessages>, StringLocalizer<IamMessages>>();
builder.Services.AddSingleton<IStringLocalizer<ProductMessages>, StringLocalizer<ProductMessages>>();
builder.Services.AddSingleton<IStringLocalizer<SalesMessages>, StringLocalizer<SalesMessages>>();
builder.Services.AddSingleton<IStringLocalizer<SuppliersMessages>, StringLocalizer<SuppliersMessages>>();
builder.Services.AddSingleton<IStringLocalizer<AlertsMessages>, StringLocalizer<AlertsMessages>>();
builder.Services.AddSingleton<IStringLocalizer<DashboardMessages>, StringLocalizer<DashboardMessages>>();

builder.Services.AddSingleton<ProblemDetailsFactory>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bodega Platform API",
        Version = "v1",
        Description = "Backend de gestion de inventario, ventas y proveedores para bodegas."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
    options.EnableAnnotations();
});

// Mediator — used for publishing domain events across bounded contexts
// (e.g. Product's StockLevelChangedEvent, consumed by Alerts in a future phase).
builder.Services.AddCortexMediator([typeof(Program)]);

// Shared infrastructure
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// IAM Bounded Context

builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();

builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IBusinessCommandService, BusinessCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IBusinessQueryService, BusinessQueryService>();
builder.Services.AddScoped<IRoleQueryService, RoleQueryService>();

// Product & Inventory Management Bounded Context

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IBatchRepository, BatchRepository>();
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();

builder.Services.AddScoped<IProductCommandService, ProductCommandService>();
builder.Services.AddScoped<IWarehouseCommandService, WarehouseCommandService>();
builder.Services.AddScoped<IInventoryCommandService, InventoryCommandService>();
builder.Services.AddScoped<IProductQueryService, ProductQueryService>();
builder.Services.AddScoped<IWarehouseQueryService, WarehouseQueryService>();
builder.Services.AddScoped<IInventoryQueryService, InventoryQueryService>();
builder.Services.AddScoped<IBatchQueryService, BatchQueryService>();
builder.Services.AddScoped<IStockMovementQueryService, StockMovementQueryService>();

builder.Services.AddScoped<IProductContextFacade, ProductContextFacade>();

// Sales & POS Management Bounded Context

builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

builder.Services.AddScoped<ISaleCommandService, SaleCommandService>();
builder.Services.AddScoped<ICustomerCommandService, CustomerCommandService>();
builder.Services.AddScoped<ISaleQueryService, SaleQueryService>();
builder.Services.AddScoped<ICustomerQueryService, CustomerQueryService>();

builder.Services.AddScoped<ISalesContextFacade, SalesContextFacade>();

// Supplier & Replenishment Management Bounded Context

builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();

builder.Services.AddScoped<ISupplierCommandService, SupplierCommandService>();
builder.Services.AddScoped<IPurchaseOrderCommandService, PurchaseOrderCommandService>();
builder.Services.AddScoped<ISupplierQueryService, SupplierQueryService>();
builder.Services.AddScoped<IPurchaseOrderQueryService, PurchaseOrderQueryService>();

builder.Services.AddScoped<ISupplierContextFacade, SupplierContextFacade>();

// Alerts & Operational Monitoring Bounded Context

builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();

builder.Services.AddScoped<IAlertCommandService, AlertCommandService>();
builder.Services.AddScoped<IAlertRuleCommandService, AlertRuleCommandService>();
builder.Services.AddScoped<IAlertQueryService, AlertQueryService>();
builder.Services.AddScoped<IAlertRuleQueryService, AlertRuleQueryService>();

// Event handlers are auto-discovered by AddCortexMediator's assembly scan
// (StockLevelChangedEventHandler, BatchRegisteredEventHandler) — no
// explicit registration needed here.
builder.Services.AddHostedService<AlertExpirationSweepJob>();

// Dashboard & Analytics Bounded Context — no aggregates of its own, composes
// Product/Sales via their ACL facades (already registered above).

builder.Services.AddScoped<IReportRepository, ReportRepository>();

builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();
builder.Services.AddScoped<IReportCommandService, ReportCommandService>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();

var app = builder.Build();

// Apply pending migrations on startup (safe to call even when schema is up to date).
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandler();

var supportedCultures = new[] { "en", "es" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("BodegaFrontend");

app.UseRouting();

app.UseRequestAuthorization();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
