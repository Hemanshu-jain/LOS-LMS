using System.Globalization;
using System.Text.RegularExpressions;
using LosLms.Data;
using LosLms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// The partner data API (/api/v1) — Paavan pushes its loan accounts into <see cref="MigratedLoan"/> and
/// reads back what LMS has disbursed. Shape mirrors Handoff's /api/v1/cases so the partner's existing
/// Upsert / Check / Delete functions map 1:1.
/// </summary>
/// <remarks>
/// Auth comes from the OAuth side: policies "loans:read" / "loans:write" (PartnerBearer scheme) and the
/// claims <c>company_id</c> and <c>partner_name</c>. The company is read from the claim ONLY — never the
/// body — and every query filters on it explicitly with <c>IgnoreQueryFilters</c>, because the global
/// tenant filter keys off the cookie claim a partner token does not carry.
/// </remarks>
public static class PartnerLoanApi
{
    public const string ReadPolicy = "loans:read";
    public const string WritePolicy = "loans:write";
    public const int MaxBatch = 500;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    private static readonly Regex PanFormat = new("^[A-Z]{5}[0-9]{4}[A-Z]$", RegexOptions.Compiled);
    private static readonly Regex MobileFormat = new(@"^\+?[0-9]{6,14}$", RegexOptions.Compiled);

    public sealed record LoanIn(
        string? AccountNumber, string? CustomerName, string? Pan, string? Mobile, string? Product,
        decimal? SanctionedAmount, decimal? OutstandingAmount, string? Status, string? DisbursedOn, string? Branch);

    public sealed record BatchIn(List<LoanIn>? Loans);

    public sealed record RowError(int Row, string Field, string Reason);

