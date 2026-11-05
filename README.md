# LOS / LMS: Loan Origination & Management System

A staff-only system for NBFCs that finance vehicles and similar assets. Loan officers and back-office staff take an
application through eight stages (customer KYC, loan & security, bank & financial, documents, RCU, eligibility, approvals,
post-sanction) and then service the loan. Borrowers never sign in. Each customer company sees only its own data.

Identity checks, e-Sign and e-NACH mandates run through [Digio](docs/digio/00-MASTER-PLAN.md).

## At a glance

| | |
|---|---|
| Runtime | .NET 8 (LTS) |
| UI | Blazor Server (interactive server rendering; `/account/*` renders statically so sign-in can set cookies) |
| Data | EF Core 8 with MySQL 8 (Pomelo provider) |
| PDFs | QuestPDF (Community licence; see the note below) |
| Hosting | **Cloud:** Docker on Render + Aiven MySQL + Supabase Storage. **Desktop:** Windows launcher with bundled MySQL and a Cloudflare tunnel |

## Repository layout

```
src/
  LosLms/              the web application
  LosLms.Licensing/    signed-licence verification
  LosLms.LicenseTool/  vendor-only CLI for client provisioning and licences
  LosLms.Server/       Windows launcher (desktop edition)
  LosLms.Shell/        WebView2 window used by the launcher
tests/
  LosLms.Tests/        automated tests (xUnit), one folder per feature area:
                       Billing, Digio, Disbursal, Documents, Integrations, Origination,
                       PartnerApi, Platform, Security, Servicing, Tenancy
docs/                  all documentation, start at docs/README.md
deploy/                files shipped inside the desktop client package
tools/                 operational PowerShell scripts (backup restore, cloud migration, runtime dependencies)
branding/              logo and icon sources
.github/               CI, deploy, nightly backup and Dependabot configuration
publish.ps1            builds the desktop package and the signed update artifact
Dockerfile             builds the cloud image
LosLms.sln             the solution
```

## Quick start (development)

```bash
git clone https://github.com/Hemanshu-jain/LOS-LMS.git && cd LOS-LMS
cd src/LosLms
dotnet user-secrets set "ConnectionStrings:LosDb" "Server=localhost;Port=3306;Database=los_lms;User Id=los;Password=los;"
dotnet run --launch-profile https          # https://localhost:7095/applications
```

You need the .NET 8 SDK and a MySQL 8 database. The app applies its own migrations on start. Full instructions, the Docker
one-liner for MySQL, and troubleshooting: [`docs/development/SETUP.md`](docs/development/SETUP.md).

Run the tests (no database or network needed): `dotnet test tests/LosLms.Tests`

## Documentation

| | |
|---|---|
| [`docs/README.md`](docs/README.md) | Index of everything |
| [`docs/development/ARCHITECTURE.md`](docs/development/ARCHITECTURE.md) | How the system is organised and why |
| [`docs/operations/`](docs/operations/) | Deploying, backing up, building the desktop package, onboarding clients |
| [`docs/digio/`](docs/digio/00-MASTER-PLAN.md) | The Digio integration plan and reference |
| [`CHANGELOG.md`](CHANGELOG.md) | What changed in each version |
| [`CONTRIBUTING.md`](CONTRIBUTING.md) | How to propose a change |

## Important to know

- **Merging to `main` deploys to production.** A push to `main` that changes more than documentation triggers a Render deploy
  (`.github/workflows/deploy.yml`). Treat `main` as the release branch. See `CONTRIBUTING.md`.
- **Secrets never go in the repository.** Connection strings, provider keys, tunnel tokens, licence and signing keys are
  supplied through user-secrets, environment variables or git-ignored files at the repository root.
- **Provider checks never invent results.** A verification, bureau score or mandate state comes from the provider (or an
  audited human decision), never from a default. See the "never fabricate" rule in the architecture doc.

## Licence notes

**QuestPDF** is used under its Community licence, which is valid only for organisations with under US$1M annual gross
revenue. A larger organisation needs a paid licence from QuestPDF (tracked in
[`docs/project/OPEN-QUESTIONS.md`](docs/project/OPEN-QUESTIONS.md)).

This software is for internal use only and is not licensed for redistribution.
