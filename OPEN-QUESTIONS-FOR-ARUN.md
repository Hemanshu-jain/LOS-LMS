# Open questions & assumptions — LOS/LMS

Everything built so far rests on assumptions that were made to keep progress moving. None of them
were confirmed with the client. This document collects all of them in one place so they can be
resolved in a single pass rather than discovered one at a time during UAT.

**Status: all nine screens are built.** Applications Dashboard plus Stages 1–8 — Customer Details,
Loan & Security, Bank & Financial, Document Checklist, Reports/RCU, Eligibility, Approvals, and
Post Sanction. The full lifecycle now runs end to end, from creating an application to releasing
the money.

That makes this document the handover: everything below is an assumption the build rests on that
nobody has confirmed.

---

## 0. Two undefined checklist items are the only thing standing between this build and released money — **read this first**

Stage 8's *Release funds* button is the last control before disbursement. It unlocks when every row
in the branch disbursement checklist reads Cleared. **Seven rows exist. The content brief says there
are nine.**

The two missing items were never specified, so they were never built, so the gate cannot check them.
An officer clearing all seven sees the button go live and can release funds with two compliance
items that no one has even named, let alone verified.

The screen states this rather than hiding it — the header reads *"7 of 9 flags cleared (2 items
pending definition)"*, not "7 of 7" — and the gate is written against **every row in the table**
rather than a count of seven, so defining the two missing items and inserting them tightens the
control automatically with no code change. The code carries a matching block comment.

**Question, and it is the highest-priority one in this document: what are the other two checklist
items?** Until they are named, Stage 8 must not go anywhere near production money.

---

## 0.1 The eligible amount is computed from invented policy

Stage 6 now calculates how much the customer may borrow, and blocks the file when that figure comes
in below what was requested. The arithmetic is real. **The policy behind it is not.**

```csharp
private const decimal FoirCapPct = 50m;   // PLACEHOLDER — Eligibility.razor
private const decimal LtvCapPct  = 85m;   // PLACEHOLDER — Eligibility.razor

maxNewEmi     = TotalIncome × 50%  −  existing EMIs
foirCapAmount = the principal that instalment services, at this ROI and tenure
ltvCapAmount  = on-road cost × 85%
eligible      = min(foirCapAmount, ltvCapAmount, requested amount)
```

Both percentages were invented so the screen would have something to compute — the reference spec
says so in its own header. They now decide, on a live application, whether an officer can proceed
without writing a justification.

**Questions:** what are the real FOIR and LTV caps? Do they vary by scheme, product, customer
category or vintage — all of which the system already records, and none of which the formula
currently consults? Should the cap be on gross income or net of declared expenses? Stage 2 already
captures household, fuel/driver and existing-EMI expenses, and this formula ignores every one of
them, using gross income only.

Until these are answered, the eligible amount on screen should not be quoted to a customer.

---

## 1. Blocking before production

### 1.1 QuestPDF licence tier — **legal, needs answering first**
CAM.pdf generation uses QuestPDF. Since 2023 it is **not** plain MIT: the Community licence is only
valid for organisations with **under $1M USD annual gross revenue**. The code currently declares
Community, which is an assertion nobody has verified:

```csharp
QuestPDF.Settings.License = LicenseType.Community;   // Program.cs
```

If the client is above that threshold this needs a paid Professional or Enterprise licence.
**Question:** what is the client's revenue tier, and who purchases the licence if one is needed?

### 1.2 There is no authentication anywhere — and documents are now reachable by URL
Every screen is reachable by anyone who can reach the server. There is no login, no session, no
role, and no per-branch access control. The "Credit Officer · Nashik West" label in the top bar is a
hardcoded placeholder, and "logged-in officer's branch" on new applications is likewise a constant.

**Stage 7 is where this stops being a to-do.** The Approvals screen sanctions the loan — it fixes
the amount, the rate and the term, records a named recommender and a named approver, and advances
the file to disbursement. All of it is done by whoever has the URL. The "approver" is a name picked
from a dropdown by an anonymous user; nothing establishes that the person operating the screen is
that officer, or any officer. This is the third time the same gap has decided something that matters
— after Stage 5's RCU override and Stage 6's approver note — and it is now attached to the
irreversible one.

**This got sharper in Stage 4.** The Document Checklist needs to preview and download uploaded
files, so a file-serving endpoint now exists:

