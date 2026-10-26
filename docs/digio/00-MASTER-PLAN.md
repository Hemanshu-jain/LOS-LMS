# Digio × LOS/LMS — Master integration plan

> **Status:** plan, not yet built. Supersedes `docs/DIGIO-KYC-INTEGRATION-PLAN.md`.
> **Sources:** this repository (read end to end) and Digio's public documentation + OpenAPI specs at
> documentation.digio.in, read on 2026-10-26 (206 pages, 79 endpoints). Where Digio's public docs are silent,
> this plan says **"confirm with Digio"** instead of guessing.

| File | What it is |
|---|---|
| `00-MASTER-PLAN.md` | This file — goals, principles, architecture, product→stage map, roadmap, risks, decisions |
| `01-FOUNDATION.md` | The platform layer every feature sits on (client, data model, webhooks, poller, UI kit, config, tests) |
| `02-FEATURES-BY-STAGE.md` | Every Digio feature, stage by stage: flow, UI, data, gates, acceptance criteria |
| `03-LMS-COLLECTIONS.md` | Post-disbursal: repayment ledger, NACH debit presentation, bounces, settlement, closure |
| `04-DIGIO-API-REFERENCE.md` | Curated endpoint/field/webhook reference for everything this plan uses |
| `05-SESSION-PROMPTS.md` | Copy-paste prompts + checklists for the local Claude session, phase by phase |
| `appendix/DIGIO-API-SPEC-SUMMARY.md` | Machine summary of all 79 Digio OpenAPI operations (offline grep source) |

---

## 1. What we are building (one paragraph)

Every identity, document, bank, signature and collection check in the eight-stage loan lifecycle that
today says **"provider not configured"** is wired to Digio. Each one goes through one gated client,
leaves an auditable evidence record, and never fills a field or passes a gate on anything Digio did not
actually return. Borrower actions (Aadhaar consent, e-Sign, e-mandate) happen on Digio's own pages, sent
to the customer as a link or opened on the officer's screen with the customer present. The app stays
staff-only and its strict CSP stays intact. After disbursal, the same mandate drives automated EMI
collection (the LMS half).

---

## 2. Non-negotiable principles (every phase is reviewed against these)

1. **Never invent data.** A field is filled, a flag is set or a gate is passed only by a verified Digio
   response, or by a human explicitly accepting a mismatch, which is recorded. This continues the rule the
   codebase already follows (`PanVerified`, `PennyDropStatus`, `CibilStatus`, `AgreementEsignStatus`).
2. **One door to Digio.** Every outbound call goes through `DigioClient` (keys → billing gate → call →
   meter → log). No second `HttpClient` path, ever.
3. **Server is the source of truth.** Browser/SDK callbacks and redirect query strings are *hints* that
   trigger a server-side status fetch. Only a verified webhook or a server-side status API call changes state.
4. **Webhooks never block Digio's queue.** Digio's queue is head-of-line per account. Any non-200 stalls
   *every* later event (mandates, signatures, KYC) and Digio disables webhooks after 24 h. The endpoint
   verifies, stores, returns 200 within milliseconds, and processes later.
5. **Idempotent everywhere.** Digio delivers at least once, and staff double-click. Every create call
   carries our own unique id (`unique_request_id` / `reference_id` / `callback` / `customer_ref_number`).
   Every webhook is de-duplicated by its `id`. Every state transition is forward-only.
6. **Sandbox can never satisfy production.** Every Digio record carries the environment it came from.
   Sandbox evidence is badged **SANDBOX** and can never pass a gate on a production company.
7. **Evidence goes stale when its input changes.** If the officer edits a PAN, an account number or the
   agreement after verification, the evidence is marked *Stale* and the gate re-closes.
8. **PII minimisation.** Secrets live only in the vault. Logs carry ids and status, never PAN, Aadhaar,
   account numbers or names. The full Aadhaar number is not kept at rest (see §8, R3). Raw Digio payloads
   are stored minimised and encrypted.
9. **Honest UI states.** Not configured · Paused (bill) · Waiting for customer · Verified · Mismatch ·
   Failed · Expired · Stale · SANDBOX. One shared component renders them, so no screen invents its own wording.
10. **Small, reversible phases.** Each phase is one branch and one PR, with tests and a sandbox test
    script. It ships behind a per-company product toggle that can be switched off without a deploy.

---

## 3. Architecture at a glance

