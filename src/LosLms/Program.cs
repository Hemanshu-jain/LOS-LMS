using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using LosLms.Components;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// UNVERIFIED LICENCE DECLARATION — see OPEN-QUESTIONS-FOR-ARUN.md, item 1.
// QuestPDF's Community licence is only valid for organisations under $1M USD annual gross revenue.
// That has NOT been confirmed for this client, and a multi-branch NBFC may well exceed it — above
// that threshold this needs a paid Professional or Enterprise licence. Confirm before production.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // A closed laptop or a backgrounded tab for half an hour comes back to the same circuit (no "connection closed"); the default is 3 minutes.
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(30);
        options.DisconnectedCircuitMaxRetained = 200;
    })
    .AddHubOptions(hub =>
    {
        // Tolerate a silent client for a minute (a network blip) before the server drops the connection; keep-alives stay at 15 s.
        hub.KeepAliveInterval = TimeSpan.FromSeconds(15);
        hub.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    });

// Lift the request-body cap so a full-quality Video KYC clip fits (Kestrel defaults to 30 MB). The
// camera upload streams a raw binary body to the /api/party-media middleware. App-wide, which is fine —
// this app also takes scan/PDF uploads.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 157_286_400); // 150 MB

// Flows the signed-in user down to every component, which is what <AuthorizeView> in the top bar and
// <AuthorizeRouteView> in Routes.razor both read.
builder.Services.AddCascadingAuthenticationState();

// PIN-code -> city/state autofill on Customer Details hits an anonymous India Post endpoint.
// Default factory, no keys, no base address — each call site passes its own URL.
builder.Services.AddHttpClient();

// The app runs against MySQL. The connection string is externally configured, never hardcoded: the
// Server package's launcher bundles a portable MySQL and passes this string in via the
// ConnectionStrings__LosDb environment variable (developers can set ConnectionStrings:LosDb in
// user-secrets/appsettings instead). Only the server machine ever touches the database; other
// machines reach it through the app, never the database port.
var connectionString = CloudConfig.ConnectionString(builder.Configuration);

// A factory, not AddDbContext. In Blazor Server a scoped DbContext lives for the whole SignalR
// circuit and is shared by every component on it, so two overlapping renders hit the same context
// and throw. Components create a short-lived context per operation instead.
//
// An explicit server version (not AutoDetect) is deliberate: AutoDetect opens a connection while
// configuring DI, which would make startup fail hard when the database is briefly unreachable. The
// startup migrate step below is the one place that must reach the database, and it fails closed on
// its own.
builder.Services.AddDbContextFactory<LosDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40))));

// ---- Tenancy ----
//
// TenantContext is scoped, so in Blazor Server there is one per circuit. The factory registration
// below deliberately REPLACES the singleton IDbContextFactory that AddDbContextFactory just
// registered — a singleton cannot see scoped services, and the later registration wins. That is what
// lets every existing `await DbFactory.CreateDbContextAsync()` call site stay exactly as it was and
// still get a company-scoped context.
// Needed by TenantContext for the non-circuit paths — the /files endpoint and the statically
// rendered /account pages, neither of which can use the Blazor authentication state provider.
builder.Services.AddHttpContextAccessor();

// Singleton: it has to outlive any one circuit, because the point is telling OTHER users' circuits
// that something changed. Single-server only — see the type's own remarks.
builder.Services.AddSingleton<AdminRequestNotifier>();

// One shared update-check result for the whole server, kept current by a background poller so the
// SuperAdmin sees a "new version available" banner without opening the System Updates page.
// The /digio/return page asks this to look at a request at once.
builder.Services.AddSingleton<IDigioReconcileTrigger, DigioReconcileTrigger>();
builder.Services.AddSingleton<UpdateNotificationService>();
// Not on a cloud host: there is no launcher to apply an update, so the container is redeployed instead.
if (!builder.Configuration.IsCloud())
{
    builder.Services.AddHostedService<UpdateCheckBackgroundService>();
}

// Uploaded and generated files: a private Supabase Storage bucket when Storage__Url is set (the cloud host's
// disk is wiped on every restart), otherwise the App_Data folder as before.
builder.Services.AddSingleton<IFileStore>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return cfg.Trimmed("Storage:Url") is { } url
        ? new SupabaseFileStore(
            new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) }),
            url,
            cfg.Trimmed("Storage:ServiceKey") ?? throw new InvalidOperationException("Storage__ServiceKey is not set."),
            cfg.Trimmed("Storage:Bucket") ?? throw new InvalidOperationException("Storage__Bucket is not set."))
        : new DiskFileStore(sp.GetRequiredService<IWebHostEnvironment>().ContentRootPath);
});