```
GET /files/{applicationId}/{folder}/{name}
```

It refuses to serve anything outside `App_Data/uploads` (verified: encoded `../` attempts return
404), and stored filenames are server-generated GUIDs so URLs cannot be guessed or enumerated. But
it has **no authorisation check**, because there is no identity to check against. Anyone who obtains
a URL — a forwarded email, a browser history, a shared screen — has the customer's Aadhaar card.

**Question:** what is the authentication model — AD/LDAP, local accounts, SSO? And what are the
roles and per-branch visibility rules? This endpoint should gain an authorisation check the moment
one exists.

### 1.3 Dedupe leaves one side of a match stale
Dedupe runs only for the party being saved. Verified behaviour: two applications given the same PAN
showed **Fail** on the one saved second and **Pass on the one saved first**, until that one was
re-saved. So one half of a duplicate pair keeps reading "✓ clear to proceed".

For a lending file that is a real compliance gap.
**Question:** should saving a party re-evaluate every other party it matches, or should dedupe run
as a scheduled sweep? Confirm before Stage 6 (Eligibility) depends on it.

---

## 2. Unconfirmed thresholds and business rules

All of these are placeholder values chosen to make the screens work. Each is a named constant in
code and easy to change once confirmed.

| Rule | Current value | Where |
|---|---|---|
| SLA overdue warning | **5 days** since queued | Applications Dashboard |
| FOIR risk bands | ≤40% healthy, 40–60% caution, >60% risk | Stage 2, Viability |
| LTV risk bands | ≤75% healthy, 75–90% caution, >90% risk | Stage 3, CAM |
| Repayment modes | NACH / Post-dated cheques / Cash | Stage 2, Loan Details |
| Customer categories | Individual / Proprietorship / Partnership firm / Private limited / HUF | Stage 1 |
| Vintage buckets | <1 yr / 1–3 / 3–5 / 5+ | Stage 3 |
| Address proof validity | **90 days**, then it shows Stale | Stage 4 |
| **FOIR cap on eligibility** | **50%** of gross income | Stage 6 — see section 0 |
| **LTV cap on eligibility** | **85%** of on-road cost | Stage 6 — see section 0 |
| Approver-note staleness tolerance | 0.5 percentage points of deviation | Stage 6 |
| **GST on charges** | **18%**, applied only to the seeded processing fee | Stage 7 — see 4.21 |
| Starter charge amounts | Documentation ₹2,500 · Stamp duty ₹5,000 · Valuation ₹3,000 | Stage 7 |

**Questions:** are these the client's actual policy numbers? The FOIR and LTV bands in particular
drive a visible red/amber/green risk signal to the officer.

---

## 3. Completion gates — inconsistent by design, needs a decision

| Stage | Gate today |
|---|---|
| Stage 1 — Customer Details | **Gated.** Applicant's Personal (8) + Contact (10) fully filled **and** dedupe = Pass. Co-applicant/guarantor block too, but only once anything at all is typed into them |
| Stage 2 — Loan & Security | **No gate.** Always enabled |
| Stage 3 — Bank & Financial | **No gate.** Always enabled |
| Stage 4 — Document Checklist | **No gate.** Always enabled |
| Stage 5 — Reports (RCU) | **Gated, hard.** Every visible party needs a non-Pending outcome; any *Not recommended* additionally needs a written override reason **and** a named approving officer |
| Stage 6 — Eligibility | **Gated, hard.** Blocked while the eligible amount can't be calculated, and blocked on a negative deviation until an approver note is written |
| Stage 7 — Approvals | **Gated, hard, in two steps.** Business Details, TVR, Approval Note and Approver & Recommender must all be complete before *Confirm sanction* is possible; only that confirmation unlocks *Sanction* |
| Stage 8 — Post Sanction | **Gated, hard — but knowingly incomplete.** Every checklist row must read Cleared before funds release. Seven rows exist where the brief says nine — see section 0 |

This follows the briefs exactly, but it means an application can pass Stages 2, 3 and 4 with almost
nothing entered — including with zero documents collected — and then hit a wall at Stage 5.
**Question:** should Stages 2–4 have real gates (e.g. 2 references + a security asset; bank account
+ at least one statement; all KYC documents collected)? Also: should Stage 1 require a customer
**photograph**? It is currently optional.