    /// <summary>Gives every 4xx/5xx the platform would send with an empty body (401/403 from auth, 429 from
    /// the rate limiter, bad JSON) the same {error,message,details[]} shape as our own replies.</summary>
    public static IApplicationBuilder UseJsonErrors(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            await next();
            var r = context.Response;
            if (r.HasStarted || r.ContentLength is not null || r.ContentType is not null || r.StatusCode < 400)
            {
                return;
            }

            var (error, message) = r.StatusCode switch
            {
                400 => ("bad_request", "The request could not be read. Send valid JSON."),
                401 => ("unauthorized", "Missing, expired or invalid access token. Refresh it or connect again."),
                403 => ("insufficient_scope", "This connection is not allowed to do that."),
                404 => ("not_found", "No such endpoint."),
                405 => ("method_not_allowed", "That method is not supported here."),
                413 => ("too_large", "The request body is too large."),
                429 => ("rate_limited", "Too many requests. Wait a minute and try again."),
                _ => ("error", "The request failed."),
            };
            await r.WriteAsJsonAsync(new { error, message, details = Array.Empty<object>() });
        });

    public static IEndpointRouteBuilder MapPartnerLoanApi(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1").DisableAntiforgery();

        api.MapPost("/loans", UpsertAsync).RequireAuthorization(WritePolicy)
            .WithMetadata(new RequestSizeLimitAttribute(2_000_000));
        api.MapGet("/loans", ListAsync).RequireAuthorization(ReadPolicy);
        api.MapGet("/loans/{**accountNumber}", GetAsync).RequireAuthorization(ReadPolicy);
        api.MapDelete("/loans/{**accountNumber}", DeleteAsync).RequireAuthorization(WritePolicy);
        api.MapGet("/disbursals", DisbursalsAsync).RequireAuthorization(ReadPolicy);
        return routes;
    }

    // ---- POST /loans ------------------------------------------------------------------------------

    private static async Task<IResult> UpsertAsync(
        BatchIn? body, HttpContext http, IDbContextFactory<LosDbContext> dbFactory, ILogger<LosDbContext> log)
    {
        if (!TryCompany(http, out var companyId)) return Err(401, "unauthorized", "Token has no company.");
        if (body?.Loans is not { Count: > 0 } loans)
        {
            return Err(422, "validation_failed", "Send {\"loans\":[...]} with at least one loan.");
        }

        if (loans.Count > MaxBatch)
        {
            return Err(422, "batch_too_large", $"At most {MaxBatch} loans per request; you sent {loans.Count}.");
        }

        var (rows, errors) = Validate(loans);
        if (errors.Count > 0)
        {
            return Err(422, "validation_failed", $"{errors.Select(e => e.Row).Distinct().Count()} loan(s) are invalid. Nothing was saved.", errors);
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var accounts = rows.Select(r => r.AccountNumber).ToList();
        var existing = (await db.MigratedLoans.IgnoreQueryFilters()
                .Where(m => m.CompanyId == companyId && accounts.Contains(m.LoanAccountNo)).ToListAsync())
            .GroupBy(m => m.LoanAccountNo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var results = new List<(int Row, string Result, MigratedLoan Loan)>();
        int created = 0, updated = 0, unchanged = 0;
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            string outcome;
            if (!existing.TryGetValue(r.AccountNumber, out var m))
            {
                m = new MigratedLoan { CompanyId = companyId, LoanAccountNo = r.AccountNumber };
                db.MigratedLoans.Add(m);
                Apply(m, r);
                outcome = "created";
                created++;
            }
            else if (Same(m, r))
            {
                outcome = "unchanged";
                unchanged++;
            }
            else
            {
                Apply(m, r);
                outcome = "updated";
                updated++;
            }

            results.Add((i + 1, outcome, m));
        }

        await db.SaveChangesAsync();

        if (created + updated > 0)
        {
            log.LogInformation(
                "AUDIT migrated loans upserted Via {Partner} company {CompanyId}: {Created} created, {Updated} updated",
                Partner(http), companyId, created, updated);
        }

        // Ids exist only after the save, so the reply is shaped here.
        var withIds = results.Select(x => new { row = x.Row, accountNumber = x.Loan.LoanAccountNo, result = x.Result, id = x.Loan.Id });
        return Results.Ok(new { created, updated, unchanged, results = withIds });
    }

    private sealed record Row(
        string AccountNumber, string CustomerName, string? Pan, string? Mobile, string? Product,
        decimal? Sanctioned, decimal? Outstanding, string? Status, DateOnly? DisbursedOn, string? Branch);

    private static (List<Row> Rows, List<RowError> Errors) Validate(List<LoanIn> loans)
    {
        var rows = new List<Row>();
        var errors = new List<RowError>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < loans.Count; i++)
        {
            var n = i + 1;
            var l = loans[i];
            if (l is null) { errors.Add(new(n, "loan", "is empty")); continue; }

            var account = Clean(l.AccountNumber);
            var name = Clean(l.CustomerName);
            var pan = Clean(l.Pan)?.ToUpperInvariant();
            var mobile = Clean(l.Mobile);
            var product = Clean(l.Product);
            var status = Clean(l.Status);
            var branch = Clean(l.Branch);
            DateOnly? disbursed = null;

            void Bad(string field, string reason) => errors.Add(new(n, field, reason));

            if (account is null) Bad("accountNumber", "is required");
            else if (account.Length > 60) Bad("accountNumber", "must be 60 characters or fewer");
            else if (!seen.Add(account)) Bad("accountNumber", "appears twice in this request");

            if (name is null) Bad("customerName", "is required");
            else if (name.Length > 200) Bad("customerName", "must be 200 characters or fewer");

            if (pan is not null && !PanFormat.IsMatch(pan)) Bad("pan", "must look like ABCDE1234F");
            if (mobile is not null && !MobileFormat.IsMatch(mobile)) Bad("mobile", "must be 6 to 14 digits, optional leading +");
            if (product?.Length > 120) Bad("product", "must be 120 characters or fewer");
            if (status?.Length > 40) Bad("status", "must be 40 characters or fewer");
            if (branch?.Length > 100) Bad("branch", "must be 100 characters or fewer");
            if (l.SanctionedAmount < 0) Bad("sanctionedAmount", "cannot be negative");
            if (l.OutstandingAmount < 0) Bad("outstandingAmount", "cannot be negative");

            if (Clean(l.DisbursedOn) is { } d)
            {
                if (DateOnly.TryParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    disbursed = parsed;
                else
                    Bad("disbursedOn", "must be a date like 2026-09-30");
            }

            if (errors.All(e => e.Row != n))
            {
                rows.Add(new Row(account!, name!, pan, mobile, product, l.SanctionedAmount, l.OutstandingAmount, status, disbursed, branch));
            }
        }

        return (rows, errors);
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static void Apply(MigratedLoan m, Row r)
    {
        m.CustomerName = r.CustomerName;
        m.Pan = r.Pan;
        m.Mobile = r.Mobile;
        m.Product = r.Product;
        m.SanctionedAmount = r.Sanctioned;
        m.OutstandingAmount = r.Outstanding;
        m.Status = r.Status;
        m.DisbursedOn = r.DisbursedOn;
        m.Branch = r.Branch;
        m.ImportedAt = DateTime.UtcNow;
    }

    private static bool Same(MigratedLoan m, Row r) =>
        m.CustomerName == r.CustomerName && m.Pan == r.Pan && m.Mobile == r.Mobile && m.Product == r.Product
        && m.SanctionedAmount == r.Sanctioned && m.OutstandingAmount == r.Outstanding && m.Status == r.Status
        && m.DisbursedOn == r.DisbursedOn && m.Branch == r.Branch;

    // ---- GET / DELETE one, GET list ---------------------------------------------------------------

    private static object Shape(MigratedLoan m) => new
    {
        id = m.Id,
        accountNumber = m.LoanAccountNo,
        customerName = m.CustomerName,
        pan = m.Pan,
        mobile = m.Mobile,
        product = m.Product,
        sanctionedAmount = m.SanctionedAmount,
        outstandingAmount = m.OutstandingAmount,
        status = m.Status,
        disbursedOn = m.DisbursedOn?.ToString("yyyy-MM-dd"),
        branch = m.Branch,
    };

    private static async Task<IResult> GetAsync(
        string accountNumber, HttpContext http, IDbContextFactory<LosDbContext> dbFactory)
    {
        if (!TryCompany(http, out var companyId)) return Err(401, "unauthorized", "Token has no company.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var m = await Find(db, companyId, accountNumber);
        return m is null
            ? Err(404, "not_found", $"No loan with account number {accountNumber}.")
            : Results.Ok(new { exists = true, loan = Shape(m) });
    }

    private static async Task<IResult> DeleteAsync(
        string accountNumber, HttpContext http, IDbContextFactory<LosDbContext> dbFactory, ILogger<LosDbContext> log)
    {
        if (!TryCompany(http, out var companyId)) return Err(401, "unauthorized", "Token has no company.");
        await using var db = await dbFactory.CreateDbContextAsync();
        var m = await Find(db, companyId, accountNumber);
        if (m is null) return Err(404, "not_found", $"No loan with account number {accountNumber}.");

        db.MigratedLoans.Remove(m);
        await db.SaveChangesAsync();
        log.LogInformation("AUDIT migrated loan {Account} deleted Via {Partner} company {CompanyId}", m.LoanAccountNo, Partner(http), companyId);
        return Results.Ok(new { deleted = true, id = m.Id, accountNumber = m.LoanAccountNo });
    }

    private static async Task<IResult> ListAsync(
        HttpContext http, IDbContextFactory<LosDbContext> dbFactory, int? page, int? pageSize)
    {
        if (!TryCompany(http, out var companyId)) return Err(401, "unauthorized", "Token has no company.");
        var (p, size) = Paging(page, pageSize);
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.MigratedLoans.IgnoreQueryFilters().AsNoTracking().Where(m => m.CompanyId == companyId);
        var total = await q.CountAsync();
        var items = await q.OrderBy(m => m.Id).Skip((p - 1) * size).Take(size).ToListAsync();
        return Results.Ok(new { page = p, pageSize = size, total, items = items.Select(Shape) });
    }

    // ---- GET /disbursals --------------------------------------------------------------------------

    private static async Task<IResult> DisbursalsAsync(
        HttpContext http, IDbContextFactory<LosDbContext> dbFactory, string? since, int? page, int? pageSize)
    {
        if (!TryCompany(http, out var companyId)) return Err(401, "unauthorized", "Token has no company.");
        DateOnly? from = null;
        if (!string.IsNullOrWhiteSpace(since))
        {
            if (!DateOnly.TryParseExact(since, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                return Err(422, "validation_failed", "since must be a date like 2026-09-30.",
                    new[] { new RowError(0, "since", "must be a date like 2026-09-30") });
            }

            from = d;
        }

        var (p, size) = Paging(page, pageSize);
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Applications.IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.Disbursed && a.DisbursalDate != null);
        if (from is { } f) q = q.Where(a => a.DisbursalDate >= f);

        var total = await q.CountAsync();
        var items = await q.OrderBy(a => a.DisbursalDate).ThenBy(a => a.Id)
            .Skip((p - 1) * size).Take(size).ToListAsync();
        return Results.Ok(new
        {
            page = p, pageSize = size, total,
            items = items.Select(a => new
            {
                applicationId = a.Id,
                customerName = a.CustomerName,
                loanAmount = a.LoanAmount,
                roi = a.Roi,
                tenure = a.Tenure,
                disbursalDate = a.DisbursalDate?.ToString("yyyy-MM-dd"),
                firstEmiDate = a.FirstEmiDate?.ToString("yyyy-MM-dd"),
                emiDayOfMonth = a.EmiDayOfMonth,
            }),
        });
    }

    // ---- helpers ----------------------------------------------------------------------------------

    private static async Task<MigratedLoan?> Find(LosDbContext db, int companyId, string account)
    {
        account = account.Trim();
        // ponytail: exact match in SQL (case-insensitive on MySQL's default collation), no second pass.
        return await db.MigratedLoans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.LoanAccountNo == account);
    }

    private static (int Page, int Size) Paging(int? page, int? pageSize) =>
        (Math.Max(page ?? 1, 1), Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));

    /// <summary>The tenant. From the token's company_id claim, nowhere else.</summary>
    private static bool TryCompany(HttpContext http, out int companyId)
    {
        companyId = 0;
        return int.TryParse(http.User.FindFirst("company_id")?.Value, out companyId) && companyId > 0;
    }

    private static string Partner(HttpContext http) => http.User.FindFirst("partner_name")?.Value ?? "partner app";

    private static IResult Err(int status, string error, string message, IEnumerable<RowError>? details = null) =>
        Results.Json(new { error, message, details = details?.ToArray() ?? Array.Empty<RowError>() }, statusCode: status);
}