// On a cloud host the Data Protection keys (cookie/antiforgery encryption) live in the database; the container's
// own disk is wiped on restart, which would otherwise sign every user out each time.
if (builder.Configuration.IsCloud())
{
    builder.Services.AddDataProtection().PersistKeysToDbContext<LosDbContext>().SetApplicationName("LosLms");
}

// Subscription licensing: verifies the baked/renewed license, warns near expiry, and freezes the app
// once expired (see MainLayout + the /account/renew page).
builder.Services.AddSingleton<LicenseService>();

// Server-side encrypted store of admin-set passwords, so an Admin can re-view a credential later
// (masked, briefly revealed). Recoverable by design — see PasswordVault's own remarks for the trade-off.
builder.Services.AddSingleton<PasswordVault>();

// Maintenance mode: a live operator switch (a SuperAdmin toggles it on the System Updates page) that
// puts a branded "under maintenance" page in front of everyone but SuperAdmins. Starts from config so a
// deployer can bring the app up already in maintenance; otherwise a restart clears it, which is the safe
// direction — a server that just came back up should serve, not sit behind a page nobody remembers.
builder.Services.AddSingleton(new MaintenanceState(builder.Configuration.GetValue("Maintenance:Enabled", false)));

// Which company each SuperAdmin is currently "acting as" (set from the company picker). Singleton +
// single-server, like the notifier above; TenantContext reads it to scope the SuperAdmin to one company.
builder.Services.AddSingleton<ActingCompanyStore>();

builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<IDbContextFactory<LosDbContext>, TenantDbContextFactory>();

// Cloudflare Turnstile bot protection on sign-in. Off unless both keys are set under Security:Turnstile
// (see TurnstileOptions), so LAN-only installs are unaffected.
builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection(TurnstileOptions.Section));
builder.Services.AddScoped<TurnstileVerifier>();

// Public web instance settings (self-service registration). The section is absent on the desktop build,
// so AllowSelfRegistration defaults false and the register page / sign-in link stay closed there.
builder.Services.Configure<WebOptions>(builder.Configuration.GetSection(WebOptions.Section));

// Gemini-backed signature detection (#6). No key => the service reports NotConfigured and the Document
// Checklist keeps the manual "Signed" tick; drop the key in (Gemini__ApiKey) to turn auto-detect on.
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.Section));
builder.Services.AddScoped<SignatureDetectionService>();

// Provisions a brand-new tenant (company + owning admin) for the public sign-up page and the demo
// seeder. Scoped, because it creates the admin through the scoped UserManager.
builder.Services.AddScoped<CompanyProvisioningService>();

// Provider (Digio) API metering + postpaid monthly billing: usage log, rate card, invoices and the
// pause-after-grace gate. Nothing calls a provider yet; the service is ready for when keys arrive.
builder.Services.Configure<ApiBillingOptions>(builder.Configuration.GetSection(ApiBillingOptions.Section));
builder.Services.AddScoped<ApiBillingService>();
builder.Services.AddScoped<DigioClient>();
builder.Services.AddScoped<DigioKyc>();
// Collections (slice 3+4): bounce handling, re-presentation, loan closure, settlement check.
builder.Services.AddScoped<BounceService>();
builder.Services.AddScoped<IBounceHandler>(sp => sp.GetRequiredService<BounceService>());
builder.Services.AddHostedService<RepresentationJob>();
builder.Services.AddScoped<LoanClosureService>();
builder.Services.AddScoped<SettlementReconciler>();
builder.Services.AddScoped<DigioWebhookService>();
builder.Services.AddScoped<DigioEsignService>();
builder.Services.AddSingleton<IAgreementSignatureBoxes, LoanAgreementSignatureBoxes>(); // where each borrower signs on the generated agreement
builder.Services.AddScoped<DigioNachService>();
builder.Services.AddScoped<DigioAsset>();
builder.Services.AddScoped<ApiUsageReport>();
builder.Services.AddScoped<DigioCollect>();
builder.Services.AddScoped<IPresentationStarter>(sp => sp.GetRequiredService<DigioCollect>()); // re-presentation after a bounce (slice 3)
builder.Services.AddHostedService<CollectionScheduler>();
builder.Services.AddSingleton<DigioWebhookHealth>();
builder.Services.AddSingleton<DigioBackgroundTasks>();
builder.Services.AddSingleton<DigioStatusNotifier>();
builder.Services.AddScoped<DigioInbox>();
builder.Services.AddSingleton<DigioEventProcessor>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DigioEventProcessor>());
builder.Services.AddHostedService<DigioReconciler>();

// "Connect to LOS-LMS": OAuth device-flow server for partner desktop software (Paavan), the PartnerBearer
// scheme + loans:read / loans:write policies the partner data API uses, the /link wrong-code limiter and
// the expired-code purge.
builder.Services.AddScoped<PartnerOAuthService>();