**The shape of this table is now the question.** Stages 1, 5, 6 and 7 gate; 2, 3 and 4 do not. Each
brief was written in isolation and each was followed exactly, but read together the pattern looks
more like an oversight than a policy. Worth one decision covering all eight stages rather than seven
separate ones — and note the ungated stages are the ones holding the loan amount, the processing
fee, the CAM and the documents, i.e. most of what the gated stages then depend on.

**Stage 6 makes the inconsistency concrete rather than theoretical.** Because Stages 2–4 are
ungated, an application can arrive at Eligibility with no loan amount, no income and no CAM — and
Eligibility is the screen that has to divide by those numbers. It handles that honestly (see 4.17),
but the only reason it must is that three earlier stages let an empty file through.

---

## 4. Data model decisions worth reviewing

### 4.1 `AssessedValue` is a single shared column (Stage 2)
Vehicle and property valuations share one column. The form keeps both apart in memory, but the
database cannot: whichever is saved last wins, and after a reload both asset types show the same
figure.
**Question:** can one application ever have both a vehicle and a property as security? If yes this
needs two columns, or a per-asset-type row.

### 4.2 Uploaded files — now viewable, but only on Stage 4
KYC scans, security documents and bank statements are stored **outside `wwwroot`** — deliberately,
because anything under `wwwroot` is downloadable by anyone who guesses the URL.

Stage 4 added the `/files` endpoint (see 1.2), so the Document Checklist can preview and download
documents. **Stages 1–3 still show filenames only** — their upload slots were built before the
endpoint existed and were not retrofitted, since this brief scoped work to Stage 4.
**Question:** should Stages 1–3 get the same preview treatment? Straightforward now the endpoint
exists.

### 4.3 Deleted statements leave orphaned files
Removing a bank statement deletes the database row; the file stays on disk. Over time this
accumulates customer bank statements with no reference pointing at them.
**Question:** what is the document retention policy? Should deletion be immediate, soft-delete, or
retained for an audit window?

### 4.4 One bank account per application
Stage 3 models a single account, assumed to be the applicant's, since NACH/EMI deduction ties to the
primary borrower. Co-applicant and guarantor accounts are not captured.
**Question:** correct? If co-applicant bank details are ever needed this is a structural change.

### 4.5 Sourcing channel vocabulary changed mid-build
Started as `DSA / Branch walk-in / Digital`, later became
`DSA — Patil Motors / DSA — Shree Associates / Branch walk-in / Digital`. Existing rows were
migrated.
**Question:** is this the complete DSA partner list? It will presumably grow, which suggests it
belongs in a table rather than a hardcoded dropdown.

### 4.6 Branch and Scheme are reused across stages
Stage 2's "Sourcing branch" and "Scheme" write to the same `Applications` columns the Dashboard
filters on.
**Question:** are the sourcing branch and the servicing branch always the same? If they can differ,
these need separate columns.

### 4.7 Every party's checklist includes "Guarantor documents"
All 13 document types apply to all three parties, so the **Applicant's** checklist contains a
"Guarantor documents" card, as does the Co-Applicant's. Because the per-tab ✓ requires all 13
collected, an Applicant can never read complete until guarantor paperwork is filed under the
applicant's own tab.
**Question:** is that intended, or should GuarantorDocs apply only to the Guarantor party? Built as
specified — the 13 types were enumerated explicitly.

### 4.8 `hasGuarantor` is derived, not stored
The Guarantor tab appears when a `Parties` row exists for Guarantor **with a non-empty FullName**.
The reference spec file argues the opposite — that it should come from a stored flag set in Stage
1/2 and "NOT re-derived here". The brief overrode that, so it is derived.
**Question:** should there be an explicit `Application.HasGuarantor` flag? Deriving it means a
half-entered guarantor (name typed, nothing saved elsewhere) silently adds 13 documents to the
application's totals.

### 4.9 Remarks are kept forever
The chase log against a document is append-only and is never deleted, including once the document is
collected — it just stops being displayed. This deliberately differs from the reference spec's demo,
which cleared the log on collect.
**Question:** confirm the audit history should be retained indefinitely, and whether officers ever
need to *see* the historical log for an already-collected document.

### 4.10 `Status` transitions — now wired, but only three of them ✅ mostly resolved
Previously `Status` never changed: a fully disbursed file still read "New" or "In progress". That is
fixed. `Status` now moves:

- **New → In progress** when Customer Details (Stage 1) is completed.
- **→ Sanctioned** when Approvals (Stage 7) sanctions the loan.
- **→ Rejected** whenever the new Reject action is used (from any screen or the dashboard).

All three verified against SQL. Stages 2–6 and Post Sanction deliberately leave `Status` alone.

**What is still an open decision:** there is **no terminal status past "Sanctioned"**. Releasing
funds at Stage 8 sets `Disbursed = true` but keeps `Status = "Sanctioned"` — so a live, disbursed
loan and a just-sanctioned one are indistinguishable by status alone. This was intentional (the
brief scoped it that way), but a real portfolio usually wants "Disbursed" / "Live" / "Closed" states
after sanction.
**Question:** what are the post-sanction statuses, and does the dashboard need a "Sanctioned ·
Disbursed" badge driven by the `Disbursed` flag that now exists but is displayed nowhere?

### 4.16 The dashboard always opens Stage 1

### 4.16 The dashboard always opens Stage 1
Both "New application" and opening an existing file from the dashboard navigate to
`/applications/{id}/customer-details`, regardless of how far the application has progressed. To reach
an in-flight file's actual stage the officer must click *Complete stage* through every earlier
screen — re-completing stages that were already done — or type the URL by hand.

*(Re-completing an earlier stage is safe: `CurrentStage` only ever moves forward. That guard was
added after testing found the opposite — see the note below.)*

**Question:** should opening an application jump to its `CurrentStage`? The stepper already knows the
number; this is a small change and it is likely to be the first thing a real user complains about.

> **Fixed during testing, no decision needed — recorded for transparency.** Each screen originally
> assigned a hardcoded stage number on completion (`CurrentStage = 2`, `= 3`, …). Combined with 4.16
> that meant an officer opening a finished file to correct a typo and clicking *Complete stage* would
> silently reset the application from stage 6 back to stage 2 — later stages re-locking in the
> stepper and the dashboard reporting the wrong position. Reproduced twice on two different stages,
> then fixed on all five screens with `Math.Max(application.CurrentStage, n)` and re-verified in both
> directions. *Save draft* never touched `CurrentStage` and was never affected.

### 4.11 Screened vs Sampled does nothing
Stage 5's RCU mode toggle is recorded on the `RcuInitiation` row, and nothing reads it. Both modes
produce an identical screen with identical rules. In real RCU practice the distinction is the whole
point — Screened means every case is verified, Sampled means a percentage is.
**Question:** what should Sampled actually change? Candidates: skipping some parties, a different
gate, or a sampling percentage recorded on the file. Until that is answered the toggle is a label.

### 4.12 RCU case reference is generated by the app, and can collide
`CaseRef` is auto-generated as `RCU-{year}-{5 digits}` (e.g. `RCU-2026-00001`) from **max + 1** over
existing rows, then left editable. Two officers initiating RCU at the same moment can be handed the
same number — the calculation and the insert are not atomic.
**Questions:** (a) is this format right, or does the vendor issue the reference? (b) if the app owns
it, should it be a database sequence rather than max+1? (c) does it reset each calendar year?

### 4.13 One RCU report covers all parties
Reports attach to the **application**, not to a party. Each round is one `RcuReports` row for the
whole file, while outcomes are recorded per party. If the vendor returns a separate PDF per party
there is nowhere to put them — the second upload replaces the first.
**Question:** does the vendor deliver one consolidated report or one per party verified?

### 4.14 Officers now exist twice
Stage 5 introduced an `Officers` table (R. Kulkarni, S. Deshpande, A. Rao) because RCU outcomes and
the override approval need a real foreign key. But `Applications.AssignedOfficer` still stores a
plain **name string** from the earlier screens. The same people are now modelled two different ways,
and nothing keeps them in sync.
**Question:** should `AssignedOfficer` become a foreign key into `Officers`? That is a small
migration now and a painful one later. Related: this table is also the obvious place for the login
identity discussed in **1.2**, which would make it three representations if left alone.

### 4.15 Stage 5 has no draft save, and no override audit trail
Two gaps that come from the same place — the brief specified the fields, not the bookkeeping:

- **No "Save draft".** That sub-header button is relabelled *Re-submit to vendor* on this screen, so
  outcome and initiation edits reach the database only via **Complete stage**, **Re-submit**, or
  **uploading a report**. An officer who types findings and closes the tab loses them. Every other
  stage can be saved half-finished.