```
 Stage screens (Blazor Server)                      Digio
 ─────────────────────────────                      ─────
  VerifyBadge / CustomerActionPanel  ──calls──▶  Feature services (PanVerify, DigiLockerKyc, Esign, Mandate…)
                                                     │
                                                     ▼
                                               DigioClient  (keys · billing gate · HTTP · metering · request log)
                                                     │  JSON / multipart / GET / binary download
                                                     ▼
                                      https://ext.digio.in:444  |  https://api.digio.in
                                                     │
            ┌────────────────────────────────────────┘ async results
            ▼
  POST /webhooks/digio/{companyId} ──▶ verify X-Digio-Checksum ──▶ DigioWebhookInbox (row) ──▶ 200 OK
                                                                         │
                         DigioEventProcessor (BackgroundService) ◀───────┘   + DigioReconciler (poller)
                                   │                                             │ (status APIs for anything
                                   ▼                                             │  not final after N minutes)
                  DigioRequest (state) ─▶ Evidence rows ─▶ domain fields (Party, Disbursement, Mandate…)
                                   │
                                   ▼
                     DigioStatusNotifier (in-process) ──▶ open stage screens refresh live
```

**Borrower-facing steps (no borrower portal is needed):**

| Mode | How | When |
|---|---|---|
| **Send link** (default) | Create the request with `notify_customer: true`. Digio sends SMS, WhatsApp or email to the customer's verified mobile or email. The customer completes it on their own phone. | Customer not at the branch, or prefers their own device |
| **Open on this device** | Open Digio's hosted gateway page (`…/#/gateway/login/{id}/{txn}/{identifier}?redirect_url=…`) in a new window: a browser tab on the web, the existing viewer window in the desktop shell. The customer completes it there. `redirect_url` returns to `/digio/return`, which only triggers a server refresh. | Customer sitting with the officer |
| **Embedded Web SDK** (`digio.js` v11, iframe) | **Not in scope at first.** It needs third-party script, frame and camera allowances in the CSP and the WebView2 shell. Revisit only if the two modes above prove insufficient (see `01-FOUNDATION.md` §9). | — |

---

## 4. Digio product → app stage map (with priority)

**P0** = needed for first production go-live. **P1** = next. **P2** = later or optional.