// Email (Brevo SMTP relay on Render): confirmation of new sign-ups and forgotten-password links. With no Smtp__* settings the
// emails are logged instead of sent, and public sign-up skips confirmation.
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<SendThrottle>();
builder.Services.AddScoped<AccountEmailService>();
builder.Services.AddSingleton<PartnerGuessLimiter>();
builder.Services.AddHostedService<PartnerOAuthPurgeService>();
builder.Services.AddPartnerAuth();

// Identity's UserStore and RoleStore resolve LosDbContext directly rather than through the factory,
// so hand them one built the same way.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<LosDbContext>>().CreateDbContext());

// Nothing set here may affect the MODEL — only behaviour. Identity reads Stores.MaxLengthForKeys
// (and ProtectPersonalData) while building the model, from the application service provider, which
// the design-time factory has no way to supply. Setting either one would make `dotnet ef migrations`
// scaffold different column types from the ones the app actually runs against. Pomelo's default
// varchar(255) keys index fine under MySQL's utf8mb4, so there is nothing to gain by pinning it.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequiredLength = 10;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddEntityFrameworkStores<LosDbContext>()
    .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

// How often a live cookie is re-checked against the user's current security stamp. Deactivating a user
// (UsersTab) rotates that stamp; without a short interval the change would not bite for up to Identity's
// default 30 minutes. Five minutes keeps "deactivate" close to immediate while a signed-in user is
// active, and also re-runs the claims factory so a changed role/company is picked up without re-login.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(5);
});

builder.Services.ConfigureApplicationCookie(AuthCookie.Configure); // "remember me" = 7 days sliding; otherwise 12 h sliding

// Remote users reach this backend through a Cloudflare tunnel that terminates HTTPS and forwards to
// the app over plain HTTP on localhost. Without this, the app sees every request as "http" and builds
// http:// redirect URLs (e.g. the sign-in redirect), bouncing the browser off HTTPS. Honouring the
// X-Forwarded-Proto that the tunnel sets keeps the app on the correct external scheme. Safe here
// because the backend binds to 127.0.0.1 only — the sole caller is the local tunnel client, so the
// header cannot be spoofed by a remote user reaching the backend directly.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Tunnel setup (no Proxy__ForwardLimit): Proto only. X-Forwarded-For is deliberately NOT applied (with every
    // proxy trusted it let any caller rewrite RemoteIpAddress); ClientIp.Of takes the tunnel's CF-Connecting-IP
    // when the socket peer is loopback. Behind Render's proxy (Proxy__ForwardLimit=1) the one entry that proxy
    // appended IS applied, so RemoteIpAddress, and so ClientIp.Of, is the real client.
    CloudConfig.ConfigureProxy(
        options, int.TryParse(builder.Configuration.Trimmed("Proxy:ForwardLimit"), out var hops) && hops > 0 ? hops : null);
});

