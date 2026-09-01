# Test Data #2 — for the TESTER (sign in as STAFF)

Fill this application in exactly as below. As a **Staff** user you'll be **stopped twice** by approval
gates — that's on purpose. Each time, click **Request admin approval**, and the admin will approve it
from their side. Once approved, carry on.

---

## Sign in

- Open the **LOS-LMS.exe** app window (the one you were sent). It connects on its own.
- Log in with the **Staff** email + password the admin gave you (e.g. `staff.imran@yourco.local`).
- If you set a new password on first sign-in, remember it.
- If you see **"Can't reach the server right now"** → the admin's server isn't up yet; wait, then click **Retry**.

---

## Stage 1 — Customer Details

**Personal information (all 8):**

| Field | Value |
|---|---|
| Full name | Imran Shaikh |
| Date of birth | 1990-11-02 |
| Gender | Male |
| Marital status | Married |
| Father / spouse name | Yusuf Shaikh |
| Customer category | Individual |
| Nationality | Indian |
| Mother tongue | Urdu |

**Contact details (all 10):**

| Field | Value |
|---|---|
| Mobile | 9767122334 |
| Alternate mobile | 9890122335 |
| Email | imran.shaikh@example.com |
| Address line 1 | 31 Mominpura |
| Address line 2 | Opp Jama Masjid |
| City | Nagpur |
| State | Maharashtra |
| PIN code | 440018 |
| Residence type | Rented |
| Years at address | 3 |

**Identity:** PAN `DEFPS5678L` · Aadhaar `5678 1234 9012`

> **Click *Save draft* FIRST**, then *Complete stage →*. (The Complete button stays greyed out until
> you save — that's normal.) Alternate mobile and Address line 2 are **required**.

### 🔒 GATE #1 — you'll be stopped here

When you click **Complete stage →** you'll see:
**"Blocked — CIBIL check required. Request admin bypass to continue."**

➡️ Click **Request admin approval**, type a short reason (e.g. "CIBIL pending, please approve for test"),
and submit. Tell the admin. Once they **Approve**, click **Complete stage →** again — it will now go through.

---

## Stage 2 — Loan & Security

**Loan Details (10):**

| Field | Value |
|---|---|
| DSA / sourcing channel | Al-Falah Motors |
| Sourcing branch | Nagpur Central |
| Scheme | CV-STD-2026 |
| Requested amount | ₹8,50,000 |
| Tenure (months) | 54 |
| ROI % | 14.00 |
| Processing fee | ₹8,500 |
| Advance EMI | 1 |
| Repayment mode | NACH |
| Expected disbursal date | 2026-09-20 |

**Security Details (12):**

| Field | Value |
|---|---|
| Make / model | Mahindra Bolero Pickup |
| Year | 2023 |
| Registration no. | MH31 CD 5678 |
| Chassis no. | MA1TA2BC3DE456789 |
| Engine no. | M2DICR55221 |
| Invoice no. | INV-2026-1205 |
| Invoice date | 2026-08-22 |
| Invoice value | ₹9,80,000 |
| Insurer | Bajaj Allianz |
| Policy no. | PL-990145 |
| Policy expiry | 2027-08-21 |
| Assessed value | ₹9,50,000 |

**Reference Details (2 rows):**

| Name | Mobile | Relationship | Address |
|---|---|---|---|
| Salim Ansari | 9975055667 | Brother-in-law | Sadar, Nagpur |
| Farhan Qureshi | 9822066778 | Business partner | Itwari, Nagpur |

**Viability:** Monthly income `₹70,000` · Monthly expenses `₹30,000`

### 🔒 GATE #2 — you'll be stopped again

When you click **Complete stage →** you'll see:
**"No loan cap configured for this vehicle — admin action required."**
*(This is because the Mahindra Bolero Pickup isn't in the loan-cap catalog.)*

➡️ Click **Request admin approval** again, add a reason, submit, and tell the admin.
Once they **Approve** the cap bypass, click **Complete stage →** — it goes through.

---

## Stage 3 — Bank & Financial

| Field | Value |
|---|---|
| IFSC | HDFC0001042 *(if it doesn't auto-detect a bank, just pick one from the dropdown)* |
| Account number | 003701555666 |
| Account type | Savings |
| Account holder name | Imran Shaikh |
| Banking vintage | 4 years |

Open the **CAM** tab, then *Complete stage →*.

---

## Stage 4 — Document Checklist

Nothing blocks here. Just click **Complete stage →**.

---

## Stage 5 — Reports (RCU)

| Field | Value |
|---|---|
| Mode | Sampled |
| Branch | Nagpur Central |
| Vendor | Verify India Pvt Ltd |
| Initiation date | (today) |
| TAT days | 3 |
| Per-applicant outcome | **Recommended** (for every party) |

Set each party's Status to **Recommended** → **Complete stage →**. Done — back to the dashboard.

---

### Summary of what you're testing
You got blocked twice (CIBIL + vehicle cap), raised a request each time, the admin approved from their
Inbox, and you finished all 5 stages. That's the admin-approval workflow working end to end.