| Stage | Feature | Digio product / API | Pri | Replaces today |
|---|---|---|---|---|
| 1 Customer Details | **PAN verification** (name + DOB match, Aadhaar-seeding) | `POST /v3/client/kyc/fetch_id_data/PAN` | P0 | "Verify OCR" stub |
| 1 | **Aadhaar + PAN via DigiLocker** (auto-fill name/DOB/gender/address/photo) | DigiStudio `with_template` → `DIGILOCKER_AADHAAR_PAN` | P0 | manual entry |
| 1 | Aadhaar Offline e-KYC (XML) as fallback | DigiStudio `OfflineKyc` | P1 | — |
| 1 | **Aadhaar masking** of every Aadhaar upload | `POST /v4/client/kyc/aadhaar/mask` | P0 | manual |
| 1 | **AML / sanctions screening** of every party | DigiShield `POST /client/v1/user/screen` | P0* | nothing |
| 1 | ID-card OCR (PAN/Aadhaar/DL/Voter/Passport upload) | `POST /v4/client/kyc/analyze/file/idcard` | P1 | "Verify OCR" stub |
| 1 | Liveness + face match on the in-app live photo | `…/analyze/file/liveness`, `…/kyc/facematch` | P1 | evidence-only photo |
| 1 | CKYC search/download before onboarding | DigiKYC CKYC (doc shared privately) | P1* | nothing |
| 1 | Name fuzzy-match across sources (PAN↔Aadhaar↔bank) | `POST /v3/client/kyc/fuzzy_match` | P1 | eyeballing |
| 1 | V-CIP (agent-assisted video KYC) | DigiStudio `2WAY-VIDEO-KYC` | P2 | in-app recording (evidence only) |
| 1 | Employment (UAN), professional, electricity-bill address | `/client/v4/apis/verify/uan`, `…/get/{idType}`, electricity | P2 | manual |
| 2 Loan & Security | **Vehicle RC verification** (owner, chassis/engine, financier/hypothecation, insurance, blacklist, challans) | `POST /client/v4/apis/verify/vehicle_rc` | P0 (used) / P1 (new, as PDD) | manual |
| 2 | Driving licence (commercial-vehicle driver) | `fetch_id_data/DRIVING_LICENSE` | P2 | manual |
| 3 Bank & Financial | Penny drop (**exists**) → harden: `AUTO` mode, idempotency, evidence, staleness | `POST /v4/client/verify/bank_account` | P0 | — |
| 3 | UPI VPA verification | `POST /v3/client/public/upi/check_vpa` | P2 | — |
| 3 | Bank statements via Account Aggregator | FIU-TSP (NBFC must become an FIU) | P2 | "Request" stub |
| 4 Document Checklist | Checklist items show linked Digio evidence (never auto-ticked without it) | — | P1 | person ticks only |
| 5 Reports (RCU) | KYC report PDF onto the file; geo distance checks | `…/v3/{id}/kyc_report`; geo APIs (confirm) | P2 | manual |
| 7 Approvals | **Business KYB**: GSTIN / GST advanced / CIN / DIN / PAN→GST / Udyam / TAN | `fetch_id_data/{type}` (+ KYB 360: confirm with Digio) | P1 | "Verify company (ROC)" stub |
| 7 | Director / partner AML screening | DigiShield | P1* | — |
| 8 Post Sanction | **Multi-party Aadhaar e-Sign** with signer identity rules, lender signatory, signed-PDF download, audit trail | DigiSign `uploadpdf`, `GET document`, `download`, `cancel` | P0 (exists, needs hardening) | single signer, no signed copy kept |
| 8 | e-Stamping of agreement (state stamp duty) | `estamp_request` on the sign request | P1 | physical stamp paper |
| 8 | **e-NACH mandate** (net banking / debit card / Aadhaar; eSign; physical) | DigiCollect `mandate/create_form` + webhooks | P0 | stub (`MandateFlow`) |
| 8 | Security NACH (loan-amount security mandate) | same, category confirm | P1 | stub |
| 8 | UPI Autopay mandate | `mandate/create_form` (`auth_mode: upi`) | P2 | — |
| LMS | **EMI debit presentation, bounce handling, settlement recon** | DigiCollect `nach_debit/*` | P0 for go-live with NACH | nothing (no servicing ledger exists) |
| LMS | Mandate cancellation on closure | `mandate/registration_cancel` | P1 | — |
| LMS | CKYC upload after account opening | `POST /v3/client/kyc/ckyc/register` | P1* | — |
| LMS | RC hypothecation-endorsement check (PDD) | `verify/vehicle_rc` | P1 | manual PDD |
| Any | Purge Digio-held KYC data after we keep our copy | `POST /client/kyc/purge/{id}` | P2 | — |

`*` = likely a **regulatory** expectation for an RBI-regulated NBFC (KYC Master Direction: CKYCR upload and
search, screening against UNSC/sanctions lists). The priority depends on the NBFC's compliance officer. This
plan does not give legal advice; see §8.

**Not Digio (keep separate, do not force into Digio):** CIBIL/bureau pull, SMS gateway (welcome SMS,
mobile OTP), payment gateway. Digio's docs contain no bureau or OTP-SMS product.

---

## 5. Roadmap (phases in dependency order)

| Phase | Name | Contains | Depends on | Output |
|---|---|---|---|---|
| **0** | Readiness (no feature code) | Decisions (§9), Digio product enablement + template names, sandbox smoke test, sample-response capture, webhook reachability test | sandbox keys ✔ | `docs/digio/samples/*.json`, answers to §9 |
| **1a** | Hotfixes to what is live today | Webhook never returns non-200 to Digio; multi-signer-safe `doc.signed`; keep the signed PDF | — | small PR |
| **1** | Foundation | `DigioClient` v2, `DigioRequest`, webhook inbox + processor, reconciler, evidence model, settings, UI kit, return page, health page, rate codes, fake-Digio test harness | 0 | platform PR(s) |
| **2** | KYC core (Stage 1) | PAN verify, DigiLocker Aadhaar+PAN, Aadhaar masking, AML screening, fuzzy-match helper, Aadhaar-at-rest change | 1 | Customer Details live |
| **3** | Sign & mandate (Stage 8) | Multi-party e-Sign hardening (+ e-stamp), e-NACH registration, disbursal gate policy | 1 (2 for signer rules) | Post Sanction live |
| **4** | Asset, business, docs | RC verify, KYB, ID OCR, liveness/face match, checklist evidence links, KYC report | 1 | Stages 2, 4, 7 live |
| **5** | LMS collections | Repayment ledger, debit presentation, bounce/re-present, settlement recon, mandate cancel, CKYC upload | 3 | EMI auto-collection |
| **6** | Advanced (optional) | V-CIP, Account Aggregator, UPI Autopay, VPA, UAN, electricity, purge, KRA | 1 | as chosen |
| **7** | Production cutover (per company) | Production keys, prod webhook group, product toggles on, pilot branch, monitoring | Digio onboarding | live |

