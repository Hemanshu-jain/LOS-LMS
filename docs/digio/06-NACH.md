# 06 — NACH mandates end to end (DigiCollect)

The single place for everything NACH. It covers where the **NACH config id** Digio sent goes, both mandate
tabs on Post Sanction, every authentication mode, the lifecycle, amend/cancel/retry, the disbursal gate, and the
hand-off to EMI collection (`03-LMS-COLLECTIONS.md`). Builds on `01-FOUNDATION.md`. API details are in
`04-DIGIO-API-REFERENCE.md` and `appendix/`.

---

## 1. The NACH config id: what it is and where it lives

- Digio's `corporate_config_id` identifies **our corporate profile on Digio's NACH setup**: sponsor bank,
  utility code and the corporate account EMIs settle into. Every `POST /v3/client/mandate/create_form` must send it.
  Debit presentations may send it too (`03` §2).
- It is **not a password**, but it is account-specific and **differs between sandbox and production**. Rules:
  - Never in the repo, appsettings, logs or chat. The repo feeds the public update channel.
  - Stored per company in `DigioSettings` (`01` §3.4) as **two separate values per environment**:
    `EnachConfigIdSandbox` / `EnachConfigIdProduction`, and (if issued) `SecurityNachConfigIdSandbox` / `…Production`.
    The client picks the one matching the company's current environment, so switching sandbox → production can
    never send a sandbox id to production or the other way round.
  - Entered by the **SuperAdmin** on the Digio keys page (it belongs with the account setup Digio gave us),
    shown masked except the last 4 characters after saving. Company Admins see "NACH: configured (sandbox)".
  - **Validated on save:** call `GET /v3/client/mandate/banks?mode=API` (proves the keys and that DigiCollect is enabled).
    The config id itself is first exercised by the Phase 0 sandbox mandate (§9). A wrong id surfaces as a clear
    "NACH profile not found — check the config id" error mapped from Digio's 400.
- **Until Phase 1 builds `DigioSettings`, keep the id with you** (password manager / private note, labelled
  *Sandbox – EMI mandate*). In `DECISIONS.md` record only "received on <date>".

Other NACH values Digio or the sponsor bank must provide (also per environment, in `DigioSettings`):

