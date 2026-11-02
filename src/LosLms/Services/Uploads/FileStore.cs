using System.Net;
using System.Net.Http.Headers;

namespace LosLms.Services;

/// <summary>
/// Where uploaded and generated files live. Every key is the relative path the database already stores
/// ("App_Data/uploads/{application}/{folder}/{file}", "App_Data/welcome-letters/{file}"), so rows written
/// before this existed still resolve once the files are copied across. Disk for local runs; a private
/// Supabase Storage bucket on Render, whose disk is wiped on every restart.
/// </summary>
public interface IFileStore
{
    /// <summary>Writes (or replaces) the file at <paramref name="key"/>.</summary>
    Task SaveAsync(string key, byte[] bytes);

    /// <summary>The file's content, or null when there is no such file. The caller disposes the stream.</summary>
    Task<Stream?> OpenReadAsync(string key);
}

public static class FileStoreExtensions
{
    public static async Task<byte[]?> ReadAllBytesAsync(this IFileStore store, string key)
    {
        await using var stream = await store.OpenReadAsync(key);
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    /// <summary>Saves under the standard per-application upload path and returns the key the database stores.</summary>
    public static async Task<string> SaveUploadAsync(
        this IFileStore store, string applicationId, string folder, string fileName, byte[] bytes)
    {
        var key = Path.Combine("App_Data", "uploads", applicationId, folder, fileName);
        await store.SaveAsync(key, bytes);
        return key;
    }
}

/// <summary>Key hygiene shared by both stores: forward slashes, under App_Data, no traversal.</summary>
public static class FileKey
{
    public static string Normalize(string key)
    {
        var parts = (key ?? string.Empty).Replace('\\', '/').Split('/');
        if (parts.Length < 2 || parts[0] != "App_Data"
            || parts.Any(p => p.Length == 0 || p is "." or ".." || p.Contains(':')))
        {
            throw new ArgumentException($"Not a valid file key: '{key}'.", nameof(key));
        }

        return string.Join('/', parts);
    }
}

/// <summary>Files under the app's content root. Used when no Storage is configured (local development).</summary>
public sealed class DiskFileStore(string contentRoot) : IFileStore
{
    public async Task SaveAsync(string key, byte[] bytes)
    {
        var path = PathFor(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, bytes);
    }

    public Task<Stream?> OpenReadAsync(string key)
    {
        var path = PathFor(key);
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }

    private string PathFor(string key) => Path.Combine(contentRoot, FileKey.Normalize(key).Replace('/', Path.DirectorySeparatorChar));
}

/// <summary>
/// A private Supabase Storage bucket, over its REST API with the service key. Nothing is ever linked
/// publicly: downloads are streamed through the app after its own authorisation checks.
/// </summary>
public sealed class SupabaseFileStore : IFileStore
{
    private readonly HttpClient _http;
    private readonly string _bucket;

    public SupabaseFileStore(HttpClient http, string url, string serviceKey, string bucket)
    {
        _http = http;
        _bucket = bucket;
        _http.BaseAddress = new Uri(url.TrimEnd('/') + "/");
        _http.DefaultRequestHeaders.Add("apikey", serviceKey);
        // The legacy service_role key is a JWT and is also sent as the bearer. The newer sb_secret_... keys are not
        // JWTs, and Storage rejects them as a bearer ("Invalid Compact JWS"), so for those the apikey header alone does it.
        if (serviceKey.StartsWith("eyJ", StringComparison.Ordinal) && serviceKey.Count(c => c == '.') == 2)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceKey);
        }
    }

    public async Task SaveAsync(string key, byte[] bytes)
    {
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var request = new HttpRequestMessage(HttpMethod.Put, ObjectUrl(key)) { Content = content };
        request.Headers.Add("x-upsert", "true");
        using var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Stream?> OpenReadAsync(string key)
    {
        // Not disposed here on purpose: disposing the response would cut the body the caller is about to
        // stream. Disposing the returned stream releases the connection.
        var response = await _http.GetAsync(ObjectUrl(key), HttpCompletionOption.ResponseHeadersRead);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            response.Dispose(); // Supabase answers a missing object with 400 or 404 depending on version
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    private string ObjectUrl(string key) =>
        "storage/v1/object/" + Uri.EscapeDataString(_bucket) + "/"
        + string.Join('/', FileKey.Normalize(key).Split('/').Select(Uri.EscapeDataString));
}
