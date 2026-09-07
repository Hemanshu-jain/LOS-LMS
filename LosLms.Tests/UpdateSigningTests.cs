using System.Diagnostics;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// Guards the update-integrity check: a real vendor signature verifies, a tampered file or a bogus
/// signature does not. The positive round-trip needs the vendor PRIVATE key (gitignored), so it runs
/// only where that key and openssl are present — locally on the vendor machine — and is skipped in CI.
/// The negative cases always run.
/// </summary>
public sealed class UpdateSigningTests
{
    [Fact]
    public void GarbageSignature_IsRejected()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "pretend update payload");
            Assert.False(UpdateSigning.VerifyFile(file, new byte[384])); // zeros are not a valid signature
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void VendorSignedFile_Verifies_AndTamperingIsRejected()
    {
        var keyPath = Path.Combine(RepoRoot(), "update-signing-private.pem");
        if (!File.Exists(keyPath) || !OpensslAvailable())
        {
            return; // vendor key / openssl not present (e.g. CI) — nothing to prove here
        }

        var file = Path.GetTempFileName();
        var sig = file + ".sig";
        try
        {
            File.WriteAllBytes(file, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            RunOpenssl($"dgst -sha256 -sign \"{keyPath}\" -out \"{sig}\" \"{file}\"");

            var signature = File.ReadAllBytes(sig);
            Assert.True(UpdateSigning.VerifyFile(file, signature), "A correctly signed file must verify.");

            // Flip a byte: the same signature must no longer match.
            var bytes = File.ReadAllBytes(file);
            bytes[0] ^= 0xFF;
            File.WriteAllBytes(file, bytes);
            Assert.False(UpdateSigning.VerifyFile(file, signature), "A tampered file must be rejected.");
        }
        finally
        {
            File.Delete(file);
            if (File.Exists(sig)) File.Delete(sig);
        }
    }

    private static string RepoRoot()
    {
        // tests run from LosLms.Tests/bin/<cfg>/net8.0 — the repo root is four levels up.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 5 && dir is not null; i++)
        {
            if (File.Exists(Path.Combine(dir.FullName, "LosLms.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory;
    }

    private static bool OpensslAvailable()
    {
        try
        {
            RunOpenssl("version");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void RunOpenssl(string args)
    {
        using var p = Process.Start(new ProcessStartInfo("openssl", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        })!;
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException($"openssl {args} failed: {p.StandardError.ReadToEnd()}");
        }
    }
}