| Setting | Used for | Source |
|---|---|---|
| `NachCorporateAccountNo` | debit presentations (`corporate_account_number*`) | Digio / sponsor bank |
| `NachCategoryEmi` (e.g. `L001`) | `management_category*` for the EMI mandate | sponsor bank (confirm) |
| `NachCategorySecurity` (e.g. `L002`) | security mandate | sponsor bank (confirm) |
| `NachFrequency` (`Adhoc` recommended) | `frequency*` | business (DECISIONS #7) |
| `NachMaxAmountMultiplier` (e.g. 1.5) | `maximum_amount = EMI × multiplier`, rounded up to ₹100 | business / compliance |
| `NachFinalDateBufferMonths` (e.g. 12) | `final_collection_date = last EMI + buffer` (allows bounce re-presentation after the last EMI) | business |
| `NachAllowedModes` | which of NetBanking / DebitCard / Aadhaar / eSign / Physical we offer | what Digio enabled |
| `NachLinkExpiryDays` (e.g. 7) | `expire_in_days` | business |

---

## 2. What exists today and what changes

| Today (`Components/Shared/MandateFlow.razor`, used by the **E-Nach** and **Security-Nach** tabs of Post Sanction) | With Digio |
|---|---|
| Step 1 Bank details: IFSC lookup + account entered twice | Keep. Also pick `destination_bank_id` from the **live banks** list (by IFSC prefix) and show which modes that bank supports |
| Step 2 Name match: always "Unavailable" | **Penny drop** on this account (reuse `BANK_ACCOUNT` evidence if the same account was already verified in Bank & Financial). The bank-returned holder name becomes `customer_name` |
| Step 3 Officer confirmation checkbox | Keep (audit record) |
| Mandate type Digital (NetBanking / DebitCard) / Physical | Digital → **API e-mandate** (NetBanking / DebitCard / **Aadhaar**); add **eSign e-NACH**; Physical → Digio **physical NACH** |
| Physical: officer picks Status = Pending / Registered / Rejected from a dropdown | **Removed for Digio-routed mandates.** Status comes only from Digio. The dropdown survives only when NACH is switched off for the company (manual mode, labelled as such) |
| Submit → "provider not configured" | Submit → `create_form` → `CustomerActionPanel` (link / open on this device / print form) |
| Statuses Pending / Registered / Rejected | Richer lifecycle (§5), mapped down to these three for the existing displays |

**EnachMandate** = the applicant's EMI account. **SecurityNachMandate** = the guarantor's account (`MandateHolder`),
a backup mandate. Each gets its own penny drop, its own Digio mandate and its own lifecycle.

### Model additions (additive migration, on **both** tables via `INachMandate`)
`DigioMandateId` (ENA…, 40), `AuthMode` (API / ESIGN / PHYSICAL, 20), `AuthSubMode` (net_banking / debit / aadhaar, 20),
`DestinationBankId` (40), `Frequency` (20), `CategoryCode` (10), `CollectionAmount`, `MaximumAmount`,
`FirstCollectionDate`, `FinalCollectionDate`, `DigioState` (raw `state`, 30), `BankState` (raw `bank_details.state`, 30),
`RejectReason` (300), `AuthenticatedAt`, `RegisteredAt`, `Environment` (10), `DigioRequestId` (26), `SignedFormPath` (400, physical).
`Status` keeps Pending / Registered / Rejected for existing screens; detail lives in `DigioState` / `BankState`.
`Umrn` is written **only** from Digio's mandate details.

---

## 3. Before a mandate can be created (validation, all server-side)

1. Company has DigiCollect enabled + a config id for the **current** environment.
2. Account number + IFSC present; IFSC resolves; the bank is in the live-banks list for the chosen mode.
3. Penny drop `Verified` on exactly this account+IFSC (fingerprint), not stale. `AcceptedMismatch` requires an Admin.
4. Loan terms known: EMI (`collection_amount` = regular EMI, **not** the loan amount: NPCI error AP13),
   first EMI date (`Disbursement.FirstEmiDate` / `Application.FirstEmiDate`), tenure → last EMI date.
5. `first_collection_date` ≥ tomorrow (IST); `maximum_amount` ≥ `collection_amount`; account type SAVINGS / CURRENT
   (NRE, PPF, PF, CC, minor and joint accounts are rejected by banks: AP16–AP20, AP31; warn when known).
6. No other live (non-final, non-rejected) mandate request for this tab (§6 idempotency).
7. Officer confirmation ticked; customer mobile or email present for link mode.

A failed check → `VerifyButton` disabled with the exact reason.

---

## 4. The three authentication routes

### 4.1 API e-mandate: net banking / debit card / Aadhaar (default)
`POST /v3/client/mandate/create_form`
```json
{ "corporate_config_id": "<per env>", "mandate_type": "CREATE", "auth_mode": "API",
  "customer_identifier": "<mobile or email>", "notify_customer": true, "include_authentication_url": true,
  "expire_in_days": 7,
  "mandate_data": { "customer_name": "<bank-verified holder name>", "customer_account_number": "…",
    "customer_account_type": "SAVINGS", "destination_bank_id": "<from banks API>",
    "customer_ref_number": "<our id>", "scheme_ref_number": "<application id>",
    "management_category": "L001", "is_recurring": true, "frequency": "Adhoc",
    "first_collection_date": "<capture format in Phase 0>", "final_collection_date": "…",
    "collection_amount": 12345, "maximum_amount": 18600,
    "customer_mobile": "…", "customer_email": "…" } }
```
→ `id` (ENA…), `state`, `authentication_url`. The customer authenticates on the bank's page through NPCI (net banking
or debit card) or on NPCI's Aadhaar page (OTP, then a bank OTP). UMRN + NPCI ACK arrive immediately in the auth
response; **final registration by the next working day**.

### 4.2 eSign e-NACH
Same call with `"auth_mode": "ESIGN"`. The customer Aadhaar-eSigns the mandate. eSigned XML → sponsor bank → NPCI →
destination bank. ACK by the next working day; **final in 3–7 days**.

