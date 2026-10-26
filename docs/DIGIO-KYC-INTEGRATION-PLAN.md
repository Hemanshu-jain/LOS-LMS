# Digio KYC integration — execution plan

Goal: turn the stubbed Digio KYC features on, using the **sandbox** credentials, one small PR-sized phase at a
time. Bank penny-drop is already built and is **out of scope** for now (Digio has not enabled it on this account).

Everything below was read from Digio's own docs/OpenAPI specs (documentation.digio.in) on 2026-10-26.
Re-check a spec before coding against it — the URLs are in "Where to read the spec".

---

## 0. Ground rules (read first)

1. **Never commit or print secrets.** Client ID/secret/webhook secret live only in the encrypted vault
   (SuperAdmin → Digio keys). Never log request bodies that contain PAN/Aadhaar numbers — log path + status only.
2. **Every call goes through `DigioClient.PostAsync`** (`LosLms/Services/Integrations/DigioClient.cs`). It already
   gives: not-configured → nothing sent, overdue bill → paused, Basic auth, sandbox/prod URL, usage metering.
   Do not create a second HttpClient path to Digio.
3. **Never invent data.** On not-configured / failed / unmatched, the officer sees an honest message and the form
   is untouched. `Apply` methods only fill **blank** fields (keep that behaviour).
4. **Meter with the right rate code** from `DigioRates.cs` (failed calls are logged, not billed — already handled).
5. **Sandbox ≠ real people.** Sandbox responses may be dummy. Match what Digio returns; don't assume it equals
   production. Use Digio's sandbox test values (ask Digio / check the mail) — do not run real customer PANs/Aadhaar
   through the sandbox.
6. One phase = one branch/PR, tests added, `dotnet test` green, docs updated.

Base URLs (already in code): sandbox `https://ext.digio.in:444`, production `https://api.digio.in`.
Auth: HTTP Basic `client_id:client_secret` (already in code).

---

## 1. What the docs say (endpoints we will use)

| # | Feature | Endpoint | Rate code | Notes |
|---|---------|----------|-----------|-------|
| 1 | **PAN verify** | `POST /v3/client/kyc/fetch_id_data/PAN` | `IdVerify` (₹1.50) | body `id_no` (PAN), optional `name`, `dob`, `unique_request_id`. Response has `status`, `name_as_per_pan_match`, `date_of_birth_match`, `aadhaar_seeding_status`, `category`. It is a **match/verify** — it does not return a full identity to auto-fill. |
| 2 | **Business KYC** | same path, `{idCardType}` = `GST`, `GST_ADVANCED`, `CIN`, `DIN`, `PAN_TO_GST`, `UDYAMAADHAAR`, `FSSAI` | `BusinessKyc` (₹3) | same body shape (`id_no`). Response fields vary by type (`details`, company fields) — **log the sandbox response once and map from it**. |
| 3 | **Aadhaar masking** | `POST /v4/client/kyc/aadhaar/mask` (base64) or `/v4/client/kyc/aadhaar/file/mask` (multipart) | `AadhaarMasking` (₹1) | base64 body: `data`, `data_content_type` (PNG/JPEG/JPG/PDF), `consent`=`"yes"`, `file_name`, `mask_qr`, `is_validate`. Response `masked_output` (base64), `details.masking_done_count`, `error_message`. Keep files ≤ 1 MB. |
| 4 | **Aadhaar / DigiLocker fetch (customer consent flow)** | `POST /client/kyc/v2/request/with_template` (DigiStudio) | `DigiLockerFetch` / `AadhaarOfflineXml` (₹2) | `template_name`: `DIGILOCKER_AADHAAR_PAN` (standard, PAN+Aadhaar) or `OfflineKyc` (Aadhaar offline). `customer_identifier` = mobile/email, `reference_id` = our party/application id, `notify_customer`, `expire_in_days`. Returns a request `id` + `status`. |
| 4b | status / result | `POST /client/kyc/v2/{id}/response` (`?detail_response=true`) | — | returns `actions[]`, each with `status` and `id_card_data_response` (name, dob, gender, address, `fathers_name`, `id_no`…). |
| 4c | webhooks for the above | same `POST /webhooks/digio/{companyId}` | — | events: `KYC_REQUEST_COMPLETED`, `KYC_REQUEST_APPROVED`, `KYC_REQUEST_REJECTED`, `KYC_REQUEST_EXPIRED`, `KYC_ACTION_COMPLETED`, `DOCUMENTS_PULLED_SUCCESSFULLY`. Payload: `{id, created_at, event, entities, payload:{KYC_REQUEST:{id,status,reference_id,transaction_id}}}`. Idempotent — Digio delivers at-least-once. |
| 5 | **ID OCR** (upload image of PAN/Aadhaar) | `analyzeMultipleFileOcr` (spec is large; read it) | `IdOcr` (₹2) | feeds the existing "Verify OCR" buttons. Do **after** 1–4. |

