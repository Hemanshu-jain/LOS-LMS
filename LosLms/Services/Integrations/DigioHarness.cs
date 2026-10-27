#if DEBUG
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace LosLms.Services;

/// <summary>
/// Phase 0 throwaway (DEBUG builds + Development + SuperAdmin only, so it cannot exist in a release): calls each planned Digio
/// SANDBOX product once with the company's vault keys, saves the response with PII masked under docs/digio/samples, and returns
/// a summary of HTTP statuses (403 = product not enabled). Refuses production keys. Nothing is metered. Delete after Phase 0.
/// GET /dev/digio-harness?company=1[&amp;mobile=98…][&amp;esign=1][&amp;nachConfigId=…]
/// </summary>
public static partial class DigioHarness
{
    private sealed record Probe(string File, HttpMethod Method, string Path, object? Body);

    public static void MapDigioHarness(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.MapGet("/dev/digio-harness", async (HttpContext http, DigioClient digio, IWebHostEnvironment env, int company, string? mobile, bool? esign, string? nachConfigId) =>
        {
            var creds = digio.Credentials(company);
            if (creds is null)
            {
                return Results.BadRequest("No Digio keys saved for that company. Enter them on the Digio keys page first.");
            }

            if (creds.Production)
            {
                return Results.BadRequest("These are PRODUCTION keys. The harness only runs against sandbox keys.");
            }

            var samples = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "docs", "digio", "samples"));
            Directory.CreateDirectory(samples);
            var run = Guid.NewGuid().ToString("N")[..8];
            var results = new List<object>();

            foreach (var p in Probes(run, mobile, esign == true, nachConfigId))
            {
                results.Add(await RunAsync(digio, company, p, samples));
            }

            // DigiStudio: fetch the result of the request the probe above just created.
            if (Existing(samples, "digilocker-request.json") is { } kid)
            {
                results.Add(await RunAsync(digio, company,
                    new Probe("digilocker-response.json", HttpMethod.Post, $"/client/kyc/v2/{kid}/response?detail_response=true", new { }), samples));
            }

            return Results.Json(results);
        }).RequireAuthorization(new AuthorizeAttribute { Roles = TenantContext.SuperAdminRole });
    }

    private static IEnumerable<Probe> Probes(string run, string? mobile, bool esign, string? nachConfigId)
    {
        yield return new("pan-verify.json", HttpMethod.Post, "/v3/client/kyc/fetch_id_data/PAN",
            new { id_no = "ABCPE1234F", name = "Test Name", dob = "1990-01-01", unique_request_id = "h" + run + "pan" });
        yield return new("pan-verify-dob-ddmmyyyy.json", HttpMethod.Post, "/v3/client/kyc/fetch_id_data/PAN",
            new { id_no = "ABCPE1234F", name = "Test Name", dob = "01/01/1990", unique_request_id = "h" + run + "pan2" });
        yield return new("gst-verify.json", HttpMethod.Post, "/v3/client/kyc/fetch_id_data/GST", new { id_no = "27AAPFU0939F1ZV", unique_request_id = "h" + run + "gst" });
        yield return new("aadhaar-mask.json", HttpMethod.Post, "/v4/client/kyc/aadhaar/mask", new
        {
            data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==",
            data_content_type = "image/png", consent = "yes", mask_qr = true, is_validate = false, unique_request_id = "h" + run + "mask",
        });
        yield return new("digilocker-request.json", HttpMethod.Post, "/client/kyc/v2/request/with_template", new
        {
            customer_identifier = mobile ?? "9999999999", template_name = "DIGILOCKER_AADHAAR_PAN", customer_name = "Test Name",
            reference_id = "h" + run + "dl", notify_customer = false, generate_access_token = true,
        });
        yield return new("vehicle-rc.json", HttpMethod.Post, "/client/v4/apis/verify/vehicle_rc", new { id = "MH12AB1234", unique_request_id = "h" + run + "rc" });
        yield return new("aml-screen.json", HttpMethod.Post, "/client/v1/user/screen", new
        {
            screening_mode = "SCREENING",
            screening_settings = new { screening_type = "SCREEN_ONLY_RESPONSE", response_mode = "DETAILED", cut_off_threshold = 70 },
            user_details = new { client_id = "h" + run + "aml", name = "Test Name" },
        });
        yield return new("mandate-banks.json", HttpMethod.Get, "/v3/client/mandate/banks?mode=API", null);

        if (esign && !string.IsNullOrWhiteSpace(mobile))
        {
            yield return new("esign-uploadpdf.json", HttpMethod.Post, "/v2/client/document/uploadpdf", new
            {
                file_name = "harness.pdf", file_data = Convert.ToBase64String(OnePagePdf),
                signers = new[] { new { identifier = mobile, name = "Test Name", sign_type = "aadhaar", reason = "Digio harness test" } },
                expire_in_days = 1, notify_signers = true, send_sign_link = true, include_authentication_url = true, reference_id = "h" + run + "es",
            });
        }

        if (!string.IsNullOrWhiteSpace(nachConfigId))
        {
            yield return new("nach-create-form-api.json", HttpMethod.Post, "/v3/client/mandate/create_form", new
            {
                corporate_config_id = nachConfigId, mandate_type = "CREATE", auth_mode = "API", notify_customer = false, include_authentication_url = true,
                customer_identifier = mobile ?? "9999999999",
                mandate_data = new
                {
                    customer_name = "Test Name", customer_account_number = "1234567890", customer_account_type = "savings",
                    management_category = "L001", is_recurring = true, frequency = "Monthly",
                    first_collection_date = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd"), collection_amount = 5000, maximum_amount = 5000,
                    customer_ref_number = "H" + run.ToUpperInvariant() + "NACH",
                },
            });
        }
    }

    private static async Task<object> RunAsync(DigioClient digio, int company, Probe p, string samples)
    {
        var r = p.Method == HttpMethod.Get
            ? await digio.GetAsync(company, null, p.Path, null)
            : await digio.PostAsync(company, null, p.Path, p.Body!, null);

        if (r.Data is { } data)
        {
            var node = JsonNode.Parse(data.GetRawText());
            var masked = new JsonObject
            {
                ["_captured"] = $"{DateTime.UtcNow:yyyy-MM-dd}, sandbox, {p.Method.Method} {p.Path.Split('?')[0]}, http {r.HttpStatus}",
                ["response"] = Mask(node, null),
            };
            await File.WriteAllTextAsync(Path.Combine(samples, p.File), masked.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        var note = r.HttpStatus == 403 ? "not enabled" : r.Status == DigioStatus.Ok ? "ok" : r.Error;
        return new { sample = p.File, http = r.HttpStatus, status = r.Status.ToString(), errorCode = r.ErrorCode, note };
    }

    private static string? Existing(string samples, string file)
    {
        var path = Path.Combine(samples, file);
        return File.Exists(path) && JsonNode.Parse(File.ReadAllText(path))?["response"]?["id"]?.GetValue<string>() is { } id ? id : null;
    }

    // ---- masking (docs/digio/samples/README.md): structure and keys kept, values masked ----

    private static JsonNode? Mask(JsonNode? node, string? key) => node switch
    {
        JsonObject o => new JsonObject(o.Select(kv => KeyValuePair.Create(kv.Key, Mask(kv.Value, kv.Key)))),
        JsonArray a => new JsonArray(a.Select(n => Mask(n, key)).ToArray()),
        JsonValue v when v.TryGetValue<string>(out var s) => JsonValue.Create(MaskString(s, key)),
        _ => node?.DeepClone(),
    };

    private static string MaskString(string s, string? key)
    {
        var k = key?.ToLowerInvariant() ?? "";
        if (s.Length > 200) { return "<base64 omitted>"; }
        if (k.EndsWith("name") && !k.StartsWith("template") && !k.StartsWith("file") && !k.StartsWith("bank") && !k.StartsWith("source") && s.Length > 0) { return "TEST NAME"; }
        if (k.Contains("address") && s.Length > 0) { return "TEST ADDRESS, CITY, STATE 000000"; }
        if (s.Contains('@')) { return "test@example.com"; }
        if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase) && s.Contains('?')) { return s[..s.IndexOf('?')] + "?<omitted>"; }

        s = PanPattern().Replace(s, m => "XXXXX" + m.Value[5..9] + "X");
        return DigitRun().Replace(s, m => new string('X', m.Length - 4) + m.Value[^4..]);
    }

    [GeneratedRegex("[A-Z]{5}[0-9]{4}[A-Z]")]
    private static partial Regex PanPattern();

    [GeneratedRegex("[0-9]{8,}")]
    private static partial Regex DigitRun();

    private static readonly byte[] OnePagePdf = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
        "3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 300 300]>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF");
}
#endif