### 4.3 Physical NACH
Same call with `"auth_mode": "PHYSICAL"` → `POST /v3/client/mandate/{ENA}/generate_file` → prefilled PDF (shown and
printed from Post Sanction; stored under `App_Data/uploads/{app}/nach/`). The customer wet-signs it. The officer scans
and uploads it → `POST /v3/client/mandate/submit_signed_mandate` (multipart `file`, `request.id`). Images → sponsor
bank → NPCI → destination bank; **final about 7 working days**. The officer's upload is recorded as an attestation; status still comes only from Digio.

Mode shown to the officer: from `NachAllowedModes` ∩ what the bank supports (live-banks flags `apiMandateNetBanking`,
`apiMandateDebitCard`, `apiMandateAadhaarCard`, `esignMandate`, `physicalMandate`).

---

## 5. Lifecycle (forward-only state machine)

```
Draft ──create_form──▶ Requested ──customer authenticates──▶ AuthSuccess ──▶ InBankProcessing ──▶ REGISTERED (UMRN final)
  │                        │                                     │                  │
  │                        ├─ AuthFailed (customer can retry)    │                  ├─ Rejected (sponsor/NPCI/destination bank, with reason)
  │                        └─ Expired (link not used)            │                  │
  └────────────────────────────────────────────────────────────────────────────────▶ Cancelled (by us) / BankCancelled / Revoked
```

| Digio signal | → state |
|---|---|
| create response `state=partial` | Requested |
| `APIMNDT.AUTHSUCCESS`, `MNDT.AUTHSUCCESS`, `state=auth_success/signed` | AuthSuccess |
| `MNDT.TRANSUCCESS`, `MNDT.SPOBANKACCEPT`, `MNDT.NPCIACCEPT`, `bank_details.state` ∈ {transfer_success, accepted_spo_bank, awaiting_ack, ack_received, awaiting_res} | InBankProcessing |
| `APIMNDT.DESTACCEPT`, `MNDT.DESTBANKACCEPT`, `bank_details.state=register_success` | **Registered** |
| `APIMNDT.AUTHFAIL`, `MNDT.AUTHFAIL`, `state=auth_failed` | AuthFailed |
| `APIMNDT.DESTREJECT`, `MNDT.TRANFAIL`, `MNDT.SPOBANKREJECT`, `MNDT.NPCIREJECT`, `MNDT.DESTBANKREJECT`, `register_failed`, `nack_received` | Rejected |
| `state=expired` | Expired |
| `MNDT.CANCELINIT` / `MNDT.BANKCANCEL` / `state=revoked` | Cancelled / BankCancelled / Revoked |

Rules:
- **Every webhook is confirmed** with `GET /v3/client/mandate/{ENA}` before a transition. That call is the source of truth.
- Reason text from `npci_auth_reject_reason` / `npci_auth_failed_error` / NPCI codes (AP01–AP33, `04` §6) is shown in plain words.
- **Multiple UMRNs:** customer retries (bank time-outs, closed screens) can produce several UMRNs under one ENA. Store
  the UMRN from Digio's mandate details **at the moment it reaches Registered**. Cancel by UMRN.
- The reconciler polls non-final mandates: every 15 min for 2 h after auth, then every 4 h up to 10 working days
  (physical/eSign), then flags "stuck" on the health page.
- `MandateType` / `DigitalMode` / auth mode are **locked** once a request exists. Changing them = cancel + new request.

---

## 6. Idempotency, retries, amend, cancel

- `customer_ref_number` = our `DigioRequest.Id` (length check in Phase 0). One live request per mandate tab; a double click
  returns the existing request. **Resend link** reuses the same ENA (re-notify or `authentication_url`).
- **AuthFailed / Expired** → "Try again" creates a new request (new ENA); the old one stays in the history.
- **Rejected** → show the reason. Fix the cause (wrong account type, name mismatch AP08, amount over limit AP10/AP12)
  → new request. Never auto-retry a rejection.
