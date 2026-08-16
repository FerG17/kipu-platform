using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Cortex.Mediator.DependencyInjection;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi;
using Serilog;
using Bodega.Platform.Iam.Application.CommandServices;
using Bodega.Platform.Iam.Application.Internal.CommandServices;
using Bodega.Platform.Iam.Application.Internal.OutboundServices;
using Bodega.Platform.Iam.Application.Internal.QueryServices;
using Bodega.Platform.Iam.Application.QueryServices;
using Bodega.Platform.Iam.Domain.Repositories;
using Bodega.Platform.Iam.Infrastructure.Bootstrap;
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
using Bodega.Platform.Alerts.Application.Acl;
using Bodega.Platform.Alerts.Application.CommandServices;
using Bodega.Platform.Alerts.Application.Internal.CommandServices;
using Bodega.Platform.Alerts.Application.Internal.OutboundServices;
using Bodega.Platform.Alerts.Application.QueryServices;
using Bodega.Platform.Alerts.Application.Internal.QueryServices;
using Bodega.Platform.Alerts.Domain.Repositories;
using Bodega.Platform.Alerts.Infrastructure.Notifications;
using Bodega.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Bodega.Platform.Alerts.Infrastructure.Pipeline.BackgroundServices;
using Bodega.Platform.Alerts.Interfaces.Acl;
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
using Bodega.Platform.Shared.Infrastructure.Time;
using Bodega.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Bodega.Platform.Shared.Resources;

// QuestPDF requires an explicit license declaration before generating any
// document, or it throws at first use — Community is free for this app's
// size (small business, not the revenue tier that requires a paid license).
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Don't leak "Server: Kestrel" (or version info) to clients.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Structured logging — console sink, enriched per-request with the
// authenticated user/business below (see UseSerilogRequestLogging). Only
// ever logs identifiers (UserId/BusinessId), never passwords, tokens, or
// other request/response bodies — no sink here captures those.
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

// Add services to the container.

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers().AddDataAnnotationsLocalization();

builder.Services.AddProblemDetails();

// FluentValidation — command validators (e.g. password policy) are
// discovered by convention from this assembly; see Iam's *CommandValidator
// classes under Domain/Model/Commands/Validation.
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// CORS — allows the Vue frontend (a different origin) to call this API.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("BodegaFrontend",
        policy => policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            // Needed so the browser actually sends the httpOnly session
            // cookie (see AuthenticationController) on cross-origin requests.
            // Only safe because allowedOrigins is always an exact origin list,
            // never "*" — the browser refuses AllowCredentials with a wildcard.
            .AllowCredentials());
});

// Behind a reverse proxy or load balancer every request arrives from the
// proxy's address, so the per-IP rate limiting below silently collapses into
// a single bucket shared by every client: one attacker exhausts the budget
// and locks the whole shop out (a trivial denial of service), while the
// per-attacker brute-force protection the limiter exists for disappears.
//
// X-Forwarded-For is client-controlled, so honouring it from an untrusted
// source hands an attacker the opposite problem — a fresh "IP" per request
// and no limit at all. It is therefore opt-in and only ever trusted from
// explicitly configured proxies (ForwardedHeaders:KnownProxies /
// KnownNetworks), rather than enabled by default. A deployment that puts
// this behind nginx, a cloud load balancer or a CDN must set them.
var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
if (knownProxies.Length > 0)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // The defaults trust one hop from localhost; we replace them with the
        // proxies this deployment actually has.
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        foreach (var proxy in knownProxies)
            if (IPAddress.TryParse(proxy, out var address))
                options.KnownProxies.Add(address);
    });
}

// Rate limiting — a global per-IP budget for the whole API, plus a much
// stricter policy for authentication endpoints specifically (sign-in/sign-up
// are the classic brute-force/credential-stuffing target).
//
// Configurable rather than hardcoded so the integration suite can raise
// them: every test signs up its own business from the same address, so the
// production auth budget ends up throttling the test run itself once the
// suite grows past a handful of tests. Defaults are the production values.
var globalPermitsPerMinute = builder.Configuration.GetValue("RateLimiting:GlobalPermitsPerMinute", 120);
var authPermitsPerMinute = builder.Configuration.GetValue("RateLimiting:AuthPermitsPerMinute", 10);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = globalPermitsPerMinute,
                QueueLimit = 0
            }));

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = authPermitsPerMinute,
                QueueLimit = 0
            }));
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
        .UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>());

    // Both of these put row data into exception messages and log lines —
    // EnableDetailedErrors adds the offending property's value, and
    // EnableSensitiveDataLogging adds every query parameter. That is a
    // customer's name and document number sitting in a log file, so neither
    // belongs anywhere but a developer's machine. EnableDetailedErrors used
    // to be on in every environment.
    if (builder.Environment.IsDevelopment())
        options.EnableDetailedErrors().EnableSensitiveDataLogging();
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

