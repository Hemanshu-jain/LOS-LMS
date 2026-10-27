# 03 — LMS: EMI collection through DigiCollect (Phase 5)

**Starting point:** today the app ends at *Release funds*. The repayment schedule is computed on screen
(`Services/Underwriting/LoanMath.cs` `Schedule(...)`, `RepaymentScheduleModal.razor`) and never stored. There is
no instalment, receipt, bounce or DPD record (`MigratedLoan` is an import-only summary). DigiCollect can
debit the borrower's account against a registered mandate, but something has to decide **what** to debit,
**when**, and **record** the outcome. That is a minimal servicing ledger, and it comes first.

Prerequisite: Phase 3 (`EnachMandate.Status == Registered` with a UMRN).

---

## 1. Minimal servicing ledger (new, additive)

| Table | Key columns | Notes |
|---|---|---|
| `LoanAccount` | `Id`, `CompanyId`, `ApplicationId` (unique), `LoanAccountNo`, `Principal`, `Roi`, `Tenure`, `FirstEmiDate`, `EmiDay`, `Status` (Active / Closed / Foreclosed / WrittenOff), `MandateId?`, `OpenedAt`, `ClosedAt?` | Created **once** at *Release funds* from the sanctioned values |
| `Instalment` | `Id`, `LoanAccountId`, `No`, `DueDate`, `Emi`, `Principal`, `Interest`, `Status` (Due / Presented / Paid / Bounced / PartPaid / Waived), `PaidAmount`, `PaidOn?` | **Snapshot** of `LoanMath.Schedule` at disbursal (incl. the first-EMI override and rounding from `Disbursement`). Never recomputed afterwards |
| `Presentation` | `Id` (our id = Digio `unique_key`), `InstalmentId`, `Attempt` (1, 2…), `Umrn`, `Amount`, `SettlementDate`, `DigioPlanId`, `DigioTxnId`, `Status`, `FailureCode`, `FailureReason`, `Utr` | One row per debit attempt |
| `Receipt` | `Id`, `LoanAccountId`, `InstalmentId?`, `Source` (NACH / UPI / Manual / Cash / Cheque), `Amount`, `ReceivedOn`, `Reference` (UTR), `PresentationId?`, `RecordedBy` | The only thing that marks an instalment paid |
| `Charge` (exists for Stage 7) | — | Reuse it or add `LoanCharge` for bounce charges. Policy decision §9.9 |

DPD, overdue and outstanding are **computed** from these, never stored (the same principle the repo
already applies to disbursement amounts).

---

## 2. Debit presentation

**Approach: one one-time presentation per instalment.** It is simpler and correct for a varying first EMI,
rounding, part-payments and re-presentation, compared with one long recurring plan.

- **Daily job** (`CollectionScheduler`, BackgroundService, runs at 18:00 IST): for every `Instalment` with
  `Status = Due` and `DueDate` in [today + 1, today + N (settings: 2)], with an active loan and a registered mandate,
  and no open `Presentation`: create one.
- **Cut-off:** Digio needs the request by late night on **T−1** for a debit on day T, and creates the NPCI file at
  00:00 on T. Holidays roll forward automatically (NPCI calendar). Create by 18:00 on T−1 at the latest.
- **Request:** `POST /v3/client/nach_debit/scheduled/register` with
  `{ umrn, amount_in_paise: EMI×100, settlement_date: DueDate (format: capture in Phase 0), frequency: "onetime",
     corporate_account_number: settings, corporate_config_id: settings, customer_name, customer_account_number,
     destination_account_type, narration: "EMI {No} {LoanAccountNo}", unique_key: presentation.Id }` → plan `id`,
     `upcoming_transaction.state`.
  `unique_key` is our idempotency key: a retry with the same key must not double-debit. **Verify this in sandbox (Phase 0).**