- **Amend** (`mandate_type: "AMEND"`, `POST /v3/client/mandate/create_form`) for a registered mandate when the EMI or
  dates change after re-scheduling. Admin-only action, re-authenticated by the customer.
- **Cancel** (`POST /v3/client/mandate/registration_cancel`, `umrn` preferred over `digio_mandate_id`, reason
  `C001–C004`: meanings to confirm with Digio) on loan closure (`03` §5), on rejection of the application after a mandate
  was registered, or when replaced. Admin-only, with a confirmation dialog.
- Regenerated loan terms after a mandate registered (EMI changed) → the mandate is flagged **Stale vs loan terms** →
  amend or cancel-and-recreate before release.

---

## 7. Disbursal gate & hand-off to collections

- `GateCheckService` (Post Sanction *Release funds*) reads the company policy (DECISIONS #8):
  - EMI mandate **Registered** (production, not stale vs the current EMI and account), or
  - **AuthSuccess / InBankProcessing** + an Admin override with a note (common because final registration lags by days),
  - Security mandate: required or optional per policy.
- After release, `LoanAccount.MandateId` points to the registered EMI mandate. Collections (`03`) present debits only
  against a **Registered** UMRN; BankCancelled / Revoked stops debits and raises a collections task.

---

## 8. UI (Post Sanction → E-Nach / Security-Nach tabs)

1. **Bank account** card (existing) + live-banks badge "Net banking ✓ Debit card ✓ Aadhaar ✗".
2. **Account check**: penny-drop chip (reused or run here).
3. **Mandate terms** (read-only, from loan terms + settings): EMI, max amount, frequency, first/last date, category.
4. **Route**: Net banking / Debit card / Aadhaar / eSign / Physical (only the allowed ones). Channel: Send link /
   Open on this device / Print form (physical).
5. `CustomerActionPanel`: sent-to (masked), expiry, Resend, Open, Refresh, Cancel.
6. **Timeline**: Requested → Authenticated → With sponsor bank → With NPCI → Registered (UMRN ••••1234), with an
   expected-by date per route (next working day / 3–7 days / ~7 working days).
7. Admin actions: Override for release (note), Amend, Cancel mandate.

Never show the full account number or UMRN in lists. Last 4 only.

---

## 9. Phase 0: NACH sandbox test (with the config id Digio sent)

Do this once the Phase 0 harness exists (`05` §3.1):
1. SuperAdmin enters the sandbox NACH config id (harness: read from the vault or a Development-only user-secret, never committed).
2. `GET /v3/client/mandate/banks?mode=API` → save a masked sample → note which modes the sandbox banks support.
3. `create_form` API mode with a test account, `notify_customer: false`, `include_authentication_url: true` → save the
   masked sample. Record the accepted **date format**, whether `customer_ref_number` accepts 26 characters, and the response `state`.
4. Open `authentication_url` → complete the sandbox bank flow → watch for `APIMNDT.AUTHSUCCESS` (sandbox webhook delay
   ~5 min) → `GET /v3/client/mandate/{ENA}` → save a masked sample.
5. **Ask Digio** how sandbox simulates `DESTACCEPT` / `DESTREJECT` (final registration) and debit success/failure.
6. Repeat 3 with `auth_mode: "PHYSICAL"` → `generate_file` → confirm the PDF.
7. Write the findings into `DECISIONS.md` §B.

## 10. Acceptance criteria (Phase 3, NACH part)
- [ ] The config id is stored per company and per environment, masked in the UI, absent from logs and repo; a production company never sends the sandbox id.
- [ ] Create is blocked with a precise reason for each failed check in §3.
- [ ] A double click creates one Digio mandate; Resend reuses it.
- [ ] Each route (API, eSign, physical) reaches its expected state in sandbox; webhook replays and out-of-order events don't move the state backwards.
- [ ] UMRN is written only from Digio's details; the manual Registered dropdown is gone when NACH is on.
- [ ] Release funds respects the policy; the override is audited.
- [ ] Missed webhooks are recovered by the reconciler; stuck mandates show on `/admin/integrations`.
- [ ] Cancel on closure works (sandbox) and is Admin-only.
