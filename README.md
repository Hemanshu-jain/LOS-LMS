# LOS / LMS — Loan Origination & Management System

Internal, staff-only Loan Origination / Management System for a multi-branch NBFC auto & vehicle financing operation. Used by loan officers and back-office staff — never exposed to the client's own borrowers.

> **Nine-screen, eight-stage lifecycle.** Applications move through Customer Details → Loan & Security → Bank & Financial → Document Checklist → Reports (RCU) → Eligibility → Approvals → Post Sanction, with Reject and Send Back as the only two reverse operations.

This repository contains the working Blazor Server application, all reference specifications (React mockups), seed data, migrations, and two end-to-end demo walkthroughs.

---

## Contents

| Path | What it is |
|---|---|
| `LosLms/` | The Blazor Server app — Components, Pages, Models, Data, Migrations |
| `LosLms.sln` | Solution file |
| `ApplicationsDashboard.spec.v2.jsx` | Reference spec for the dashboard |
| `CustomerDetails.spec.jsx` | Reference spec for Stage 1 |
| `DocumentChecklist.spec.v2.jsx` | Reference spec for Stage 4 |
| `Eligibility.spec.v2.jsx` | Reference spec for Stage 6 |
| `ReportsRCU.spec.v2.jsx` | Reference spec for Stage 5 |
| `OPEN-QUESTIONS-FOR-ARUN.md` | All unconfirmed assumptions the build rests on — read first |
| `WALKTHROUGH-STAGES-1-TO-5.md` | Original walkthrough (Sunil Wagh sample, application `LN-2026-004875`) |
| `DEMO-WALKTHROUGH-APPLICATION-LN-2026-004900.md` | **Demo 1** — Hemant Bhalerao, CV, three parties, exercises RCU override |
| `DEMO-WALKTHROUGH-APPLICATION-LN-2026-004901.md` | **Demo 2** — Priya Deshpande, LAP, single party, exercises deviation approver note |
| `Wireframe nine-screen flow_1.zip` | Original 9-screen wireframe archive |

---

## Tech stack

| Layer | Choice |
|---|---|
| Runtime | .NET 8.0 (LTS) |
| UI | Blazor Server — every component `InteractiveServer` (SignalR circuit) |
| ORM | Entity Framework Core 8.0.29 |
| Database | MySQL 8.0 via `Pomelo.EntityFrameworkCore.MySql` 8.0.3 |
| PDF generation | QuestPDF (Community licence — see OPEN-QUESTIONS §1.1) |

Staff must be online to use the app. There is no offline mode and no local sync.

> Blazor Server here means the .NET 8 **Blazor Web App** template with `--interactivity Server --all-interactive`. The old `blazorserver` template no longer ships in the .NET 8 SDK.

---

## Prerequisites

You will need:

1. **.NET 8 SDK** (8.0.x). Verify with `dotnet --list-sdks`. If you don't have it: <https://dotnet.microsoft.com/download/dotnet/8.0>
2. **MySQL 8.0** reachable from your machine. (Not strictly required just to start the app — see *Running without MySQL* below. Required to actually persist anything.)
3. **A modern browser.** Chrome is what the app was built and tested against. Firefox and Edge should work.
4. **Git** to clone this repo.

### Trust the HTTPS dev certificate (one time, on Windows)

```bash
dotnet dev-certs https --trust
```

A Windows prompt will appear asking you to confirm. Skip this and the browser will warn on every HTTPS load.

### Install the EF Core CLI tool (one time, per machine)

```bash
dotnet tool install --global dotnet-ef
```

This is only needed if you want to add new migrations. To just build and run the app, you don't need it.

---

## Running the app — step by step

### 1. Clone the repository

```bash
git clone https://github.com/Hemanshu-jain/LOS-LMS.git
cd LOS-LMS
```

### 2. Start MySQL

If MySQL is already running on `localhost:3306`, skip this step.

If you have MySQL installed but not running:

```bash
# Windows (PowerShell, admin)
net start MySQL80
```

If you don't have MySQL installed, the simplest path is the official Docker image:

```bash
docker run --name los-lms-mysql -e MYSQL_ROOT_PASSWORD=root -e MYSQL_DATABASE=los_lms -p 3306:3306 -d mysql:8.0
```

