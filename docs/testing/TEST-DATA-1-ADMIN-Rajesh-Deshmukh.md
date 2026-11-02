# Test Data #1 — for YOU (sign in as ADMIN)

This application flows straight through with no blocks, because an **Admin bypasses every gate
automatically**. It proves the whole 5-stage pipeline end to end. Your friend's file (Test Data #2)
is the one that exercises the admin-approval flow — which **you** clear from the Admin Inbox.

---

## Sign in

- Open the app window (or the shareable link in a browser).
- Use the admin login from **`publish\server\app\FIRST-RUN-LOGIN.txt`**
  (email like `admin@loslms.local` + the temporary password shown there).
- You'll be asked to set a new password on first sign-in.

## One-time setup (do this once, before either test)

Go to **Company Setup** and complete:

1. **Company** → name it, e.g. `Sai Finance Pvt Ltd`.
2. **Branches** → add `Pune West` and `Nagpur Central`.
3. **Vehicle Caps** tab → add ONE cap:
   - Make `Tata` · Model `Ace Gold` · Year `2024` · Max loan `₹6,50,000`
   - *(Only this one — your friend's vehicle is deliberately not in the catalog, so his file hits the cap gate.)*
4. **Users** tab → create a **Staff** user for your friend, e.g. `staff.imran@yourco.local` + a password.
   Send him those and the file `TEST-DATA-2-STAFF-Imran-Shaikh.md`.

---

## Stage 1 — Customer Details

**Personal information (all 8):**

| Field | Value |
|---|---|
| Full name | Rajesh Deshmukh |
| Date of birth | 1985-07-14 |
| Gender | Male |
| Marital status | Married |
| Father / spouse name | Madhukar Deshmukh |
| Customer category | Individual |
| Nationality | Indian |
| Mother tongue | Marathi |

**Contact details (all 10):**

| Field | Value |
|---|---|
| Mobile | 9822011345 |
| Alternate mobile | 9922011346 |
| Email | rajesh.deshmukh@example.com |
| Address line 1 | 14 Shivaji Nagar |
| Address line 2 | Near Ganesh Mandir |
| City | Pune |
| State | Maharashtra |
| PIN code | 411005 |
| Residence type | Owned |
| Years at address | 8 |

**Identity:** PAN `ABCPD1234K` · Aadhaar `4321 8765 2109`

> Order matters: **click *Save draft* first** (dedupe only runs on save), THEN *Complete stage →*.
> Alternate mobile and Address line 2 are **required** even though they look optional.

---

## Stage 2 — Loan & Security

**Loan Details (10):**

| Field | Value |
|---|---|
| DSA / sourcing channel | Patil Motors |
| Sourcing branch | Pune West |
| Scheme | CV-STD-2026 |
| Requested amount | ₹6,00,000 |
| Tenure (months) | 48 |
| ROI % | 13.25 |
| Processing fee | ₹6,000 |
| Advance EMI | 1 |
| Repayment mode | NACH |
| Expected disbursal date | 2026-09-15 |

**Security Details (12):**

| Field | Value |
|---|---|
| Make / model | Tata Ace Gold |
| Year | 2024 |
| Registration no. | MH12 AB 1234 |
| Chassis no. | MAT445566GH778899 |
| Engine no. | 275IDI99887 |
| Invoice no. | INV-2026-1180 |
| Invoice date | 2026-08-20 |
| Invoice value | ₹7,20,000 |
| Insurer | ICICI Lombard |
| Policy no. | PL-778812 |
| Policy expiry | 2027-08-19 |
| Assessed value | ₹7,20,000 *(LTV ≈ 83%)* |

**Reference Details (2 rows):**

| Name | Mobile | Relationship | Address |
|---|---|---|---|
| Ganesh Kulkarni | 9820033445 | Friend | Kothrud, Pune |
| Anil More | 9730044556 | Colleague | Aundh, Pune |

**Viability:** Monthly income `₹95,000` · Monthly expenses `₹35,000`

*Complete stage →*

---

## Stage 3 — Bank & Financial

| Field | Value |
|---|---|
| IFSC | HDFC0004412 *(auto-detects "HDFC Bank")* |
| Account number | 50100234567890 |
| Account type | Savings |
| Account holder name | Rajesh Deshmukh |
| Banking vintage | 6 years |

*(Statement upload / penny-drop show "not configured" — that's expected, no integrations in this build.)*
Open the **CAM** tab to generate CAM.pdf, then *Complete stage →*.

---

## Stage 4 — Document Checklist

Nothing blocks here. Just click **Complete stage →**.
*(Optional: PAN / Aadhaar / Photograph are read live from Stage 1; Bulk upload fills the active party's slots.)*

---

## Stage 5 — Reports (RCU)

| Field | Value |
|---|---|
| Mode | Screened |
| Branch | Pune West |
| Vendor | Verify India Pvt Ltd |
| Initiation date | (today) |
| TAT days | 3 |
| Per-applicant outcome | **Recommended** (for every party) |

Set each party's Status to **Recommended** (verification date stamps itself) → **Complete stage →**.
You return to the dashboard — this file is done.

---

## Your other job: approve your friend's requests

While your friend works through Test Data #2 (as Staff), he will get blocked and raise requests.
Open **Admin → Inbox** (the Admin Inbox / Approvals area). Requests appear **live, no refresh needed**:

1. **CIBIL bypass** (he's stuck on Stage 1) → click **Approve**.
2. **Vehicle-cap bypass** (his Mahindra isn't in the catalog) → click **Approve**.

To also test a rejection: click **Deny** with a note — he'll see your reason and stay blocked.
