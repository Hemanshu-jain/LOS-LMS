using LosLms.Components;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

// An empty or absent LosDb connection string switches the whole app onto a self-contained SQLite
// file — this is the portable client-demo build, which needs no MySQL server. Development keeps its
// real MySQL connection string in appsettings and behaves exactly as before.
var connectionString = builder.Configuration.GetConnectionString("LosDb");
var usePortableSqlite = string.IsNullOrWhiteSpace(connectionString);

// A factory, not AddDbContext. In Blazor Server a scoped DbContext lives for the whole SignalR
// circuit and is shared by every component on it, so two overlapping renders hit the same context
// and throw. Components create a short-lived context per operation instead.
//
// ponytail: the MySQL server version is pinned instead of using ServerVersion.AutoDetect(...),
// which opens a MySQL connection during startup and would take every branch offline whenever the
// central database is unreachable. EF opens no connection until the first query this way.
// Bump the constant to match the central server; move it to configuration only if it ever
// needs to differ per deployment.
if (usePortableSqlite)
{
    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "los_lms.db");
    builder.Services.AddDbContextFactory<LosDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
}
else
{
    builder.Services.AddDbContextFactory<LosDbContext>(options =>
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));
}

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

builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<IDbContextFactory<LosDbContext>, TenantDbContextFactory>();

// Identity's UserStore and RoleStore resolve LosDbContext directly rather than through the factory,
// so hand them one built the same way.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<LosDbContext>>().CreateDbContext());

// Nothing set here may affect the MODEL — only behaviour. Identity reads Stores.MaxLengthForKeys
// (and ProtectPersonalData) while building the model, from the application service provider, which
// the design-time factory has no way to supply. Setting either one would make `dotnet ef migrations`
// scaffold different column types from the ones the app actually runs against. Pomelo's default
// varchar(255) indexes fine under InnoDB's 3072-byte limit, so there is nothing to gain by pinning it.
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

builder.Services.AddAuthorization(options =>
{
    // Fail closed. Every endpoint requires a signed-in user unless it says [AllowAnonymous] out loud,
    // so a page added later is protected by default rather than by whoever remembers to protect it.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// The portable SQLite build has no migrations run against it (migrations are MySQL-specific), so
// build the schema and seed data (the HasData calls in LosDbContext) from the model on first
// launch. No-op once los_lms.db already exists, so the client's test data survives restarts.
if (usePortableSqlite)
{
    // Built directly rather than resolved from DI: startup is outside any request, so there is no
    // signed-in user for a tenant-scoped context to read, and the seeding tenant has to see
    // everything to be able to create it.
    await using var db = new LosDbContext(
        app.Services.GetRequiredService<DbContextOptions<LosDbContext>>(),
        TenantContext.ForSeeding());

    await db.Database.EnsureCreatedAsync();
}

// Roles, the initial users and their temporary passwords. Idempotent, so it is safe on every start
// and it has to be — the SQLite path never runs a migration.
await IdentitySeeder.SeedAsync(app.Services, app.Logger);

// Fifteen worked-through applications spread across the eight stages, so every screen has real data
// to open. Only ever runs against an empty Applications table.
if (app.Configuration.GetValue("Seed:DemoApplications", true))
{
    await DemoSeedData.SeedAsync(app.Services, app.Logger, app.Environment.ContentRootPath);
}

// A throwaway second company, only for proving tenant isolation. Never on by default.
if (app.Configuration.GetValue<bool>("Seed:IsolationFixture"))
{
    await IdentitySeeder.SeedIsolationFixtureAsync(app.Services, app.Logger);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// The portable build serves plain HTTP on localhost only (no cert), so skip the redirect —
// otherwise Kestrel just logs a "failed to determine https port" warning on every request.
if (!usePortableSqlite)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
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

app.Run();
