# Onboarding a client + the subscription (licence) system

Every client is a fully separate island — **own subdomain, own tunnel, own build, own machine, own
database, own signed licence.** Nothing is shared. The licence controls the subscription: it lasts a
fixed term, warns before expiry, and **freezes the app** once expired until renewed.

## One-time vendor setup

- **`license-signing-private.pem`** (repo root, gitignored) — the licence signing key. Its public half
  is baked into the app (`LicenseKeys.cs`). Back this up safely; it signs every client's licence.
- **`cloudflare-config.json`** (copy from `cloudflare-config.example.json`, gitignored) — Cloudflare
  `AccountId`, `ZoneId`, and an `ApiToken` with **Account → Cloudflare Tunnel: Edit** and **Zone
  (bhodhix.com) → DNS: Edit**. Used by the tool to create tunnels + DNS automatically.

The tool: `dotnet run --project LosLms.LicenseTool -- <command>` (build it once; it's vendor-only, never shipped).

---

## Onboard a new client

**1. Provision (one command)** — creates their tunnel + DNS + signed licence, records them:
```powershell
dotnet run --project LosLms.LicenseTool -- new-client "ABC Finance" abc 12
```
This creates the Cloudflare tunnel `LOS-LMS-abc`, points `abc.bhodhix.com → http://localhost:5037`,
signs a 12-month licence, writes `tunnel-tokens\abc.txt` + `clients\abc.license`, and adds a row to
`clients-registry.json`. It prints the exact build command:

**2. Build their package:**
```powershell
.\publish.ps1 -Subdomain abc.bhodhix.com -TunnelTokenFile tunnel-tokens\abc.txt -LicenseFile clients\abc.license -Label abc -SkipUpdateArtifact
```
→ `publish\LOS-LMS-abc-v<version>-win-x64.zip` with **their** URL, tunnel token, and licence all baked in.

**3. Deliver:** they extract (`LOS-LMS.exe` + `app\`), run it → **"Set up the SERVER"** → `abc.bhodhix.com`
goes live automatically, licensed. Staff run it → **"Connect."** No pasting, no config.

## Renew a client (before or after expiry)

```powershell
dotnet run --project LosLms.LicenseTool -- renew abc 12
```
Prints a **renewal key**. Send it to the client; their **SuperAdmin** pastes it into the app's
**"Subscription paused / Renew"** screen. The app verifies it (bound to their host, must extend) and
resumes. No rebuild needed. `clients\abc.license` is also updated for their next full build.

## See everyone
```powershell
dotnet run --project LosLms.LicenseTool -- list
```
Every client, host, expiry, and status (active / Nd left ⚠ / EXPIRED).

---

## How enforcement behaves
- **> 14 days left:** normal.
- **≤ 14 days:** an amber "subscription ends in N days" banner (Admin/SuperAdmin).
- **Expired:** the whole app freezes to the renew screen — data untouched. Clock-rollback is defended
  (the app remembers the latest time it has seen, so setting the PC clock back doesn't extend it).
- A build with **no licence** (a plain `dotnet run`, no `-LicenseFile`) is *unlicensed* and never
  freezes — that's for development.

## The honest ceiling
Verification runs on the client's own machine, so a determined reverse-engineer could patch the binary.
Signed keys stop forgery, expiry-editing, and casual bypass — enough for contract-bound B2B financiers.
For a hard kill-switch later, add an online check (the app already reaches Cloudflare).

## For the existing default deployment (bhodhix)
Its tunnel already exists, so issue a licence without touching Cloudflare:
```powershell
dotnet run --project LosLms.LicenseTool -- issue "Bhodhix" los-lms.bhodhix.com <tunnel-id> 12
copy clients\los-lms.license license.txt   # the default -LicenseFile
.\publish.ps1                               # bakes tunnel-token.txt + license.txt
```