// Calendar dates ("today", "which sales belong to this day") must follow the
// bodega's wall clock, not the server's UTC — see IBusinessClock.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IBusinessClock, BusinessClock>();

// IAM Bounded Context

// The JWT signing key uses the same %VAR% placeholder convention as the
// connection string above, so it needs the same expansion — this was
// missing, which silently turned the literal placeholder into the HMAC key
// (see JwtSecretGuard for the full story). Validated eagerly here so a
// misconfigured deployment crashes at startup instead of booting with a
// publicly known key.
var tokenSettings = builder.Configuration.GetSection("TokenSettings").Get<TokenSettings>() ?? new TokenSettings();
tokenSettings.Secret = Environment.ExpandEnvironmentVariables(tokenSettings.Secret);
JwtSecretGuard.EnsureUsable(tokenSettings.Secret);

builder.Services.Configure<TokenSettings>(settings =>
{
    settings.Secret = tokenSettings.Secret;
    settings.ExpirationDays = tokenSettings.ExpirationDays;
});

// Public sign-up is closed (see AuthenticationController.SignUp) — the
// endpoint stays capable of creating a business (the integration suite
// still relies on that for tenant-isolation-based test setup, and the
// domain isn't hard-coded to a single tenant forever), but calling it now
// requires this shared secret, which only the platform administrator holds.
var bootstrapSettings = builder.Configuration.GetSection("Bootstrap").Get<BootstrapSettings>() ?? new BootstrapSettings();
bootstrapSettings.Key = Environment.ExpandEnvironmentVariables(bootstrapSettings.Key);
BootstrapKeyGuard.EnsureUsable(bootstrapSettings.Key);

builder.Services.Configure<BootstrapSettings>(settings => settings.Key = bootstrapSettings.Key);

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
builder.Services.AddScoped<IProductSupplierRepository, ProductSupplierRepository>();

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
builder.Services.AddScoped<IPaymentPlanRepository, PaymentPlanRepository>();

builder.Services.AddScoped<ISaleCommandService, SaleCommandService>();
builder.Services.AddScoped<ICustomerCommandService, CustomerCommandService>();
builder.Services.AddScoped<IPaymentPlanCommandService, PaymentPlanCommandService>();
builder.Services.AddScoped<ISaleQueryService, SaleQueryService>();
builder.Services.AddScoped<ICustomerQueryService, CustomerQueryService>();
builder.Services.AddScoped<IPaymentPlanQueryService, PaymentPlanQueryService>();

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

builder.Services.AddScoped<IAlertsContextFacade, AlertsContextFacade>();

// Placeholder until a real email/push implementation exists — swapping it
// in later is a one-line change here, no changes needed anywhere alerts
// are created (see IAlertNotificationDispatcher).
builder.Services.AddScoped<IAlertNotificationDispatcher, NoOpAlertNotificationDispatcher>();

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

// Must run before anything reads the caller's address or scheme — the rate
// limiter partitions on the address, and the request log records it. Only
// registered when trusted proxies are configured; see ForwardedHeaders above.
if (knownProxies.Length > 0) app.UseForwardedHeaders();

// One structured log line per request (method, path, status, duration),
// enriched with who made it once auth resolves it downstream — the "who did
// what and when" audit trail for this phase. Never logs bodies, so a
// password/token in a request never ends up in a log line.
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var businessId = httpContext.User.FindFirst("business_id")?.Value;
        if (userId != null) diagnosticContext.Set("UserId", userId);
        if (businessId != null) diagnosticContext.Set("BusinessId", businessId);
    };
});

// Security headers on every response — defense in depth against MIME
// sniffing, clickjacking, and leaking which tech stack this is (helps an
// attacker pick known exploits, no reason to make that easy).
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers.Remove("X-Powered-By");

    // Swagger UI (dev/staging only, see below) needs to load its own
    // inline scripts/styles — a strict CSP there would break it, so this
    // only applies once Swagger is gone in Production, where every
    // response is plain JSON and can be locked all the way down.
    if (app.Environment.IsProduction())
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

    await next();
});

if (!app.Environment.IsDevelopment())
{
    // HTTPS is a hard requirement in every environment except local dev
    // (where running without TLS is fine/expected).
    app.UseHsts();
}

var supportedCultures = new[] { "en", "es" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

// Swagger UI documents (and lets you call) every endpoint — fine as a dev
// tool, not something to expose publicly once this is handling a real
// business's data.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("BodegaFrontend");

app.UseRouting();

app.UseRateLimiter();

app.UseRequestAuthorization();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
///     Exposed so the integration test project can boot the real application
///     through WebApplicationFactory&lt;Program&gt; — top-level statements would
///     otherwise leave this class internal.
/// </summary>
public partial class Program;
