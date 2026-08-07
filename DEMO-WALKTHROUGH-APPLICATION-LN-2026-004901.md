# Demo Walkthrough 2 — Application `LN-2026-004901`

> A complete, beginner-friendly tutorial for a fresh application. Every click, every keystroke, and every decision is explained in plain language. Designed to take someone who has never opened the app before through the entire lifecycle.

---

## Before you start — what you need

1. The app must already be running. If it isn't, open a terminal and type:

   ```bash
   cd D:\Hemanshu\PERSNOAL\LMS\LosLms
   dotnet run
   ```

2. Your browser should be open at <http://localhost:5037/applications>. If you see the dashboard, you're ready.

3. **Time required:** approximately 30–45 minutes if you fill every field carefully, 10–15 minutes if you skim.

---

## How this guide works

Each stage has three parts:

- **What this stage does** — a plain-English description of why the stage exists and what it captures.
- **What to fill** — every field, with the exact value to type.
- **What to watch for** — the buttons that sometimes refuse to click, and what to do when they do.

When you see a box like this:

```
> Click: [Save draft]
```

It means: stop typing, find the button labelled exactly that, and click it once. Then keep reading.

---

## At a glance — the whole application

| Field | Value |
|---|---|
| Application ID | `LN-2026-004901` |
| Customer | Priya Deshpande |
| Branch | Pune Camp |
| Loan product | Loan against property (LAP) |
| Scheme | LAP-STD-2026 |
| Loan amount | ₹32,50,000 |
| Tenure | 60 months |
| ROI | 12.75% |
| Status (start → end) | New → In progress → Sanctioned → Disbursed |
| Officer | S. Deshpande |
| Parties | Applicant only (no co-applicant, no guarantor) — simpler than Guide 1 |

> **Why a two-party case this time?** Guide 1 used three parties. This one uses just the applicant to show how the app behaves when a file is genuinely single-borrower — fewer tabs, fewer documents, fewer RCU outcomes. Both paths are normal.

---

# Part A — Getting started

## A.1 Create a new application

1. Look at the top-right of the dashboard. There is a button labelled **+ New application**.
2. Click it once.
3. The browser navigates to a new screen. At the top of that screen you will see `← Applications · APP #LN-2026-004901`. That confirms the application was created.
4. You are now on **Stage 1 — Customer Details**.

> The application ID `LN-2026-004901` is generated automatically by the system. You don't type it; you just see it appear. Today's "today" used throughout is 2026-08-07.

---

# Stage 1 — Customer Details

## What this stage does

This is where the system learns who is borrowing the money. It captures the applicant's personal details, contact information, employment, and identity documents (PAN and Aadhaar). When you finish this stage, the system knows who the customer is well enough to run a **dedupe check** — a check that makes sure this person isn't already in our database with a different application.

There are three tabs at the top of the form: **Applicant**, **Co-Applicant**, and **Guarantor**. Each tab is its own complete record. For this demo we will only fill the Applicant tab.

## What to fill — Applicant tab

The form is split into four sections. Fill each in order.

### Section 1: Personal information

Click on each field and type the value below.

| Field | Type this |
|---|---|
| Full name (as per PAN) | Priya Deshpande |
| Date of birth | 1985-06-22 |
| Gender | Female |
| Marital status | Married |
| Father / spouse name | Ramesh Deshpande |
| Customer category | Individual |
| Nationality | Indian |
| Mother tongue | Marathi |

When you click out of the last field, watch the section header. It will collapse itself and the summary line will read "8 fields · complete". That is the system telling you the section is done.

### Section 2: Identity

To the right of the Personal section there is a smaller panel titled **Identity · OCR verify**. Fill two values there:

| Field | Type this |
|---|---|
| PAN | `DSPP7219M` |
| Aadhaar | `782143659012` |

> Don't worry about the **Verify OCR** button or the file upload slots — those need integrations that aren't connected in this build. Clicking them shows "PAN verification unavailable — API key not configured", which is the system being honest, not broken.

### Section 3: Contact details

| Field | Type this |
|---|---|
| Mobile | `9765432108` |
| Alternate mobile | `9765432109` |
| Email | `priya.deshpande@example.in` |
| Address line 1 | 14, Sahyadri Heights |
| Address line 2 | Karve Nagar |
| City | Pune |
| State | Maharashtra |
| PIN code | `411052` |
| Residence type | Owned |
| Years at address | 12 |

