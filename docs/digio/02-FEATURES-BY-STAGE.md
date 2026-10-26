# 02 — Features, stage by stage

Each feature uses the foundation in `01-FOUNDATION.md`: `DigioClient`, `DigioRequest`, the inbox,
processor and reconciler, `VerificationEvidence`, and the UI kit. Field names are from Digio's OpenAPI
specs (`04-DIGIO-API-REFERENCE.md`). **"Capture in Phase 0"** means: read the real sandbox response and
save it to `docs/digio/samples/` before mapping. Do not guess formats such as date strings.

Template for every feature: **Trigger & UI → Request → Result mapping → Evidence & gate → Edge cases → Acceptance.**

---

## Stage 1 — Customer Details (per party: Applicant, Co-Applicant, Guarantor)

### 1.1 PAN verification — P0 (Phase 2)
- **Trigger & UI:** the existing "Verify OCR" button next to PAN becomes **Verify PAN** (`VerifyButton`).
  It is enabled when the PAN matches `^[A-Z]{5}[0-9]{4}[A-Z]$` and the party has a full name (DOB optional but recommended).
  The result is a `VerifyBadge` under the field.
- **Request:** `POST /v3/client/kyc/fetch_id_data/PAN` with `{ id_no, name, dob, unique_request_id }`.
  The `dob` format is **captured in Phase 0**. Synchronous. Rate code `KYC_ID_VERIFY` (₹1.50), unless Digio bills PAN under a different code.
- **Result mapping:** `status` (valid/invalid/…), `name_as_per_pan_match`, `date_of_birth_match`,
  `aadhaar_seeding_status`, `category` (Individual / Company / …). It does **not** return an identity to fill.
- **Evidence:** `PAN` subject. `Verified` when the PAN is valid and the name matches (and DOB matches when DOB was sent).
  `Mismatch` when the PAN is valid but name or DOB does not match. `Failed` when invalid. Set `Party.PanVerified` from evidence only.
  Category ≠ Individual on an individual party → `Mismatch` with reason "PAN is not an individual's".
- **Edge cases:** PAN edited after verify → `Stale`. "Aadhaar not seeded" is shown as an amber note, not a
  failure (policy decision §9.3). Network failure leaves the field editable and the honest message shown.
- **Acceptance:** a sandbox test PAN gives Verified. A wrong name gives Mismatch. Editing the PAN gives Stale.
  One usage row per click. A double click makes one call.

### 1.2 Aadhaar + PAN via DigiLocker — P0 (Phase 2)
- **Trigger & UI:** a new **Fetch from DigiLocker** action in the Identity panel. It needs a `ConsentCheckbox`
  (consent text v1, stored), the customer's mobile (or email), and a channel choice (**Send link** / **Open
  on this device**). It opens a `CustomerActionPanel` (Waiting for customer → Completed / Expired / Failed).
- **Request:** `POST /client/kyc/v2/request/with_template` with
  `{ customer_identifier: mobile|email, customer_name, template_name: settings.DigiLockerTemplate ("DIGILOCKER_AADHAAR_PAN"),
     reference_id: ourRequestId, transaction_id: applicationId, notify_customer, customer_notification_mode,
     expire_in_days, generate_access_token: true }` → `id` (KID…), `status`, `access_token.id`.
- **Completion:** webhooks `KYC_REQUEST_COMPLETED` / `KYC_REQUEST_APPROVED` / `DOCUMENTS_PULLED_SUCCESSFULLY` /
  `KYC_REQUEST_EXPIRED` / `KYC_REQUEST_REJECTED`. **Always** fetch `POST /client/kyc/v2/{id}/response?detail_response=true`
  and map from it, never from the webhook alone.
- **Result mapping:** the `digilocker` action → `id_card_data_response` / `action_data` / `sub_actions[].details`.
  The exact location of each field is **captured in Phase 0**. Map `name`, `dob`, `gender`, `fathers_name`, `address`
  (+ `address_information` parts: city, state, pincode), the last 4 digits of the Aadhaar number, and the PAN when it was fetched.
  The photo comes from `GET /client/kyc/v2/media/{file_id}?doc_type=AADHAAR` (and `xml=true` only if the
  compliance decision allows keeping the XML). Store the photo through `IFileStore` and set `Party.PhotoPath` only if it is blank.
