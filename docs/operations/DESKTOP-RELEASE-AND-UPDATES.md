# Desktop edition: building, installing and updating

LOS/LMS runs in two ways. This document is about the **Windows desktop edition**: one office PC hosts the system and
branch staff reach it over the internet at that client's own address. The cloud edition (Render) is described in
[`CLOUD-DEPLOY.md`](CLOUD-DEPLOY.md). Per-client provisioning and licences are in
[`NEW-CLIENT-ONBOARDING.md`](NEW-CLIENT-ONBOARDING.md).

| | Desktop edition | Cloud edition |
|---|---|---|
| Hosted on | One Windows PC at the client | Render (Docker) |
| Database | MySQL 8, bundled with the package and started by the launcher | Aiven MySQL |
| Files | `app\backend\App_Data\` on that PC | Supabase Storage (private bucket) |
| Public address | The client's own Cloudflare named tunnel (e.g. `https://abc.bhodhix.com`) | Render custom domain |
| How a new version arrives | Signed update zip from a GitHub Release, applied by the launcher | A new deploy of `main` |

---

## 1. What a package contains

Extracting the package gives exactly two things:

```
LOS-LMS.exe        the launcher / watchdog: the only thing anyone double-clicks (single file, self-contained)
app\
  backend\         the web application (LosLms.exe), swapped wholesale on every update
  mysql\           bundled portable MySQL 8 (+ the Visual C++ runtime DLLs it needs on a clean PC)
  mysql-data\      the database files, created on the first run and kept forever
  backups\         nightly gzipped database dumps (the latest 14 are kept)
  cloudflared.exe  the tunnel client
  server-config.example.json   optional operator settings (see "Operator settings")
  READ ME FIRST.txt            the short instructions the client sees
  LOS-LMS.log                  the record of what happened (the launcher's windows are hidden)
```

Nothing has to be installed on the PC: no .NET, no database, no VC++ redistributable.

---

## 2. Building a package

Run from the repository root on a **Windows** machine with the **.NET 8 SDK**, **openssl** on the PATH and the
Visual C++ Redistributable (x64) installed. The first run downloads and caches the bundled MySQL and `cloudflared`
under `.tools\` (`tools\fetch-runtime-deps.ps1`).

```powershell
.\publish.ps1                                   # the default client
.\publish.ps1 -Subdomain abc.bhodhix.com -TunnelTokenFile tunnel-tokens\abc.txt `
              -LicenseFile clients\abc.license -Label abc -SkipUpdateArtifact   # one specific client
```

The script reads **vendor secrets from files at the repository root. All of them are git-ignored and must never be
committed** (the repository is the public source of the app, and the update artifact is public):

| File | Purpose | If missing |
|---|---|---|
| `tunnel-token.txt` (or the file named by `-TunnelTokenFile`) | The client's Cloudflare tunnel token, baked into the launcher | The host runs on the local network only |
| `license.txt` (or `-LicenseFile`) | The client's signed subscription licence, baked into the backend | Unlicensed build: enforcement is off (fine for testing) |
| `master-key.txt` | The vendor break-glass SuperAdmin password (see `master-key.example.txt`) | No master account is created |
| `update-signing-private.pem` | Signs the update artifact | The build **fails**: clients reject unsigned updates |