> **Important:** Alternate mobile and Address line 2 look like they could be left blank. They cannot. If either is empty, the *Complete stage* button will refuse to enable. This is the most common reason people get stuck on Stage 1.

### Section 4: Employment details

| Field | Type this |
|---|---|
| Employment type | Self-employed |
| Employer / Business name | Deshpande Foods |
| Designation / Nature of business | Proprietor |
| Monthly income | `125000` |
| Years in current job / business | 11 |
| Office / business address | 14, Sahyadri Heights, Karve Nagar, Pune |

The Employment section collapses with "6 fields · complete" when you finish it.

## Save and complete

Now you need to do two things in order:

> Click: [Save draft]

Watch the right-hand panel titled **Gates**. It has a coloured box called **Dedupe check**. After you click Save draft, that box should turn green and read:

> ✓ No match found — clear to proceed

If you see that, dedupe passed. The PAN and Aadhaar we used (`DSPP7219M` and `782143659012`) do not match any existing customer in the database.

> Click: [Complete stage →]

The browser moves to **Stage 2 — Loan & Security** automatically.

## What to watch for

- **The button looks disabled (greyed out).** Hover over it without clicking. A tooltip will appear with the exact reason. Common reasons and fixes:
  - *"Applicant: personal information is incomplete"* — count the personal fields; all 8 must have values.
  - *"Applicant: contact details are incomplete"* — most often a missing Alternate mobile or Address line 2.
  - *"Applicant: save the draft to run the dedupe check"* — you skipped Save draft.
- **Save draft does nothing visible.** That is normal until the first time you press it. After pressing it once, every subsequent edit will save silently.

---

# Stage 2 — Loan & Security

## What this stage does

This is where the loan itself is described: how much is being borrowed, for how long, at what rate, against what security. The screen has four inner tabs at the top:

- **Loan Details** — the loan amount, tenure, rate, fees.
- **Security Details** — the asset being used as collateral (vehicle or property).
- **Reference Details** — two people who can vouch for the borrower.
- **Viability** — income versus expenses, used to calculate FOIR (Fixed Obligations to Income Ratio).

You need to visit all four tabs. Stages 3 and 5 have nothing to show if you skip this stage.

## 2.1 Loan Details tab

You are on this tab by default. Fill the 10 fields:

| Field | Type this |
|---|---|
| DSA / sourcing channel | Branch walk-in |
| Sourcing branch | Pune Camp |
| Scheme | LAP-STD-2026 |
| Requested amount | `3250000` |
| Tenure (months) | 60 |
| ROI % | 12.75 |
| Processing fee | `48750` *(1.5% of loan amount)* |
| Advance EMI | 1 |
| Repayment mode | NACH |
| Expected disbursal date | 2026-09-30 |

While you type, watch the right-hand **Application Summary** panel. The amount, tenure, and ROI update as you type. The EMI calculation runs and shows **₹74,267/month** at the bottom.

> **What is FOIR?** It's the percentage of monthly income that goes to loan EMIs. Lenders cap it (usually around 50%) so the borrower isn't over-committed. We will compute it on the Viability tab.

## 2.2 Security Details tab

This tab has a small toggle at the top: **Asset type** with two choices — Vehicle and Property. Because this is a Loan Against Property, pick **Property**.

| Field | Type this |
|---|---|
| Property type | Residential flat |
| Property address | Flat 402, Karve Nagar, Pune 411052 |
| Area (sq ft) | `1180` |
| Ownership type | Freehold |
| Sale deed no. | SD-2018/PUNE/00934 |
| Valuation ref no. | VR/2026/CBB/1172 |
| Encumbrance ref | EC/2026/PUNE/KARVE/441 |
| **Assessed value** | `3850000` |

> Assessed value is the number Stage 6's LTV calculation uses. ₹32,50,000 ÷ ₹38,50,000 = **84.4% LTV** — caution band. The system will show this on Stage 3's CAM tab.

## 2.3 Reference Details tab

Two starter rows are already there. Fill both. Blank rows are dropped on save, so it's safe to leave one empty if you want — but for this demo, fill both.

**Reference 1**

