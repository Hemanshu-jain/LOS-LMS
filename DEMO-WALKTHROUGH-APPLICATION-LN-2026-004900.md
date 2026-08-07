# Demo Walkthrough — Application `LN-2026-004900`

> Complete end-to-end demonstration of the LOS/LMS lifecycle for a fresh application. Every field is filled, every gate is exercised, and the application is carried all the way from creation through disbursement.

---

## Pre-flight

- **Confirmed no collision** with existing seeded IDs. Database range is `LN-2026-004795` through `LN-2026-004875`. This demo uses `LN-2026-004900`.
- All names, PANs, Aadhaar numbers, IFSC codes, account numbers, mobile numbers, and case references below are fabricated for demonstration only.
- **Officer roster** (from `Officers` table): R. Kulkarni, S. Deshpande, A. Rao.

---

## At a glance

| Field | Value |
|---|---|
| Application ID | `LN-2026-004900` |
| Customer | Hemant Bhalerao |
| Branch | Nashik West |
| Loan product | Commercial vehicle |
| Scheme | CV-STD-2026 |
| Loan amount | ₹14,75,000 |
| Tenure | 36 months |
| ROI | 13.50% |
| Status (start → end) | New → In progress → Sanctioned |
| Officer | R. Kulkarni |
| Parties | Applicant + Co-Applicant + Guarantor (full three-party) |
| RCU mode | Screened, with **one party returning Not recommended** to exercise the override gate |

---

## How to start

```bash
cd D:\Hemanshu\PERSNOAL\LMS\LosLms
dotnet run
```

Open <http://localhost:5037/applications> and click **+ New application** (top right). The application ID `LN-2026-004900` is assigned on click and you land on Stage 1.

---

# Stage 1 — Customer Details *(Gated)*

The Stage 1 gate requires the Applicant's Personal (8) + Contact (10) sections fully filled and dedupe = Pass. Co-applicant and guarantor only block if you touch them at all — leave a tab empty and it is ignored.

## 1.1 Applicant tab

### Personal information — 8 fields (all required)

| Field | Enter |
|---|---|
| Full name | Hemant Bhalerao |
| Date of birth | 1989-11-04 |
| Gender | Male |
| Marital status | Married |
| Father / spouse name | Dattatray Bhalerao |
| Customer category | Individual |
| Nationality | Indian |
| Mother tongue | Marathi |

### Identity — PAN + Aadhaar

| Field | Enter |
|---|---|
| PAN | `BLRPM4621H` |
| Aadhaar | `478219635021` |

> Dedupe runs on save. This PAN/Aadhaar combination does not exist in the database, so the dedupe check will read **Pass**.

### Contact details — 10 fields (all required)

| Field | Enter |
|---|---|
| Mobile | `9876543210` |
| Alternate mobile | `9123456780` |
| Email | `hemant.bhalerao@example.in` |
| Address line 1 | Flat 7, Saptashringi Apartments |
| Address line 2 | Behind CIDCO Bus Stand |
| City | Nashik |
| State | Maharashtra |
| PIN code | `422009` |
| Residence type | Owned |
| Years at address | 6 |

> Alternate mobile and address line 2 are required. They look optional and they are not — the most likely place to get stuck.

### Employment details — 6 fields

| Field | Enter |
|---|---|
| Employment type | Self-employed |
| Employer / Business name | Bhalerao Transport |
| Designation / Nature of business | Proprietor |
| Monthly income | `85000` |
| Years in current job / business | 8 |
| Office / business address | Shop 3, Dwarka Corner, Nashik |

**Click *Save draft*.** Section collapses. Dedupe card turns green: *"✓ No match found — clear to proceed"*.

## 1.2 Co-Applicant tab

| Field | Enter |
|---|---|
| Full name | Vaishali Bhalerao |
| DOB | 1991-02-18 |
| Gender | Female |
| Marital status | Married |
| Father / spouse name | Madhav Kulkarni |
| Customer category | Individual |
| Nationality | Indian |
| Mother tongue | Marathi |
| PAN | `KVLPB7812F` |
| Aadhaar | `912487650134` |
| Mobile | `9988776655` |
| Alt mobile | `9876501234` |
| Email | `vaishali.b@example.in` |
| Address line 1 | Flat 7, Saptashringi Apartments |
| Address line 2 | Behind CIDCO Bus Stand |
| City | Nashik |
| State | Maharashtra |
| PIN code | `422009` |
| Residence type | Owned |
| Years at address | 6 |
| Employment type | Self-employed |
| Employer | Bhalerao Transport (Partner) |
| Designation | Partner |
| Monthly income | `42000` |
| Years in job | 5 |
| Office address | Shop 3, Dwarka Corner, Nashik |