builder.Services.AddAuthorization(options =>
{
    // Fail closed. Every endpoint requires a signed-in user unless it says [AllowAnonymous] out loud,
    // so a page added later is protected by default rather than by whoever remembers to protect it.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// HSTS only bites over the HTTPS tunnel (the LAN is plain HTTP); a one-year max-age with subdomains
// is the standard production value once a browser has seen the app over HTTPS at least once.
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

// Rate-limit the sign-in POST per client IP. The per-account lockout (5 fails / 15 min, above) stops
// guessing at ONE account; this caps the request rate from one source across ALL accounts, which is
// what credential-stuffing needs. Over the Cloudflare tunnel every request arrives from 127.0.0.1, so
// key off CF-Connecting-IP (set by the tunnel edge) when present and fall back to the socket IP on the
// LAN. Only the POST to /account/login and /account/register is limited; everything else is unrestricted.
// Registration is included because on the public web instance it creates rows anonymously, so a single
// source must not be able to flood it with new companies.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Partner data API: generous (a daily sync of a whole book is a few hundred rows per call) but bounded.
        if (context.Request.Path.StartsWithSegments("/api/v1"))
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                "api:" + ClientIp.Of(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
        }

        // Partner software polling /oauth/* is limited per IP too, at a higher ceiling than sign-in:
        // a device poll every 5 s is 12 a minute.
        var isOAuth = context.Request.Path.StartsWithSegments(PartnerOAuthEndpoints.PathPrefix, StringComparison.OrdinalIgnoreCase);
        var isSensitivePost = HttpMethods.IsPost(context.Request.Method)
            && (context.Request.Path.StartsWithSegments("/account/login", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.StartsWithSegments("/account/register", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.StartsWithSegments("/account/forgot-password", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.StartsWithSegments("/account/resend-confirmation", StringComparison.OrdinalIgnoreCase));
        if (!isSensitivePost && !isOAuth)
        {
            return RateLimitPartition.GetNoLimiter("unlimited");
        }

        var clientIp = ClientIp.Of(context);
        return RateLimitPartition.GetFixedWindowLimiter(isOAuth ? "oauth:" + clientIp : clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = isOAuth ? 60 : 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

var app = builder.Build();

// A cloud host without mail settings silently has no email verification and no password reset: say so loudly at startup.
if (app.Configuration.IsCloud() && !app.Services.GetRequiredService<IEmailSender>().IsConfigured)
{
    app.Logger.LogWarning("Email verification and password reset are OFF: Smtp__* is not set (needs at least Smtp__Host and Smtp__From).");
}

// Startup database step: bring the schema up to date, then seed. Wrapped so a database that is
// unreachable logs a clear diagnostic and lets the app start anyway — pages then fail honestly
// (fail-closed) rather than the whole server refusing to boot. All of it is idempotent and safe on
// every start.
try
{
    // Built directly rather than resolved from DI: startup is outside any request, so there is no
    // signed-in user for a tenant-scoped context to read, and the seeding tenant has to see everything.
    await using (var db = new LosDbContext(
        app.Services.GetRequiredService<DbContextOptions<LosDbContext>>(),
        TenantContext.ForSeeding()))
    {
        // Apply any pending EF Core migrations, every startup. This is what makes an update swap in a
        // new build and have its schema changes apply automatically to the existing database — no
        // manual step, and the client never loses data.
        await db.Database.MigrateAsync();
    }

    // Roles and the two bootstrap accounts (one Admin, one SuperAdmin) with their temporary passwords.
    await IdentitySeeder.SeedAsync(app.Services, app.Logger);

    // One-time move of a desktop install's file vault into the database: dotnet LosLms.dll --import-vault <folder>
    // (the folder holds vault.key and credential-vault.dat). Prints the count and exits without serving.
    if (Array.IndexOf(args, "--import-vault") is >= 0 and var importAt && importAt + 1 < args.Length)
    {
        var imported = app.Services.GetRequiredService<PasswordVault>().ImportLegacyFiles(args[importAt + 1]);
        app.Logger.LogInformation("Imported {Count} credential vault entries.", imported);
        return;
    }

    // Demo data — fifteen worked-through applications and a vehicle-cap catalog — for development only.
    // OFF by default so the shipped build is a blank slate with no seeded places, customers or figures.
    // Turn it on in development with `dotnet run --Seed:DemoApplications=true` (or user-secrets/env).
    if (app.Configuration.GetValue("Seed:DemoApplications", false))
    {
        await DemoSeedData.SeedAsync(app.Services, app.Logger);
    }

    // A throwaway second company, only for proving tenant isolation. Never on by default.
    if (app.Configuration.GetValue<bool>("Seed:IsolationFixture"))
    {
        await IdentitySeeder.SeedIsolationFixtureAsync(app.Services, app.Logger);
    }

    // Every company gets the built-in DSA/sourcing, scheme and RCU-vendor options the first time it has
    // none — so those dropdowns are never empty after moving off the old hardcoded arrays. Idempotent.
    await LookupSeeder.SeedAsync(app.Services);

    // Give every company a unique short code (the prefix of its application ids) — including any that
    // existed before the code column, so their next new application is numbered under their own code.
    await CompanyCodeSeeder.SeedAsync(app.Services);

    // One ready-to-use demo tenant for the public web instance — a company with a known login, a branch
    // and a few vehicle caps, so a visitor lands straight in a working (but empty) app. OFF unless
    // Seed:DemoTenant is set, which only the web build's config does; idempotent and never on for desktop.
    if (app.Configuration.GetValue("Seed:DemoTenant", false))
    {
        await DemoTenantSeeder.SeedAsync(app.Services, app.Configuration, app.Logger);
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex,
        "Startup database step failed — could not migrate or seed the database. This usually means the "
        + "MySQL server is not reachable yet or the ConnectionStrings:LosDb value is wrong. The app will "
        + "start, but data pages will not work until this is resolved.");
}

// Said out loud at startup, not just in a code comment. The failure mode this warns about is silent:
// on two or more instances the Admin Inbox keeps working and simply stops updating for anyone whose
// circuit is on a different instance from the one that raised the request. Nothing errors, so the
// only way anyone learns is by noticing stale data. A deployer scaling out needs to read this and
// add a backplane (Redis or Azure SignalR) before they do.
app.Logger.LogInformation(
    "Admin Inbox real-time delivery is in-process and assumes a SINGLE server instance. " +
    "Running more than one instance needs a SignalR backplane, or admins will silently see stale data.");

// Configure the HTTP request pipeline.
// First, before anything reads the request scheme: apply the tunnel's forwarded scheme/host so
// redirects and absolute URLs use https when the user arrived over the HTTPS tunnel.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Without this, any error status with an empty body (a 404 from a mistyped URL, the 429 the rate
// limiter returns, a 400, a 403) reaches the browser as a blank page — so the WebView2/Chrome host
// paints its own "This page isn't working" screen and the product looks broken. Re-executing the
// pipeline against /Error/{code} replaces that with our own branded, polite page while preserving the
// original status code. Runs in every environment: these pages leak nothing, and it lets us preview
// any of them by opening /Error/404, /Error/503, etc. directly. Unhandled 500s are handled separately
// by UseExceptionHandler above (non-dev only, so developers still get the full stack trace).
app.UseStatusCodePagesWithReExecute("/Error/{0}");

// Plain HTTP on the LAN, no certificate — branch staff reach the one server by its local address.
// No HTTPS redirect (it would only log a "failed to determine https port" warning every request).
// Put the server behind a reverse proxy if TLS is ever required.

// Security response headers on every response (static files included, so this runs before UseStaticFiles).
// The CSP is tuned for Blazor Server: same-origin scripts (blazor.server.js under /_framework),
// same-origin SignalR websocket ('self' covers the ws/wss upgrade of this origin), inline styles that
// Razor components emit as style="…" attributes, and data: image URIs for the base64 document previews
// on the Customer Details stage. If the circuit ever fails to connect under a stricter browser, widen
// connect-src to 'self' ws: wss:.
// Turnstile, when enabled, loads a script and an iframe from Cloudflare, so the CSP has to allow that
// one origin for scripts and frames. When it is off the policy stays fully self-only.
var turnstileEnabled = app.Services.GetRequiredService<IOptions<TurnstileOptions>>().Value.Enabled;
var scriptSrc = turnstileEnabled ? "script-src 'self' https://challenges.cloudflare.com; " : "script-src 'self'; ";
var frameSrc = turnstileEnabled ? "frame-src https://challenges.cloudflare.com; " : "frame-src 'none'; ";
var csp =
    "default-src 'self'; " +
    "base-uri 'self'; " +
    "object-src 'none'; " +
    "frame-ancestors 'none'; " +
    "img-src 'self' data: blob:; " +
    // media-src covers the recorded video/photo played back from /files and any blob: preview URL the
    // capture code creates; blob: is also allowed on img-src for the live-photo preview.
    "media-src 'self' blob:; " +
    "style-src 'self' 'unsafe-inline'; " +
    scriptSrc +
    frameSrc +
    "connect-src 'self'; " +
    "form-action 'self'";

// Anonymous liveness/version probe for release checks and the launcher's post-update verification.
// Branched off BEFORE auth, the maintenance gate and the company-setup redirect so it always answers;
// returns only status + version, never data.
var runningVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unknown";
var runningCommit = (app.Configuration.Trimmed("Hosting:Commit") ?? app.Configuration.Trimmed("RENDER_GIT_COMMIT")) is { } sha
    ? sha[..Math.Min(sha.Length, 12)]
    : "unknown";
app.MapWhen(
    c => c.Request.Path == "/health",
    b => b.Run(async c =>
    {
        // A real round trip to the database (SELECT 1), so the host's health check fails over a dead database
        // and not just a live process. Bounded, so a hung connection answers 503 instead of hanging the probe.
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await using var db = new LosDbContext(
                c.RequestServices.GetRequiredService<DbContextOptions<LosDbContext>>(), TenantContext.ForSeeding());
            await db.Database.ExecuteSqlRawAsync("SELECT 1", timeout.Token);
        }
        catch (Exception)
        {
            c.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await c.Response.WriteAsJsonAsync(new { status = "db_down" });
            return;
        }

        await c.Response.WriteAsJsonAsync(new { status = "ok", version = runningVersion, commit = runningCommit });
    }));

// Partner data API: every error, including the empty-bodied 401/403/429 the platform would send, comes
// back as {error, message, details[]}. Wraps the rest of the pipeline for /api/v1 only.
app.UseWhen(c => c.Request.Path.StartsWithSegments("/api/v1"), b =>
{
    var maintenanceState = app.Services.GetRequiredService<MaintenanceState>();
    b.UseJsonErrors().UseMaintenanceWriteBlock(() => maintenanceState.IsOn);
});

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["Content-Security-Policy"] = csp;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    // Camera and microphone are allowed for THIS origin only — the live-photo capture and Video KYC
    // recording on Customer Details need them. Geolocation stays fully denied.
    headers["Permissions-Policy"] = "camera=(self), microphone=(self), geolocation=()";
    await next();
});

app.UseDefaultFiles(); // /docs/ -> /docs/index.html
app.UseStaticFiles();
app.UseRateLimiter();

// "Connect to LOS-LMS" token endpoints (/oauth/*): server-to-server from partner software, so a terminal
// branch before auth, maintenance, the company-setup redirect and antiforgery. Rate-limited above.
app.UsePartnerOAuth();

// Digio webhooks: POST /webhooks/digio/{companyId}. Server-to-server, so no user, no antiforgery: handled here, before
// auth/maintenance/antiforgery, and short-circuits. Trust comes from the HMAC (X-Digio-Checksum, per-company secret). It
// ALWAYS answers 200 (Digio's queue is head-of-line; non-200s stall it and get webhooks disabled): see DigioWebhookEndpoint.
app.UseDigioWebhook();
app.MapDigioMaskedView();
#if DEBUG
app.MapDigioHarness(); // Phase 0 throwaway: Development + SuperAdmin only; absent from Release builds.
#endif

app.UseAuthentication();

// Maintenance gate. When maintenance mode is on, everyone except a signed-in SuperAdmin gets the branded
// maintenance page (503 + Retry-After) instead of the app, so planned downtime reads as deliberate rather
// than broken. Placed AFTER authentication (so the SuperAdmin exemption can read the user's role) but
// BEFORE authorization — otherwise the fallback "must be signed in" policy would 302 an anonymous visitor
// to the login page before this runs, and they'd never see the maintenance page. Static assets are already
// served by UseStaticFiles above, so they never reach here; /account and the Blazor framework/SignalR
// paths stay open so a SuperAdmin can still sign in and switch it back off. The 503 carries a real HTML
// body, so UseStatusCodePages does not re-execute it to the error page.
var maintenance = app.Services.GetRequiredService<MaintenanceState>();
var maintenancePage = new Lazy<string>(() =>
{
    var path = Path.Combine(app.Environment.WebRootPath, "maintenance.html");
    return File.Exists(path)
        ? File.ReadAllText(path)
        : "<!doctype html><meta charset=utf-8><title>Under maintenance</title>"
          + "<h1>We'll be right back</h1><p>LOS/LMS is briefly offline for maintenance. Please try again shortly.</p>";
});

static bool IsMaintenanceExempt(HttpContext context)
{
    // A signed-in SuperAdmin keeps full access — they are the one who turns maintenance off again.
    if (context.User.IsInRole(TenantContext.SuperAdminRole))
    {
        return true;
    }

    var path = context.Request.Path;
    return path.StartsWithSegments("/account")       // sign in to reach the toggle
        || path.StartsWithSegments("/_framework")    // Blazor runtime
        || path.StartsWithSegments("/_blazor")       // SignalR circuit
        || path.StartsWithSegments("/_content")      // component library assets
        || path.StartsWithSegments("/api/v1")        // partner API keeps answering (JSON) during maintenance
        || path.StartsWithSegments("/Error");        // never mask a real error with the maintenance page
}

app.Use(async (context, next) =>
{
    if (maintenance.IsOn && !IsMaintenanceExempt(context))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers["Retry-After"] = "120";
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(maintenancePage.Value);
        return;
    }

    await next();
});

// After the maintenance gate: an anonymous, non-exempt request in maintenance mode must see the
// maintenance page rather than be bounced to login by the fallback policy, which is why this runs here.
app.UseAuthorization();

// Camera media upload (live photo + Video KYC), handled here as its own middleware — BEFORE the
// antiforgery middleware — and short-circuited, so Blazor SSR's antiforgery/named-form validation never
// runs on it. That validation cannot be satisfied by a plain binary upload and could not be disabled
// per-endpoint in this setup. The upload is safe on its own: it requires an authenticated user and
// resolves the application through the tenant-filtered DbContext, so it only ever writes the caller's
// own tenant data, and the path segments are whitelisted before composing a filesystem path.
app.Use(async (context, next) =>
{
    if (!HttpMethods.IsPost(context.Request.Method)
        || !context.Request.Path.StartsWithSegments("/api/party-media", out var rest))
    {
        await next();
        return;
    }

    // Writes a status with a small JSON body. The body matters: the app's custom 4xx/5xx pages
    // (StatusCodePages re-execute) only re-run the pipeline when the response has no body, and that
    // re-run — a POST to the error route — would surface a misleading antiforgery failure. A body here
    // keeps the real result, and the camera's fetch reads res.ok.
    async Task RespondAsync(int status, object payload)
    {
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(payload);
    }

    if (context.User?.Identity?.IsAuthenticated != true)
    {
        await RespondAsync(StatusCodes.Status401Unauthorized, new { error = "Not signed in." });
        return;
    }

    var segments = rest.Value?.Trim('/').Split('/') ?? Array.Empty<string>();
    if (segments.Length != 3)
    {
        await RespondAsync(StatusCodes.Status400BadRequest, new { error = "Malformed request." });
        return;
    }

    var applicationId = Uri.UnescapeDataString(segments[0]);
    var partyType = segments[1];
    var kind = segments[2];

    if (partyType is not ("Applicant" or "CoApplicant" or "Guarantor"))
    {
        await RespondAsync(StatusCodes.Status400BadRequest, new { error = "Unknown party." });
        return;
    }

    var extension = kind switch { "photo" => ".png", "videokyc" => ".webm", _ => string.Empty };
    if (extension.Length == 0)
    {
        await RespondAsync(StatusCodes.Status400BadRequest, new { error = "Unknown media kind." });
        return;
    }

    var dbFactory = context.RequestServices.GetRequiredService<IDbContextFactory<LosDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    if (!await db.Applications.AnyAsync(a => a.Id == applicationId))
    {
        // Tenant-filtered: a miss is 404 whether the file belongs to another tenant or does not exist.
        await RespondAsync(StatusCodes.Status404NotFound, new { error = "Application not found." });
        return;
    }

    using var buffer = new MemoryStream();
    await context.Request.Body.CopyToAsync(buffer);
    var bytes = buffer.ToArray();
    if (bytes.Length == 0 || !LosLms.Services.FileSignature.Matches(extension, bytes))
    {
        await RespondAsync(StatusCodes.Status400BadRequest, new { error = "The captured file was invalid." });
        return;
    }

    var name = $"{kind}-{Guid.NewGuid():N}{extension}";
    await context.RequestServices.GetRequiredService<IFileStore>().SaveUploadAsync(applicationId, partyType, name, bytes);

    if (kind == "videokyc")
    {
        // In-house Video KYC is billed per completed recording on its length. The browser reports the length; the file size gives a floor
        // (the recorder is capped at 8 Mbit/s = 1 MB per second), so under-reporting cannot make it cheaper than the file itself allows.
        _ = int.TryParse(context.Request.Query["d"], out var reported);
        var seconds = Math.Clamp(Math.Max(reported, (int)Math.Ceiling(bytes.Length / 1_000_000d)), 1, 3600);
        var companyId = await db.Applications.Where(a => a.Id == applicationId).Select(a => a.CompanyId).FirstAsync();
        await context.RequestServices.GetRequiredService<ApiBillingService>().RecordVideoKycAsync(companyId, applicationId, name, seconds);
        await context.Response.WriteAsJsonAsync(new { name, seconds });
        return;
    }

    await context.Response.WriteAsJsonAsync(new { name });
});

// First-run gate. Until a company's setup is complete — a real name and at least one branch — every
// company-scoped user is redirected to Company Setup and can reach nothing else, because there is
// nothing meaningful to work with yet. SuperAdmin (no company of their own) is exempt and roams
// freely. This is the plain-HTTP half; the backstop in MainLayout covers interactive SignalR
// navigations that never hit this pipeline. Runs after auth so the user and claims are populated.
app.Use(async (context, next) =>
{
    static bool IsExempt(PathString p) =>
        p.StartsWithSegments("/account", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/settings", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/company-setup", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/billing", StringComparison.OrdinalIgnoreCase) // a paused company must always reach the pay page
        || p.StartsWithSegments("/files", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/system", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/_content", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/Error", StringComparison.OrdinalIgnoreCase);

    var user = context.User;
    if (user.Identity?.IsAuthenticated == true
        && !user.IsInRole(TenantContext.SuperAdminRole)
        && !IsExempt(context.Request.Path)
        && int.TryParse(user.FindFirst(TenantContext.CompanyIdClaim)?.Value, out var companyId))
    {
        var factory = context.RequestServices.GetRequiredService<IDbContextFactory<LosDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        if (!await CompanySetupState.IsCompleteAsync(db, companyId))
        {
            context.Response.Redirect("/settings");
            return;
        }
    }

    await next();
});

app.UseAntiforgery();

// Streams an uploaded document back to the browser so the Document Checklist can preview it.
//
// Uploads live outside wwwroot on purpose — they are PII (Aadhaar, PAN, bank statements) and
// anything under wwwroot is downloadable by anyone who guesses the URL. Stored filenames are
// server-generated GUIDs, so these URLs cannot be enumerated.
app.MapGet("/files/{applicationId}/{folder}/{name}", async (
    string applicationId,
    string folder,
    string name,
    IFileStore files,
    IDbContextFactory<LosDbContext> dbFactory,
    HttpContext http) =>
{
    // Requiring a signed-in user is not enough on its own. Without this check a user at company A
    // could read company B's Aadhaar and bank PDFs simply by holding a URL. Resolving the
    // application through the tenant-filtered context first means the file is only ever served to
    // somebody who can already see the application it belongs to — and a miss is a 404, not a 403,
    // so the response does not confirm that the application exists.
    await using var db = await dbFactory.CreateDbContextAsync();
    if (!await db.Applications.AnyAsync(a => a.Id == applicationId))
    {
        return Results.NotFound();
    }

    // FileKey rejects "..", empty and rooted segments, so a traversal attempt can't leave uploads/.
    Stream? stream;
    try
    {
        stream = await files.OpenReadAsync($"App_Data/uploads/{applicationId}/{folder}/{name}");
    }
    catch (ArgumentException)
    {
        return Results.NotFound();
    }

    if (stream is null)
    {
        return Results.NotFound();
    }

    // Opens in the browser as before, but "Save as" proposes the file's own (descriptive) name.
    http.Response.Headers.ContentDisposition = $"inline; filename=\"{name.Replace("\"", "")}\"";
    return Results.Stream(stream, ContentTypeFor(Path.GetExtension(name)));
}).RequireAuthorization();

// Served without a download filename so PDFs render inline in an iframe; the UI's Download link
// carries the `download` attribute when a file should be saved instead.
static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
{
    ".pdf" => "application/pdf",
    ".jpg" or ".jpeg" => "image/jpeg",
    ".png" => "image/png",
    ".webp" => "image/webp",
    ".webm" => "video/webm",
    ".mp4" => "video/mp4",
    _ => "application/octet-stream",
};

app.MapPartnerLoanApi();

// The vendor's uploaded UPI QR image, shown on the payment page. Any signed-in user may fetch it (it is just
// the vendor's public pay-to code, and a paused company must be able to see it).
// The signed-in landing: / used to be a placeholder page. Everyone goes to the applications list (a SuperAdmin without a company is sent on to the company picker from there).
app.MapGet("/", () => Results.Redirect("/applications"));

// API usage log as CSV for the month (Admin / SuperAdmin; an Admin gets their own company, a SuperAdmin the company they act as, else all).
app.MapGet("/admin/api-usage.csv", async (string? month, TenantContext tenant, ApiUsageReport report, IDbContextFactory<LosDbContext> dbFactory) =>
{
    await tenant.EnsureLoadedAsync();
    var m = DateOnly.TryParseExact((month ?? "") + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed)
        ? parsed : ApiBillingService.TodayIst();
    var entries = await report.EntriesAsync(tenant.ScopeCompanyId, m);
    await using var db = await dbFactory.CreateDbContextAsync();
    var names = await db.Companies.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name);
    var csv = ApiUsageReport.Csv(entries, id => names.GetValueOrDefault(id) ?? "#" + id);
    return Results.File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv; charset=utf-8", $"api-usage-{m:yyyy-MM}.csv");
}).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { Roles = TenantContext.AdminRoles });