- **Webhooks** (`NACH_DEBIT`): `TXN.CREATED` → `SUBMITTEDTOBANK` → `TRANSUCCESS` / `SPOBANKACCEPT` (in progress) →
  **`NACH.DEBIT.SUCCESS`** (paid) | **`NACH.DEBIT.FAILED`** (bounced) | `DELAYED` | `SKIPPED` | `TRANFAIL` / `SPOBANKREJECT`
  (not presented: our side or the bank's side, fix and retry). Always confirm with `GET /v3/client/nach_debit/transaction/{txnId}`
  (`details.status`, `failure_code`, `failure_reason`, `utr`).
- **On success:** create the `Receipt` (NACH, UTR), mark the instalment Paid, and close the presentation. **On failure:** mark
  the instalment Bounced, store the code, and apply the bounce-charge policy.

## 3. Bounces & re-presentation (policy-driven, not hard-coded)

- Digio and NPCI return codes (`04` §6) say whether **re-presentation (RPN)** is allowed (e.g. *Balance
  insufficient* → yes; *Account closed* or *Mandate cancelled* → no, which escalates the mandate as broken).
- Digio-side rules: **DG103**: no new debit within 3 days of the last failure. **DG104**: a debit already exists for
  this UMRN. **DG101**: on a Monthly-frequency mandate only one success per period (which is why `Adhoc` is recommended).
- Re-presentation: settings `MaxAttempts` (e.g. 2) and `RepresentAfterDays` (≥ 3). A non-RPN code → stop and raise a
  collections task. Nothing is re-presented automatically after a non-RPN code.
- Officer actions: **Cancel** a scheduled debit (`POST /v3/client/nach_debit/transaction/action` `{action:"cancel"}`),
  **Reattempt** (`{action:"reattempt"}`), and pause or resume a plan (`…/scheduled/action`).

## 4. Settlement reconciliation

- Daily: `GET /v3/client/nach_debit/requester_settlement/{id}` (consolidated settlement) → match the credited total
  to the Receipts for that settlement date; differences go to the health page.
- Show the bank credit date vs the debit date, because the cash lands in the NBFC's account later than the debit.

## 5. Closure

- Instalments all Paid, or foreclosure recorded → `LoanAccount.Status = Closed` → **cancel the mandate**:
  `POST /v3/client/mandate/registration_cancel` `{ umrn, digio_mandate_id, reason: C00x }` (reason codes confirm
  with Digio) → event `MNDT.CANCELINIT`. Cancel any future `Presentation`. (Leaving a mandate live after closure
  is a customer-harm and regulatory issue.)
- `MNDT.BANKCANCEL` / `UPI.MNDT.REVOKED` while the loan is active → mark the mandate broken and raise a
  collections task (no further debits).

## 6. CKYC upload (account opened) — P1*
- After *Release funds* (the account-based relationship starts), queue a CKYC upload for each individual party:
  `POST /v3/client/kyc/ckyc/register` (multipart: `register_ckyc_request` JSON + `image1..3`: photo, POI, POA) → `reference_id`.
  Webhook `CKYC.STATUS.UPDATE` (`AWAITING_APPROVAL / APPROVED / REJECTED / FAILURE` + remarks) → store the KIN on approval.
- The regulatory deadline and field mapping are **confirmed with compliance and Digio's private CKYC doc**.

## 7. UI

- **Loan account page** (new, `/loans/{id}`, linked from the application once disbursed): schedule (snapshot),
  status per instalment, presentations with Digio status, receipts, bounces, DPD, mandate state, and actions
  (record manual receipt, cancel/reattempt debit, cancel mandate on closure).
- **Collections dashboard** (new, `/collections`): due in the next 3 days, presented today, bounced (by code),
  re-presentation queue, broken mandates, settlement mismatches. Branch filter (existing officer and branch filters).

## 8. Acceptance (sandbox)
- [ ] Disbursing creates exactly one `LoanAccount` and the instalment snapshot (equal to the modal's schedule).
- [ ] The scheduler creates one presentation per due instalment; a re-run creates none (idempotent).
- [ ] A success webhook creates one receipt (replay → still one).
- [ ] A failure webhook marks the instalment bounced; RPN-allowed codes re-present after N days, others do not.
- [ ] Closing the loan cancels the mandate and future presentations.
- [ ] The reconciler recovers a missed debit webhook.
