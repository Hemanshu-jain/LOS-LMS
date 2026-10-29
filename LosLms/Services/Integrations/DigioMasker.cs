using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LosLms.Data;
using LosLms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// Keeps a Digio reply's structure and keys but masks every value that could identify a person. Used for the saved samples (docs/digio/samples)
/// and for the SuperAdmin-only view below, so a real reply can be read without its PII ever leaving the server.
/// </summary>
public static partial class DigioMasker
{
    public static JsonNode? Mask(JsonNode? node, string? key = null) => node switch
    {
        JsonObject o => new JsonObject(o.Select(kv => KeyValuePair.Create(kv.Key, Mask(kv.Value, kv.Key)))),
        JsonArray a => new JsonArray(a.Select(n => Mask(n, key)).ToArray()),
        JsonValue v when v.TryGetValue<string>(out var s) => JsonValue.Create(MaskString(s, key)),
        _ => node?.DeepClone(),
    };

    public static string MaskString(string s, string? key)
    {
        var k = key?.ToLowerInvariant() ?? "";
        if (s.Length > 200) { return "<base64 omitted>"; }
        if (k.EndsWith("name") && !k.StartsWith("template") && !k.StartsWith("file") && !k.StartsWith("bank") && !k.StartsWith("source") && s.Length > 0) { return "TEST NAME"; }
        if (k.Contains("address") && s.Length > 0) { return "TEST ADDRESS, CITY, STATE 000000"; }
        if ((k.Contains("dob") || k.Contains("birth")) && s.Length > 0) { return Regex.Replace(s, "[0-9]", "0"); }
        if (k is "city" or "state" or "district" or "locality" or "house" or "street" or "landmark" or "co" or "vtc" or "po" or "subdist" && s.Length > 0) { return "TEST"; }
        if (k.Contains("pin") && s.Length > 0) { return "000000"; }
        if ((k.Contains("locality") || k.Contains("city") || k.Contains("district")) && s.Length > 0) { return "TEST"; }
        if (k.EndsWith("id_number") && s.Length > 4) { return new string('X', s.Length - 4) + s[^4..]; }
        if (s.Contains('@')) { return "test@example.com"; }
        if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase) && s.Contains('?')) { return s[..s.IndexOf('?')] + "?<omitted>"; }

        s = PanPattern().Replace(s, m => "XXXXX" + m.Value[5..9] + "X");
        return DigitRun().Replace(s, m => new string('X', m.Length - 4) + m.Value[^4..]);
    }

    [GeneratedRegex("[A-Z]{5}[0-9]{4}[A-Z]")]
    private static partial Regex PanPattern();

    [GeneratedRegex("[0-9]{8,}")]
    private static partial Regex DigitRun();

    /// <summary>
    /// SuperAdmin only. GET /system/digio/masked-response/{requestId} or /system/digio/masked-response/app/{applicationId} (the application's latest
    /// DigiLocker request, whatever its state). Asks Digio for the result and returns it MASKED.
    /// </summary>
    public static void MapDigioMaskedView(this WebApplication app)
    {
        static async Task<IResult> Show(DigioRequest? req, DigioKyc kyc)
        {
            if (req?.DigioId is null)
            {
                return Results.NotFound();
            }

            var r = await kyc.FetchDigiLockerAsync(req.CompanyId, req.DigioId, null);
            return r.Data is { } d
                ? Results.Json(new JsonObject { ["request"] = req.Id, ["http"] = r.HttpStatus, ["response"] = Mask(JsonNode.Parse(d.GetRawText())) })
                : Results.Json(new { request = req.Id, http = r.HttpStatus, error = r.Error });
        }

        var superAdmin = new AuthorizeAttribute { Roles = TenantContext.SuperAdminRole };
        app.MapGet("/system/digio/masked-response/{requestId}", async (string requestId, DigioKyc kyc, IDbContextFactory<LosDbContext> dbFactory) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            return await Show(await db.DigioRequests.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(r => r.Id == requestId), kyc);
        }).RequireAuthorization(superAdmin);

        app.MapGet("/system/digio/masked-response/app/{applicationId}", async (string applicationId, DigioKyc kyc, IDbContextFactory<LosDbContext> dbFactory) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            return await Show(await db.DigioRequests.IgnoreQueryFilters().AsNoTracking()
                .Where(r => r.ApplicationId == applicationId && r.Operation == "DIGILOCKER_AADHAAR_PAN" && r.DigioId != null)
                .OrderByDescending(r => r.RequestedAt).FirstOrDefaultAsync(), kyc);
        }).RequireAuthorization(superAdmin);
    }
}