app.MapGet("/billing/qr", async (IDbContextFactory<LosDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var qr = await db.PaymentDetails.AsNoTracking().Select(p => new { p.QrImage, p.QrContentType }).FirstOrDefaultAsync();
    return qr?.QrImage is { Length: > 0 } bytes ? Results.File(bytes, qr.QrContentType ?? "image/png") : Results.NotFound();
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Announce the LAN-reachable address once the server is listening, so whoever runs the server knows
// exactly what to tell branch staff to open. Only meaningful when bound to all interfaces
// (Urls = http://0.0.0.0:PORT in appsettings) — a localhost-only bind still logs, but only this
// machine can reach it.
app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Services.GetService<IServer>()?.Features.Get<IServerAddressesFeature>()?.Addresses;
    var port = addresses?
        .Select(a => Uri.TryCreate(a, UriKind.Absolute, out var u) ? u.Port : 0)
        .FirstOrDefault(p => p > 0) ?? 0;
    if (port == 0)
    {
        port = 5037;
    }

    var ip = LocalIPv4() ?? "localhost";
    app.Logger.LogInformation(
        "LOS/LMS is running. THIS machine is the server. On every OTHER device (staff, branches), open "
        + "this address in a web browser — do NOT run LOS-LMS.exe there, or it starts a separate, empty "
        + "system. They all share this one server's data, live:  http://{Ip}:{Port}",
        ip, port);
});

app.Run();

// The machine's primary LAN IPv4. The UDP socket picks the outbound interface without sending
// anything; falls back to the first non-loopback IPv4 from DNS if that fails.
static string? LocalIPv4()
{
    try
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect("8.8.8.8", 65530);
        return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
    }
    catch
    {
        try
        {
            return Array.Find(
                Dns.GetHostAddresses(Dns.GetHostName()),
                a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
