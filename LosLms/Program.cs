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
    .AddInteractiveServerComponents();

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
var connectionString = builder.Configuration.GetConnectionString("LosDb");

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
builder.Services.AddSingleton<UpdateNotificationService>();
builder.Services.AddHostedService<UpdateCheckBackgroundService>();

// Subscription licensing: verifies the baked/renewed license, warns near expiry, and freezes the app
// once expired (see MainLayout + the /account/renew page).
builder.Services.AddSingleton<LicenseService>();

// Server-side encrypted store of admin-set passwords, so an Admin can re-view a credential later
// (masked, briefly revealed). Recoverable by design — see PasswordVault's own remarks for the trade-off.
builder.Services.AddSingleton<PasswordVault>();

builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<IDbContextFactory<LosDbContext>, TenantDbContextFactory>();

// Cloudflare Turnstile bot protection on sign-in. Off unless both keys are set under Security:Turnstile
// (see TurnstileOptions), so LAN-only installs are unaffected.
builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection(TurnstileOptions.Section));
builder.Services.AddScoped<TurnstileVerifier>();

// Public web instance settings (self-service registration). The section is absent on the desktop build,
// so AllowSelfRegistration defaults false and the register page / sign-in link stay closed there.
builder.Services.Configure<WebOptions>(builder.Configuration.GetSection(WebOptions.Section));

// Provisions a brand-new tenant (company + owning admin) for the public sign-up page and the demo
// seeder. Scoped, because it creates the admin through the scoped UserManager.
builder.Services.AddScoped<CompanyProvisioningService>();

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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    // A signed-in user who fails a role check is NOT sent to the sign-in form. They are already
    // signed in; a login page would read as "your session expired" rather than "you lack the role".
    options.AccessDeniedPath = "/account/denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Remote users reach this backend through a Cloudflare tunnel that terminates HTTPS and forwards to
// the app over plain HTTP on localhost. Without this, the app sees every request as "http" and builds
// http:// redirect URLs (e.g. the sign-in redirect), bouncing the browser off HTTPS. Honouring the
// X-Forwarded-Proto that the tunnel sets keeps the app on the correct external scheme. Safe here
// because the backend binds to 127.0.0.1 only — the sole caller is the local tunnel client, so the
// header cannot be spoofed by a remote user reaching the backend directly.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
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
        var isSensitivePost = HttpMethods.IsPost(context.Request.Method)
            && (context.Request.Path.StartsWithSegments("/account/login", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.StartsWithSegments("/account/register", StringComparison.OrdinalIgnoreCase));
        if (!isSensitivePost)
        {
            return RateLimitPartition.GetNoLimiter("unlimited");
        }

        var clientIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

var app = builder.Build();

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

    // Demo data — fifteen worked-through applications and a vehicle-cap catalog — for development only.
    // OFF by default so the shipped build is a blank slate with no seeded places, customers or figures.
    // Turn it on in development with `dotnet run --Seed:DemoApplications=true` (or user-secrets/env).
    if (app.Configuration.GetValue("Seed:DemoApplications", false))
    {
        await DemoSeedData.SeedAsync(app.Services, app.Logger, app.Environment.ContentRootPath);
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
    "img-src 'self' data:; " +
    "style-src 'self' 'unsafe-inline'; " +
    scriptSrc +
    frameSrc +
    "connect-src 'self'; " +
    "form-action 'self'";

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["Content-Security-Policy"] = csp;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

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
        || p.StartsWithSegments("/files", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/system", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase)
        || p.StartsWithSegments("/_content", StringComparison.OrdinalIgnoreCase)
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
    IWebHostEnvironment environment,
    IDbContextFactory<LosDbContext> dbFactory) =>
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

    var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "uploads"));
    var candidate = Path.GetFullPath(Path.Combine(root, applicationId, folder, name));

    // Compare the CANONICAL path, not the raw string: GetFullPath has already collapsed any ".."
    // so a traversal attempt lands outside the root and fails this check.
    var isInsideRoot = candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    return isInsideRoot && File.Exists(candidate)
        ? Results.File(candidate, ContentTypeFor(Path.GetExtension(candidate)))
        : Results.NotFound();
}).RequireAuthorization();

// Served without a download filename so PDFs render inline in an iframe; the UI's Download link
// carries the `download` attribute when a file should be saved instead.
static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
{
    ".pdf" => "application/pdf",
    ".jpg" or ".jpeg" => "image/jpeg",
    ".png" => "image/png",
    ".webp" => "image/webp",
    _ => "application/octet-stream",
};

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
