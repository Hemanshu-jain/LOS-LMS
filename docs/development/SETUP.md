# Developer setup

How to run LOS/LMS on your own machine, run the tests, and add a database migration.

## What you need

| Tool | Why | Check |
|---|---|---|
| **.NET 8 SDK** (8.0.x) | build and run | `dotnet --list-sdks` |
| **MySQL 8.0** reachable from your machine | the application database | `mysql --version` |
| **Git** | source control | `git --version` |
| A modern browser (Chrome is what it is built and tested against) | the UI | |
| **Windows** (only if you build the desktop launcher) | `LosLms.Server` and `LosLms.Shell` use WinForms/WebView2 | |

The web application and the tests build and run on Windows, Linux and macOS. Only the two desktop projects need Windows
(they are why the CI job runs on `windows-latest`).

## 1. Get the code

```bash
git clone https://github.com/Hemanshu-jain/LOS-LMS.git
cd LOS-LMS
```

## 2. Start a MySQL database

If you already run MySQL on `localhost:3306`, create a database and a user and skip ahead. Otherwise, with Docker:

```bash
docker run --name los-lms-mysql -e MYSQL_ROOT_PASSWORD=root -e MYSQL_DATABASE=los_lms -p 3306:3306 -d mysql:8.0
docker exec -it los-lms-mysql mysql -uroot -proot -e "CREATE USER 'los'@'%' IDENTIFIED BY 'los'; GRANT ALL ON los_lms.* TO 'los'@'%'; FLUSH PRIVILEGES;"
```

## 3. Give the app its connection string

`src/LosLms/appsettings.json` ships a **blank** `ConnectionStrings:LosDb` on purpose. **Never put real credentials in a
committed file.** Use the .NET user-secrets store (already initialised for the project):

```bash
cd src/LosLms
dotnet user-secrets set "ConnectionStrings:LosDb" "Server=localhost;Port=3306;Database=los_lms;User Id=los;Password=los;"
```

An environment variable also works: `ConnectionStrings__LosDb=...`. Other settings you may want (all are optional and
documented in the file they live in): `PublicBaseUrl`, `Smtp:*` for email, `Gemini:ApiKey`, `ApiBilling:*`,
`Security:Turnstile:*`. The full list for the cloud edition is in
[`../operations/CLOUD-DEPLOY.md`](../operations/CLOUD-DEPLOY.md).

## 4. Run it

```bash
cd src/LosLms
dotnet run --launch-profile https
```

Open <https://localhost:7095/applications> (the HTTP address is `http://localhost:5037`). The first time, trust the
development certificate: `dotnet dev-certs https --trust`.

On start the app **applies any pending database migrations itself** and seeds the roles. It does not need
`dotnet ef database update`. A fresh database shows a **Create your administrator account** screen on the first visit.

To also load the worked-through demo applications (development only), start with demo data switched on:

```bash
dotnet run --launch-profile https --Seed:DemoApplications=true
```

The app starts even with no database running (it fails closed: pages that need data show an error rather than inventing
anything).

## 5. Run the tests

```bash
dotnet test tests/LosLms.Tests
```

The suite runs against in-memory SQLite and a fake Digio, so it needs **no MySQL and no network**. To build the whole
solution (Windows only, because of the desktop projects): `dotnet build LosLms.sln -c Release`.

## 6. Add a database migration

The schema is managed with EF Core. MySQL is the target, so every string property needs an explicit `[MaxLength]`
(otherwise Pomelo maps it to `LONGTEXT`, which MySQL cannot index). Prefer **additive** migrations; production is live.

```bash
dotnet tool install --global dotnet-ef      # once per machine
cd src/LosLms
dotnet ef migrations add <DescriptiveName>
```

Review the generated file, run the app to apply it, and commit the migration together with the model change.

## If you moved here from the old folder layout

The projects used to sit at the repository root (`LosLms/`, `LosLms.Tests/`, …) and now live in `src/` and `tests/`.
Git moves the tracked files for you, but **git ignores your local runtime files, so they stay in the old folder**:
if you ran the app from source before, copy `LosLms/App_Data/` (uploads, `vault.key`, `license.json`) into
`src/LosLms/App_Data/` and then delete the leftover `LosLms/` folder. Delete any stray `bin/` and `obj/` folders too.

## Common problems

| Symptom | Fix |
|---|---|
| Browser warns about the certificate | `dotnet dev-certs https --trust` |
| Every page that needs data errors | MySQL is down or the connection string is wrong: `dotnet user-secrets list` |
| Page shows unstyled HTML after a change | Hard-reload (Ctrl+Shift+R); the browser cached old CSS |
| `dotnet ef` not found | `dotnet tool install --global dotnet-ef` |
| A second PDF download silently does nothing | Chrome blocks repeated automatic downloads: allow them for the site |
| `HTTP Failed to determine the https port for redirect` | Harmless: you ran `dotnet run` without `--launch-profile https` |
