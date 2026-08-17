# LOS/LMS — deployment & updates

How to build the Windows release, run it on a branch server, and ship updates.

---

## What the build ships as

A **blank slate that installs by double-click**. No database to install, no connection string to
configure, no demo data, no seeded places or names — only sensible, editable policy-threshold
defaults. The whole app runs on a single embedded **SQLite** database file created automatically on
first run, so there is nothing to set up.

On first run the app **forces Company Setup**: until an Admin enters a real company name and at least
one branch, every company user is redirected to Company Setup and nothing else is reachable. A short
walkthrough guides the first setup.

Two accounts are seeded so you can get in:

| Account | Email | Role |
|---|---|---|
| Administrator | `admin@loslms.local` | Admin (does Company Setup, manages staff) |
| Platform SuperAdmin | `superadmin@loslms.local` | SuperAdmin (manages System Updates) |

Both get a **generated temporary password on first start**, shown in two places: the console window,
and a `FIRST-RUN-LOGIN.txt` file written next to the app. Both must be changed at first sign-in.
Create your real staff from Company Setup → User Management, then deactivate the seeded Administrator
and delete `FIRST-RUN-LOGIN.txt`.

### Why SQLite (and what it means for multiple branches)

Every branch device connects over the network to **one** server machine through a browser; only that
server ever touches the database. Live, no-delay updates between users come from the server pushing
changes over its real-time connection, not from the database — so an embedded SQLite file behind that
one server fully supports many branches and devices at branch scale. WAL mode is enabled so many users
read while one writes without lock stalls. The only thing SQLite cannot do is let *several separate
server machines* share one database, but this app is single-server by design regardless (see Notes).

---

## 1. Build the release

From the repo root, in PowerShell:

```powershell
.\publish.ps1
```

This produces, under `.\publish\`, **two zips for two different jobs**:

```
publish\
  app\                                    the main server (self-contained, single-file LosLms.exe)
  LOS-LMS.exe                             the launcher the client double-clicks

  los-lms-v1.0.0-SETUP-win-x64.zip        >>> SEND THIS TO THE CLIENT for a first install <<<
                                          (contains LOS-LMS.exe + app\ together)

  los-lms-v1.0.0-win-x64.zip              the UPDATE artifact — attach to a GitHub Release (§4).
                                          App only, no launcher (a running exe can't replace itself).
```

- **First install → give the client `los-lms-v1.0.0-SETUP-win-x64.zip`.** They unzip it and
  double-click `LOS-LMS.exe`. That's the whole install.
- **Every later update → the `los-lms-vX.Y.Z-win-x64.zip`** goes on a GitHub Release; the client never
  hand-installs it — the in-app updater pulls it (§4).

The exe is fully self-contained: the target machine needs **no .NET install** and **no database**.

---

## 2. Configure — nothing required

There is nothing a client has to configure. Two optional settings live in `app\appsettings.json`:

- `Urls` — defaults to `http://0.0.0.0:5037` (all network interfaces, so LAN clients can connect).
  Change the port here only if 5037 is taken.
- `Updates:GitHubOwner` / `Updates:GitHubRepo` — the public repo the update check reads
  (defaults to `Hemanshu-jain` / `LOS-LMS`).

The database is an SQLite file at `app\App_Data\los_lms.db`, created and migrated automatically. It
lives under `App_Data` on purpose: the updater preserves that folder, so client data survives every
update.

---

## 3. Run it — double-click `LOS-LMS.exe`

Unzip the SETUP bundle anywhere and double-click **`LOS-LMS.exe`**. It:

1. starts the server (creating and migrating the SQLite database on first run),
2. opens the default browser to `http://localhost:5037`,
3. keeps the server running, and is the only thing that can apply an update.

`LOS-LMS.exe` is the launcher/watchdog — a running Windows exe cannot overwrite itself, so it runs the
server as a child and does the swap for it. Launch this, **not** `app\LosLms.exe` directly, or the
in-app "Apply now" will have nothing to apply.

The console window shows the address to hand out to branch staff:

```
LOS/LMS is running. Branch staff connect to:  http://192.168.1.24:5037  (this machine's LAN address).
```

Branch staff open that URL in a browser. The server machine's firewall must allow inbound TCP on the
port (5037 by default).

First sign-in: use `admin@loslms.local` with the temporary password from the console (or
`FIRST-RUN-LOGIN.txt`), change it when prompted, then complete Company Setup (name + at least one
branch) to unlock the app.

---

## 4. Shipping an update

1. **Bump the version** in `LosLms\LosLms.csproj` (`<Version>1.0.1</Version>`).
2. `.\publish.ps1` → produces `publish\los-lms-v1.0.1-win-x64.zip`.
3. **Cut a GitHub Release** on the configured repo:
   - Tag: `v1.0.1` (the leading `v` is fine; it's compared as a version).
   - Title + description: the description becomes the changelog shown in-app — write it for the operator.
   - **Attach `los-lms-v1.0.1-win-x64.zip`** as a release asset (the updater downloads the `.zip` asset).
   - Publish the release (not a draft).
4. On the running server, sign in as the **SuperAdmin** → **System Updates**:
   - It checks automatically (and via "Check now"), showing current vs latest and the changelog.
   - **Download update** stages the zip.
   - **Apply now** (with an explicit confirmation) signals the launcher, which stops the server, swaps
     in the new build, and restarts it. Staff are briefly disconnected; they just reload.

The SQLite database (`App_Data`) and any uploaded documents (also under `App_Data`) are **preserved**
across the swap. Schema changes in the new build apply automatically via EF Core migrations on restart.

### If an update fails

The launcher **backs up the current install before swapping** and, if anything goes wrong (bad zip,
the new build won't start, …), **restores the previous version and restarts it** — the install is
never left half-swapped. Watch the console; a failed apply logs the reason and "Rollback complete."
In the worst case (rollback itself fails) it preserves the old install at `publish\_backup` and tells
you to rename it back to `app`.

---

## Notes

- **Migrations** apply automatically on every start (`Database.Migrate()` against the SQLite file), so
  a new build's schema changes take effect the moment the launcher restarts it — no manual step.
- **Single server instance only.** The Admin Inbox's real-time delivery is in-process; running two
  instances against one database needs a SignalR backplane (logged at startup). SQLite matches this
  single-server design.
- **HTTP, not HTTPS.** Intended for an internal LAN. Put it behind a reverse proxy if you need TLS.