Phases 2, 3 and 4 are independent once Phase 1 is done and can run in any order. Phase 5 needs Phase 3
(registered mandates).

---

## 6. Definition of done (every phase)

- [ ] Every new call goes through `DigioClient`; every create call sends our idempotency id.
- [ ] Every async result arrives by webhook **and** is recoverable by the reconciler.
- [ ] Every result writes an evidence row (environment, request id, who, when, verdict, minimised data).
- [ ] UI uses the shared components; every state in §2.9 is reachable and honest.
- [ ] Gates read evidence, not UI flags; sandbox evidence cannot pass a gate on a production company.
- [ ] Metering uses the right rate code; failed calls are logged but not billed.
- [ ] No PII in logs (grep test); secrets only in the vault.
- [ ] Unit tests map **real captured sandbox samples** (PII masked); webhook and state-machine tests pass;
      `dotnet build` and `dotnet test` are green; no live network in tests.
- [ ] EF migration is additive and MySQL-safe (`[MaxLength]` on every string); it applies on both desktop and cloud.
- [ ] The sandbox test script in `05-SESSION-PROMPTS.md` was run end to end and its results pasted into the PR.
- [ ] Docs updated: `PENDING-API-INTEGRATIONS.md`, and a `WHATS-NEW` entry when user-visible.

---

## 7. Deployment realities this plan accounts for

| Reality (from this repo) | Consequence | Handled in |
|---|---|---|
| Two hosts: **desktop server** (Windows, embedded MySQL, Cloudflare tunnel) and **cloud** (Render + Aiven + Supabase) | Webhook URL differs per host; a desktop server **without a tunnel token cannot receive webhooks at all** | Reconciler is mandatory; health page self-test; keys page shows the URL actually reachable (`PublicBaseUrl`) |
| Render free plan sleeps; cold start can exceed Digio's 10 s timeout | First webhook after idle fails, and Digio retries (5 times in hour 1, then hourly up to 24 h) | Inbox makes retries harmless; reconciler; recommend a paid always-on plan for production |
| Sandbox webhooks are delayed **~5 minutes** (production ~5 s) | Testers think it is broken | "Refresh status" button + reconciler; documented in test scripts |
| CSP is self-only; `frame-ancestors 'none'`; camera is `self` only; WebView2 shell | Digio's Web SDK would need CSP + shell changes | Link / "open on this device" modes first; SDK deferred (`01-FOUNDATION.md` §9) |
| Single server instance; in-process SignalR notifier (`AdminRequestNotifier` pattern) | Live refresh works in-process only | Same pattern for `DigioStatusNotifier`; inbox claim is still multi-instance-safe |
| MySQL in production, SQLite in tests | Migrations and types must work on both | Additive migrations, `[MaxLength]`, no provider-specific SQL |
| Multi-tenant via EF global query filters; webhooks have no user | Webhook processing must scope by URL `companyId` with `IgnoreQueryFilters()`, as `ApplyWebhookAsync` already does | Processor rules in `01-FOUNDATION.md` §4 |
| Per-NBFC Digio accounts (separate keys, webhook secret, sandbox vs prod) | All config is per company | `DigioSettings` per company |

---

## 8. Risks & how the plan neutralises them