| Field | Type this |
|---|---|
| Name | Vikram Kulkarni |
| Relationship | Brother |
| Mobile | `9876512340` |
| Address | Aundh, Pune |
| Known since | 2010 |

**Reference 2**

| Field | Type this |
|---|---|
| Name | Anjali Thakur |
| Relationship | Business associate |
| Mobile | `9876512341` |
| Address | Koregaon Park, Pune |
| Known since | 2015 |

## 2.4 Viability tab

| Field | Type this |
|---|---|
| Total monthly income | `125000` |
| Household expenses | `32000` |
| Fuel / driver | `0` |
| Existing EMIs | `8500` |

The tab calculates FOIR automatically: `(74,267 + 8,500) ÷ 125,000 × 100 = 66.2%` — risk band (over 60%). Don't worry about that; this is context, not a hard fail.

## Complete the stage

> Click: [Complete stage →]

The browser moves to **Stage 3 — Bank & Financial**.

---

# Stage 3 — Bank & Financial

## What this stage does

This stage captures where the money will be disbursed and where EMI repayments will come from. It has two inner tabs: **Bank Details** and **CAM** (Credit Appraisal Memo — a PDF the system generates).

## 3.1 Bank Details tab

You will see five fields: IFSC, Account number, Account type, Account holder name, Banking vintage.

> **The trick:** type the IFSC first. Don't pick the bank from the dropdown. When you type a recognised IFSC, the system replaces the bank dropdown with a read-only field captioned "Auto-detected from IFSC". If you pick the bank first, the IFSC will redo your work.

| Field | Type this |
|---|---|
| **IFSC** | `ICIC0000451` ← type this first |
| Account number | `624501234567` |
| Account type | Savings |
| Account holder name | Priya Deshpande |
| Banking vintage | 5+ years |

> The system only knows 12 bank prefixes — HDFC, ICIC, SBIN, UTIB, BARB, PUNB, KKBK, YESB, IDFB, INDB, CNRB, UBIN. Any other IFSC falls back to a manual dropdown. ICIC works, so we get the auto-detect.

You'll see two extra panels below the form: **Statement upload** and **Penny-drop verification**. Both will say "Not configured". This is honest, not broken — no banking integrations are connected in this build.

## 3.2 CAM tab

Click on the **CAM** tab at the top.

> Click: [Generate CAM.pdf]

A PDF downloads (or tries to — see the Chrome quirk at the bottom of this guide). The CAM contains the header, cost breakdown, sanction summary, and a full repayment schedule for 60 months.

> Click: [Complete stage →]

The browser moves to **Stage 4 — Document Checklist**.

---

# Stage 4 — Document Checklist

## What this stage does

This stage tracks the 13 documents each party needs to provide. With only the Applicant populated, you will see one tab and 13 document rows.

The 13 document types are:

1. PAN card
2. Aadhaar (masked)
3. Photograph
4. Signature proof
5. Address proof
6. Income proof — ITR / GST
7. Stability proof
8. Utility proof
9. Ownership proof
10. Existing loan statement
11. Trade licence / permit
12. Route / trip sheets
13. Guarantor documents

The aggregate line at the top reads **"Across all parties: 0 collected · 13 pending"**.

## Fastest path

Nothing on this stage blocks completion. The demo can move on without uploading anything.

> Click: [Complete stage →]

## Optional — exercising the upload

If you want to see how uploads work:

1. Click the **Applicant** tab (it's the only one).
2. The first three rows — PAN, Aadhaar, Photograph — are special: they pull live from Stage 1. If you upload them here, they write back to the Applicant's record on Stage 1.
3. Click any **Upload** slot. A file picker opens. Pick any PDF.
4. The row's status changes from **Pending** to **Collected**, the upload date stamps, and the row turns green.

> PAN, Aadhaar, and Photograph uploaded here also appear as Collected on Stage 1's Identity panel. That's by design — there is only one source of truth.

**Address proof** is the only type that goes **Stale** — 90 days after upload, the system marks it Stale. That's the only doc that can show Stale. We won't wait 90 days for the demo.

> Click: [Complete stage →]

The browser moves to **Stage 5 — Reports (RCU)**.

---

# Stage 5 — Reports (RCU) — the gated one

## What this stage does

RCU stands for **Reports — Credit Underwriting** (sometimes also called field verification). It is when an external agency verifies the information the borrower gave us: address confirmation, employment check, civil-records check, neighbour interview, etc.

This is the **first gate that will actually stop you** if you don't fill it correctly. The system requires:

1. Every party's outcome to be set to **Recommended** or **Not recommended** (not Pending).
2. If any party is **Not recommended**, the system demands **both** a written override reason **and** a named approving officer.

There is also no "Save draft" button on this screen. The button that looks like one — labelled **Re-submit to vendor** — actually starts a new RCU round. Your edits reach the database only when you click **Complete stage** or upload a report.

On arrival, the system has already:
- Generated an RCU case reference: **`RCU-2026-00006`**
- Created outcome rows for the one visible party (Applicant)
- Pulled the party name from Stage 1 ("Priya Deshpande")
- Set overall status to **Pending**
- Disabled the *Complete stage* button

## 5.1 RCU initiation

| Field | Type this |
|---|---|
| Mode | Screened |
| Branch | Pune Camp |
| Vendor | TransUnion CIBIL RCU |
| Initiation date | 2026-08-07 |
| TAT days | 4 |

## 5.2 Per-applicant outcome

| Field | Set to |
|---|---|
| Party | Applicant — Priya Deshpande |
| Status | **Recommended** |
| Verified on | (auto-stamps to today, 2026-08-09) |
| Verified by | S. Deshpande |
| Remarks | Address confirmed at site, employer verified, no civil/criminal records found |

> As soon as you change Status from Pending to Recommended, the **Verification date** stamps itself automatically. Set Status back to Pending and the date clears. That's intentional — the system only records a verification date when there is something verified.

The overall status card at the top turns green: "Recommended".

## 5.3 Complete the stage

> Click: [Complete stage →]

The browser returns you to the dashboard. Stage 5 is closed.

## What to watch for — the override path

If a party came back **Not recommended**, here is what would happen:

1. Overall status card flips red.
2. *Complete stage* disables.
3. A red panel appears titled **Override**.
4. The panel demands two things:
   - **Override reason** — a free-text box. The reason is stored verbatim.
   - **Approving officer** — a dropdown of R. Kulkarni / S. Deshpande / A. Rao. Picking one is the named authorization.

> Both fields are required. Type a reason but leave the officer blank → button stays disabled. Pick an officer but leave the reason blank → button stays disabled. Untick the override → button disables again. There is no way past the gate without a real, attributed override.

For this demo we did not exercise the override path because we have only one party and we marked her Recommended.

---

# Stage 6 — Eligibility

## What this stage does

This is where the system decides how much the customer is eligible to borrow. It runs two calculations and takes the smaller of the two:

- **FOIR cap** — based on income minus existing EMIs.
- **LTV cap** — based on the property's assessed value.

If the requested amount exceeds the eligible amount, the difference is shown as a "deviation" and an approver note is required to override it. If the deviation is zero or positive (we asked for less than or equal to what they're eligible for), no note is needed.

## What you'll see

This screen has 4 inner tabs. For this demo we'll only briefly look at the first.

### Borrower Classification tab

| Field | Type this |
|---|---|
| PSL | Yes - Priority Sector |
| PSL sub-category | MSME |
| Risk sharing % | 0 |
| Co-lending partner | None |
| End use of funds | Business expansion |
| Priority sector amount | `3250000` |

### Existing Loans tab

Two starter rows are pre-filled with example loans. For this demo, leave them as-is — they contribute to the FOIR calculation.

### Banking tab

Two starter rows are pre-filled. The system explicitly tells you "Banking conduct is captured for context only and does not feed the eligibility calculation". Leave them as-is.

### Approver Note tab

The system calculates:

- Total income: ₹1,25,000
- Existing EMIs: ₹8,500
- FOIR cap EMI: `125000 × 50% − 8500 = 54,000`
- FOIR cap amount: principal that gives ₹54,000 EMI at 12.75%/60mo ≈ ₹22,20,000
- LTV cap amount: `3850000 × 85% = 32,72,500`
- **Eligible = min(22,20,000, 32,72,500, 32,50,000) = ₹22,20,000**
- **Deviation = (22,20,000 − 32,50,000) ÷ 32,50,000 = −31.7%**

That's a negative deviation. The screen turns red. *Complete stage* disables.

> **This is the hardest moment in the demo.** The system is telling the officer: the customer asked for more than our policy allows. To proceed, the officer must type a justification.

### Writing the approver note

In the Approver Note box, type:

> Property is in a high-appreciation micro-market in Pune; recent comparable sales support assessed value of ₹42L. Existing EMI of ₹8,500 is on a personal loan nearing closure (balance under ₹80K, three instalments left). FOIR headroom in Q4-2026 will exceed 70% once that loan closes. Recommend sanction at requested amount with quarterly income re-verification.

When you click out of the box, the system records **the deviation you wrote against** (in this case −31.7%). If the deviation later changes, the screen warns that the note is stale.

*Complete stage* enables. Click it. Stage 6 closes.

---

# Stage 7 — Approvals

## What this stage does

This is the screen that fixes the final loan terms and sanctions the loan. There are four inner sections, all of which must be complete before the sanction button unlocks. There are **two** clicks required: **Confirm sanction** first, then **Sanction**.

## 7.1 Business Details

| Field | Type this |
|---|---|
| Constitution | Proprietorship |
| Trade name | Deshpande Foods |
| Years in business | 11 |

## 7.2 TVR (Tele-Verification)

| Field | Type this |
|---|---|
| Status | Positive - Confirmed |
| Verified by | S. Deshpande |
| Date | 2026-08-12 |
| Remarks | Spoke with applicant for 15 minutes, employment and business confirmed |

## 7.3 Approval Note

> Eligible amount capped by FOIR at ₹22.20L, but applicant carries significant property equity (assessed at ₹38.5L against market value of ₹42L) and existing loan EMI terminates in three instalments. Combined LTV at 84.4% is within risk appetite for this product. RCU returned Recommended. Banking conduct clean. Recommend sanction at requested ₹32.50L with quarterly income re-verification for the first 12 months.

## 7.4 Approver & Recommender

| Field | Type this |
|---|---|
| Recommender | S. Deshpande (Senior Credit Analyst) |
| Approver | **A. Rao** (Branch Credit Manager) — must differ from recommender |

The screen enforces segregation of duties: the recommender and approver cannot be the same person. If you try to pick the same name for both, the screen will reject it.

## Sanctioned terms

These default from the requested loan. Leave them at:

| Field | Value |
|---|---|
| Sanctioned amount | `3250000` |
| Sanctioned ROI | 12.75 |
| Sanctioned tenure | 60 |

## Confirm and sanction

> Click: [Confirm sanction]

A confirmation panel appears summarising the loan terms. After confirming, the **Sanction** button enables.

> Click: [Sanction →]

`Status` flips to **Sanctioned**. The browser moves to **Stage 8 — Post Sanction**.

---

# Stage 8 — Post Sanction

## What this stage does

This is the last stage. It tracks the operational checklist before the money moves: sanction letter signed, KYC complete, mandates registered, pre-disbursement documents received. Once all the defined items are cleared, funds can be released.

> **Known incomplete.** The brief specified 9 checklist rows. Only 7 are defined. The header reads **"7 of 9 flags cleared (2 items pending definition)"**. The button stays disabled until all 7 defined rows are Cleared, regardless of the 2 undefined ones. Defining the missing two will tighten the gate without code changes.

## 8.1 Checklist

For each of the 7 defined rows, click the status dropdown and pick **Cleared**:

| Row | Set to |
|---|---|
| Sanction letter signed & filed | Cleared |
| KYC documents complete | Cleared |
| E-Nach registration | Cleared |
| Security Nach mandate | Cleared |
| PDD — Invoice | Cleared |
| PDD — Insurance policy | Cleared |
| PDD — RTO registration copy | Cleared |

When the 7th row clears, the **Release funds** button enables.

## 8.2 Disbursement

| Field | Type this |
|---|---|
| Disbursement date | 2026-08-26 |
| Disbursement account | `624501234567` (the account from Stage 3) |
| Amount disbursed | `3176250` *(gross loan − processing fee and other charges)* |

> Click: [Release funds →]

A confirmation dialog appears. Confirm.

`Disbursed = true`. **The application is now fully disbursed.**

---

# Verifying it worked

```sql
SELECT Id, Status, CurrentStage, Disbursed, LoanAmount, CustomerName
FROM Applications WHERE Id = 'LN-2026-004901';
```

Expected: `Status = Sanctioned`, `CurrentStage = 8`, `Disbursed = 1`, `LoanAmount = 3250000`, `CustomerName = Priya Deshpande`.

```sql
SELECT PartyType, FullName, DedupeStatus FROM Parties WHERE ApplicationId = 'LN-2026-004901';
```

Expected: 1 row — Applicant only, `DedupeStatus = Pass`.

```sql
SELECT Amount, AssessedValue, LoanToValueRatio FROM SecurityDetails
WHERE ApplicationId = 'LN-2026-004901';
```

Expected: 1 row with the property details and LTV around 84%.

```sql
SELECT Status, VerifiedByOfficerId FROM RcuOutcomes
WHERE ApplicationId = 'LN-2026-004901';
```

Expected: 1 row, `Status = Recommended`.

---

# What this demo exercises

| Coverage | Why this case |
|---|---|
| Single-borrower path | Confirms the app works with just an Applicant — no guarantor/Co-Applicant tabs, no extra dedupe gates |
| LAP product | Loan Against Property is a different product type from Commercial Vehicle; uses the Property asset toggle on Stage 2 |
| FOIR-bound eligibility | Eligible amount was capped by FOIR (income), not LTV — exercises the binding-constraint path |
| Negative deviation | Required an approver note to release the gate — exercises the deviation override |
| Approver vs Recommender split | Recommender S. Deshpande, Approver A. Rao — exercises segregation of duties |
| Full eight-stage lifecycle | CurrentStage lands at 8, Status at Sanctioned, Disbursed = true |
| Distinct ID space | `LN-2026-004901` does not collide with any seeded data |

---

# Known quirks (not bugs)

**Unstyled page after a CSS change.** Chrome cached the old stylesheet. Hard-reload with **Ctrl+Shift+R**.

**Second download silently does nothing.** Chrome blocks repeated automatic downloads from the same origin. The first CAM.pdf download works; later ones are dropped without any error. Allow automatic downloads for localhost in site settings, or reload the page.

**Always lands on Stage 1.** Opening any application from the dashboard always lands on Stage 1, whatever stage it is really at. To get back to an in-flight file's current screen, either click *Complete stage* through the earlier screens again (safe — `CurrentStage` only ever moves forward) or edit the URL directly:

```
http://localhost:5037/applications/LN-2026-004901/customer-details
http://localhost:5037/applications/LN-2026-004901/loan-security
http://localhost:5037/applications/LN-2026-004901/bank-financial
http://localhost:5037/applications/LN-2026-004901/document-checklist
http://localhost:5037/applications/LN-2026-004901/reports-rcu
```

**Stage 5's "Re-submit to vendor" is not Save draft.** Stage 5 has no draft save. Your edits reach the database only when you complete the stage, re-submit, or upload a report. Fill it in carefully or you'll lose work.

---

# What this demo does not exercise

- Three-party KYC (covered in Guide 1)
- RCU override flow with one party Not recommended (covered in Guide 1)
- Vehicle security type (this demo used Property)
- PDD expected-date chase (would require waiting past the date)
- 90-day address-proof staleness
- The two undefined Post Sanction checklist items (per `OPEN-QUESTIONS-FOR-ARUN.md §0`)

Add any of these only if the demo needs to touch them.

---

# Comparison with Guide 1

| | Guide 1: `LN-2026-004900` | Guide 2: `LN-2026-004901` |
|---|---|---|
| Customer | Hemant Bhalerao | Priya Deshpande |
| Product | Commercial vehicle | Loan against property |
| Branch | Nashik West | Pune Camp |
| Parties | Applicant + Co-Applicant + Guarantor | Applicant only |
| Loan amount | ₹14,75,000 | ₹32,50,000 |
| Tenure | 36 months | 60 months |
| RCU outcome | 2 Recommended, 1 Not recommended (override used) | 1 Recommended (clean) |
| Eligibility gate | Zero deviation, no approver note needed | −31.7% deviation, approver note required |
| Stages exercised hardest | Stage 5 override | Stage 6 deviation note |
| Disbursed | Yes | Yes |

Run Guide 1 first if you want to show the override path, then Guide 2 if you want to show the deviation-note path. Both end at the same terminal state.

---

*Generated for demonstration of the LOS/LMS lifecycle. Application ID `LN-2026-004901` does not conflict with seeded data or with Guide 1 (`LN-2026-004900`).*