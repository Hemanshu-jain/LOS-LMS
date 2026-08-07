# Driving an application from creation to Stage 5

Every step below was executed against the running app and a real MySQL database on 2026-08-06,
creating application **LN-2026-004875** from scratch and finishing with `CurrentStage = 6`. Field
names, button labels and gate messages are copied from what the screens actually rendered.

**Start the app**

```
cd D:\Hemanshu\PERSNOAL\LMS\LosLms
dotnet run
```

Then open <http://localhost:5037/applications>.

---

## What actually blocks you

Only two of the five stages can stop you. This is the whole picture:

| Stage | Can it block? | What it demands |
|---|---|---|
| 1 — Customer Details | **Yes** | Applicant's 8 personal + 10 contact fields, then a saved draft so dedupe can run and pass |
| 2 — Loan & Security | No | Nothing. *Complete stage* is always live |
| 3 — Bank & Financial | No | Nothing |
| 4 — Document Checklist | No | Nothing — you can pass with zero documents collected |
| 5 — Reports (RCU) | **Yes** | Every visible party needs a non-Pending outcome; any *Not recommended* also needs an override reason **and** an approving officer |

Everything not named in that table is optional and can be left blank.

---

## Stage 1 — Customer Details

**Get there:** dashboard → **+ New application** (top right). The application is created immediately
with a real ID and you land on Stage 1.

### Fill these — they are the gate

*Personal information* — all 8:

| Field | Example |
|---|---|
| Full name | Sunil Wagh |
| Date of birth | 1986-03-12 |
| Gender | Male |
| Marital status | Married |
| Father / spouse name | Ashok Wagh |
| Customer category | Individual |
| Nationality | Indian |
| Mother tongue | Marathi |

*Contact details* — all 10: Mobile, **Alternate mobile**, Email, **Address line 1**, **Address line
2**, City, State, PIN code, Residence type, Years at address.

> Alternate mobile and address line 2 are required. They look optional and they are not — this is
> the most likely place to get stuck.

The section collapses itself the moment it is complete, with a "8 fields · complete" summary. That
collapse is the confirmation you got it right.

### Then save, then complete

1. Enter a PAN and Aadhaar (Identity block). Not strictly required, but dedupe is meaningless
   without a PAN.
2. Click **Save draft**. Dedupe runs on save — never before.
3. Click **Complete stage →**.

**If Complete stage is greyed out, hover it.** The tooltip names the exact problem, e.g.
`Applicant: save the draft to run the dedupe check.` or `Applicant: contact details are incomplete.`

Three things worth knowing:

- **Dedupe only runs on save.** Filling every field leaves the button disabled until you click *Save
  draft*. This tripped the test run.
- **Co-Applicant and Guarantor block only if you touch them.** Leave a tab completely empty and it
  is ignored. Type one character into it and that party must now be completed in full and pass
  dedupe too.
- **A guarantor exists only if you give them a full name.** That single field decides whether the
  Guarantor tab appears on Stages 4 and 5.

Stage 1 completing takes you to Stage 2 automatically.

---

## Stage 2 — Loan & Security

Nothing here can block you, but Stages 3 and 5 have nothing to show if you skip it.

**This screen has four inner tabs** — *Loan Details · Security Details · Reference Details ·
Viability* — and only the open one is on screen. Fields on the other three are not hidden, they are
not rendered. Click through all four.

*Loan Details* (10 fields): DSA / sourcing channel, Sourcing branch, Scheme, Requested amount,
Tenure (months), ROI %, Processing fee, Advance EMI, Repayment mode, Expected disbursal date.

Test values used: `DSA — Patil Motors`, `Nashik West`, `CV-STD-2026`, ₹18,50,000, 48 months, 13.25%,
₹18,500, 1 advance EMI, NACH.

The right-hand summary rail updates live as you type — amount, tenure and ROI appear there
immediately. EMI works out to **₹49,861/month** on those numbers.

*Security Details* (12 fields): Make/model, Year, Registration no., Chassis no., Engine no., Invoice
no., Invoice date, Invoice value, Insurer, Policy no., Policy expiry, **Assessed value**.

Assessed value is the one that matters downstream — Stage 3's LTV is calculated from it. Test value
₹22,60,000, which puts LTV at 82% (caution band).

*Reference Details*: two starter rows. Blank rows are discarded on save, so partially filling one is
safe.

*Viability*: income vs expense, drives FOIR.

Click **Complete stage →** to move on.

---

## Stage 3 — Bank & Financial

Two inner tabs: *Bank Details* and *CAM*.

**Type the IFSC first.** Entering a recognised IFSC (e.g. `HDFC0004412`) replaces the bank-name
dropdown entirely with an auto-detected read-only value captioned "Auto-detected from IFSC". If you
pick the bank first you are just doing work the IFSC will redo. Only 12 bank prefixes are known;
anything else leaves the dropdown in place for manual selection.

