using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Cortex.Mediator.DependencyInjection;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi;
using Serilog;
using Kipu.Platform.Iam.Application.CommandServices;
using Kipu.Platform.Iam.Application.Internal.CommandServices;
using Kipu.Platform.Iam.Application.Internal.OutboundServices;
using Kipu.Platform.Iam.Application.Internal.QueryServices;
using Kipu.Platform.Iam.Application.QueryServices;
using Kipu.Platform.Iam.Domain.Repositories;
using Kipu.Platform.Iam.Infrastructure.Bootstrap;
using Kipu.Platform.Iam.Infrastructure.Email.Logging.Services;
using Kipu.Platform.Iam.Infrastructure.Email.Resend.Configuration;
using Kipu.Platform.Iam.Infrastructure.Email.Resend.Services;
using Kipu.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;
using Kipu.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Kipu.Platform.Iam.Infrastructure.Pipeline.Middleware.Extensions;
using Kipu.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using Kipu.Platform.Iam.Infrastructure.Sessions;
using Kipu.Platform.Iam.Infrastructure.Tokens.Jwt.Services;
using Kipu.Platform.Iam.Resources;
using Kipu.Platform.Products.Application.Acl;
using Kipu.Platform.Products.Application.CommandServices;
using Kipu.Platform.Products.Application.Internal.CommandServices;
using Kipu.Platform.Products.Application.Internal.QueryServices;
using Kipu.Platform.Products.Application.QueryServices;
using Kipu.Platform.Products.Domain.Repositories;
using Kipu.Platform.Products.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Kipu.Platform.Products.Interfaces.Acl;
using Kipu.Platform.Products.Resources;
using Kipu.Platform.Dashboard.Application.CommandServices;
using Kipu.Platform.Dashboard.Application.Internal.CommandServices;
using Kipu.Platform.Dashboard.Application.Internal.QueryServices;
using Kipu.Platform.Dashboard.Application.QueryServices;
using Kipu.Platform.Dashboard.Domain.Repositories;
using Kipu.Platform.Dashboard.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Kipu.Platform.Dashboard.Resources;
using Kipu.Platform.Alerts.Application.Acl;
using Kipu.Platform.Alerts.Application.CommandServices;
using Kipu.Platform.Alerts.Application.Internal.CommandServices;
using Kipu.Platform.Alerts.Application.Internal.OutboundServices;
using Kipu.Platform.Alerts.Application.QueryServices;
using Kipu.Platform.Alerts.Application.Internal.QueryServices;
using Kipu.Platform.Alerts.Domain.Repositories;
using Kipu.Platform.Alerts.Infrastructure.Notifications;
using Kipu.Platform.Alerts.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Kipu.Platform.Alerts.Infrastructure.Pipeline.BackgroundServices;
using Kipu.Platform.Alerts.Interfaces.Acl;
using Kipu.Platform.Alerts.Resources;
using Kipu.Platform.Sales.Application.CommandServices;
using Kipu.Platform.Sales.Application.Internal.CommandServices;
using Kipu.Platform.Sales.Application.Internal.QueryServices;
using Kipu.Platform.Sales.Application.QueryServices;
using Kipu.Platform.Sales.Domain.Repositories;
using Kipu.Platform.Sales.Application.Acl;
using Kipu.Platform.Sales.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Kipu.Platform.Sales.Interfaces.Acl;
using Kipu.Platform.Sales.Resources;
using Kipu.Platform.Shared.Application;
using Kipu.Platform.Suppliers.Application.CommandServices;
using Kipu.Platform.Suppliers.Application.Internal.CommandServices;
using Kipu.Platform.Suppliers.Application.Internal.QueryServices;
using Kipu.Platform.Suppliers.Application.QueryServices;
using Kipu.Platform.Suppliers.Domain.Repositories;
using Kipu.Platform.Suppliers.Application.Acl;
using Kipu.Platform.Suppliers.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Kipu.Platform.Suppliers.Interfaces.Acl;
using Kipu.Platform.Suppliers.Resources;
using Kipu.Platform.Shared.Domain.Repositories;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Kipu.Platform.Shared.Infrastructure.Pipeline.Middleware.Extensions;
using Kipu.Platform.Shared.Infrastructure.Security;
using Kipu.Platform.Shared.Infrastructure.Time;
using Kipu.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Kipu.Platform.Shared.Resources;

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