- **The override records what, not who or when.** An override stores its reason and the approving
  officer, but not the officer who *applied* it or the timestamp. It is the single control that lets
  a *Not recommended* file proceed, so it is exactly the field an auditor will ask about.

**Questions:** should Stage 5 get a real draft save (an explicit third button, since the secondary
slot is taken)? And should overrides carry `AppliedBy` + `AppliedAt`, or a full audit table?

### 4.17 Eligibility refuses to guess, and that is a decision worth confirming
An application can reach Stage 6 with no loan amount, no income and no CAM, because Stages 2–4 do
not gate. Rather than show ₹0 eligible and −100% deviation — a confident-looking figure that means
nothing — the screen replaces the whole panel with a list of what is missing and links to the stage
that owns it. Completion is blocked while it shows.

This also removes a real crash: `deviationPct` divides by the requested loan amount, which is zero
on any file that skipped Stage 2.

**Question:** is "say nothing until the inputs exist" the right behaviour, or should a partial
figure be shown with a warning? The current choice assumes an officer must never be able to quote a
number the system could not actually compute.

### 4.18 The four Stage-6 vocabularies are all invented
`Classification` stores PSL status, sub-category, risk sharing %, co-lending partner and end use.
Every dropdown behind those is a placeholder list taken from the reference file:

| Field | Current options |
|---|---|
| PSL sub-category | Transport Sector - CV / Agriculture / MSME / Other |
| Co-lending partner | None / HDFC Bank / ICICI Bank / State Bank of India / Axis Bank |
| End use of funds | Fleet expansion / Vehicle replacement / New business setup / Working capital / Other |
| RTR (per existing loan) | Regular / Irregular / Overdue |

**Questions:** are these the client's real classifications, and are the co-lending partners the
actual arrangements in place? PSL reporting is an RBI-facing obligation, so the sub-categories in
particular need to match the client's regulatory return, not a plausible-looking list. Also: nothing
validates that priority-sector amount ≤ loan amount, or that risk sharing % is between 0 and 100.

### 4.19 Banking conduct is captured and then ignored
The Banking tab records months reviewed, average balance, cheque bounces and inward/outward
transaction percentages — and feeds none of it into the eligible amount. The tab says so on screen,
so it is honest rather than hidden, and it was specified that way deliberately.

But bounce counts and average balance are exactly what a credit policy usually keys on.
**Question:** should banking conduct affect eligibility — as a cap, a haircut, or a hard decline
rule? The same applies to the existing loans' Max DPD and RTR columns, which are likewise recorded
and unused. Only the EMI column influences anything today.

### 4.20 The approver note is one field standing in for an approval
A negative deviation is released by typing free text into a box. There is no approver identity, no
timestamp, no second pair of eyes, and no record of who typed it — the same gap already noted for
Stage 5's RCU override in **4.15**, now appearing a second time on the screen that decides how much
money is lent.

The one safeguard that does exist: the deviation the note was written against is stored, so if the
figures later move, the screen warns that the note was written against a different number
(verified — it survives a reload). That flags a stale justification; it does not establish who
approved it.

**Questions:** who is actually authorised to approve a deviation, and does the authority depend on
its size? A −5% haircut and a −43% haircut are the same single text box today.

### 4.21 The 18% GST rate is a guess, and the processing fee it applies to was empty
Stage 7 seeds a Processing fee charge whose GST is `round(amount × 0.18)`. 18% is the common Indian
GST slab for financial services — nobody has confirmed it applies to every charge head here, or that
Documentation / Stamp duty / Valuation attract the same treatment. Stamp duty is seeded at **0%**
GST purely because the reference file showed it that way.

**Worse, the fee itself was blank.** The processing fee is correctly pulled from
`Applications.ProcessingFee` (Stage 2) rather than re-invented — but on the real test application
that column was **NULL**, because Stage 2 has no completion gate. The charge sheet therefore
computed ₹0 fee and ₹0 GST without complaint. Three earlier documents assumed ₹18,500 for this
number; the database had nothing.

**Questions:** what GST rate applies to each charge head? And should the processing fee be
mandatory at Stage 2, or derived from a scheme rule (e.g. 1% of loan amount) rather than typed?

