# Full-System Smoke Test Report — LOS/LMS

**Date:** 2026-08-12
**Build:** `main` @ c1e9cbc · .NET 8 Blazor Server · MySQL 8.0 (`los_lms`) · QuestPDF community
**Scope:** Read / verify / report only. No application code or schema was changed. The only state
changes are those produced by the product's own UI actions being tested (submitting/approving
requests, send-back, reject) plus an **authorised database reset** (see §0.1).
**Method:** Live app driven over HTTP/Chrome (`http://localhost:5037`), authenticated `curl` for
route/isolation checks, direct MySQL reads for state verification, and source reading for the
regression sweep. Each finding notes how it was established.

---

## 0. Environment & real records

### 0.1 Test-environment finding (read this first)

The brief assumed the database already held "real, varied seed data from prior sessions — pending
admin requests, resolved requests, a second company, no placeholder applications." **It did not.**
The live MySQL database was the pristine 15-application demo seed:

| Expected (per brief) | Actual on arrival |
|---|---|
| Pending AdminRequest(s) | `adminrequest` = **0 rows** |
| Resolved AdminRequest in history | 0 rows |
| Reject / Send-back history | `rejectionlog` = 0, `sendbacklog` = 0 |
| A second company (isolation) | `companies` = **1** ("Default Company") |
| Accounts used at least once | All 4 accounts `MustChangePassword = 1` (never signed in) |

This is an **environment/hand-off gap, not a product defect** — the "prior session" data was never in
this database. With your explicit approval, the database was **dropped, rebuilt from all 17
migrations, and reseeded with `Seed:IsolationFixture=true`** so that (a) login credentials were
capturable and (b) a real second company existed for isolation testing. The rebuild also
incidentally re-verified the fresh-MySQL migration chain fixed in commit `8edb800` — it applied
cleanly end-to-end.

### 0.2 Real records used (post-reseed, all traceable)

**Companies:** `1` = "Default Company" (Company A) · `2` = "Isolation Test Company" (Company B).

**Users** (all forced a password change at first sign-in; reset to a known test value during testing):

| Role | Email | Company |
|---|---|---|
| Admin | r.kulkarni@placeholder.local | A |
| Staff | s.deshpande@placeholder.local | A |
| Staff | a.rao@placeholder.local | A |
| SuperAdmin | superadmin@placeholder.local | (none — cross-company) |
| Staff | b.sharma@placeholder.local | B |

**Applications by state (Company A unless noted):**

| State required by brief | Real record(s) |
|---|---|
| Fully sanctioned **+ disbursed** | `LN-2026-005001` (Stage 8, Sanctioned, Disbursed) |
| Sanctioned, not disbursed | `LN-2026-005002`, `LN-2026-005003` |
| Mid-flow early (stage 1–3) | `005013`/`005014` (S1), `005012` (S2), `005011` (S3) |
| Mid-flow later (stage 4–6) | `005010` (S4), `005008`/`005009` (S5), `005006`/`005007` (S6) |
| Blocked on CIBIL, no bypass yet | `005013`, `005014`, `005015` (`CibilGateStatus=Blocked`) |
| Seeded Rejected (with log) | `005015` (Stage 5, RejectionLog present) |
| Second company (isolation) | `LN-2026-009001` (Company B, Stage 1) |
| Pending / resolved AdminRequest | **created live during Part 3 & 6** (none pre-existed) |

Note: 12 of 15 Company-A apps are seeded `CibilGateStatus=Passed`, i.e. the demo seed pre-clears CIBIL
for most files; the "app-wide permanent block" is only observable on the 3 `Blocked` apps.

---

## 1. Role & auth verification — ✅ PASS

| Test | Expected | Actual |
|---|---|---|
| Staff / Admin / SuperAdmin sign-in | Succeeds; forces password change on first use | ✅ All three; change-password screen enforced and cleared |
| Sign-out | Explicit POST confirmation, then session ends | ✅ CSRF-safe confirm page → returns to login |
| Unauthenticated → any page | Redirect to login (fail-closed) | ✅ `/applications` → `/account/login` (fallback auth policy) |
| **Staff** → `/company-setup` (direct URL) | Denied (not hidden nav) | ✅ 302 → `/account/denied`; "Access denied" page |
| **Staff** → `/admin/inbox` (direct URL) | Denied | ✅ 302 → `/account/denied` |
| **Admin** → Company Setup + Admin Inbox | Reachable, own company only | ✅ Nav links present; inbox scoped to Company A |
| **SuperAdmin** → across companies | Sees all companies | ✅ Dashboard shows Company B `009001` alongside Company A (§2) |