**Save draft.** Dedupe: Pass.

## 1.3 Guarantor tab

| Field | Enter |
|---|---|
| Full name | Prakash Joshi |
| DOB | 1972-07-22 |
| Gender | Male |
| Marital status | Married |
| Father / spouse name | Vishnu Joshi |
| Customer category | Individual |
| Nationality | Indian |
| Mother tongue | Hindi |
| PAN | `JSPK3117K` |
| Aadhaar | `654231987045` |
| Mobile | `9090909090` |
| Alt mobile | `9090909091` |
| Email | `prakash.joshi@example.in` |
| Address line 1 | 12, Vrindavan Colony |
| Address line 2 | Near Mumbai Naka |
| City | Nashik |
| State | Maharashtra |
| PIN code | `422002` |
| Residence type | Owned |
| Years at address | 14 |
| Employment type | Salaried |
| Employer | Maharashtra State Electricity Board |
| Designation | Senior Clerk |
| Monthly income | `65000` |
| Years in job | 22 |
| Office address | MSEB Sub-Division, Nashik |

**Save draft.** Dedupe: Pass.

**Click *Complete stage →*.** Application auto-advances to Stage 2.

> The three-party setup means Stage 4 shows 3 tabs and 39 document slots, and Stage 5 must collect an outcome for every party — exercising the gate that blocks completion when any party is left Pending.

---

# Stage 2 — Loan & Security *(Not gated)*

Four inner tabs. Fill each, then complete.

## 2.1 Loan Details — 10 fields

| Field | Enter |
|---|---|
| DSA / sourcing channel | DSA — Shree Associates |
| Sourcing branch | Nashik West |
| Scheme | CV-STD-2026 |
| Requested amount | `1475000` |
| Tenure (months) | 36 |
| ROI % | 13.50 |
| Processing fee | `22125` *(1.5% of loan amount)* |
| Advance EMI | 1 |
| Repayment mode | NACH |
| Expected disbursal date | 2026-09-15 |

**Live in summary rail:** EMI works out to **₹49,847/month**.

## 2.2 Security Details — 12 fields

| Field | Enter |
|---|---|
| Make / model | Tata LPT 1109 HEXA |
| Year | 2026 |
| Registration no. | MH-15-FT-4421 |
| Chassis no. | MAT552301K7H91208 |
| Engine no. | 497TC49EEX10234 |
| Invoice no. | TML/INV/2026/09211 |
| Invoice date | 2026-08-02 |
| Invoice value | `1850000` |
| Insurer | ICICI Lombard |
| Policy no. | 3001/27891441/2026 |
| Policy expiry | 2027-08-01 |
| **Assessed value** | `1750000` |

> Assessed value drives Stage 3's LTV. ₹14,75,000 / ₹17,50,000 = **84.3% LTV** — caution band.

## 2.3 Reference Details

Two starter rows. Fill both, or leave blank — blank rows are dropped on save.

**Reference 1**

| Field | Enter |
|---|---|
| Name | Suresh Wagh |
| Relation | Family friend |
| Mobile | `9001122334` |
| Address | Indira Nagar, Nashik |

**Reference 2**

| Field | Enter |
|---|---|
| Name | Lata Patil |
| Relation | Business associate |
| Mobile | `9001122335` |
| Address | College Road, Nashik |

## 2.4 Viability

| Field | Enter |
|---|---|
| Total monthly income | `192000` *(85000 + 42000 + 65000 guarantor)* |
| Household expenses | `28000` |
| Fuel / driver | `15000` |
| Existing EMIs | `12500` |

**Click *Complete stage →*.** Stage 3 opens.

---

# Stage 3 — Bank & Financial *(Not gated)*

## 3.1 Bank Details tab

| Field | Enter |
|---|---|
| **IFSC** | `HDFC0004412` ← type this FIRST; bank name auto-fills as "HDFC Bank · Auto-detected from IFSC" |
| Account number | `50100281144772` |
| Account type | Current |
| Account holder name | Bhalerao Transport |
| Banking vintage | 5+ years |

> Statement upload and penny-drop both show "Not configured" — honest, not broken. No integrations are connected in this build.

## 3.2 CAM tab

Click *Generate CAM.pdf*. CAM downloads with header, cost breakdown, sanction summary, and full repayment schedule.

**Click *Complete stage →*.** Stage 4 opens.

---

# Stage 4 — Document Checklist *(Not gated)*