Then create a non-superuser for the app (optional but recommended):

```bash
docker exec -it los-lms-mysql mysql -uroot -proot -e "CREATE USER 'los'@'%' IDENTIFIED BY 'los'; GRANT ALL ON los_lms.* TO 'los'@'%'; FLUSH PRIVILEGES;"
```

### 3. Configure the connection string

The committed `LosLms/appsettings.json` holds a **placeholder only**:

```
Server=localhost;Port=3306;Database=los_lms;User Id=REPLACE_ME;Password=REPLACE_ME;
```

Never put real credentials in `appsettings.json`. For local development, use the .NET user-secrets store (already initialized on this project):

```bash
cd LosLms

# Replace with your actual MySQL user/password.
dotnet user-secrets set "ConnectionStrings:LosDb" "Server=localhost;Port=3306;Database=los_lms;User Id=los;Password=los;"
```

User-secrets live outside the repo and override `appsettings.json` in Development.

If you'd rather not use user-secrets, just edit `appsettings.json` for local use and don't commit it back. The connection string above with `User Id=los;Password=los` matches the Docker setup.

### 4. Apply migrations and seed the database

```bash
cd LosLms
dotnet ef database update
```

This creates all 14 tables and seeds 128 sample applications. **You only run this once per fresh database.** Re-running it is safe but a no-op.

If `dotnet ef` complains the tool is missing, see the Prerequisites section.

### 5. Build and run

```bash
cd LosLms
dotnet build
dotnet run --launch-profile https
```

You'll see output ending with:

```
Now listening on: https://localhost:7095
Now listening on: http://localhost:5037
Application started.
```

### 6. Open the app

Visit <https://localhost:7095/applications> in your browser.

If Chrome warns about an untrusted certificate, click "Advanced" → "Proceed to localhost (unsafe)" — that's normal for a self-signed dev cert.

If Chrome refuses to load CSS / shows an unstyled page after a code change, **hard-reload with Ctrl+Shift+R**.

### 7. Use the demo walkthroughs

Two guides walk a brand-new application all the way from creation through disbursement:

- **Demo 1:** [`DEMO-WALKTHROUGH-APPLICATION-LN-2026-004900.md`](./DEMO-WALKTHROUGH-APPLICATION-LN-2026-004900.md) — Hemant Bhalerao, Commercial Vehicle, 3 parties (Applicant + Co-Applicant + Guarantor). Exercises the **RCU override gate** (one party returns Not recommended).
- **Demo 2:** [`DEMO-WALKTHROUGH-APPLICATION-LN-2026-004901.md`](./DEMO-WALKTHROUGH-APPLICATION-LN-2026-004901.md) — Priya Deshpande, Loan Against Property, 1 party (Applicant only). Exercises the **Eligibility deviation approver note** gate.

Both end at the same terminal state (`Status = Sanctioned`, `Disbursed = true`). Run them in either order.

---

## Running without MySQL

The app **starts fine with no MySQL running.** EF Core opens no connection until the first query, so a dead or unconfigured database never blocks startup at a branch.

You'll see the dashboard, but every page that hits the database will error. This is honest, not broken — it means the app fails closed if the database is unreachable rather than silently starting with no data.

To verify it starts without MySQL:

```bash
dotnet run --launch-profile https
# Point browser at https://localhost:7095/ — you should see the dashboard
```

---

## Project structure

