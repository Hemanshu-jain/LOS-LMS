using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

/// <summary>Digio webhook trust (HMAC checksum) and the only path that may mark an agreement Signed.</summary>
public sealed class DigioTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<LosDbContext> _options;

    public DigioTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection).Options;
        using var db = Anonymous();
        db.Database.EnsureCreated();
        db.Companies.Add(new Company { Id = 2, Name = "Other" });
        db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
        db.SaveChanges();
        db.Disbursements.Add(new Disbursement { ApplicationId = "A1", AgreementEsignStatus = "Sent", EsignDocumentId = "DID123" });
        db.SaveChanges();
    }

    [Fact]
    public void Checksum_AcceptsDigioHmac_RejectsAnythingElse()
    {
        const string body = "{\"event\":\"doc.signed\"}";
        var hex = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("s3cret"), Encoding.UTF8.GetBytes(body)));

        Assert.True(DigioClient.ChecksumMatches("s3cret", body, hex.ToLowerInvariant()));
        Assert.True(DigioClient.ChecksumMatches("s3cret", body, hex)); // case-insensitive hex
        Assert.False(DigioClient.ChecksumMatches("wrong", body, hex));
        Assert.False(DigioClient.ChecksumMatches("s3cret", body + " ", hex)); // tampered body
        Assert.False(DigioClient.ChecksumMatches("s3cret", body, null));
    }

    [Fact]
    public async Task Webhook_DocSigned_MarksSignedOnce()
    {
        var signed = Payload("DOC.SIGNED", "DID123");

        await using (var db = Anonymous()) { Assert.True(await DigioClient.ApplyWebhookAsync(db, 1, signed)); }
        await using (var db = Anonymous()) { Assert.False(await DigioClient.ApplyWebhookAsync(db, 1, signed)); } // redelivery
        await using (var db = Anonymous())
        {
            Assert.False(await DigioClient.ApplyWebhookAsync(db, 1, Payload("doc.sign.failed", "DID123"))); // never un-signs
            Assert.Equal("Signed", db.Disbursements.IgnoreQueryFilters().Single().AgreementEsignStatus);
        }
    }

    [Fact]
    public async Task Webhook_ForAnotherCompany_OrUnknownDoc_ChangesNothing()
    {
        await using var db = Anonymous();
        Assert.False(await DigioClient.ApplyWebhookAsync(db, 2, Payload("doc.signed", "DID123")));
        Assert.False(await DigioClient.ApplyWebhookAsync(db, 1, Payload("doc.signed", "NOPE")));
        Assert.False(await DigioClient.ApplyWebhookAsync(db, 1, "{\"event\":\"kyc.something\"}"));
        Assert.Equal("Sent", db.Disbursements.IgnoreQueryFilters().Single().AgreementEsignStatus);
    }

    private static string Payload(string evt, string docId) =>
        $"{{\"id\":\"wh1\",\"event\":\"{evt}\",\"entities\":[\"document\"],\"payload\":{{\"document\":{{\"id\":\"{docId}\",\"agreement_status\":\"completed\"}}}}}}";

    /// <summary>A webhook has no user — exactly how the real endpoint runs.</summary>
    private LosDbContext Anonymous()
    {
        var tenant = new TenantContext(new NoUser(), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    public void Dispose() => _connection.Dispose();

    private sealed class NoUser : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