13 document types × 3 parties = 39 slots. Aggregate line shows **"Across all parties: 0 collected · 39 pending"**.

## Fast pass — just click *Complete stage →*

If you want to exercise it, upload PAN / Aadhaar / Photo for each party. The easiest path:

- **Applicant** tab → Bulk upload → drop three PDFs.
- **Co-Applicant** tab → same.
- **Guarantor** tab → same.

> PAN, Aadhaar and Photograph write back to the Party record, not a separate Document row, so Stage 1 also shows them as Collected if you upload here.

**Click *Complete stage →*.** Stage 5 opens.

---

# Stage 5 — Reports (RCU) *(Gated, hard — this is the demo's centerpiece)*

On arrival, **RCU-2026-00005** is generated automatically. Outcomes are created for all three parties (Applicant, Co-Applicant, Guarantor). Overall status starts **Pending**; *Complete stage* is disabled.

> The button on the left is "Re-submit to vendor", not "Save draft". Stage 5 has no draft save. Your edits reach the database only when you complete the stage, re-submit, or upload a report. Do not fill this screen in and walk away.

## 5.1 RCU initiation

| Field | Enter |
|---|---|
| Mode | Screened |
| Branch | Nashik West |
| Vendor | Verified Field Services |
| Initiation date | 2026-08-07 |
| TAT days | 5 |

## 5.2 Per-applicant outcomes — the gate exercise

Set each party's outcome. **Two Recommended, one Not recommended** — this is what trips the override gate so you can demonstrate it.

| Party | Status | Verified on | Verified by | Remarks |
|---|---|---|---|---|
| Applicant — Hemant Bhalerao | **Recommended** | 2026-08-09 | R. Kulkarni | Clean CIBIL, no adverse media |
| Co-Applicant — Vaishali Bhalerao | **Recommended** | 2026-08-09 | R. Kulkarni | Address confirmed via neighbour |
| Guarantor — Prakash Joshi | **Not recommended** | 2026-08-10 | R. Kulkarni | One civil suit pending at Nashik court since 2024 |

**Overall status flips red.** *Complete stage* is now disabled. A red override panel appears. To unlock the button you need **both**:

| Override field | Enter |
|---|---|
| Override reason | Guarantor civil suit is for a recovery of ₹2.4L by a former business partner; case is at the evidence stage, no decree yet, no impact on MSEB salary or primary borrower. Recommended to proceed with enhanced monitoring and a clause in the loan agreement indemnifying the lender. Risk rated acceptable. |
| Approving officer | S. Deshpande |

**Both fields filled → *Complete stage →* goes live. Click it.**

## 5.3 Upload report (optional, exercises the file endpoint)

Drop a PDF in the *Report* panel — upload is the save, no separate button. *View* opens it in a modal with a working download link.

**Done. Stage 5 closes. You return to dashboard.**

---

# Stage 6 — Eligibility *(Gated)*

LTV: 84.3% (caution band). FOIR cap = `192000 × 50% − 12500 = 83,500` available EMI → principal at 13.5%/36mo ≈ ₹24,02,000. LTV cap = `1750000 × 85% = ₹14,87,500`. **Binding constraint: LTV.**

- Eligible amount: **₹14,75,000** (matches requested — zero deviation)
- Proposed EMI at eligible: ~₹49,847

Since deviation is **0%**, no approver note is required. The *Approver note* field stays empty.

**Click *Complete stage →*.**

---

# Stage 7 — Approvals *(Gated, two-step)*

Four inner blocks. Fill all four before *Confirm sanction* enables, then *Sanction* appears.

## 7.1 Business Details

| Field | Enter |
|---|---|
| Constitution | Proprietorship |
| Trade name | Bhalerao Transport |
| Years in business | 8 |

## 7.2 TVR (Tele-Verification)

| Field | Enter |
|---|---|
| Status | Positive - Confirmed |
| Verified by | R. Kulkarni |
| Date | 2026-08-12 |
| Remarks | Spoke with applicant, employment and business confirmed |

## 7.3 Approval Note

> Standard recommendation. FOIR acceptable at 32%, LTV at 84% within risk appetite, banking conduct clean. Civil suit on guarantor is sub-material (₹2.4L pending recovery, no decree). RCU override approved by S. Deshpande. Recommend sanction.

## 7.4 Approver & Recommender

| Field | Enter |
|---|---|
| Recommender | R. Kulkarni (Credit Analyst) |
| Approver | **S. Deshpande** (Senior Credit Analyst) — must differ from recommender |

**Sanctioned terms** (default from requested):

