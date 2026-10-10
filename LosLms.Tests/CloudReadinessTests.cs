using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LosLms.Data;
using LosLms.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace LosLms.Tests;

/// <summary>The pieces that let the backend run as a stateless container: file store, DB-backed vault, cloud config.</summary>
public sealed class CloudReadinessTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cloud-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    // ---- file store -------------------------------------------------------------------------------

    [Fact]
    public async Task DiskStore_RoundTrips_AndTreatsBackslashKeysAsTheSameFile()
    {
        var store = new DiskFileStore(_dir);
        await store.SaveAsync(@"App_Data\uploads\LN-1\security\a.pdf", new byte[] { 1, 2, 3 });

        Assert.Equal(new byte[] { 1, 2, 3 }, await store.ReadAllBytesAsync("App_Data/uploads/LN-1/security/a.pdf")); // old Windows row, new key
        Assert.Null(await store.OpenReadAsync("App_Data/uploads/LN-1/security/missing.pdf"));
    }

    [Theory]
    [InlineData("App_Data/uploads/../../secret.txt")]
    [InlineData("App_Data/uploads//x.pdf")]
    [InlineData("other/uploads/x.pdf")]
    [InlineData("/etc/passwd")]
    [InlineData("App_Data/uploads/C:/x.pdf")]
    public async Task Keys_ThatEscapeAppData_AreRefused(string key)
    {
        var store = new DiskFileStore(_dir);
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync(key, new byte[1]));
        await Assert.ThrowsAsync<ArgumentException>(() => store.OpenReadAsync(key));
    }

    [Fact]
    public async Task SupabaseStore_SpeaksTheStorageRestApi_WithTheServiceKey()
    {
        var handler = new RecordingHandler();
        var store = new SupabaseFileStore(new HttpClient(handler), "https://proj.supabase.co/", "svc-key", "loan files");

        await store.SaveAsync("App_Data/uploads/LN-1/rcu/report 1.pdf", new byte[] { 9, 9 });
        var put = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, put.Method);
        Assert.Equal("https://proj.supabase.co/storage/v1/object/loan%20files/App_Data/uploads/LN-1/rcu/report%201.pdf", put.Uri);
        Assert.Equal("svc-key", put.Headers["apikey"]);
        Assert.Equal("Bearer svc-key", put.Headers["Authorization"]);
        Assert.Equal("true", put.Headers["x-upsert"]);

        handler.Reply = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 7 }) };
        Assert.Equal(new byte[] { 7 }, await store.ReadAllBytesAsync("App_Data/uploads/LN-1/rcu/report 1.pdf"));
        Assert.Equal(HttpMethod.Get, handler.Requests[^1].Method);

        handler.Reply = new HttpResponseMessage(HttpStatusCode.NotFound);
        Assert.Null(await store.OpenReadAsync("App_Data/uploads/LN-1/rcu/none.pdf"));
        handler.Reply = new HttpResponseMessage(HttpStatusCode.BadRequest); // what Supabase sends for a missing object
        Assert.Null(await store.OpenReadAsync("App_Data/uploads/LN-1/rcu/none.pdf"));

        handler.Reply = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await Assert.ThrowsAsync<HttpRequestException>(() => store.OpenReadAsync("App_Data/uploads/LN-1/rcu/x.pdf"));
    }

    // ---- credential vault -------------------------------------------------------------------------

    [Fact]
    public void Vault_StoresInTheDatabase_UnderTheConfiguredKey()
    {
        var options = NewDatabase();
        var vault = NewVault(options, "a-long-secret-from-the-environment");

        vault.Store("u1", "P@ssw0rd!");
        Assert.True(vault.Has("u1"));
        Assert.Equal("P@ssw0rd!", vault.TryReveal("u1"));
        Assert.Null(vault.TryReveal("nobody"));

        vault.Store("u1", "changed"); // replace, not duplicate
        Assert.Equal("changed", NewVault(options, "a-long-secret-from-the-environment").TryReveal("u1")); // survives a "restart"

        Assert.Null(NewVault(options, "a-different-key").TryReveal("u1")); // a database dump without the key reveals nothing
    }

    [Fact]
    public void Vault_ImportsTheOldFileVault_Once()
    {
        var options = NewDatabase();
        var oldKey = RandomNumberGenerator.GetBytes(32);
        Directory.CreateDirectory(_dir);
        File.WriteAllBytes(Path.Combine(_dir, "vault.key"), oldKey);
        File.WriteAllText(Path.Combine(_dir, "credential-vault.dat"),
            JsonSerializer.Serialize(new Dictionary<string, string> { ["u7"] = OldEncrypt(oldKey, "legacy-pass"), ["bad"] = "AAAA" }));

        var vault = NewVault(options, "new-key");
        Assert.Equal(1, vault.ImportLegacyFiles(_dir)); // the entry the old key can't open is skipped
        Assert.Equal("legacy-pass", vault.TryReveal("u7"));
        Assert.Equal(0, vault.ImportLegacyFiles(_dir)); // running it again changes nothing
    }

    // ---- connection string and proxy --------------------------------------------------------------

    [Fact]
    public void ConnectionString_IsTrimmed_AndCloudModeForcesTls()
    {
        const string cs = "Server=db.example.com;Port=12345;Database=los;User Id=u;Password=p;";

        Assert.Equal(cs, CloudConfig.ConnectionString(Config(("ConnectionStrings:LosDb", cs + " \n"))));  // off: only trimmed

        var required = CloudConfig.ConnectionString(Config(("ConnectionStrings:LosDb", cs), ("Hosting:Cloud", "true")))!;
        Assert.Contains("SSL Mode=Required", required, StringComparison.OrdinalIgnoreCase);

        var kept = CloudConfig.ConnectionString(Config(("ConnectionStrings:LosDb", cs + "SslMode=None;"), ("Hosting:Cloud", "true")))!;
        Assert.DoesNotContain("Required", kept); // an explicit choice in the string wins

        var verified = CloudConfig.ConnectionString(Config(
            ("ConnectionStrings:LosDb", cs), ("Hosting:Cloud", "true"),
            ("Database:SslCaPem", "-----BEGIN CERTIFICATE-----\\nABC\\n-----END CERTIFICATE-----")))!;
        Assert.Contains("SSL Mode=VerifyCA", verified, StringComparison.OrdinalIgnoreCase);
        var caPath = verified.Split(';').Single(p => p.StartsWith("CA Certificate File=", StringComparison.OrdinalIgnoreCase)
            || p.StartsWith("SSL CA=", StringComparison.OrdinalIgnoreCase)).Split('=', 2)[1];
        Assert.Equal("-----BEGIN CERTIFICATE-----\nABC\n-----END CERTIFICATE-----", File.ReadAllText(caPath));
    }

    [Fact]
    public void Proxy_LimitTurnsOnForwardedFor_OtherwiseSchemeOnly()
    {
        var tunnel = new ForwardedHeadersOptions();
        CloudConfig.ConfigureProxy(tunnel, null);
        Assert.Equal(ForwardedHeaders.XForwardedProto, tunnel.ForwardedHeaders);

        var render = new ForwardedHeadersOptions();
        CloudConfig.ConfigureProxy(render, 1);
        Assert.Equal(ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor, render.ForwardedHeaders);
        Assert.Equal(1, render.ForwardLimit);
        Assert.Empty(render.KnownProxies);
    }

    // ---- helpers ----------------------------------------------------------------------------------

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))).Build();

    private DbContextOptions<LosDbContext> NewDatabase()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection).Options;
        using var db = new LosDbContext(options, TenantContext.ForSeeding());
        db.Database.EnsureCreated();
        return options;
    }

    private PasswordVault NewVault(DbContextOptions<LosDbContext> options, string key) =>
        new(options, Config(("Vault:Key", key + " ")), new StubEnvironment(_dir));

    private static string OldEncrypt(byte[] key, string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    private sealed class StubEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Test";
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public sealed record Seen(HttpMethod Method, string Uri, Dictionary<string, string> Headers);

        public List<Seen> Requests { get; } = new();
        public HttpResponseMessage Reply { get; set; } = new(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
            Requests.Add(new Seen(request.Method, request.RequestUri!.AbsoluteUri, headers));
            return Task.FromResult(Reply);
        }
    }
}
