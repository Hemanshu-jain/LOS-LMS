# Cloud deploy (Render + Aiven MySQL + Supabase Storage)

The backend alone (`LosLms`) as a Docker container. No launcher, tunnel or updater: a new version is a redeploy.
The container's disk is wiped on every restart, so everything that must last lives outside it: MySQL for data,
the vault and the Data Protection keys; a private Storage bucket for files.

## Render service

| Setting | Value |
|---|---|
| Runtime | Docker (uses the `Dockerfile` at the repo root) |
| Region | Singapore |
| Plan | Free (about 512 MB RAM) |
| Health check path | `/health` |
| Custom domain | `los-lms.bhodhix.com` |
| Build arg (optional) | `COMMIT` (shown by `/health`; Render's own `RENDER_GIT_COMMIT` is used if it is not set) |

`/health` answers `{"status":"ok","version":"...","commit":"..."}` after a real `SELECT 1`, or `503 {"status":"db_down"}`.

## Environment variables (names only; set the values in Render)

| Name | Required | What it is |
|---|---|---|
| `ConnectionStrings__LosDb` | yes | MySQL connection string (Aiven host, port, database, user, password) |
| `Database__SslCaPem` | recommended | Aiven's CA certificate, the whole PEM text. With it the server certificate is verified (`VerifyCA`); without it TLS is still on (`Required`) but not verified |
| `Storage__Url` | yes | Supabase project URL, e.g. `https://xxxx.supabase.co` |
| `Storage__ServiceKey` | yes | Supabase service-role key |
| `Storage__Bucket` | yes | Name of a **private** bucket |
| `Vault__Key` | yes | Encrypts the stored passwords. A long random value; never change it once in use, or the stored passwords become unreadable |
| `Proxy__ForwardLimit` | yes | `1` on Render: trust the one `X-Forwarded-For` / `-Proto` entry Render adds |
| `PORT` | set by Render | The container listens on it (10000 if unset) |
| `ASPNETCORE_ENVIRONMENT` | in the image | `Web`: loads `appsettings.Web.json` (public web mode, base URL, demo tenant) |
| `Hosting__Cloud` | in the image | `true`: no update checker, TLS on the database, Data Protection keys in the database |
| `Hosting__Commit` | in the image | From the `COMMIT` build arg |
| `Security__Turnstile__SiteKey`, `Security__Turnstile__SecretKey` | optional | Bot check on sign-in |
| `Gemini__ApiKey` | optional | Signature detection |
| `Seed__DemoTenantEmail`, `Seed__DemoTenantPassword` | optional | Override the demo login in `appsettings.Web.json` |
| `Maintenance__Enabled` | optional | Start in maintenance mode |

Values are trimmed, so a stray newline pasted into the Render form is harmless.

## First deploy

1. Create the private bucket in Supabase and the MySQL database in Aiven (`utf8mb4`).
2. Set the variables above and deploy. Migrations run at boot (additive; this release adds the tables
   `CredentialVault` and `DataProtectionKeys`).
3. Moving from the desktop host: run `tools\migrate-to-cloud.ps1` (it does the database, the files and the vault below in
   one go, with checks; start with `-WhatIf`). By hand, if you prefer:
   - **Files:** copy the host's `app\backend\App_Data\uploads\` and `...\welcome-letters\` folders into the bucket,
     keeping the paths: object `App_Data/uploads/{application}/{folder}/{file}`. The database rows already store
     exactly those paths, so they resolve unchanged.
   - **Passwords vault:** copy `vault.key` and `credential-vault.dat` from the host's `App_Data` into a folder and run
     once, with the same environment variables set:
     `dotnet LosLms.dll --import-vault /path/to/that/folder`
     It re-encrypts each entry under `Vault__Key`, prints how many it imported, and exits. Entries the old key cannot
     open are skipped; running it again imports nothing twice.
4. Sign-ins from before the move stop working once (new Data Protection keys); users sign in again.

## Not in the cloud build

- Self-update (`Hosting__Cloud=true` removes the checker). Redeploy instead.
- The licence file (`App_Data/license.json`): the web build runs unlicensed.
- Several instances: the Admin Inbox's live updates are in-process. Keep it at one instance.