Verified both live (Chrome, real 403 pages) and via authenticated `curl` status codes.
(`/company-setup/users` returned 404 — that exact path is **not a route**; the real sub-routes are
protected at the parent. Not a hole.)

---

## 2. Multi-tenant isolation (two real companies) — ✅ PASS

| Direction | Test | Expected | Actual |
|---|---|---|---|
| A → B | Company-A Staff opens `LN-2026-009001` by URL | No access, no data | ✅ 200 **shell only**, "No application" — 0 Company-B markers (4.7 KB vs 23 KB for a real file) |
| A → B | Company-A dashboard | B app absent | ✅ Counts = Company A only (2 New; `009001` not present) |
| B → A | Company-B `b.sharma` dashboard | Only B apps | ✅ Shows `009001`, **zero** `005xxx` rows |
| B → A | `b.sharma` opens `005010` by URL | No access, no leak | ✅ Not-found shell; no "Nilesh Chavan" leak |
| B → A | `b.sharma` → `/admin/inbox` | Denied (Staff) | ✅ 302 → `/account/denied` |
| Super | SuperAdmin dashboard | Both companies | ✅ 16 apps across A + B |

Isolation is enforced by EF Core **global query filters** (company-scoped), so it is symmetric and a
forgotten `Where` cannot leak rows. SuperAdmin (CompanyId = null) correctly overrides the filter.
See finding **F4** on the 200-vs-404 status nuance (data is protected either way).

---

## 3. Critical path — CIBIL bypass loop — ✅ PASS (unblocks everything downstream)

Executed end-to-end on real blocked apps:

| Step | Expected | Actual |
|---|---|---|
| 1. Staff opens blocked `005013`, tries to advance | Blocked with correct messaging | ✅ Red banner "Blocked — CIBIL check required…", COMPLETE STAGE disabled, stages 2–8 LOCKED |
| 2. Staff submits CIBIL bypass | Request persisted | ✅ `adminrequest` row: `CibilBypass / Pending / usr-s-deshpande`; button → "Bypass requested — awaiting an Admin" |
| 3. **Admin inbox updates without manual refresh** | Live push | ✅ Inbox open in a 2nd tab went **Pending 1 → 2** live when a new bypass was submitted, no reload |
| 4. Admin approves | Gate clears, app advances | ✅ `adminrequest.Status=Approved` (reviewer `usr-r-kulkarni`); `005013.CibilGateStatus` flipped **Blocked → Bypassed**; red banner gone |
| 5. Post-bypass, next gate | Stage's own gate governs | ✅ Dedupe gate then ran on save: "✓ No match found — clear to proceed" |

**Real-time caveat (honest):** a single Chrome profile cannot hold two identities at once, so the
*observed live update* used a second **Admin** tab submitting on the admin's own blocked file
(`005014`). The notifier→inbox push path is fully exercised; the request that a **Staff** user raised
(`005013`) also appeared correctly in the Admin inbox (on navigation). The exact
"Staff-submits-while-Admin-watches-simultaneously" scenario is inferred from these two, not driven as
one simultaneous cross-user action. The notifier is in-process/single-instance by design (logged at
startup) — fine here, needs a SignalR backplane if ever scaled out.

Because Part 3 passed, Parts 4–6 proceeded normally.

---

## 4. Full 8-stage walkthrough on real data — ✅ PASS

Each stage opened on a real, already-progressed application; gate behaviour observed live where
interactive, confirmed in source otherwise.

| Stage | App | Real data rendered | Gate(s) verified |
|---|---|---|---|
| 1 Customer Details | `005013` | Deepak More, PAN, DOB, CV ₹14L | **Dedupe** ran on save → "clear to proceed" ✅; party tabs Applicant/Co-Applicant/Guarantor ✅; per-party **CHECK CIBIL** present ✅ |
| 2 Loan & Security | `005012`, `005010` | CV loan terms, viability tab | **Vehicle cap** — under-cap passes; **over-cap** set live (₹25L > ₹22L cap for *Tata Intra V50 2024*) → "exceeds the cap… Request admin bypass", COMPLETE STAGE disabled ✅ |
| 3 Bank & Financial | `005011` | SBI acct, holder name | **IFSC auto-fetch** = real external call: `HDFC0000001` → "HDFC Bank / Nariman Pt / Tulsiani Chambers" (live, not seeded) ✅; **fallback** = bad code → "Auto-fetch unavailable — enter manually", no crash ✅; penny-drop & statement-parse honestly "not configured" |
| 4 Document Checklist | `005010` | Per-party KYC rows, "4 collected · 22 pending" | Per-party tabs & tracking ✅; PAN/Aadhaar/Photograph shown as checklist rows |
| 5 Reports / RCU | `005005` | TransUnion vendor, case ref, report PDF | **Outcome gate**: "Recommended" derived from per-applicant outcomes → COMPLETE STAGE enabled ✅ (override path is the "Not recommended" branch, exercised by demo `005015`) |
| 6 Eligibility | `005006` | Live FOIR/LTV calc | **Live calc** ✅: Eligible ₹19,49,860 "Capped by FOIR" (FOIR ₹19.5L < LTV ₹22.7L); deviation −8.9% → **approver-note gate** required, COMPLETE STAGE disabled ✅ |
| 7 Approvals | `005005` | Business/TVR/Note/Charges tabs, real firm | **Two-step sanction** (confirm on tab → sub-header Sanction) ✅; **SoD** enforced (recommender ≠ approver, else sign-off incomplete → sanction blocked) ✅; **Send Back** ✅ (§6) |
| 8 Post Sanction | `005002` | Disbursement checklist, mandates | **Checklist gate**: "3 of 9 flags cleared (2 items pending definition)" → RELEASE FUNDS disabled ✅; Repayment / E-Nach / Security-Nach / MANDATES present ✅ |