| # | Risk | Mitigation |
|---|---|---|
| R1 | Webhook endpoint returns 400/401 (today it does, for bad JSON, a bad checksum or an unknown company), which stalls all of the account's events and gets webhooks disabled after 24 h | Phase 1a: always 200 after reading; bad signature → no processing + rate-limited admin alert |
| R2 | e-Sign marked Signed when only one of several signers has signed | Phase 1a: Signed only when `agreement_status == "completed"` (all parties signed), confirmed by `GET /v2/client/document/{id}` |
| R3 | **Full Aadhaar number stored at rest** (`Party.Aadhaar`, used for dedupe); likely not permitted for an entity that is not an Aadhaar authentication user | Phase 2: keep last 4 digits + keyed HMAC for dedupe + Digio reference; mask every Aadhaar image; one-time backfill migration. **Confirm with the compliance officer** |
| R4 | Sandbox results leaking into production decisions | Environment on every row; gates refuse sandbox evidence on production companies; SANDBOX badge |
| R5 | Double-click or retry creates duplicate e-Sign documents or mandates (double billing, two links to the customer) | Our idempotency id on every create (`callback` for DigiSign, `customer_ref_number` for mandates, `reference_id`/`unique_request_id` for KYC) + "already in progress" UI lock |
| R6 | Missed webhooks (desktop without tunnel, Render asleep, Digio disabled the group) | Reconciler polls every non-final request; health page shows the last webhook received |
| R7 | Customer completes a flow while the officer's page is stale and an autosave overwrites the result | Results write server-side only; autosave never copies Digio-owned fields (the pattern already used for `AgreementEsignStatus`) |
| R8 | Wrong person signs or authenticates | DigiSign `signature_verification` rules (NAME fuzzy, YOB, GENDER, AADHAAR last 4) from verified KYC; DigiLocker name/DOB compared to PAN evidence |
| R9 | Fields overwritten by Digio data | `Apply` only fills blanks; conflicts show a "Digio says … — Apply?" diff; accepting a mismatch is an audited human action |
| R10 | Product not enabled on the account → 403 or odd errors | Per-company product toggles; a 403 maps to "Not enabled on your Digio account — contact Digio", not a code error |
| R11 | Billing surprises | Rate code per call; DigiStudio and e-stamp billing rules **confirmed with Digio's invoice** before production |
| R12 | Webhook payload shape changes (Digio adds fields) | Tolerant parsing (unknown fields ignored), as Digio's FAQ asks; samples re-captured each phase |
| R13 | Regulatory gaps (CKYC upload, sanctions screening, V-CIP rules, NACH cancellation on closure, data retention) | Listed as explicit items with owner "Compliance to confirm"; built as configurable gates rather than hard-coded assumptions |

---

## 9. Decisions needed from the business (Phase 0)

Record the answers in `docs/digio/DECISIONS.md` (Phase 0 creates it). Each one changes what gets built.

1. **Products bought / enabled** on the Digio account (sandbox now, production later). Ask Digio for the
   list and for the **DigiStudio template names** (`DIGILOCKER_AADHAAR_PAN`, `OfflineKyc`, `2WAY-VIDEO-KYC`,
   `REVERSE_PENNY_DROP` or custom).
2. **Default customer channel:** send a link (SMS / WhatsApp / email) or open on the officer's device? Both
   are built; which one is the default?
3. **Minimum KYC evidence per product** to pass Customer Details (e.g. PAN verified + Aadhaar via DigiLocker
   + AML clear). Configurable per company, but we need the defaults.
4. **Aadhaar at rest:** confirm the plan in R3 (last 4 + hash + masked copy only).
5. **Signers on the loan agreement:** applicant, co-applicants, guarantors, and the lender's authorised
   signatory (who, which email)? Sequential or parallel?
6. **e-Stamp:** which states, which denominations, who orders the inventory (5–7 day lead time)?
7. **NACH policy:** e-NACH vs physical default; frequency (`Adhoc` vs `Monthly`); maximum amount (EMI ×
   ?); category code for EMI vs security mandate (sponsor bank to confirm); NACH **corporate_config_id(s)**
   and the corporate (sponsor) account number from Digio.
8. **Disbursal gate:** must the agreement be **Signed** and the mandate **Registered** before *Release
   funds*? Today neither gates release. Recommended: yes for both, with an Admin bypass that is logged.
9. **Collections (LMS):** do we auto-present EMIs through Digio? Bounce charge policy, re-presentation
   policy, and where receipts are recorded.
10. **Compliance owner** for R3/R13: who signs off on CKYC, screening and V-CIP requirements?

---

## 10. What changes for each role (so the UI is designed around people)

- **Loan officer:** clicks *Verify* or *Send to customer*, sees a live status chip, and never re-types what
  Digio fetched. Gets told exactly what is blocking and why.
- **Admin / credit:** sees the evidence trail on each file, approves mismatches and bypasses (audited),
  and sees the company's Digio health and usage.
- **SuperAdmin (vendor):** keys, environment, product enablement and webhook health per company; billing
  (existing).
- **Customer:** gets one Digio link per action, or completes it on the officer's screen. No new app or login.
