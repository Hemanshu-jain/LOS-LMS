# 01 — Foundation (Phase 1a + Phase 1)

The platform layer. Build it once and well: every feature in `02` and `03` is a thin service on top of it.
Code references are to the current repo (`LosLms/...`).

---

## 0. What exists today (keep, extend — do not rewrite)

| Piece | File | Keep / change |
|---|---|---|
| Credentials per company in the encrypted vault (`digio:{companyId}`) | `Services/Integrations/DigioClient.cs`, `Services/PasswordVault.cs` | **Keep** |
| `PostAsync` gate: keys → `ApiBillingService.CheckAccessAsync` → Basic auth → meter | `DigioClient.cs` | **Extend** (see §2) |
| Sandbox `https://ext.digio.in:444`, production `https://api.digio.in` | `DigioClient.cs` | **Keep** (matches Digio's environments page) |
| Webhook `POST /webhooks/digio/{companyId}` + HMAC `X-Digio-Checksum` | `Program.cs` (~480–535) | **Change** response policy (§4) |
| `ApplyWebhookAsync` (DigiSign events only) | `DigioClient.cs` | **Move** into the event processor (§4) |
| Rate card + usage log + monthly invoice + pause | `DigioRates.cs`, `ApiBillingService.cs`, `Models/Integrations/ApiBilling.cs` | **Keep**, add codes (§10) |
| SuperAdmin keys page | `Pages/SuperAdmin/DigioKeys.razor` | **Extend** (§7) |
| Penny drop, e-Sign dispatch | `BankFinancial.razor`, `EsignService.cs` | Migrate onto the foundation in Phases 2–3 |
| Tests: checksum + signed-only-by-webhook | `LosLms.Tests/DigioTests.cs` | **Keep** and extend |

---

## 1. Phase 1a — hotfixes to what is live today (do first, small PR)

1. **Webhook must not block Digio's queue.** Today it returns `400` (bad id, oversize, bad JSON) and `401`
   (bad checksum). Digio's queue is head-of-line per account: one stuck event blocks every later one,
   and after 24 h of failures Digio disables webhooks for the account.
   - Bad or unknown company id → `200 "ignored"` + warning log.
   - Checksum missing or invalid → `200 "ignored"` + warning log + rate-limited Admin alert ("Digio webhook
     signature mismatch — check the webhook secret on the Digio keys page"). Never process it.
   - Unparseable JSON with a **valid** checksum → `200`, store it as `Failed` in the inbox (§4), alert.
   - Keep the 1 MB cap, but still answer `200`.
2. **Multi-signer-safe signing.** `doc.signed` can arrive per signer. Set `Signed` only when the payload's
   `agreement_status == "completed"`. Confirm with `GET /v2/client/document/{id}` when it is unclear. Map
   `doc.sign.failed` and `doc.sign.rejected` as today, but keep the reason (`error_code`, `error_message`, and the
   `validation_results` summary for a rejection).
3. **Keep the signed agreement.** On completion, `GET /v2/client/document/download?document_id=…` →
   `IFileStore` under `App_Data/uploads/{app}/agreement/signed-{DID}.pdf`. Add `Disbursement.SignedAgreementFilePath`.
   Show a download link on Post Sanction. The signed PDF is the legal artefact; today it is never fetched.
4. Tests: one for each rule above, plus a replay of the documented `doc.signed` sample with two signing parties.

---

## 2. `DigioClient` v2

Keep `PostAsync` behaviour and signature for existing callers. Add:

```csharp
Task<DigioResult> SendAsync(DigioCall call);            // one implementation behind every verb
// DigioCall: CompanyId, ApiCode (nullable), Method (GET/POST), Path, JsonBody | Multipart parts,
//            Query, ApplicationId, PartyId, Purpose, IdempotencyKey, Timeout, ExpectBinary
Task<DigioBinaryResult> DownloadAsync(...);              // signed PDF, media, KYC report, NACH form
```

Rules:
- **Environment** comes from `DigioCredentials.Production`. Every result carries `Environment`
  ("Sandbox" or "Production"), and so does every row it writes.
- **Base URLs**, in one static class `DigioEndpoints`: API (`ext.digio.in:444` / `api.digio.in`), gateway
  (`https://ext.digio.in/#/gateway/login/` / `https://app.digio.in/#/gateway/login/`), enterprise dashboard
  (`ext-enterprise.digio.in` / `enterprise.digio.in`), webhook source IPs (sandbox `35.154.20.28`,
  production `13.126.198.236`).
- **Timeouts:** 30 s for synchronous verifies, 60 s for uploads. **No automatic retry** on POSTs that
  create anything. Retry is the user's "Try again", and it reuses the same idempotency key. GETs may
  retry once on a network error.
- **Error mapping** (Digio's `GenericExceptionDto` is `{code, message, details}`):
  401 → "Digio keys rejected — check environment/keys"; 403 → "Not enabled on your Digio account";
  400 → Digio's `message`; 5xx or timeout → "Digio unavailable, try again". Never surface raw bodies to the UI.
- **Request log:** every call writes a `DigioRequest` row (§3) **before** sending (status `Sending`) and
  updates it after. A crash mid-call leaves an inspectable row, never a silent gap.
- **Logging:** path + status + our request id + Digio id. **Never** bodies, PAN, Aadhaar, account numbers or names.
- **Metering:** unchanged rules (log failures, bill successes). The `ProviderRef` is Digio's id.

---

## 3. Data model (new tables, one additive migration)

All tables are company-scoped. The application-linked ones follow the existing filter pattern
(`x.Application!.CompanyId == _companyId`); company-only ones use `CompanyId == _companyId`. Every string has `[MaxLength]`.

### 3.1 `DigioRequest`: one row per thing we asked Digio for
| Column | Notes |
|---|---|
| `Id` (string 26, ULID) | **Our** id. Used as `unique_request_id` / `reference_id` / `callback` / `customer_ref_number` → idempotency + webhook correlation |
| `CompanyId`, `ApplicationId?`, `PartyId?` | scope |
| `Environment` | Sandbox / Production |
| `Product` | `KYC_ID`, `KYC_STUDIO`, `KYC_MASK`, `KYC_FACE`, `AML`, `BANK`, `RC`, `KYB`, `ESIGN`, `MANDATE`, `DEBIT`, `CKYC`… |
| `Operation` | e.g. `PAN_VERIFY`, `DIGILOCKER_AADHAAR_PAN`, `SIGN_AGREEMENT`, `ENACH_CREATE` |
| `DigioId` | `KID…`, `DID…`, `ENA…`, plan/txn ids; unique per company when set |
| `Status` | our normalised state: `Sending, Sent, WaitingForCustomer, Processing, Succeeded, Failed, Rejected, Expired, Cancelled` |
| `ProviderStatus` | Digio's raw status string (for support) |
| `InputFingerprint` | SHA-256 of the inputs that matter (PAN+name+DOB, account+IFSC, agreement bytes…) → **staleness check** |
| `RequestedByUserId`, `RequestedAt`, `CompletedAt`, `LastPolledAt`, `PollAttempts`, `ExpiresAt` | lifecycle |
| `CustomerChannel` | Link / OnDevice / None; `NotifiedTo` = masked mobile/email only |
| `ErrorCode`, `ErrorMessage` | Digio's, trimmed |
| `ResultJson` | **minimised** result (only the fields we use), AES-GCM encrypted with the vault key |

### 3.2 `DigioWebhookInbox`: every webhook received, before processing
`Id`, `CompanyId`, `DigioEventId` (payload `id`, e.g. `WHN…`; **unique with CompanyId** → de-dupe),
`Event`, `EntityType`, `EntityId`, `ReceivedAt`, `SourceIp`, `Status` (`Pending/Processed/Ignored/Failed`),
`Attempts`, `NextAttemptAt`, `LastError`, `PayloadJson` (encrypted), `ProcessedAt`.

### 3.3 `VerificationEvidence`: the thing gates read
`Id`, `CompanyId`, `ApplicationId`, `PartyId?`, `Subject` (`PAN`, `AADHAAR`, `FACE`, `LIVENESS`, `AML`, `CKYC`,
`BANK_ACCOUNT`, `VEHICLE_RC`, `GSTIN`, `CIN`, `DIN`, `AGREEMENT_SIGNED`, `MANDATE`…), `Verdict`
(`Verified, Mismatch, Failed, Unavailable, AcceptedMismatch`), `Score?`, `Summary` (short, non-PII text
such as "Name match 92 · DOB match · Aadhaar seeded"), `DigioRequestId`, `Environment`, `InputFingerprint`,
`IsStale` (computed on read by comparing the fingerprint with current data), `DecidedByUserId?` + `DecisionNote?`
(for `AcceptedMismatch`), `CreatedAt`.

Existing booleans (`Party.PanVerified`, `AadhaarVerified`) and status strings (`BankDetail.PennyDropStatus`,
`EnachMandate.NameMatchStatus`, `Disbursement.AgreementEsignStatus`) **stay** for display and backwards
compatibility. They are written only by the evidence writer, never by the UI.

### 3.4 `DigioSettings`: per-company, non-secret configuration
Product toggles (`PanVerify`, `DigiLocker`, `AadhaarMask`, `Aml`, `IdOcr`, `Face`, `Rc`, `Kyb`, `Esign`,
`Estamp`, `Enach`, `Debits`, `Ckyc`…), template names, default customer channel
(`SMS|WHATSAPP|ALL|EMAIL`), link expiry days, NACH `corporate_config_id` (EMI) and (security), sponsor/corporate
account number, NACH category codes, max-amount multiplier, lender signatory (name, email), e-stamp tag
per state, brand logo URL (https), sign-placement defaults. Secrets stay in the vault, never here.

### 3.5 Aadhaar at rest (Phase 2, listed here because it is a schema change)
`Party.AadhaarLast4` (4), `Party.AadhaarHash` (64, HMAC-SHA256 with a server key that is **not** the vault
key), plus a migration backfill that computes both from the existing `Aadhaar` and then **nulls `Aadhaar`**.
Dedupe (`CustomerDetails.razor` ~1527–1565) switches to `AadhaarHash`. The input box keeps accepting
12 digits; they are only used transiently (to compute the hash and last 4, or to pass to Digio). Gated by the
compliance decision (§9.4 of the master plan). Ship it as a separate migration with a tested rollback note.

---

## 4. Webhook pipeline

```
POST /webhooks/digio/{companyId}
  1. read body (≤ 1 MB)  2. look up company + webhook secret  3. verify X-Digio-Checksum (existing ChecksumMatches)
  4. parse minimal envelope {id, event, entities, created_at}
  5. INSERT DigioWebhookInbox (unique CompanyId+DigioEventId → duplicate = no-op)
  6. signal the processor (Channel<long>)  7. return 200 "ok"      ← total budget well under 1 s
```
- **Every** branch returns 200 (see Phase 1a). Only a valid signature is stored and processed.
- Optional IP check: log `SourceIp` (from `CF-Connecting-IP` / forwarded headers under the existing
  `Proxy:ForwardLimit` trust). Alert only; do not reject, because proxies differ between desktop and cloud.

**`DigioEventProcessor` (BackgroundService):**
- Claims `Pending` rows (oldest first), processes each in its own DbContext using
  `TenantContext.ForSeeding()`-style scoping + `IgnoreQueryFilters()` **and** an explicit `CompanyId == row.CompanyId` filter.
- Routes by product: `doc.*` → e-Sign handler; `KYC_REQUEST_*` / `KYC_ACTION_*` / `DOCUMENTS_PULLED_*` →
  DigiStudio handler; `MNDT.*` / `APIMNDT.*` / `UPI.MNDT.*` → mandate handler; `NACH.DEBIT.*` / UPI debit →
  collections handler; `CKYC.STATUS.UPDATE` → CKYC handler.
- Each handler finds its `DigioRequest` (by `reference_id` / `DigioId`), applies a **forward-only**
  transition, writes evidence and domain fields, meters on success where billing is on completion, and
  raises `DigioStatusNotifier`.
- An unknown entity or event → `Ignored` (logged). An exception → `Failed` with backoff (1, 5, 15, 60 min, max 8
  attempts) → then an Admin alert. **Never** throws back to the HTTP request.
- **Handlers fetch the authoritative object** (status API) when the webhook payload is partial. Sandbox
  payloads are known to be thin, and the status API is the source of truth.

**`DigioReconciler` (BackgroundService, every 5 min):** for every `DigioRequest` not final and older than
2 min (sandbox: 6 min), call its status API with backoff (5 min → 15 → 60 → 4 h; stop at `ExpiresAt` + 1 day),
then apply the same handler. Rate-limit per company. This makes desktop servers without a tunnel and a
sleeping Render instance still converge.

**Startup catch-up:** on boot, sweep `Pending` inbox rows and non-final requests once.

---

## 5. Live UI updates

`DigioStatusNotifier` is a singleton in-process pub/sub in the style of `AdminRequestNotifier`, keyed by
`applicationId`. Stage pages subscribe in `OnInitialized` and unsubscribe in `Dispose`, and re-load only
the affected section on notify. **Autosave must never write Digio-owned fields.** List them in one place
(`DigioOwnedFields`) and test that no stage save copies them, extending the existing
`AgreementEsignStatus` rule in `PostSanction.razor` (~1339).

---

## 6. Shared UI kit (`Components/Shared/Digio/`)

| Component | Purpose |
|---|---|
| `VerifyBadge` | One chip for every state: Not run · In progress · Waiting for customer · ✓ Verified · ✗ Mismatch · Failed · Expired · Stale · Not configured · Paused (bill) · **SANDBOX** tag. Tooltip = evidence summary + time + who |
| `VerifyButton` | Disabled-with-reason when inputs are missing or invalid, the product is off, or a request is already in flight; shows a spinner; one click → one request (idempotent) |
| `CustomerActionPanel` | For async flows: what was sent, channel, masked destination, expiry countdown, **Resend**, **Open on this device**, **Refresh status**, **Cancel**; mini timeline of events |
| `DigioDiff` | "Digio says X, file says Y" with **Apply** (fills) / **Keep mine** / **Accept mismatch** (Admin, note required → `AcceptedMismatch` evidence) |
| `EvidenceList` | Per-application list of evidence and requests (Summary Rail link + a section on Document Checklist) |
| `ConsentCheckbox` | Captures "Customer consents to … (text vN)" before any Aadhaar / AML / CKYC action; stored with the request |

Styling: reuse existing `.verify-button`, `.gate`, `.chip`, `.pill` classes. No inline scripts (CSP).

---

## 7. Configuration & admin pages

- **SuperAdmin → Digio keys** (`/system/digio`): keep the keys and the sandbox/production toggle. Add product
  toggles, a **Test connection** button (makes a harmless authenticated call, e.g. `GET /v3/client/mandate/banks`
  if DigiCollect is enabled, otherwise a PAN verify of a Digio sandbox test PAN; shows OK / 401 / 403), the webhook URL
  built from **`PublicBaseUrl`** (not `Nav.BaseUri`, which can be an internal address on the desktop host),
  and a "last webhook received" timestamp.
  Switching sandbox → production requires typing the company code. It never mixes evidence: old sandbox
  evidence stays badged.
- **Company Admin → Company Setup → Integrations tab** (new): the operational `DigioSettings` (channel,
  expiry, signatory, NACH config, e-stamp tags, minimum KYC evidence policy). Read-only view of which
  products are enabled.
- **Admin → Integrations health** (new, `/admin/integrations`): inbox backlog, failed events (with retry),
  pending customer actions, requests stuck more than 24 h, last webhook time, sandbox/production, links to
  `/admin/api-usage`.

---

## 8. Customer hand-off (link / on-device) and the return page

- **Link mode:** create with `notify_customer: true` + `customer_notification_mode` from settings. Store the masked destination.
- **On-device mode:** build the gateway URL:
  `{gateway}{DigioId}/{randomTxn}/{identifier}?redirect_url={enc(PublicBaseUrl + "/digio/return?r=" + ourRequestId)}&logo={enc(logo)}`.
  This is documented for DigiSign (DID). **Phase 0 must verify it for KID (DigiStudio) and ENA (mandates)**.
  Mandates and sign requests can also return an `authentication_url` (`include_authentication_url: true`). Prefer that when present.
  Open it with `target=_blank`. The desktop shell already opens new windows in its viewer (`ShellWindow.OnNewWindowRequested`).
- **`/digio/return`** (anonymous-safe, static render): ignores `status` and `digio_doc_id` query values for
  state. It looks up `r`, enqueues an immediate reconcile for that request, and shows "Thank you, you can close
  this window". No PII on the page.
- **Resend** = regenerate the link (DigiStudio: `POST /user/auth/generate_token` or re-notify), never a new request.
- Expiry from settings (default 7 days for KYC, 10 for e-Sign, 7 for mandates). Expired → `Expired`, with an offer to create a new one.

---

## 9. Embedded Web SDK (deferred; the design is here so it never surprises us)

If ever needed: load `https://{ext|app}.digio.in/sdk/v11/digio.js` **only** on the pages that use it, through a
JS module in `wwwroot/js/digio-bridge.js`. That needs `script-src`, `frame-src` and `connect-src` for the Digio
origins, `Permissions-Policy: camera=(self "https://app.digio.in")`, and the iframe `allow="camera;microphone"`.
The WebView2 `OnPermissionRequested` must be restricted to the app origin and the Digio origins. `digio.init()`
must run on a user click (popup blockers). The callback only triggers a server reconcile (§2.3 of the master
plan). Follow the existing conditional Turnstile CSP pattern in `Program.cs` (~403–406).

---

## 10. Billing additions

Add to `DigioRates.Seed` (rates **TBC from the Digio quote/invoice**; 0.00 placeholders must not ship. A
test asserts every code used by a call site exists and is > 0 in production seed):
`KYC_PAN_VERIFY` (or reuse `KYC_ID_VERIFY`), `KYC_VEHICLE_RC`, `KYC_DL`, `AML_SCREEN`, `CKYC_SEARCH`,
`CKYC_UPLOAD`, `ESTAMP` (pass-through stamp value + fee, invoiced separately), `MANDATE_UPI`,
`KYC_LIVENESS` (if distinct from `KYC_SELFIE`), `KYC_REPORT`.
Rule: synchronous APIs are metered at call time. DigiStudio, e-Sign and mandates are metered on the completing
event (as e-Sign is today), **after confirming against Digio's invoice** which events Digio bills.

---

## 11. Test harness (no live network in tests, ever)

- `FakeDigioHandler : HttpMessageHandler` registered via `IHttpClientFactory` in tests. It is driven by fixtures
  in `LosLms.Tests/Fixtures/Digio/*.json`: **real sandbox responses captured in Phase 0, with PII masked**.
- Webhook tests post fixture bodies with a computed HMAC through the real endpoint (`WebApplicationFactory`
  if available; otherwise test the handler + processor directly, as `DigioTests` does today).
- State-machine tests per product: every legal transition, rejected backward transitions, duplicate events,
  out-of-order events (`completed` before `requested`), a reconciler result racing a webhook.
- Staleness tests: edit PAN after verify → evidence stale → gate closed.
- Sandbox-vs-production gate test.
- PII-in-logs test: capture log output for a full flow and assert there is no PAN or Aadhaar pattern.

---

## 12. Phase 1 acceptance criteria

- [ ] All of Phase 1a merged, with its tests.
- [ ] `DigioClient.SendAsync`/`DownloadAsync` with environment, idempotency key, error mapping and request log.
- [ ] Tables 3.1–3.4 migrated (MySQL + SQLite tests). Tenant filters are applied and tested.
- [ ] Inbox + processor + reconciler running. A replayed webhook is a no-op. A missing webhook is recovered by the reconciler.
- [ ] Notifier refreshes an open page (manual check, with two browsers).
- [ ] UI kit components render every state (a demo page in Development only, or a bUnit test if added).
- [ ] Keys page: toggles, Test connection, webhook URL from `PublicBaseUrl`, last-webhook time.
- [ ] Health page lists backlog and failures, with a retry button.
- [ ] Existing penny-drop and e-Sign still work, unchanged for users (regression test).