### 4.22 Both approval vocabularies are invented
| Field | Current options |
|---|---|
| Recommender / Approver role | Credit Analyst / Senior Credit Analyst / Branch Credit Manager / Regional Credit Head |
| Approval authority | Free text |
| TVR status | Positive - Confirmed / Negative - Discrepancy / Unable to Contact |
| Constitution | Proprietorship / Partnership firm / Private limited company / HUF |

**Questions:** are these the client's actual credit designations? And should approval authority be a
controlled list tied to a sanctioning-limit matrix rather than free text — which is what would let
the system check that the named approver is actually allowed to approve this amount?

### 4.23 Segregation of duties checks the name, and nothing else
The rule enforced is: the recommender and the approver must not be the **same person**. That is the
whole check. It does not verify that the approver is senior to the recommender, that either holds a
role permitted to sign, or that the approver's authority covers the amount being sanctioned — a
Credit Analyst can approve ₹17,04,452 today, with a Regional Credit Head recommending it.

**Question:** what is the real matrix — which roles may recommend, which may approve, and up to what
amount each? The name check is the floor, not the policy.

### 4.24 TVR status is recorded and ignored
The TVR tab colours its status green / amber / red and stores it, but nothing acts on it. An
application whose tele-verification came back **"Negative - Discrepancy"** sanctions exactly as
smoothly as a positive one — the gate only checks that the TVR *fields are filled in*, not what they
say. Same pattern as `Classification.Psl` and the whole Banking tab.
**Question:** should a negative TVR block the sanction, or require an override like a negative RCU
outcome does at Stage 5?

### 4.25 Send Back is the only reverse gear, and it discards a confirmed sanction
Stage 7 introduced the first backward transition in the system. It writes a `SendBackLog` row
(from-stage, to-stage, reason, timestamp), moves `CurrentStage` down, and lands the officer on the
target screen. That log is the **only** record that a file ever moved backwards —
`Applications.CurrentStage` records where a file is, never where it has been.

Two behaviours worth confirming:

- **A confirmed sanction is silently cleared.** Sending back resets `SanctionConfirmed`, so the file
  must be re-confirmed when it returns. This was a deliberate choice, not in the brief: the
  alternative is a file sent back for missing documents returning with a live Sanction button and no
  re-verification. The sanctioned *terms* are kept — only the confirmation is withdrawn.
- **Nothing is notified and nothing is un-done.** The target stage's own data is untouched, so an
  application sent back to Stage 4 arrives with its checklist exactly as it was. No one is told.

**Questions:** is clearing the confirmation correct? Should sending back to a stage invalidate that
stage's completion, and who should be notified when a file lands back in their queue?

### 4.26 Constitution is now stored in two places
`Business.Constitution` (Stage 7) offers Proprietorship / Partnership firm / Private limited company
/ HUF — the same list `Parties.CustomerCategory` already captured at Stage 1. Nothing keeps them in
sync, and the screens do not show each other's value.
**Question:** should Stage 7 read the Stage 1 value instead of asking again? This is the same shape
as the `Officers` / `AssignedOfficer` duplication in **4.14**.

### 4.27 Two wireframe figures were corrected — and correcting them was not one edit each
The Post Sanction brief surfaced reference data showing two numbers used throughout this build were
invented. Both were corrected on the sample application `LN-2026-004871` by the migration
`CorrectSampleApplicationValues`:

| Figure | Was | Now |
|---|---|---|
| CAM margin / down payment | ₹4,10,000 | **₹3,20,000** |
| Processing fee | ₹18,500 (1.0%) | **₹27,750 (1.5%)** |

The brief expected each to be a single update, on the assumption that every screen reads these
values live. **Two of them do not**, and both only came to light by checking the screens afterwards
rather than trusting the assumption:

- **The processing fee is copied, not read, by Stage 7.** A charge has to be its own editable,
  waivable row, so Approvals *snapshots* `Applications.ProcessingFee` into a `Charges` row the first
  time the screen opens. Correcting the parent column left that copy at the old figure — and Stage
  8's net disbursement is computed from those charge rows, so it would have inherited it.
- **The CAM margin is split into Draft and Applied.** Correcting `AppliedMargin` alone left
  `DraftMargin` at ₹4,10,000, which made the CAM tab display the old number in its editable field,
  show "unsaved changes", and — the real problem — restore the wrong value over the correction the
  next time anyone pressed *Recalculate CAM*.