- **Write-back:** `DigiKycService.Apply` (already fill-blanks-only) gets a `DigiKycData`. Conflicts with
  officer-entered values open a `DigioDiff`. Set `AadhaarVerified`, `AadhaarGeneratedOn` (= fetch date), `AadhaarLast4`, `AadhaarHash`.
- **Evidence:** `AADHAAR` (Verified when the DigiLocker Aadhaar was fetched). `PAN` too, if the PAN doc was fetched and matches the party PAN.
  Cross-check: fuzzy-match the DigiLocker name against the PAN name (`1.8`) and record the score.
- **Edge cases:** the customer has no DigiLocker account (Digio's flow creates one). The PAN is not in DigiLocker
  (the user can add it in the flow). Partial pulls (`DOCUMENTS_PULLED_PARTIALLY`) → stay *Waiting*, and show what has arrived.
  Expired → offer a new request. The customer finishes after the officer closes the page → the result is applied server-side, and the next open shows it.
- **Acceptance:** sandbox journey by link and on-device. Fields fill only where blank. The diff appears on a conflict.
  A webhook replay is a no-op. With webhooks disabled, the reconciler completes it.

### 1.3 Aadhaar Offline e-KYC (XML) — P1 (Phase 2 or 4)
Same as 1.2 with `template_name: "OfflineKyc"` (Digio's standard template). Use it when DigiLocker is
unavailable. Face-match options are configured in DigiStudio (SOURCE / MATCH REQUIRED). Error codes are in
`04` §6 (e.g. 10012 mobile not linked to Aadhaar).

### 1.4 Aadhaar masking — P0 (Phase 2)
- **Trigger:** every upload to `UploadKind.AadhaarScan` (and any document classified as Aadhaar later).
- **Request:** `POST /v4/client/kyc/aadhaar/mask` with `{ data: base64, data_content_type: PNG|JPEG|JPG|PDF, file_name,
  consent: "yes", mask_qr: true, is_validate: true, unique_request_id }`. Compress or reject files over about 1 MB (Digio's guidance).
- **Result:** `masked_output` (base64) + `details.masking_done_count` / `already_masked_count` + `error_message`.
- **Storage:** store **only the masked file** as `AadhaarScanPath`. The original is discarded unless the
  compliance decision says otherwise (decision §9.4). If `masking_done_count == 0 && already_masked_count == 0` → reject the
  upload: "No Aadhaar number found to mask. Upload a clearer image."
- **Consent:** only call it with a recorded consent (the API takes `consent: "yes"`; we must actually have it).
- **Acceptance:** an uploaded Aadhaar is stored masked. An unmasked original never reaches `IFileStore`.

### 1.5 AML / sanctions screening — P0* (Phase 2)
- **Trigger:** automatic on Customer Details completion for every party (plus a manual **Screen now**).
  Re-screen when the name, DOB or PAN changes.
- **Request:** `POST /client/v1/user/screen` with `{ screening_mode: "SCREENING", screening_settings: { screening_type:
  "SCREEN_ONLY_RESPONSE" (or SCREEN_AND_RAISE_ALERT if the NBFC uses Digio's case manager), response_mode: "DETAILED",
  lists: settings, risk_thresholds, cut_off_threshold: settings (Digio recommends 75–90) },
  user_details: { client_id, name, pan, dob, gender, nationality, address } }`.
  Per Digio's integration page only `client_id` (≤ 36 chars) and `name` (≤ 50) are mandatory. The rest improve accuracy.
  `client_id` = a stable **non-PII** id (`{companyId}-{partyId}`), never the PAN. The DOB format is **captured in Phase 0**.
- **Result:** `risk_level`, `found`, `total_alerts`, `high/medium/low_severity_alerts`, `top_match_score`,
  `*_alert_details[]` (name, source_name, source_url, fuzzy_match_score).
- **Evidence & gate:** `AML` subject. No alerts → `Verified` (clear). Any alert → `Mismatch` → Customer Details
  completion is **blocked** until an Admin reviews the alert list and records *False positive* (→
  `AcceptedMismatch` + note) or *Reject* (existing rejection flow). High-severity alerts are never auto-cleared.
- **Monitoring (P2):** `SCREEN_RAISE_ALERT_AND_MONITOR` for ongoing monitoring of disbursed customers.

### 1.6 ID-card OCR — P1 (Phase 4)
- **Use:** the fallback when DigiLocker is not possible. It reads uploaded PAN / Aadhaar (masked) / DL / Voter / Passport images.
- **Request:** `POST /v4/client/kyc/analyze/file/idcard` (multipart `front_part`, `back_part`, `unique_request_id`).
- **Result:** `detections[]` with `id_type` + `id_attributes` (name, fathers_name, dob, id_no, address,
  gender…) + `verification_result` + `output_images` (photo, signature, masked images).
- **Write-back:** fill blanks only (`DigioDiff` on conflict). OCR alone is **not** verification: evidence is
  `Verified` only when `verification_result.verified == true`; otherwise it is `Unavailable` with "OCR read only".

### 1.7 Liveness + face match on the live photo — P1 (Phase 4)
- The app already captures a live photo (`Party.PhotoPath`). Send it to `POST /v3/client/kyc/analyze/file/liveness`
  (`image`) → `result PASS|FAIL|UNKNOWN`, `score`. Then `POST /v3/client/kyc/facematch` (`file1` = live photo,
  `file2` = DigiLocker/Aadhaar photo, `minimum_match` from settings) → `score`, `result`.
- **Evidence:** `LIVENESS`, `FACE`. `UNKNOWN` → manual review (Admin accept/reject), never auto-pass.

### 1.8 Fuzzy name/address match helper — P1 (Phase 2, internal)
`POST /v3/client/kyc/fuzzy_match` with `{ source:{text}, target:{text}, context: name|address, confidence }` →
`matched`, `match_score`. Use it for PAN name ↔ Aadhaar name ↔ bank-holder name ↔ RC owner name. Store the
score in the evidence summary. Cache per pair of inputs (fingerprint) so a page refresh does not bill again.

### 1.9 CKYC search / download — P1* (Phase 4–5)
Digio publishes the CKYC API in a private Google Doc (linked from `/digikyc/ckyc/api_integration`). **Get it
from Digio.** Plan: search by PAN before onboarding. If a KIN is found, download the record and offer it as a
pre-fill source (fill blanks, show the diff). Record the KIN on the party (new `Party.CkycNumber`). Upload is in `03`.

### 1.10 V-CIP (agent-assisted video KYC) — P2 (Phase 6)
DigiStudio `2WAY-VIDEO-KYC`. RBI V-CIP requires a live trained official. Digio provides the agent console;
our staff would be the agents. The current in-app recording stays as *evidence only* and must keep saying so.
Needs a separate design pass with compliance (scripts, geo-tag, recording retention).

### 1.11 Mobile verification & CIBIL — not Digio
Keep `MobileVerified` and the CIBIL gate exactly as they are (honest stubs) until an SMS provider and a bureau
are chosen. One useful side effect: a completed DigiLocker or e-Sign link sent to the mobile proves the customer
controls it. You *may* record that as a `MOBILE` evidence row with summary "link completed on this number",
but only if compliance accepts it.

### Stage 1 completion gate (Phase 2)
`GateCheckService` gets a second check after CIBIL: **KYC evidence policy** (per company, `DigioSettings`).
The default suggestion, pending decision §9.3: for every party, PAN `Verified`, AADHAAR `Verified` (DigiLocker,
Offline XML or Admin-accepted), AML clear or accepted. Not stale, not sandbox on a production company.
Blocked → a clear reason plus an *Admin bypass* request (reuse the `AdminRequest` flow, type `KycBypass`).

---

## Stage 2 — Loan & Security

### 2.1 Vehicle RC verification — P0 for used vehicles, P1 for new (Phase 4)
- **Trigger:** **Verify RC** next to `SecurityDetail.RegNo` (used vehicle, or a new vehicle once registered).
- **Request:** `POST /client/v4/apis/verify/vehicle_rc` with `{ id: RegNo, unique_request_id }`.
- **Result (`RCDetailsResponse`):** `user_details.owner_name`, `vehicle_details.{rc_number, registration_date,
  vehicle_chassis_number, vehicle_engine_number, maker_model, fuel_type, rc_status, norms_type, fit_up_to,
  vehicle_category, challan_details, registered_at, rto_code}`, `finance_details.{financier, financed,
  insurance_company, insurance_policy_number, insurance_upto, tax_upto}`, `permit_details.{blacklist_status, permit_*, noc_details, pucc_upto}`.
- **Checks shown to the officer** (each a ✓/✗ row):
  1. Owner name ↔ seller (used vehicle) or applicant (new vehicle PDD): fuzzy match.
  2. Chassis / engine ↔ `ChassisNo` / `EngineNo`. Compare the last 5 characters when Digio returns masked values.
  3. **Existing hypothecation:** `financed == true` with a financier other than this NBFC → a used-vehicle red
     flag (needs NOC / closure of the old loan).
  4. Insurance valid till ≥ today; insurer ↔ `InsuranceProvider`.
  5. `rc_status` active; `blacklist_status` clear; challans pending (amber).
  6. Fitness / permit validity for commercial vehicles.
- **Evidence:** `VEHICLE_RC`. Fill blanks in `MakeModel` and `MfgYear` (registration year) only.
- **PDD (post-disbursal):** after registration, the same call confirms `financier` = **this NBFC** (hypothecation
  endorsed) → closes the RC PDD item (`Pdd` model) with evidence. Schedule a reconciler re-check if it is still pending after N days.

### 2.2 Driving licence (commercial-vehicle driver) — P2
`fetch_id_data/DRIVING_LICENSE` (`id_no`, `dob`) → validity, classes (`Transport` / `Non-Transport`), holder name.

---

## Stage 3 — Bank & Financial

### 3.1 Penny drop (exists) → harden — P0 (Phase 2)
- Keep `POST /v4/client/verify/bank_account`. Add `unique_request_id`, `validation_mode: "AUTO"` (pennyless
  first, then penny drop; confirm the rate difference with Digio), and `beneficiary_name` = the applicant's verified name.
- Map `verified`, `beneficiary_name_with_bank`, `fuzzy_match_result`, `fuzzy_match_score`, `error_code`,
  `validation_mode` → `BANK_ACCOUNT` evidence + the existing `PennyDropStatus` (Matched / NotMatched / Unavailable).
- Staleness: an edit to the account number or IFSC resets to NotRun. The bank-holder name is stored for the mandate (3.x → 8.3).
- Reuse the same evidence for the e-NACH account. Do not pay twice for the same account (fingerprint match).

### 3.2 UPI VPA verification — P2
`POST /v3/client/public/upi/check_vpa` (`virtual_address`, `name`) → `status`, `customer_name`, `fuzzy_match_score`.
`search_vpa` (mobile → VPA) is only with consent.

### 3.3 Bank statements via Account Aggregator — P2 (Phase 6)
FIU-TSP needs the NBFC itself to be an **FIU** (regulatory onboarding, Sahamati, about 30–35 days). Flow: consent
request from a template → the customer approves on the AA → FI request → fetch FI data / analytics →
`BankStatementAnalysisService.ApplyAsync` (already built, dormant). Separate design pass once FIU onboarding is done.

---

## Stage 4 — Document Checklist (P1, Phase 4)
- Each checklist item that has a matching evidence subject shows the evidence chip and a link
  ("PAN — Verified via Digio 26 Oct 14:02 by R. Deshmukh").
- **Never auto-tick.** The officer still ticks, but an item backed by `Mismatch`, `Failed`, `Stale` or `SANDBOX`
  evidence **cannot** be ticked without an Admin note (it would contradict the evidence).
- Optional: attach Digio's KYC report PDF (`POST /client/kyc/v3/{id}/kyc_report`) as a checklist document.

## Stage 5 — Reports (RCU) (P2)
- RCU stays manual (no RCU vendor in Digio).
- Optional signals: UAN employment history (`/client/v4/apis/verify/uan`) for salaried applicants; electricity
  bill verification for the address; geo distance (Digio has geocode/distance schemas, endpoint **confirm with Digio**).

## Stage 6 — Eligibility
Nothing from Digio until Account Aggregator (3.3) exists. Do not derive income from KYC.

---

## Stage 7 — Approvals (Business / non-individual borrowers)

### 7.1 Business KYB — P1 (Phase 4)
- **Trigger:** the existing **Verify company (ROC)** button (`Approvals.razor` ~156/851) → `BusinessKycService.VerifyAsync`.
- **Requests** (all `fetch_id_data/{type}` with `{ id_no, unique_request_id }`): `GST` or `GST_ADVANCED`
  (GSTIN), `CIN`, `DIN` (each director), `PAN_TO_GST` (`is_advanced`, `listing_type`), `UDYAMAADHAAR`, `FSSAI`; TAN via its own endpoint.
- **Result:** the GST response (legal name, trade name, constitution, registration date, status, jurisdiction,
  filing status), the CIN response (name, date, status, PAN/TAN), directors. Digio's 360° **KYB Business Verification**
  and **B2B report** pages publish **no endpoint**. Ask Digio for them if the deeper report (MCA compliance,
  defaulter check, credit rating, UNSC lookup) is wanted.
- **Write-back:** `BusinessKycService.Apply` (fill blanks: firm name, incorporation date). GST status ≠ Active → `Mismatch`.
- **Evidence:** `GSTIN`, `CIN`, `DIN`. Screen each director / partner / signatory through AML (1.5).

---

## Stage 8 — Post Sanction

### 8.1 Loan agreement e-Sign (multi-party) — P0 (Phase 3; the 1a hotfixes come earlier)
- **Signers:** applicant + co-applicants + guarantors (each a party with verified KYC) + optionally the
  lender's authorised signatory (settings). Per decision §9.5: `sequential` true or false and the order (`index`).
- **Request:** `POST /v2/client/document/uploadpdf` with
  `{ file_name, file_data: base64(agreement), signers:[{ identifier: mobile|email, name, reason: "Loan agreement",
     sign_type: "aadhaar", signer_tag, index }], sequential, expire_in_days, notify_signers, send_sign_link,
     customer_notification_mode, display_on_page: "custom" + sign_coordinates (or "last"),
     signature_verification: { <identifier>: { abortOnFail: true, maxAttempt: 3, rules: [ { operation: "AND",
       conditions: [ {field:"NAME", matchType:"FUZZY", value: verifiedName, threshold: 70},
                     {field:"YOB", matchType:"EXACT", value: yearOfBirth},
                     {field:"AADHAAR", matchType:"EXACT", value: aadhaarLast4},
                     {field:"GENDER", matchType:"EXACT", value: "M|F"} ] } ] } },
     callback: ourRequestId  /* Digio de-duplicates on this */, reference_id: applicationId,
     include_authentication_url: true, estamp_request: (8.2) }`.
  The `signature_verification` map shape is **captured in Phase 0** against a real sandbox call. The plan relies
  on the documented `ValidationRuleSet`, `ValidationRule` and `Condition` schemas.
- **Agreement integrity:** store the SHA-256 of the PDF sent (`DigioRequest.InputFingerprint`). If the agreement is
  regenerated after sending → the old request is `Stale` → **Cancel** (`POST /v2/client/document/{id}/cancel`, only
  while `requested`) and re-send. Never two live sign requests for one application.
- **Status UI:** per-signer rows from `signing_parties[]` (`requested / signed / failed / expired`), each with
  **Resend** / **Open on this device** (`authentication_url`), plus overall `agreement_status`.
- **Completion:** `agreement_status == "completed"` (confirmed by `GET /v2/client/document/{id}`) → download the signed
  PDF (`GET /v2/client/document/download`) → store it → `AgreementEsignStatus = Signed` → `AGREEMENT_SIGNED`
  evidence → meter `ESIGN_AADHAAR` per credit consumed (**confirm with Digio**: per signer or per document).
  `doc.sign.rejected` → show which rule failed (`validation_results.ruleResults[].conditions[]`).
- **Gate (decision §9.8):** *Release funds* requires `AGREEMENT_SIGNED` (production, not stale).

### 8.2 e-Stamping — P1 (Phase 3)
- Ops prerequisite: e-stamp inventory ordered in the Digio dashboard per state and denomination, with a tag
  (`<entity>_<STATE>_<amount>_<LOB>`). 5–7 business days lead time; reminder at 80% consumption.
- **Request:** add `estamp_request: { tags: { "<tag>": qty }, sign_on_page: "first", note_on_page, note_content }`
  to 8.1. The tag is chosen from settings by branch state and loan amount (stamp-duty table per state; **compliance to supply**).
- An unused stamp auto-returns to inventory if signing expires. Show the stamp id (`ExternalEstampMasterResponse`) on the file.

### 8.3 e-NACH mandate registration — P0 (Phase 3)
> **Full specification: `06-NACH.md`** (config id handling, validation, all routes, lifecycle, amend/cancel,
> gate, sandbox test). The summary below is kept for overview; where they differ, `06` wins.

- **Replace the stub** in `Components/Shared/MandateFlow.razor` (E-Nach tab) for `EnachMandate`.
- **Before the request:** penny-drop evidence on the debit account (3.1 → reuse it). Call `GET /v3/client/mandate/banks?mode=API`
  (cache it daily) to show which auth modes the customer's bank supports (`apiMandateNetBanking`,
  `apiMandateDebitCard`, `apiMandateAadhaarCard`, `esignMandate`, `physicalMandate`).
- **Request:** `POST /v3/client/mandate/create_form` with
  `{ corporate_config_id: settings.EnachConfigId, mandate_type: "CREATE", auth_mode: API|ESIGN|PHYSICAL,
     customer_identifier: mobile|email, notify_customer, include_authentication_url: true, expire_in_days,
     mandate_data: { customer_name: bankHolderName, customer_account_number, customer_account_type: SAVINGS|CURRENT,
       destination_bank_id (from banks API), customer_ref_number: ourRequestId (idempotency + recon;
         the spec gives no max length and NPCI reference fields are short, so check the 26-char id in Phase 0 and use a compact id if it is rejected),
       scheme_ref_number: applicationId, management_category: settings (EMI, e.g. L001: confirm with sponsor bank),
       is_recurring: true, frequency: settings (Adhoc recommended so re-presentation works; Monthly allows only
       one success per month, DG101), first_collection_date: firstEmiDate, final_collection_date: lastEmiDate (+ buffer),
       collection_amount: EMI, maximum_amount: EMI × settings.multiplier } }`
  → `id` (ENA…), `state`, `authentication_url`.
- **Physical NACH:** `POST /v3/client/mandate/{id}/generate_file` → prefilled PDF to print. The signed scan is uploaded via
  `POST /v3/client/mandate/submit_signed_mandate` (multipart). Keep the existing physical checkbox flow as the officer's attestation.
- **Lifecycle (webhooks):** `APIMNDT.AUTHSUCCESS` / `MNDT.AUTHSUCCESS` (authenticated, *not* final) →
  `…TRANSUCCESS` / `SPOBANKACCEPT` / `NPCIACCEPT` (in progress) → `APIMNDT.DESTACCEPT` / `MNDT.DESTBANKACCEPT`
  (**Registered**, UMRN final) or `…DESTREJECT` / `…DESTBANKREJECT` / `…NPCIREJECT` / `…AUTHFAIL` (Rejected + reason
  `npci_auth_reject_reason` / `npci_auth_failed_error`). Always confirm with `GET /v3/client/mandate/{mandate_id}`.
  Store `Umrn`, `Status`, and the bank and NPCI states. Note that **one ENA can carry multiple UMRNs** after customer retries; keep the one in `umrn` of the latest successful state.
- **TAT shown to the officer:** API mandate ≈ next working day; eSign 3–7 days; physical ~7 working days.
- **Gate (decision §9.8):** *Release funds* requires mandate `Registered`, or `AuthSuccess` + Admin override, per policy.

### 8.4 Security NACH — P1
The same flow on `SecurityNachMandate` with its own `corporate_config_id` / category (**confirm with the sponsor bank**).

### 8.5 UPI Autopay mandate — P2
`create_form` with `auth_mode: "upi"`, `customer_vpa`, `auto_debit_amount_in_paise`; events `UPI.MNDT.*`. Check the
UPI per-transaction limits for EMIs before offering it.

### 8.6 Welcome letter / SMS
Not Digio. The welcome letter stays QuestPDF. The SMS stays a stub until an SMS provider exists.

---

## Cross-cutting: consent register (P0, Phase 2)
Every Aadhaar, DigiLocker, AML, CKYC, masking, e-Sign and mandate action records: party, consent text
version, channel (in-branch / link), the officer, a timestamp, and the Digio request id. Show it on the evidence list.
The texts are supplied by compliance and stored in `DigioSettings` (versioned).

## Cross-cutting: retention & purge (P2)
After our copy is stored and the retention rule is met, call `POST /client/kyc/purge/{KID}` to remove Digio's
copy. Our own records follow the NBFC's KYC record-retention policy (compliance to confirm the period).
