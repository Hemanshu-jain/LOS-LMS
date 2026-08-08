using LosLms.Components;
using LosLms.Data;
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

var app = builder.Build();

// The portable SQLite build has no migrations run against it (migrations are MySQL-specific), so
// build the schema and seed data (the HasData calls in LosDbContext) from the model on first
// launch. No-op once los_lms.db already exists, so the client's test data survives restarts.
if (usePortableSqlite)
{
    using var scope = app.Services.CreateScope();
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LosDbContext>>();
    using var db = contextFactory.CreateDbContext();
    db.Database.EnsureCreated();
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
app.UseAntiforgery();

// Streams an uploaded document back to the browser so the Document Checklist can preview it.
//
// Uploads live outside wwwroot on purpose — they are PII (Aadhaar, PAN, bank statements) and
// anything under wwwroot is downloadable by anyone who guesses the URL. Stored filenames are
// server-generated GUIDs, so these URLs cannot be enumerated.
//
// THERE IS STILL NO AUTHENTICATION IN THIS BUILD, so possession of a URL is possession of the
// document. See OPEN-QUESTIONS-FOR-ARUN.md, items 1.2 and 4.2.
app.MapGet("/files/{applicationId}/{folder}/{name}", (
    string applicationId,
    string folder,
    string name,
    IWebHostEnvironment environment) =>
{
    var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "uploads"));
    var candidate = Path.GetFullPath(Path.Combine(root, applicationId, folder, name));

    // Compare the CANONICAL path, not the raw string: GetFullPath has already collapsed any ".."
    // so a traversal attempt lands outside the root and fails this check.
    var isInsideRoot = candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    return isInsideRoot && File.Exists(candidate)
        ? Results.File(candidate, ContentTypeFor(Path.GetExtension(candidate)))
        : Results.NotFound();
});

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