Both were then corrected properly. **The general question this raises is worth more than the two
figures:** the system has several places where a number is deliberately copied rather than
referenced (charges, CAM Draft/Applied, sanctioned terms vs requested terms). Each copy is
defensible on its own, but together they mean "fix the value at source" is not a reliable
instruction. **Question:** is there an authoritative source for each of these figures, and should
corrections propagate automatically or be re-entered per stage?

### 4.28 "Sunita Pawar" does not exist
The brief instructed that the Co-Applicant's name must not be changed anywhere, because six screens
depend on it. Checked before touching anything: **no party named Sunita Pawar exists in the
database.** `LN-2026-004871` has an Applicant (Ramesh Pawar) and a Guarantor (Vijay Kulkarni), and
no co-applicant row at all. The only similar name is "Sunita Deshmukh" on an unrelated application.

The name lived in the wireframe reference, not in the built system. Nothing was changed.
**Question:** should this application have a co-applicant at all? Several screens size their party
tabs off whether one exists.

### 4.29 `Disbursed` is a new flag because the stage model ran out of room
Stage 8 is the last stage, so `CurrentStage` stops at 8 whether or not the money ever moved. A
`Disbursed` boolean was added to `Applications` because otherwise nothing on the application
distinguishes a fully disbursed loan from one sitting on the Post Sanction screen doing nothing.

This is the same shape as the `Status` problem in **4.10**, and the two now compound: a released
loan has `CurrentStage = 8`, `Disbursed = true`, and `Status = "In progress"`.
**Question:** should the lifecycle be modelled as a status rather than a stage number plus a growing
set of booleans? A second flag for the next terminal state would make this a pattern.

### 4.30 Post Sanction records several things that control nothing
Consistent with earlier stages, and listed together because the pattern is now systemic:

- **E-Nach / Security-Nach status.** Funds can be released with both mandates Pending. The checklist
  does carry an "E-Nach registration" row, so the control exists — but it is a person ticking a box,
  not the mandate record itself.
- **PDD expected dates.** Nothing chases them. A PDD can sit Open past its expected date forever
  with no warning anywhere in the system, which is precisely what a PDD register exists to prevent.
- **Down payment vs CAM margin.** `DownPaymentRecord.AmountReceived` is never reconciled against
  `CamCostBreakdown.AppliedMargin` — the margin the CAM assumed. Two screens, two people, no check
  that they agree.

**Questions:** should any of these block release, and should PDD ageing surface on the dashboard?

### 4.31 The Document Upload tab should probably be deleted
Built as specified, with the caution note the brief asked for, but nobody could say what it is for —
the brief itself flagged it as possibly redundant with Insurance Upload or Stage 4's checklist. It
is one nullable column and one tab.
**Recommendation:** cut it unless a real, non-redundant purpose turns up. It is cheaper to delete
now than to explain to users later.

### 4.32 The assigned officer is now stored twice
The dashboard filter, the drawer assignment and the Summary Rail were all wired to a real
`AssignedOfficerId` foreign key this pass, and every existing application was backfilled from the
older `AssignedOfficer` name string so nothing regressed. The name string is now **redundant** — it
is kept in sync only so the two never disagree.
**Recommendation:** drop `Applications.AssignedOfficer` (the string) once nothing else reads it, and
let the FK be the single source of truth. Same shape as the officer duplication already noted in
**4.14**; this pass fixed the assignment path but left the vestigial column in place to keep the
change additive.

### 4.33 Rejection is one-way and blocked after sanction — both by design
The new Reject action is deliberately constrained:

- **Hidden once `Status == "Sanctioned"`** (verified: gone from every screen and the drawer on a
  sanctioned file). Un-sanctioning is a loan-closure / cancellation concern, which is a different
  workflow and was not built.
- **No un-reject.** Rejecting sets `Status = "Rejected"` and writes a `RejectionLog` row; there is no
  screen to move a file back out of Rejected. This matches the one-way nature of the other terminal
  actions (sanction, disburse).

**Questions:** is there ever a legitimate "un-reject" (rejected in error) or "cancel after sanction"
path the business needs? If so, both are new workflows, not tweaks — and both should themselves be
audit-logged like the reject and send-back trails already are.

### 4.34 Reject does not stop work already in flight
Rejecting sets the status and logs it, but it does **not** lock the screens — an officer can still
open a rejected file's stage screens and edit fields (the red banner tells them the live state, but
nothing is disabled). This was the brief's explicit instruction (officers may still need to
reference the data). It does mean a rejected file's data can keep changing.
**Question:** should a rejected (or sanctioned) file become read-only, or is reference-only-by-
convention enough?