```
LOS-LMS/
├── LosLms.sln
├── README.md                           ← this file
├── OPEN-QUESTIONS-FOR-ARUN.md          ← all unconfirmed assumptions
├── WALKTHROUGH-STAGES-1-TO-5.md        ← original walkthrough (Sunil Wagh)
├── DEMO-WALKTHROUGH-APPLICATION-LN-2026-004900.md   ← Demo 1
├── DEMO-WALKTHROUGH-APPLICATION-LN-2026-004901.md   ← Demo 2
├── *.spec.v2.jsx                       ← reference React specs (one per screen)
├── Wireframe nine-screen flow_1.zip    ← original 9-screen wireframe archive
└── LosLms/
    ├── LosLms.csproj
    ├── Program.cs                      ← startup, DI, /files endpoint
    ├── appsettings.json                ← placeholder connection string
    ├── appsettings.Development.json
    ├── Components/
    │   ├── App.razor                   ← root document
    │   ├── Routes.razor                ← router
    │   ├── Layout/                     ← MainLayout
    │   └── Shared/                     ← 8-stage stepper, summary card
    ├── Pages/
    │   ├── Home.razor
    │   ├── Error.razor
    │   ├── ApplicationsDashboard/      ← Stage 0 — entry list
    │   ├── CustomerDetails/            ← Stage 1
    │   ├── LoanAndSecurity/            ← Stage 2
    │   ├── BankAndFinancial/           ← Stage 3
    │   ├── DocumentChecklist/          ← Stage 4
    │   ├── ReportsRcu/                 ← Stage 5
    │   ├── Eligibility/                ← Stage 6
    │   ├── Approvals/                  ← Stage 7
    │   └── PostSanction/               ← Stage 8
    ├── Data/
    │   ├── LosDbContext.cs
    │   └── ApplicationSeedData.cs      ← 128 seeded applications
    ├── Models/                         ← entity classes
    ├── Migrations/                     ← EF Core migrations
    ├── Services/
    ├── Properties/
    ├── App_Data/                       ← upload destination (gitignored)
    ├── _Imports.razor
    ├── bin/                            ← build output (gitignored)
    └── obj/                            ← build output (gitignored)
```

Screen folders are PascalCase without numeric prefixes: a leading digit is not a valid namespace segment. Stage order belongs in the stepper component, not in folder names.

Each screen keeps its own logic beside its markup in a single `.razor` file. Only genuinely shared chrome (stepper, summary card) lives in `Components/Shared/`.

---

## Common issues and fixes

| Symptom | Fix |
|---|---|
| Browser warns about untrusted certificate | `dotnet dev-certs https --trust` |
| Dashboard loads but every page errors | MySQL isn't running or connection string is wrong. Check `dotnet user-secrets list` |
| `dotnet ef` says tool not found | `dotnet tool install --global dotnet-ef` |
| Page renders as raw unstyled HTML | Hard-reload with Ctrl+Shift+R. Chrome cached an old CSS file |
| Second CAM.pdf download silently does nothing | Chrome blocks repeated automatic downloads. Allow them in site settings, or reload the page |
| Opening any application always lands on Stage 1 | Working as built. Either click *Complete stage* forward, or edit the URL by hand |
| `HTTP Failed to determine the https port for redirect` warning at startup | Harmless. Means you ran `dotnet run` instead of `dotnet run --launch-profile https` |

---

## What's intentionally not implemented

Every screen is reachable by anyone who can reach the server. **There is no authentication, no session, no role, no per-branch access control.** The "Credit Officer · Nashik West" label in the top bar is a hardcoded placeholder. Document files served from `/files/{applicationId}/{folder}/{name}` are PII (Aadhaar, PAN, bank statements); without auth, anyone with a URL has the document.

Other things explicitly out of scope of this build, with reasoning in [`OPEN-QUESTIONS-FOR-ARUN.md`](./OPEN-QUESTIONS-FOR-ARUN.md):

- Real FOIR / LTV policy thresholds (the screen ships 50% / 85% placeholders)
- Real OCR / verification integrations (PAN, Aadhaar, mobile OTP, video KYC all report "not configured")
- Real RCU vendor hand-off (Stage 5 is fully manual)
- Real banking integrations (penny-drop, statement parsing both report "not configured")
- Real CIBIL / bureau pull
- The two undefined Post Sanction checklist items (header reads "7 of 9 flags cleared (2 items pending definition)")

Read that document before treating this as a production-ready system.

---

## Licence notes

**QuestPDF** is used under its Community licence. That licence is only valid for organisations with **under $1M USD annual gross revenue**. If your organisation exceeds that threshold, this needs a paid Professional or Enterprise licence from QuestPDF. See OPEN-QUESTIONS §1.1.

---

## Contributing

1. Create a feature branch from `main`
2. Make changes
3. Run `dotnet build` to confirm it compiles
4. Commit using Conventional Commits format (`feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`)
5. Open a pull request

The project has no test project yet — adding one is a candidate first contribution.

---

## License

Internal use only. Not for redistribution.