Remaining fields: Account number, Account type, Account holder name, Banking vintage.

Statement upload and penny-drop verification both report "not configured" — that is honest, not
broken. No integrations are connected in this build.

The *CAM* tab produces CAM.pdf. Click **Complete stage →**.

---

## Stage 4 — Document Checklist

13 document types per party. With no guarantor you get two tabs and the aggregate line reads
**"Across all parties: 0 collected · 26 pending"** — 2 parties × 13.

Nothing here blocks completion, so for a fast pass just click **Complete stage →**. If you want to
exercise it:

- **PAN, Aadhaar and Photograph are read live from Stage 1.** Upload one here and it writes back to
  the customer record, not to a separate document row. Upload it in Stage 1 and it already shows as
  Collected here.
- **Address proof expires after 90 days** and then shows **Stale**. It is the only type that can.
- **Target date and remarks only appear on chaseable rows** (Pending or Stale). A collected document
  shows "—", and its remark history is kept in the database, just not displayed.
- **Bulk upload** fills outstanding slots for the active party only, in the fixed order PAN →
  Aadhaar → Photo → Signature → Address → the 8 other types. Extra files are ignored once slots run
  out.
- **Print checklist** opens the real browser print dialog.

---

## Stage 5 — Reports (RCU)

This is the one that will actually stop you.

On arrival: the RCU case reference is generated for you (`RCU-2026-00002`), outcome rows are created
for every visible party, and party names are pulled live from the customer record — a party with no
record shows "—". Overall status starts **Pending** and *Complete stage* starts disabled.

**Note the button on the left is "Re-submit to vendor", not "Save draft".** Stage 5 has no draft
save. Your edits reach the database only when you complete the stage, re-submit, or upload a report.
Do not fill this screen in and walk away.

### The normal path

1. Fill *RCU initiation*: Mode (Screened/Sampled), Branch, Vendor, Initiation date, TAT days.
2. In *Per-applicant outcome*, set every party's **Status** to `Recommended`. Verification date
   stamps itself automatically; set it back to Pending and both audit fields clear again.
3. Optionally add Verified-by officer and remarks.
4. **Complete stage →** is now live. Click it. You return to the dashboard.

If TAT is exceeded and no completion date is set, a red **"⚠ TAT breached by N days"** appears and
disappears once you enter a completion date.

### When a party comes back Not recommended

Setting any party to `Not recommended` turns the overall card red and re-disables *Complete stage*.
A red override panel appears. To proceed you need **both**:

- a written **override reason**, and
- an **approving officer** selected.

One without the other keeps the button disabled. Untick the override and it disables again. There is
no way past this gate other than a real, attributed override — which is the point.

### Uploading the vendor's report

Drop the PDF into the *Report* panel. **The upload is the save** — there is no separate button.
*View* opens it in a modal with a working download link.

**Re-submit to vendor** opens a confirmation dialog, then starts a new round: the current report
moves into history, dates reset, and the new round shows "Awaiting upload". History entries stay
clickable.

---

## Two browser quirks that are not bugs

**1. A screen renders completely unstyled.** Chrome cached the old stylesheet. Hard-reload with
**Ctrl+Shift+R**. This happens after CSS changes and looks alarming — the page appears as raw
unstyled HTML.

**2. A download silently does nothing the second time.** Chrome blocks repeated automatic downloads
from the same origin. The first CAM.pdf or report download works, later ones are dropped without any
error. Allow automatic downloads for localhost in the site settings, or just reload the page. This
was investigated and confirmed to be Chrome, not the app.

---

## Verifying it worked

```sql
SELECT Id, Status, CurrentStage FROM Applications WHERE Id = 'LN-2026-004875';
```

`CurrentStage = 6` means all five stages are done. Row counts after the test run:

| Table | Rows | Why |
|---|---|---|
| Parties | 1 | Only the Applicant had data — untouched parties create no row |
| SecurityDetails | 1 | |
| BankDetails | 1 | |
| Documents | 26 | 2 parties × 13 types |
| RcuInitiation | 1 | |
| RcuOutcomes | 2 | One per visible party |

**`Status` will still say "New".** Nothing advances it yet — see item 4.10 in
`OPEN-QUESTIONS-FOR-ARUN.md`.

## One thing that will annoy you immediately

Opening any application from the dashboard always lands on **Stage 1**, whatever stage it is really
at. To get back to an in-flight file's current screen either click *Complete stage* through the
earlier screens again — which is safe, progress only ever moves forward — or edit the URL directly:

```
http://localhost:5037/applications/{id}/customer-details
http://localhost:5037/applications/{id}/loan-security
http://localhost:5037/applications/{id}/bank-financial
http://localhost:5037/applications/{id}/document-checklist
http://localhost:5037/applications/{id}/reports-rcu
```

Raised as item 4.16 for Arun.