Outputs, under `publish\`:

| File | Give it to | Notes |
|---|---|---|
| `LOS-LMS-[<label>-]v<version>-win-x64.zip` | The client (first install) | The whole package of section 1 |
| `LOS-LMS-Update-v<version>-win-x64.zip` and `…zip.sig` | GitHub Release (section 4) | Backend only, built **without** the master password, signed. Skipped by `-SkipUpdateArtifact` |

---

## 3. First install and day-to-day

1. The client extracts the zip anywhere and double-clicks `LOS-LMS.exe`.
2. First run asks one question: **"Set up the SERVER on this computer"** (on the single host PC) or **"Connect to the
   server"** (on every staff computer; it just opens the client's address). The choice is remembered per computer.
3. The server starts the bundled MySQL (bound to `127.0.0.1`, generated credentials), the backend, and the tunnel. Staff
   reach the system at the client's permanent address; the address is also written to `app\address.txt`.
4. The first browser visit shows **Create your administrator account** (name, email, a password of 10+ characters with a
   symbol). That person then adds staff under **Company Setup → User Management** and completes Company Setup (company
   name and at least one branch), which unlocks the rest of the app.

Launch `LOS-LMS.exe`, never `app\backend\LosLms.exe` directly: a running Windows program cannot replace itself, so the
launcher is what stops the backend, swaps in a new version and restarts it.

### Operator settings (optional)

Copy `app\server-config.example.json` to `app\server-config.json` to change:
the hosted URL, a tunnel token (if the build had none baked in), and the optional `BackupFtp*` fields that upload each
nightly backup to an FTP server so a copy survives the loss of the PC. Left blank, backups stay on the PC only.

### Where the data lives (back this up)

`app\mysql-data\` (database), `app\backups\` (dumps), `app\backend\App_Data\` (uploaded documents, the credential
vault key, the licence). The updater preserves all of them. Keep an offsite copy.

---

## 4. Shipping an update

1. **Bump the version in both places** and keep them equal: `<Version>` in `src/LosLms.Server/LosLms.Server.csproj`
   (read by `publish.ps1`) and in `src/LosLms/LosLms.csproj` (the version the running app compares with the release).
2. Add the release notes to [`CHANGELOG.md`](../../CHANGELOG.md).
3. Run `.\publish.ps1` (no `-SkipUpdateArtifact`) to produce the update zip and its `.sig`.
4. Create a **GitHub Release** (published, not a draft) on the configured repository (`Updates:GitHubOwner` /
   `Updates:GitHubRepo` in `appsettings.json`):
   - tag `vX.Y.Z`;
   - description = the changelog entry (the app shows it to the operator);
   - attach **both** `LOS-LMS-Update-vX.Y.Z-win-x64.zip` and its `.sig`.
5. Installs pick it up on their own: each checks GitHub Releases on startup and every 6 hours, downloads the zip,
   **verifies the signature against the vendor public key baked into the app**, and asks the launcher to apply it. To
   make installs notify instead of applying, set `Updates:AutoApply` to `false`; a SuperAdmin can then use
   **Settings → Usage & Updates → System updates** to check, download and apply by hand.

An update never touches `mysql-data` or `backups` (they sit outside the backend folder), and the swap copies the
backend's `App_Data` and `appsettings.json` across. New database migrations run automatically when the new backend starts.

### Safety nets

- The launcher takes a **database dump before swapping**, because a file rollback cannot undo a schema migration. If that
  backup cannot be taken, **the update is not applied** and the current version keeps running (the next 6-hourly check retries).
- It moves the current backend to `_backup\`, applies the new one, and **restores the old version and restarts it** if
  anything fails (bad zip, a build that will not start). A failed attempt is recorded in `LOS-LMS.log`.
- An unsigned or tampered update zip is refused before it is staged.

### Constraint: updates are read from GitHub Releases without a login

The update check calls the public GitHub API for the configured repository and sends no credentials. **If that
repository is made private, installed desktop copies stop seeing new releases** (the app reports "No published release
found … the repo may be private"). They keep running, but updates must then be delivered another way. The cloud edition
is not affected.

---

## 5. Notes

- **One server instance.** Real-time delivery to the Admin Inbox is in-process; two instances on one database would
  need a SignalR backplane (logged at startup).
- **Maintenance mode.** A SuperAdmin can put the app in maintenance mode from System updates; everyone else then sees
  the maintenance page while they work.
- **A licence that expires** freezes the app to a renewal screen with data untouched. Renewing is described in
  [`NEW-CLIENT-ONBOARDING.md`](NEW-CLIENT-ONBOARDING.md).