All gates remain correctly enforced now that they are **stacked on the same file** (CIBIL → dedupe →
cap → RCU → eligibility → sanction → checklist). No gate was found bypassed or double-applied.

---

## 5. Cross-cutting regression — CIBIL retrofit — ✅ PASS

The CIBIL gate was retrofitted into every stage's completion handler after each stage was built.
Verified by reading all 8 call sites plus `GateCheckService`:

- **Single implementation.** `GateCheckService.CanAdvanceAsync` is the only CIBIL check; there are no
  per-stage copies to drift out of sync.
- **Clean guard, original logic intact.** Every handler (`CustomerDetails:989`, `LoanSecurity:1061`,
  `BankFinancial:890`, `DocumentChecklist:800`, `ReportsRcu:779`, `Eligibility:772`,
  `Approvals:988`, `PostSanction:877`) calls `CanAdvanceAsync` as the **first line**, returning early
  if blocked. The stage's **original** save + `CurrentStage = Math.Max(...)` advance + navigation runs
  **unchanged after** the guard — not replaced, not duplicated.
- **Stage-specific gates still fire.** e.g. Stage 2 runs the CIBIL guard **and then** its vehicle-cap
  gate (`CapGateAsync`) before persisting; Stage 1 runs the CIBIL guard then the dedupe/`BlockedReason`
  guard. The retrofit *added* a check; it removed nothing.
- **Defence in depth.** Each handler re-checks server-side even though the sub-header already disables
  the button ("a disabled button is a hint, not a guarantee").

No stage's pre-existing completion behaviour was broken, duplicated, or silently bypassed.

---

## 6. Send Back, Reject-as-request, Admin Inbox — ✅ PASS

**Send Back** (`005005`, from Approvals): sent to Stage 5.

| Check | Expected | Actual |
|---|---|---|
| Target stage reopens | Stage 5 screen live | ✅ Navigated to Reports/RCU, stepper `05 CURRENT`, 6–8 re-locked |
| `CurrentStage` reset | 7 → 5 | ✅ DB `CurrentStage=5` |
| Audit | SendBackLog written | ✅ `From 7 → To 5`, reason recorded |
| Sanction re-armed | must re-confirm | ✅ `SanctionConfirmed` reset to 0 |

**Reject-as-request** (raised as **Staff** on two files, decided as **Admin**):

| Check | Expected | Actual |
|---|---|---|
| Submit does **not** change status | App unchanged | ✅ `005012` stayed *In progress*, `005011` stayed *In progress*; banner "unchanged until then" |
| Appears in Admin Inbox | Visible, scoped | ✅ Both listed, raised-by S. Deshpande |
| **Approve** one (`005012`) | Status=Rejected + RejectionLog | ✅ `Status=Rejected` (stage preserved at 2); RejectionLog written with reason; request `Approved` |
| **Deny** the other (`005011`) | App unaffected, denial visible | ✅ `Status` unchanged (In progress/3); request `Denied` with note; officer sees "Your rejection request was denied — …" |

---

## 7. Findings

No **Critical** issues. No functional breakage was found — every feature behaved correctly against
real data. The findings below are control-design and consistency observations.

### F1 — Test data did not match the assumed "current state" · **Major (process, not code)**
- **Where:** database / hand-off, not source.
- **Detail:** see §0.1. The live DB was the fresh demo seed; the described prior-session data was
  absent, so several Part-0 target states had to be *created* during testing and a reset was needed
  to obtain credentials and a second company.
- **Likely cause:** prior "self-verified in separate sessions" work ran against a different or
  since-reset database; nothing persisted into this one.
- **Action:** none in code. Establish a persistent, snapshotted test dataset if this suite is to be
  re-run against "current" data.

