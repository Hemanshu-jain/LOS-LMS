using System.Net;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// DownloadAsync staging behaviour: one download at a time, and nothing unverified (or half-written)
/// is ever left in the staging folder. The "good signature" path needs the vendor private key, which
/// CI does not have, so it is covered by UpdateSigningTests instead.
/// </summary>
public sealed class UpdateDownloadTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "upd-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public async Task BadSignature_IsRejected_AndNothingIsLeftBehind()
    {
        using var http = new HttpClient(new FakeHandler());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => UpdateService.DownloadAsync(http, "http://x/u.zip", "u.zip", _dir));

        Assert.Empty(Directory.GetFileSystemEntries(_dir));
    }

    [Fact]
    public async Task FailedDownload_LeavesNoFile()
    {
        using var http = new HttpClient(new FakeHandler { FailMidStream = true });

        await Assert.ThrowsAnyAsync<Exception>(
            () => UpdateService.DownloadAsync(http, "http://x/u.zip", "u.zip", _dir));

        Assert.Empty(Directory.GetFileSystemEntries(_dir));
    }

    [Fact]
    public async Task StaleLeftoverPartial_DoesNotBlockDownload()
    {
        Directory.CreateDirectory(_dir);
        var stale = Path.Combine(_dir, "u.zip.part");
        await using var held = new FileStream(stale, FileMode.Create, FileAccess.Write, FileShare.Read);
        held.Write(new byte[] { 1 });
        held.Dispose();
        using var http = new HttpClient(new FakeHandler());

        // Fails on the signature (expected) — what matters is it got past the stale partial and cleaned up.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => UpdateService.DownloadAsync(http, "http://x/u.zip", "u.zip", _dir));

        Assert.Empty(Directory.GetFileSystemEntries(_dir));
    }

    [Fact]
    public async Task SecondConcurrentCall_WaitsForTheFirst()
    {
        var gate = new TaskCompletionSource();
        var handler = new FakeHandler { Gate = gate.Task };
        using var http = new HttpClient(handler);

        var first = UpdateService.DownloadAsync(http, "http://x/u.zip", "u.zip", _dir);
        await handler.FirstRequestStarted.Task;
        var second = UpdateService.DownloadAsync(http, "http://x/u.zip", "u.zip", _dir);

        await Task.Delay(200);
        Assert.Equal(1, handler.ZipRequests); // second is parked on the lock, not downloading

        gate.SetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(() => first);
        await Assert.ThrowsAsync<InvalidOperationException>(() => second);
        Assert.Equal(2, handler.ZipRequests);
        Assert.Equal(1, handler.MaxInFlight);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        public bool FailMidStream { get; init; }
        public Task? Gate { get; init; }
        public TaskCompletionSource FirstRequestStarted { get; } = new();
        public int ZipRequests, MaxInFlight;
        private int _inFlight;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith(".sig"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[384]) };
            }

            var now = Interlocked.Increment(ref _inFlight);
            Interlocked.Increment(ref ZipRequests);
            InterlockedMax(ref MaxInFlight, now);
            FirstRequestStarted.TrySetResult();
            try
            {
                if (Gate is not null) await Gate;
                if (FailMidStream) throw new HttpRequestException("connection dropped");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) };
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }

        private static void InterlockedMax(ref int target, int value)
        {
            int cur;
            while (value > (cur = target) && Interlocked.CompareExchange(ref target, value, cur) != cur) { }
        }
    }
}