---

## 5. Integrations — none are connected

Every one of these follows an "attempt, then report honestly" pattern. **None ever fakes a success.**

| Integration | Stage | Current behaviour |
|---|---|---|
| PAN OCR / verification | 1 | "PAN verification unavailable — API key not configured" |
| Aadhaar OCR / verification | 1 | Same, for Aadhaar |
| Mobile OTP | 1 | "Mobile verification unavailable — SMS provider not configured" |
| Video KYC | 1 | "Video KYC unavailable — service not configured yet" |
| Vahan RC lookup | 2 | Manual entry only; caption says v2 scope |
| Penny-drop name match | 3 | "Unavailable — banking verification provider not configured" |
| Bank statement parsing | 3 | Always shows "Not configured" |
| RCU vendor hand-off | 5 | Fully manual. "Re-submit to vendor" only records a new round in the database — nothing is sent, and the returned report is uploaded by hand |
| CIBIL / bureau pull | — | Not built |

**Questions:** which providers has the client selected, and which are contracted? Two behavioural
notes:
- The `MobileVerified` / `PanVerified` / `AadhaarVerified` flags can **never** become true in this
  build, so the green "Verified" badge never appears. That is honest, not broken.
- Once penny-drop reports Unavailable the retry button is hidden — there is nothing to retry until a
  provider exists. When one is connected, existing applications stuck at Unavailable will need a way
  to re-run.

### 5.1 The RCU vendor panel is a made-up list
Stage 5's vendor dropdown offers *Verified Field Services, TransUnion CIBIL RCU, CRISIL Risk
Solutions* and the rest — placeholder names, not the client's empanelled agencies. The TAT field is
likewise a free-typed number with no default, so nothing enforces the agreed turnaround.
**Questions:** who are the actual empanelled RCU agencies, and what TAT is contracted with each?
A per-vendor default TAT would make the breach warning mean something.

### 5.2 IFSC lookup is a 12-bank starter list
Bank name auto-detection covers HDFC, ICIC, SBIN, UTIB, BARB, PUNB, KKBK, YESB, IDFB, INDB, CNRB,
UBIN. Any other IFSC falls back to a manual dropdown.
**Question:** should this load the full RBI-published IFSC dataset, and who maintains it as banks
merge?

---

## 6. Validation formats — confirm these match the client's rules

| Field | Pattern | Notes |
|---|---|---|
| PAN | `^[A-Z]{5}[0-9]{4}[A-Z]$` | Format only; not checked against NSDL |
| Aadhaar | `^\d{12}$` | No Verhoeff checksum validation |
| Mobile | `^[6-9]\d{9}$` | Indian mobile |
| PIN code | `^\d{6}$` | Not validated against a real PIN directory |
| Bank account | `^\d{9,18}$` | Numeric only |
| IFSC | `^[A-Z]{4}0[A-Z0-9]{6}$` | Standard format |

None of these block saving a draft — partial data is always allowed. They only show an inline
warning.
**Question:** should any of them become hard blocks at stage completion?

---

## 7. Smaller items

- **Aadhaar is masked on screen** (`•••• •••• 1234`) once valid, but stored in full. Confirm the
  masking rule and whether the full value should be encrypted at rest.
- **Dropdowns all have a blank first option.** Necessary — without it an untouched dropdown reads as
  "filled" and breaks completion tracking.
- **Empty reference rows are skipped on save**, so two blank starter rows do not create blank
  database rows.
- **Repayment schedule final balance** lands near zero rather than exactly zero, because EMI is a
  rounded figure. Real lenders absorb the difference into the final instalment.
  **Question:** should the last EMI be adjusted to close the balance exactly?
- **The 128 applications currently in the database are generated test data**, not real records.
- **Loading, empty and error states have not been designed** for stages 2–8. Worth solving once at
  the shared-component level rather than eight times.
- **CAM.pdf contents** — currently header, cost breakdown, sanction summary and full repayment
  schedule. Confirm this matches what the credit team actually needs in a CAM.

---

*Last updated after the cross-cutting final pass (real Status transitions, Reject application,
assigned-officer wiring). All nine screens built and the lifecycle status/reject/assignment now live.
This is the handover document; work through section 0 first.*
