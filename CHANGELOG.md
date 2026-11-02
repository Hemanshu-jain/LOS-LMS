# Changelog

All notable changes to LOS/LMS, newest first. The format follows [Keep a Changelog](https://keepachangelog.com/).
Entries are written for the people who use the system (operators and staff), because the same text is used as the
release description that the in-app updater shows. The version number lives in `src/LosLms.Server/LosLms.Server.csproj`
(`<Version>`) and is stamped on the build by `publish.ps1`.

## [Unreleased]

Work merged to `main` since 1.2.4 (Digio verification, e-Sign and e-NACH, loan agreements, servicing ledger, and others)
has no release notes yet. Add them here when the next version is cut. Until then `git log` is the record.

### Releasing funds (Post Sanction)
- **Funds are now released in four steps with two people.** An officer prepares the payment (payee name, account, **IFSC**, mode)
  and submits it; **a different Admin approves** it; an Admin pays from the bank and then enters the **bank reference (UTR) and the
  date paid** to release. Once submitted, the payee and amount are locked: change anything and the Admin must approve again.
- A reference already used on another loan is refused. "Verify payee account" asks the bank once per account.
- CAM approval and Rate & PF approval can only be cleared by an Admin, and every checklist item now shows who cleared it.
  Every step is kept in a history on the file.

### Stages
- **Stage 3 is now Verification.** PAN / Aadhaar checks, scans, Aadhaar date, address proof and Video KYC moved here from
  Stage 1, which keeps only PAN and Aadhaar entry, CIBIL and the photograph. Bank details and CAM are further tabs on Stage 3.
- **Stage 2** reads "Vehicle Details". A SuperAdmin can allow property loans per company (Companies page). The TG Transport
  check is a "Manual RC check" card and switches off once Digio has verified the RC.
- **Schemes carry loan terms** (amount, tenure, ROI, processing fee, advance EMI), set in Settings, Scheme Charges. Choosing
  a scheme fills them in; anything the officer changed is kept.
- Reports shows **Approved / Rejected / Pending approval** instead of Recommended / Not recommended / Pending.
- **Borrower Classification** lists (PSL sub-category, co-lending partner, end use) are editable in Settings, Classification
  Lists, and the classification is printed on the CAM.

### Paying your bill
- **Pay with UPI now shows a QR code.** The button used to open a link that only works on a phone, so on a computer it did
  nothing. It now opens a QR code with the amount and bill reference already filled in; scan it with any UPI app.
- **The "bill due" banner appears from the due date (the 8th), not before.** From the day after, the pause screen carries Pay now.

### Readability
- The bill banner's **View bill** and **Pay now** links, and the **"An unhandled error has occurred. Reload"** bar, are now
  clearly readable (they were close to invisible). Pay now is a button.

## [1.2.4] - 2026-10-09

Installs silently in the background.

### Paying your bill
- If verification services are paused for an unpaid bill, the pause screen now has a **Pay now** button. It shows the amount, a **Pay with UPI** button, and the bank details.
- After paying, enter the UTR / payment reference. Service resumes as soon as the payment is confirmed.
- **Sign out** now works from the pause screen and the licence-expired screen.

### Settings
- **API Usage**, **Connected apps** and **System updates** have moved into **Settings → Usage & Updates**, which keeps the top menu shorter.

### Connecting partner software
- The **Connect an app** page now also links to the Connect guide and the API reference for your software vendor.

### Fixes
- A rare error on the sign-in page right after signing out.
- Background clean-up of expired connection codes.

## [1.2.3] - 2026-10-09

Installs silently in the background.

### Verification services (Digio)
- **Digio keys** (SuperAdmin): each company's Digio credentials are entered once and stored encrypted. Nothing is sent to Digio until keys are entered.
- **Bank penny-drop check** on Bank & Financial, and **Aadhaar e-Sign** of the loan agreement on Post-Sanction, switch on as soon as keys are set.
- Monthly usage bills: if a bill stays unpaid past its due date, verification services pause until it is marked paid.

### Connect partner software
- Partner software (such as Paavan Software) can now link to your company with a **Connect** button: an Admin enters the short code on the **Connect an app** page and clicks Allow. No API keys to copy.
- **Connected apps** (Admin) shows every linked app, when it was last used, and a **Disconnect** button.
- Developer guide for partners at `/docs/`.

### Reliability
- Updates download more reliably (no more "file in use" errors).
- The database is backed up automatically before each update.

## [1.2.2] - 2026-09-29

A small update. Installs silently in the background.

### Billing
- **API Usage report** (Admin → **API Usage**): a monthly view of every provider API used, how many times, the rate, and the amount payable. Visible to Admins only.
- Groundwork for monthly billing of verification services (KYC, e-Sign, bank checks). Nothing changes in day-to-day work until those services are switched on.

## [1.2.1] - 2026-09-25

A small follow-up update. Installs silently in the background.

### Loan structuring
- **EMI start date on Loan & Security:** you can now set the **Preferred EMI date** (5th / 10th, or an exact date) and the **First EMI date** right at the Loan & Security stage. What you set here flows down to the Post-Sanction repayment schedule.

### Disbursement
- The **Disbursement type** now includes a **Trade Advance (TA)** option (alongside Direct to dealer / customer / seller).

### Reliability
- Friendlier **connection and maintenance screens** while the server restarts or updates, instead of a raw error page.

## [1.2.0] - 2026-09-23

A big data-quality and KYC release. Everything below is live after the in-app update.

### Getting the data right the first time
- **Mandatory fields are marked with a red \***. If you try to complete a stage with something missing,
  the form now **jumps straight to the missing field and highlights it** — no more hunting for what's blank.
- **Alternate mobile** and **Address Line 1** (which must include a door/plot number) are now required.
- **Employment details** are now compulsory.

### Live photo & Video KYC (new)
- **Take the applicant's photo live from the camera** — or upload a file as before.
- **Record a Video KYC clip right inside the app.** The photo and video are stored with the application
  at full quality and can be viewed any time.

### KYC & documents
- **Aadhaar freshness:** record when the masked Aadhaar was generated — the app flags it if it's more
  than 5 days old.
- **Address changed since Aadhaar?** Tick it and attach the address proof.
- **Per-document "Signed" tick**, with an **Auto-check** button that reads the scan for you (turns on once
  the document-reading service is connected).

### Approvals & company
- **Firm details flow from your Company profile** into every application's Approvals — no more retyping
  per file. (Annual turnover and the partners/directors table were removed at your request.)
- New **"Verify company (ROC)"** button on Approvals (activates once the verification service is connected).

### Loans, EMIs & charges
- **Preferred EMI date (5th or 10th):** the repayment schedule is built on your chosen date, and any
  **past-due instalment shows how many days it's overdue**.
- **Per-scheme charges:** set the charges for a scheme once in Settings → they auto-fill on every new
  application for that scheme.
- **Sourcing & staff codes:** DSA/sourcing entries can carry an agent code and staff can carry an
  employee code — both are recorded on each application for tracking.

### Rejections
- Capture a **CIBIL rejection category** when rejecting a file, and **export all rejections as a CSV**
  for reporting.

---

*A few items above (Aadhaar auto-fetch, document signature auto-read, company/ROC verification) are built
and waiting on their verification service to be connected — they switch on with no further update.*