| Field | Enter |
|---|---|
| Sanctioned amount | `1475000` |
| Sanctioned ROI | 13.50 |
| Sanctioned tenure | 36 |

**Click *Confirm sanction* → then *Sanction →*.**

`Status` flips to **Sanctioned**. Stage 8 opens.

---

# Stage 8 — Post Sanction *(Gated, hard — known-incomplete)*

Seven of nine checklist rows are defined. Two items are pending definition per `OPEN-QUESTIONS-FOR-ARUN.md §0` — header reads **"7 of 9 flags cleared (2 items pending definition)"**.

## 8.1 Checklist — clear all 7 defined rows

| Row | Set to |
|---|---|
| Sanction letter signed & filed | Cleared |
| KYC documents complete | Cleared |
| E-Nach registration | Cleared |
| Security Nach mandate | Cleared |
| PDD (Pre-Disbursement Document) — Invoice | Cleared |
| PDD — Insurance policy | Cleared |
| PDD — RTO registration copy | Cleared |

## 8.2 Disbursement

| Field | Enter |
|---|---|
| Disbursement date | 2026-08-25 |
| Disbursement account | `50100281144772` (from Stage 3) |
| Amount disbursed | `1435125` *(gross loan − charges)* |

**Click *Release funds →*.**

`Disbursed = true`. Application is done.

---

# Verifying it worked

```sql
SELECT Id, Status, CurrentStage, Disbursed, LoanAmount, CustomerName
FROM Applications WHERE Id = 'LN-2026-004900';
```

Expected: `Status = Sanctioned`, `CurrentStage = 8`, `Disbursed = 1`, `LoanAmount = 1475000`, `CustomerName = Hemant Bhalerao`.

```sql
SELECT PartyType, FullName, DedupeStatus FROM Parties WHERE ApplicationId = 'LN-2026-004900';
```

Expected: 3 rows — Applicant / CoApplicant / Guarantor, all `DedupeStatus = Pass`.

```sql
SELECT PartyType, Status, VerifiedByOfficerId, IsOverride FROM RcuOutcomes
WHERE ApplicationId = 'LN-2026-004900';
```

Expected: 3 rows, one with `Status = Not recommended`, `IsOverride = 1`, approver set.

---

# What this demo exercises that a minimal pass would not

| Coverage | Why this case |
|---|---|
| 3-party KYC | Guarantor tab appears only when a guarantor has a full name — confirms derivation works |
| Dedupe gate | All three PANs/Aadhaars fresh, so Pass on all three |
| LTV caution band | 84.3% — yellow, not green — proves the band logic |
| RCU override flow | One Not recommended forces both override reason AND approver — exercises the hardest gate |
| Stage 6 zero-deviation | Confirms the no-approver-note path, then Stage 7 still requires a written approval note |
| Full eight-stage lifecycle | CurrentStage lands at 8, Status at Sanctioned, Disbursed = true |
| Distinct ID space | `LN-2026-004900` sits in the gap between 004875 (last walked-through) and the next available number — no seed collision, no dashboard clutter from a duplicate |

---

# Known quirks (not bugs)

**Unstyled page after a CSS change.** Chrome cached the old stylesheet. Hard-reload with **Ctrl+Shift+R**.

**Second download silently does nothing.** Chrome blocks repeated automatic downloads from the same origin. The first CAM.pdf or report download works, later ones are dropped without any error. Allow automatic downloads for localhost in the site settings, or just reload the page.

**Always lands on Stage 1.** Opening any application from the dashboard always lands on Stage 1, whatever stage it is really at. To get back to an in-flight file's current screen either click *Complete stage* through the earlier screens again (safe — `CurrentStage` only ever moves forward) or edit the URL directly:

```
http://localhost:5037/applications/LN-2026-004900/customer-details
http://localhost:5037/applications/LN-2026-004900/loan-security
http://localhost:5037/applications/LN-2026-004900/bank-financial
http://localhost:5037/applications/LN-2026-004900/document-checklist
http://localhost:5037/applications/LN-2026-004900/reports-rcu
```

---

# What this demo does not exercise

- PDD expected-date chase (would require waiting past the date)
- 90-day address-proof staleness (would require waiting 90 days)
- Co-applicant or guarantor bank account (the model has only one account per application)
- Stage 6 negative deviation (this case hits 0% deviation so no approver note is required)
- The two undefined Post Sanction checklist items (per `OPEN-QUESTIONS-FOR-ARUN.md §0`)

Add any of these only if the demo needs to touch them.

---

*Generated for demonstration of the LOS/LMS lifecycle. Application ID `LN-2026-004900` is in the free ID range and does not conflict with seeded data.*