Already verified against the spec (no change needed): penny-drop `POST /v4/client/verify/bank_account` and
the Digio environments (sandbox `ext.digio.in:444`).

**Needs confirmation from Digio (ask Surendra D):** which of these products + the **DigiStudio template names**
are enabled on the sandbox account (`DIGILOCKER_AADHAAR_PAN`, `OfflineKyc`), and sandbox test PAN/GSTIN/Aadhaar
values. A 403/“not enabled” response on a call means a missing product, not a code bug.

### Where to read the spec (curl works; WebFetch may not)
```
https://documentation.digio.in/swagger/post_fetchIdCards.json         # PAN + business (fetch_id_data)
https://documentation.digio.in/swagger/post_maskAadhaarImageBase64.json
https://documentation.digio.in/swagger/post_maskAadhaarFile.json
https://documentation.digio.in/swagger/final_post_requestUserKYC.json # DigiStudio create request
https://documentation.digio.in/swagger/post_getKycDeepResponse.json   # DigiStudio status/result
https://documentation.digio.in/swagger/post_analyzeMultipleFileOcr.json
https://documentation.digio.in/digikyc/digilocker/integration_guide
https://documentation.digio.in/digistudio/integration/webhooks
https://documentation.digio.in/webhooks/structure   +  /webhooks/faq   # checksum / signature details
```
(The sandbox/cloud environment's network policy must allow `documentation.digio.in` and `ext.digio.in`.)

---

## 2. Phases

### Phase 0 — Connectivity smoke test (no code change, ~10 min)
- App running locally, SuperAdmin → Digio keys saved (sandbox). 
- Make one raw call and confirm auth + reachability, e.g. PAN verify with a sandbox test PAN.
- **Outcome:** you know whether the creds work, which port/network issues exist, and the *real* response JSON shape.
  **Save the sample response (with PAN masked) into `docs/digio-samples/` — later phases map from it.**

### Phase 1 — PAN verification  (`DigiKycService` + Customer Details)
Changes:
1. `DigiKycService.VerifyAsync` gets `DigioClient digio, int companyId` (+ optional `name`, `dob`) and calls
   `digio.PostAsync(companyId, DigioRates.IdVerify, "/v3/client/kyc/fetch_id_data/PAN", new { id_no = pan, name, dob, unique_request_id }, applicationId)`.
2. New result type that carries **match flags**, not a fake identity: `PanVerified`, `NameMatch`, `DobMatch`,
   `AadhaarSeeded`, `Status`, plus the not-configured/paused/failed states from `DigioStatus`.
3. `CustomerDetails.razor` (`VerifyKycAsync`, two buttons around lines 211/228): inject `DigioClient`, pass company id,
   show: ✓ "PAN valid — name matches" / ✗ "PAN invalid or name mismatch" / the existing not-configured notice.
4. Store the verdict on the party (add fields only if a migration path exists — check how this repo does DB changes
   first; otherwise keep it in the UI/notice for now and flag it).
- **Outcome:** click Verify on a PAN → real Digio call → clear verified/mismatch/unavailable result; one row in
  `/admin/api-usage` (₹1.50 on success).
- **Tests:** unit-test the response mapping with the saved sample JSON (no network) in `LosLms.Tests`.

### Phase 2 — Business KYC (GSTIN / CIN)  (`BusinessKycService` + Approvals)
Changes: implement `BusinessKycService.VerifyAsync` via `fetch_id_data/GST` (and `CIN` when the model carries it);
map the real sandbox response to `BusinessKycData` (legal name, incorporation/registration date); `Apply` already
fills blanks only. Button: Approvals → "Verify company (ROC)" (line ~156/851).
- **Outcome:** GSTIN in → firm name/registration date pre-filled where blank; honest message otherwise; ₹3 metered.

### Phase 3 — Aadhaar masking
Changes: after an Aadhaar image/PDF upload in Customer Details, call `/v4/client/kyc/aadhaar/mask` (base64,
`consent: "yes"`); store the **masked** output and keep/replace the original per the app's retention rules
(ask the owner — compliance decision, do not guess). Respect the 1 MB guidance.
- **Outcome:** a masked copy (first 8 digits hidden) is stored; `masking_done_count` checked; ₹1 metered.

### Phase 4 — Aadhaar / DigiLocker fetch (async, customer consent)  ← biggest piece
Flow: officer clicks "Fetch Aadhaar via DigiLocker" → app calls `with_template` (`DIGILOCKER_AADHAAR_PAN`,
customer mobile/email, `reference_id` = our party id) → Digio SMS/WhatsApps the customer → customer consents →
Digio webhook → app fetches `/client/kyc/v2/{id}/response` → maps `id_card_data_response` into `DigiKycData` →
`DigiKycService.Apply` fills blanks.
Changes:
1. Persist the request (`KycRequest`: Digio request id, party id, company id, status, timestamps) — needs a new
   table; follow the repo's existing migration approach.
2. Extend the webhook handler (`Program.cs` ~480 and `DigioClient.ApplyWebhookAsync`): today it only understands
   `doc.*` e-Sign events. Add the KYC events above. Keep: checksum verified first, idempotent, always 200 fast,
   scoped by `companyId`. **Read `/webhooks/faq` to confirm the signature header/algorithm before touching it.**
3. UI: status chip (Requested → Completed → Approved/Rejected/Expired) with a refresh button (webhook can be missed).
4. Meter on success only (`DigiLockerFetch`).
- **Outcome:** end-to-end sandbox journey; party Personal/Contact blanks filled from DigiLocker data; webhook
  replay does not double-apply or double-bill.

### Phase 5 — ID OCR ("Verify OCR" buttons)
Implement via `analyzeMultipleFileOcr` once Phases 1–4 are stable. Read its spec first (it is large).

### Phase 6 — Docs + cleanup
Update `docs/PENDING-API-INTEGRATIONS.md` ("Still stubbed — endpoint not in Digio's public docs" is **no longer
true** — the endpoints are public); mark what is now live. Update the Digio keys page help text if needed.

---

## 3. Prompts for the local session

### 3.1 Kickoff (paste first)
```
You are working in the LOS-LMS repo (C#/.NET Blazor, NBFC loan origination). Goal: integrate Digio KYC APIs
using the SANDBOX credentials already saved in SuperAdmin → Digio keys. 
Read docs/DIGIO-KYC-INTEGRATION-PLAN.md fully, then read
LosLms/Services/Integrations/DigioClient.cs, DigiKycService.cs, BusinessKycService.cs, EsignService.cs,
DigioRates.cs and LosLms.Tests/DigioTests.cs so you match the existing style.
Rules: never print or commit secrets; all Digio calls go through DigioClient.PostAsync; never invent data;
one phase per branch/PR; add unit tests that use saved sample JSON (no live network in tests).
Do NOT start coding yet — reply with (1) your understanding of the current stub/gate design, (2) anything in the
plan that conflicts with the code, (3) questions. Then wait.
```

### 3.2 Phase 0 (smoke test)
```
Phase 0 of docs/DIGIO-KYC-INTEGRATION-PLAN.md. Help me make ONE sandbox call to
POST {sandbox}/v3/client/kyc/fetch_id_data/PAN with a Digio sandbox test PAN, using the creds from the vault via
a tiny throwaway console/test harness or a temporary SuperAdmin-only endpoint (remove it afterwards; never
log the secret). Report: HTTP status, Digio error code/message if any, and the full response JSON with the PAN
masked. Save the masked sample to docs/digio-samples/pan-verify.json. If it fails, diagnose (wrong env, product not
enabled, network/port 444) before any feature code.
```

### 3.3 Phase 1 (PAN)
```
Phase 1 of the plan: implement PAN verification. Use docs/digio-samples/pan-verify.json as the source of truth for
the response shape (and swagger post_fetchIdCards.json for field names). Change DigiKycService.VerifyAsync to call
DigioClient.PostAsync with DigioRates.IdVerify, return a result with match flags (not a fabricated identity),
and update the two Verify buttons in Pages/Applications/Stages/CustomerDetails.razor to show
verified / mismatch / not-configured / paused / failed. Add xUnit tests for response mapping. Run dotnet build and
dotnet test. Show me the diff summary and how to test it in the UI with sandbox values. Do not touch other phases.
```

### 3.4 Phase 2 (Business KYC)
```
Phase 2: implement BusinessKycService.VerifyAsync via fetch_id_data/GST (CIN only if the model has a CIN field).
First call the sandbox once with a Digio test GSTIN and save the masked response to docs/digio-samples/, then map
only fields that really come back to BusinessKycData. Keep Apply fill-blanks-only. Wire the Approvals "Verify company
(ROC)" button (Approvals.razor ~851). Meter with DigioRates.BusinessKyc. Tests + build + summary.
```

### 3.5 Phase 3 (masking) — ask me the retention question first
```
Phase 3: Aadhaar masking via /v4/client/kyc/aadhaar/mask (base64, consent "yes"). Before coding, ask me whether the
original unmasked upload must be kept, replaced, or deleted (compliance decision). Then implement after the
Aadhaar upload in CustomerDetails.razor, enforce ~1 MB, meter DigioRates.AadhaarMasking, add tests, summarise.
```

### 3.6 Phase 4 (DigiLocker)
```
Phase 4: DigiLocker/Aadhaar fetch through DigiStudio. Read the swagger final_post_requestUserKYC.json and
post_getKycDeepResponse.json, https://documentation.digio.in/digikyc/digilocker/integration_guide,
/digistudio/integration/webhooks and /webhooks/faq. Propose the design first (KycRequest table + how this repo does
migrations, webhook event handling, UI status chip, idempotency, metering on success only). Wait for my OK, then
implement. The webhook must keep: checksum verification first, companyId scoping, fast 200, idempotent replays.
Tell me which template_name my sandbox account actually has enabled (ask me to confirm with Digio if a call
returns "not enabled").
```

### 3.7 Review prompt (after each phase)
```
Review the last phase adversarially: any secret/PII in logs or exceptions? any path that fills a field with
data Digio did not return? double-billing or double-apply on retries/webhook replays? tenant (companyId) leaks?
paused/not-configured states still honest? Fix what you find, then re-run build + tests.
```

---

## 4. Expected end state
- Verify PAN, verify company (GSTIN/CIN), mask Aadhaar and fetch Aadhaar via DigiLocker all work against the
  sandbox, each metered on `/admin/api-usage`, each with honest not-configured / paused / failed messages.
- Flipping the **Production** box on the Digio keys page (with production creds) is the only change needed to go live.
- `PENDING-API-INTEGRATIONS.md` reflects reality. Bank statement analyzer, eNACH and V-CIP remain future work.