### F2 — No segregation-of-duties on admin-request approval · **Major**
- **Where:** `Components/Shared/StageSubHeader.razor` → `ReviewAsync` (~line 332–355).
- **Detail:** The single approve/deny handler for **CIBIL bypass, vehicle-cap bypass, and Reject**
  requests sets `ReviewedByUserId = Tenant.UserId` with **no check that the reviewer differs from the
  requester**. An Admin/SuperAdmin who raises a request can approve their own. Observed: `005014`'s
  bypass was raised by Admin *R. Kulkarni*, who also holds approval rights over it.
- **Contrast:** the *sanction* path explicitly enforces recommender ≠ approver
  (`Approvals.razor:810 SameSignatory`). The admin-request path — which is the whole point of the
  "admin-approved gate" control — does not.
- **Mitigation:** only Admin/SuperAdmin can approve, so a pure Staff user still cannot self-approve;
  the intended flow (Staff raises, Admin approves) has two people. The gap is Admins who also work
  files.
- **Likely cause:** SoD was designed for sanction only; not carried into `ReviewAsync`.
- **Verified:** by source (no requester comparison) + circumstantial live evidence.

### F3 — Bypass requests capture no requester justification · **Minor**
- **Where:** `StageSubHeader.razor` → `RequestBypassAsync` (~line 319–322).
- **Detail:** A CIBIL/vehicle-cap bypass request's reason is auto-filled with the canned block
  message (`reason: gate.BlockedReason`); the requester types nothing. **Reject** requests, by
  contrast, require a typed reason. The audit trail therefore records *that* a file was blocked, not
  *why* the officer believes a bypass is warranted.
- **Likely cause:** deliberate "same sentence the officer read" choice; acceptable for CIBIL (no data
  yet) but weak as an approval-justification record.

### F4 — Cross-tenant app URL returns 200 (empty) rather than 404 · **Minor**
- **Where:** stage screens via `LosDbContext` global filter; contrast `Program.cs` `/files` endpoint.
- **Detail:** A user hitting another company's application URL gets **HTTP 200** with a "No
  application" shell. No data leaks (confirmed), but the `/files` PII endpoint deliberately returns
  **404** on a miss so as "not to confirm that the application exists." The stage screens are
  inconsistent with that least-disclosure stance.
- **Likely cause:** the filtered query returns null → the page renders its empty state at 200 instead
  of raising a 404.

### F5 — Stepper shows completed forward stages as "UNLOCKED" when revisiting · **Minor (cosmetic)**
- **Where:** stage stepper (`Components/Shared`).
- **Detail:** opening an earlier stage on a further-along file (e.g. Stage 2 on a Stage-4 file)
  relabels the already-`DONE` forward stages as `UNLOCKED`. No data change; purely visual.

### F6 — Interactive controls inert until the SignalR circuit connects · **Minor (informational)**
- **Detail:** buttons (bypass, complete-stage, submit, sign-in) silently do nothing if clicked in the
  ~2–4 s before the Blazor Server circuit attaches on page load. Inherent to Blazor Server, but a
  real user clicking immediately gets no feedback. Consider a "connecting…" affordance / disabled
  state until interactive.

---

## 8. Severity summary

| Severity | Count | Items |
|---|---|---|
| **Critical** | **0** | — |
| **Major** | 2 | F1 (process/data), F2 (SoD on request approval) |
| **Minor** | 4 | F3, F4, F5, F6 |

**Bottom line:** the core workflow is sound. The CIBIL bypass loop — the precondition for everything
downstream — passes end-to-end with live real-time delivery; all eight stages, every gate, send-back,
and the reject-as-request lifecycle behave correctly against real data; and the CIBIL retrofit is a
clean, single-implementation guard that left each stage's original logic intact. **Zero Critical
findings.** The one code-level item worth a decision before client hand-off is **F2** (self-approval
of admin-approved gates); everything else is minor consistency/UX polish.

---

## Appendix — state changed during testing (product actions only, for traceability)
- DB dropped/rebuilt/reseeded with isolation fixture (authorised).
- Passwords for the 5 accounts changed to a known test value at forced first-login.
- `005013` — CIBIL bypass **approved** (`CibilGateStatus=Bypassed`).
- `005014` — CIBIL bypass **pending** (raised, left un-approved).
- `005012` — **Rejected** via approved reject request (RejectionLog written).
- `005011` — reject request **denied** (application unaffected).
- `005005` — **sent back** 7 → 5.
- Unsaved form edits on `005010` (loan amount) and `005011` (IFSC) were **discarded**, not persisted.
- App left running at `http://localhost:5037` (HTTP-only profile, to avoid the dev-cert interstitial).