CorsAllowedOriginsGuard.EnsureUsable(allowedOrigins, builder.Environment.IsProduction());

builder.Services.AddCors(options =>
{
    options.AddPolicy("KipuFrontend",
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

// KnownProxies only works for a proxy with a fixed, known address (e.g. a
// self-hosted nginx). A PaaS edge like Railway's terminates every connection
// from a rotating address it never publishes, so KnownProxies can never be
// populated for it and UseForwardedHeaders() above never activates — every
// caller then shares the edge's own address, collapsing both rate limiters
// into one global bucket (see the partition key below).
//
// This is a narrower, explicit opt-in for exactly that case: trust the
// right-most X-Forwarded-For entry unconditionally, with no IP allowlist.
// It is only safe because of Railway's specific topology — the container
// port is never reachable except through Railway's own edge, so nothing
// can inject that right-most hop except Railway itself. It must stay off
// (the default) for any deployment where the app's port might be reached
// directly, since a direct caller could then set X-Forwarded-For itself and
// get a fresh bucket on every request — worse than not forwarding at all.
var trustLastProxyHop = builder.Configuration.GetValue("ForwardedHeaders:TrustLastProxyHop", false);

// Shared by both limiters below so a caller's global and auth-specific
// budgets are keyed on the same address.
string ResolveClientAddress(HttpContext context)
{
    if (trustLastProxyHop)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        var lastHop = forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();
        if (!string.IsNullOrEmpty(lastHop)) return lastHop;
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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
            partitionKey: ResolveClientAddress(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = globalPermitsPerMinute,
                QueueLimit = 0
            }));

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ResolveClientAddress(context),
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
    var connectionString = Environment.ExpandEnvironmentVariables(connectionStringTemplate ?? string.Empty);
    DatabaseConnectionStringGuard.EnsureUsable(connectionString);

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
        Title = "Kipu Platform API",
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

builder.Services.Configure<PasswordResetSettings>(builder.Configuration.GetSection("PasswordReset"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPasswordResetCodeRepository, PasswordResetCodeRepository>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<ISessionCookieService, SessionCookieService>();

// Falls back to logging the code instead of emailing it whenever no API key
// is configured — every automated test, and local dev before the owner sets
// one up — so the reset flow is fully exercisable without a real Resend account.
builder.Services.AddHttpClient();
var resendSettings = builder.Configuration.GetSection("Resend").Get<ResendSettings>() ?? new ResendSettings();
resendSettings.ApiKey = Environment.ExpandEnvironmentVariables(resendSettings.ApiKey);
// FromEmail used to skip this expansion entirely — in an environment where
// RESEND_FROM_EMAIL wasn't set, the literal "%RESEND_FROM_EMAIL%" shipped as
// the sender address, which Resend rejects outright, silently killing every
// password-reset email.
resendSettings.FromEmail = Environment.ExpandEnvironmentVariables(resendSettings.FromEmail);

// ExpandEnvironmentVariables leaves the literal "%VAR%" in place rather than
// returning empty when the variable isn't set. That string is not blank, so
// it used to slip past the IsNullOrWhiteSpace check below and register the
// real ResendEmailService with a garbage API key instead of falling back to
// LoggingEmailService — the same class of bug JwtSecretGuard/BootstrapKeyGuard
// exist to catch, just for a setting that's allowed to be genuinely unset.
if (UnexpandedPlaceholderPattern().IsMatch(resendSettings.ApiKey))
    resendSettings.ApiKey = string.Empty;

builder.Services.Configure<ResendSettings>(settings =>
{
    settings.ApiKey = resendSettings.ApiKey;
    settings.FromEmail = resendSettings.FromEmail;
    settings.FromName = resendSettings.FromName;
});
if (!string.IsNullOrWhiteSpace(resendSettings.ApiKey))
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
else
    builder.Services.AddScoped<IEmailService, LoggingEmailService>();

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
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

builder.Services.AddScoped<IProductCommandService, ProductCommandService>();
builder.Services.AddScoped<IWarehouseCommandService, WarehouseCommandService>();
builder.Services.AddScoped<IInventoryCommandService, InventoryCommandService>();
builder.Services.AddScoped<ICategoryCommandService, CategoryCommandService>();
builder.Services.AddScoped<IProductQueryService, ProductQueryService>();
builder.Services.AddScoped<IWarehouseQueryService, WarehouseQueryService>();
builder.Services.AddScoped<IInventoryQueryService, InventoryQueryService>();
builder.Services.AddScoped<IBatchQueryService, BatchQueryService>();
builder.Services.AddScoped<IStockMovementQueryService, StockMovementQueryService>();
builder.Services.AddScoped<ICategoryQueryService, CategoryQueryService>();

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
builder.Services.AddScoped<IExpirationAlertSweepService, ExpirationAlertSweepService>();
builder.Services.AddScoped<IAlertQueryService, AlertQueryService>();
builder.Services.AddScoped<IAlertRuleQueryService, AlertRuleQueryService>();

builder.Services.AddScoped<IAlertsContextFacade, AlertsContextFacade>();

// Placeholder until a real email/push implementation exists — swapping it
// in later is a one-line change here, no changes needed anywhere alerts
// are created (see IAlertNotificationDispatcher).
builder.Services.AddScoped<IAlertNotificationDispatcher, NoOpAlertNotificationDispatcher>();

AlertSweepIntervalGuard.EnsureUsable(builder.Configuration.GetValue("Alerts:SweepIntervalHours", 6.0));

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

// Apply pending migrations on startup (safe to call even when schema is up to
// date) — but only where that's actually convenient rather than dangerous.
// Outside Production (local dev against a fresh Docker MySQL, the
// integration suite's disposable database) this default stays on, same as
// always. In Production it now defaults off: a redeploy with a bad migration
// used to alter live schema automatically on every boot, with no review step
// and no rollback, and it required the app's own DB user to hold permanent
// DDL permissions just for this. Migrating there is now an explicit,
// deliberate action — set Database__MigrateOnStartup=true for the one deploy
// that needs it, then unset it again.
var migrateOnStartup = app.Configuration.GetValue("Database:MigrateOnStartup", !app.Environment.IsProduction());
if (migrateOnStartup)
{
    using var scope = app.Services.CreateScope();
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

app.UseCors("KipuFrontend");

app.UseRouting();

app.UseRateLimiter();

app.UseRequestAuthorization();

// Only meaningful when this app itself terminates TLS (a direct Kestrel
// deployment with no edge in front). Behind Railway's edge, TLS is already
// terminated before the request reaches here — the connection Kestrel sees
// is always plain HTTP, ASPNETCORE_HTTPS_PORT is never legitimately set, and
// if it ever were, this would issue a 307 to every single request (the
// request already looks like HTTP from Kestrel's side, so it "redirects" to
// HTTPS on this same host, which still looks like HTTP once it arrives —
// an infinite loop, total outage). Redirecting to HTTPS behind such an edge
// is the edge's job, not this app's.
if (knownProxies.Length == 0 && !trustLastProxyHop) app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
///     Exposed so the integration test project can boot the real application
///     through WebApplicationFactory&lt;Program&gt; — top-level statements would
///     otherwise leave this class internal.
/// </summary>
public partial class Program
{
    /// <summary>Same convention as JwtSecretGuard/BootstrapKeyGuard's own copy of this pattern — an env-var placeholder that never got expanded.</summary>
    [GeneratedRegex(@"^%[A-Za-z_][A-Za-z0-9_]*%$")]
    private static partial Regex UnexpandedPlaceholderPattern();
}
