# Architecture overview

A map of the system for someone new to the code. It describes how things are *organised* and *why*; the code is the
detail. Paths are relative to the repository root.

## What it is

A staff-only **Loan Origination and Loan Management System** for NBFCs that finance vehicles and similar assets. Loan
officers and back-office staff use it; borrowers never sign in. Each customer company (a *tenant*) sees only its own data.

An application moves through **eight stages**, with *Reject* and *Send back* as the only reverse operations:

1. Customer Details → 2. Loan & Security → 3. Bank & Financial → 4. Document Checklist → 5. Reports (RCU) →
6. Eligibility → 7. Approvals → 8. Post Sanction (agreement, e-NACH mandate, disbursal). After disbursal the loan lives on
in servicing (receipts, EMI collection, bounces). Stage numbers, labels and routes are defined once, in
`src/LosLms/Data/LoanStages.cs`.

## Solution layout

```
src/
  LosLms/            the web application (Blazor Server, .NET 8): everything below
  LosLms.Licensing/  signed-licence verification, shared by the app and the vendor tool
  LosLms.LicenseTool/vendor-only CLI: provisions clients, signs/renews licences (never shipped)
  LosLms.Server/     Windows launcher for the desktop edition: bundled MySQL, tunnel, watchdog, self-update
  LosLms.Shell/      WinForms/WebView2 window the launcher uses on the host PC
tests/
  LosLms.Tests/      xUnit tests for the application
docs/  deploy/  tools/  publish.ps1  Dockerfile  .github/
```

## The web application (`src/LosLms`)

| Folder | What lives there |
|---|---|
| `Pages/` | Routable screens. `Applications/Stages/` is one file per stage; `Admin/`, `CompanySetup/`, `SuperAdmin/`, `Account/`, `Collections/`, `Loans/`, `Digio/`, `Billing/` are the rest |
| `Components/` | `Layout/` (app shell) and `Shared/` (stepper, dialogs, form primitives); `Shared/Digio/` holds the reusable verification UI (badge, verify button, e-Sign and mandate panels) |
| `Models/` | EF Core entities, grouped by domain (Origination, Parties, Banking, Credit, Disbursal, Servicing, Integrations, Tenancy…) |
| `Data/` | `LosDbContext` (tenant query filters), seeders, `LoanStages` |
| `Services/` | All business logic, grouped by area (below) |
| `Migrations/` | EF Core migrations (MySQL) |
| `wwwroot/` | Static assets and the in-app partner developer guide (`/docs/`) |

Every screen is **interactive server-rendered Blazor** (a SignalR circuit per user), except `/account/*`, which renders
statically so sign-in can set cookies.

### `Services/` by area

| Area | Responsibility |
|---|---|
| `Tenancy/` | Who is signed in and which company they act for (`TenantContext`), company provisioning and policy |
| `Gates/` | "May this application move on?" in one place: the CIBIL gate, vehicle caps, rejections, admin-request notifications |
| `Underwriting/` | Eligibility (FOIR/LTV), loan maths and the repayment schedule |
| `Origination/` | Prefill and default rules for the stage screens, party de-duplication, checklist verification, scheme-charge templates, migrated-loan import |
| `Servicing/` | The loan ledger after disbursal: loan accounts and closure, receipts, EMI presentation, bounces and re-presentation, collection scheduling |
| `Digio/` | **Everything that talks to Digio** (below) |
| `Billing/` | Metering of provider API calls and the postpaid monthly invoice |
| `Documents/` | PDF generation (QuestPDF): CAM, loan agreement, KFS, hypothecation agreement, disbursement memo, welcome letter |
| `PartnerApi/` | OAuth device-flow and the read API that licensed partner software uses (`/api/v1`) |
| `Integrations/` | Other third parties: IFSC lookup, Gemini signature detection, Cloudflare Turnstile, SMS, TG portal |
| `Updates/` | Desktop self-update (check, signature verification, notification) |
| `Hosting/`, `Security/`, `Uploads/`, `Email/`, `Licensing/`, `Ui/`, `Reference/` | Cross-cutting infrastructure |

## Ideas the whole system relies on

- **Multi-tenancy by query filter.** `LosDbContext` applies a global filter (`CompanyId == current company`) to every
  tenant table, and child tables are filtered through their application. A forgotten `Where` therefore cannot leak another
  company's rows. Roles are `Staff`, `Admin`, `SuperAdmin` (the vendor). Background jobs and webhooks have no user, so they
  scope explicitly and deliberately bypass the filter (`IgnoreQueryFilters()` plus an explicit company id).
- **Never fabricate a result.** A verification, bureau score, mandate state or signature status is only ever set from a
  real provider answer or an audited human decision. "Not configured" and "unavailable" are honest states.
- **One door to Digio.** All outbound Digio calls go through `Services/Digio/DigioClient.cs`, which checks that keys exist,
  that the company's bill is not overdue, sends the call, and meters it. An architecture test fails the build if a call
  site does not declare how it is billed. Digio's asynchronous results arrive by **webhook** (`/webhooks/digio/{companyId}`,
  HMAC-verified, always answered `200`), are queued, and are applied by a background processor with a reconciler as a
  safety net. Details and the integration plan: [`../digio/`](../digio/00-MASTER-PLAN.md).
- **Gates are enforced twice.** A stage's *Complete* button is disabled by `GateCheckService` (a hint), and the completion
  handler checks again before doing any work (the actual control).
- **Secrets stay out of the database and the repository.** Per-company provider keys live in an encrypted vault
  (`Services/Security/PasswordVault.cs`, AES-GCM, key from configuration), never in plain columns or git.
- **Security headers.** A strict Content-Security-Policy (same-origin scripts), `X-Frame-Options: DENY` and a limited
  permissions policy are set in `Program.cs`. Anything that loads third-party script or frames needs a deliberate CSP change.

## Two ways it is hosted

| | Desktop edition | Cloud edition |
|---|---|---|
| Switch | default | `Hosting:Cloud=true` (set by the Docker image) |
| Database | bundled MySQL started by `LosLms.Server` | managed MySQL (Aiven) |
| Files | `App_Data/` on disk | Supabase Storage (`IFileStore`) |
| Updates | in-app, from signed GitHub Releases | redeploy of `main` on Render |
| Docs | [`DESKTOP-RELEASE-AND-UPDATES.md`](../operations/DESKTOP-RELEASE-AND-UPDATES.md) | [`CLOUD-DEPLOY.md`](../operations/CLOUD-DEPLOY.md) |

The app is **single instance** by design (real-time updates are in-process). Running more than one instance needs a
SignalR backplane.

## Background services

`DigioEventProcessor` and `DigioReconciler` (Digio events), `CollectionScheduler` and `RepresentationJob` (EMI
presentation and re-presentation), `PartnerOAuthPurgeService` (expired connection codes), and, on the desktop edition only,
`UpdateCheckBackgroundService`.

## Testing

`tests/LosLms.Tests` uses xUnit against in-memory SQLite with a fake Digio HTTP handler, so it needs no database or
network. Several tests are architectural (billing declarations, no code outside `ReceiptService` marks an instalment paid,
tenant isolation). They scan the source tree, so keep the `src/LosLms` layout the tests expect.

## Deeper reading

[`../project/OPEN-QUESTIONS.md`](../project/OPEN-QUESTIONS.md) (assumptions awaiting client confirmation) ·
[`../integrations/PENDING-API-INTEGRATIONS.md`](../integrations/PENDING-API-INTEGRATIONS.md) ·
[`../digio/`](../digio/00-MASTER-PLAN.md) · [`../operations/`](../operations/).
