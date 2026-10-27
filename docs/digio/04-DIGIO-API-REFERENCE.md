# 04 — Digio API reference (curated for this app)

The full machine summary of all 79 public operations is in `appendix/DIGIO-API-SPEC-SUMMARY.md`. The page
text (webhooks, error codes, SDK, guides) is in `appendix/DIGIO-DOCS-TEXT.md`. This file is the short list this plan uses.
Each spec can be re-fetched from `https://documentation.digio.in/swagger/<operationId>.json`.

## 1. Environments & auth

| | Sandbox | Production |
|---|---|---|
| API base (server-to-server) | `https://ext.digio.in:444` | `https://api.digio.in` |
| Hosted gateway (customer pages) | `https://ext.digio.in/#/gateway/login/` | `https://app.digio.in/#/gateway/login/` |
| Web SDK script (not used initially) | `https://ext.digio.in/sdk/v11/digio.js` | `https://app.digio.in/sdk/v11/digio.js` |
| Enterprise dashboard (templates, webhooks, e-stamp, NACH config) | `https://ext-enterprise.digio.in` | `https://enterprise.digio.in` |
| Webhook source IP | `35.154.20.28` | `13.126.198.236` |
| Webhook latency | ~5 min | ~5 s |

Auth: HTTP Basic `client_id:client_secret` on every API call. Webhooks: `X-Digio-Checksum` = hex HMAC-SHA256
of the raw body with the account's (webhook-group's) secret. The secret is per account and per environment.

## 2. Endpoints used by this plan

| Feature (plan §) | Method & path | Key request fields | Key response fields | Rate code |
|---|---|---|---|---|
| PAN verify (02 §1.1) | `POST /v3/client/kyc/fetch_id_data/PAN` | `id_no`, `name`, `dob`, `unique_request_id` | `status`, `name_as_per_pan_match`, `date_of_birth_match`, `aadhaar_seeding_status`, `category` | `KYC_ID_VERIFY` |
| GST / CIN / DIN / Udyam / FSSAI / DL / Voter / Passport (02 §7.1, §2.2) | `POST /v3/client/kyc/fetch_id_data/{GST\|GST_ADVANCED\|CIN\|DIN\|PAN_TO_GST\|UDYAMAADHAAR\|FSSAI\|DRIVING_LICENSE\|VOTER_ID\|PASSPORT}` | `id_no`, (`dob`, `file_no` for passport), `is_advanced`, `listing_type` (PAN_TO_GST) | type-specific (see the appendix: `FetchIdCardResponse`, `GstVerificationResponse`, `CinVerificationResponse`) | `KYC_BUSINESS` / `KYC_ID_VERIFY` |
| ID OCR (02 §1.6) | `POST /v4/client/kyc/analyze/file/idcard` (multipart) | `front_part`, `back_part`, `unique_request_id` | `detections[].{id_type, id_attributes, verification_result, output_images}` | `KYC_ID_OCR` |
| Aadhaar masking (02 §1.4) | `POST /v4/client/kyc/aadhaar/mask` | `data` (b64), `data_content_type`, `consent:"yes"`, `mask_qr`, `is_validate`, `unique_request_id` | `masked_output`, `details.{masking_done_count, already_masked_count}`, `error_message` | `KYC_AADHAAR_MASK` |
| Liveness (02 §1.7) | `POST /v3/client/kyc/analyze/file/liveness` (multipart) | `image` | `result PASS\|FAIL\|UNKNOWN`, `score` | `KYC_SELFIE` (confirm) |
| Face match (02 §1.7) | `POST /v3/client/kyc/facematch` (multipart) | `file1`, `file2`, `minimum_match` | `result`, `score` | `KYC_FACE_MATCH` |
| Fuzzy match (02 §1.8) | `POST /v3/client/kyc/fuzzy_match` | `source.text`, `target.text`, `context name\|address`, `confidence` | `matched`, `match_score` | `KYC_FUZZY_MATCH` |
| DigiStudio request (DigiLocker / Offline KYC / V-CIP / reverse penny drop) (02 §1.2) | `POST /client/kyc/v2/request/with_template` | `customer_identifier*`, `template_name*`, `customer_name`, `reference_id`, `transaction_id`, `notify_customer`, `customer_notification_mode`, `expire_in_days`, `generate_access_token` | `id` (KID), `status`, `actions[]`, `access_token.id` | on completion: `KYC_DIGILOCKER` / `KYC_AADHAAR_XML` |
| DigiStudio status/result | `POST /client/kyc/v2/{id}/response?detail_response=true` | — | `status`, `actions[].{type, status, id_card_data_response, action_data, sub_actions, file_id}` | — |
| DigiStudio media (photo, XML, docs) | `GET /client/kyc/v2/media/{fileId}?doc_type=AADHAAR&xml=&base64=` | — | file | — |
| DigiStudio approve/reject, reattempt, token | `POST /client/kyc/v2/request/{id}/manage_approval`, `…/reattempt`, `POST /user/auth/generate_token` | `status`; `action_ids`; `entityId` | — | — |
| KYC report PDF | `POST /client/kyc/v3/{id}/kyc_report` | — | pdf | `KYC_REPORT` (confirm) |
| Purge Digio copy | `POST /client/kyc/purge/{id}` | — | `success` | — |
| AML screening (02 §1.5) | `POST /client/v1/user/screen` | `screening_mode`, `screening_settings.{screening_type, response_mode, lists, cut_off_threshold}`, `user_details.{client_id*, name*, pan, dob, gender, nationality, address}` | `risk_level`, `found`, `total_alerts`, `*_alert_details[]`, `case_reference` | `AML_SCREEN` (new) |
| Vehicle RC (02 §2.1) | `POST /client/v4/apis/verify/vehicle_rc` | `id` (reg no), `unique_request_id` | `user_details`, `vehicle_details`, `finance_details.{financier, financed, insurance_*}`, `permit_details.{blacklist_status,…}` | `KYC_VEHICLE_RC` (new) |
| UAN (02 Stage 5) | `POST /client/v4/apis/verify/uan` | `identifier`, `uan_history` | `employment_history[]` | `KYC_UAN` |
| Penny drop / pennyless (02 §3.1) | `POST /v4/client/verify/bank_account` | `beneficiary_account_no*`, `beneficiary_ifsc*`, `beneficiary_name`, `validation_mode PENNY_LESS\|PENNY_DROP\|AUTO`, `unique_request_id` | `verified`, `beneficiary_name_with_bank`, `fuzzy_match_result`, `fuzzy_match_score`, `error_code`, `validation_mode` | `BANK_PENNY_DROP` / `BANK_PENNYLESS` |
| VPA (02 §3.2) | `POST /v3/client/public/upi/check_vpa` | `virtual_address`, `name` | `status`, `customer_name`, `fuzzy_match_score` | `KYC_UPI_VPA` |
| e-Sign create (02 §8.1) | `POST /v2/client/document/uploadpdf` | `file_name*`, `file_data*`, `signers[].{identifier*, name, sign_type, reason, index, signer_tag}`, `sequential`, `expire_in_days`, `notify_signers`, `send_sign_link`, `display_on_page`, `sign_coordinates`, `signature_verification`, `estamp_request`, **`callback` (idempotency)**, `reference_id`, `include_authentication_url`, `will_self_sign`+`signatory` | `id` (DID), `agreement_status draft\|requested\|completed\|expired\|failed`, `signing_parties[].{status, authentication_url}` | `ESIGN_AADHAAR` on completion |
| e-Sign status / download / cancel | `GET /v2/client/document/{docId}`; `GET /v2/client/document/download?document_id=`; `POST /v2/client/document/{docId}/cancel` (only while `requested`) | — | as above; pdf | — |
| Mandate create (02 §8.3) | `POST /v3/client/mandate/create_form` | `corporate_config_id*`, `mandate_type:CREATE*`, `auth_mode API\|ESIGN\|PHYSICAL\|upi`, `customer_identifier`, `notify_customer`, `include_authentication_url`, `mandate_data.{customer_name*, customer_account_number*, customer_account_type*, management_category*, is_recurring*, frequency*, first_collection_date*, final_collection_date, collection_amount*, maximum_amount*, customer_ref_number, scheme_ref_number, destination_bank_id}` | `id`/`mandate_id` (ENA), `state`, `authentication_url` | `MANDATE_API` / `MANDATE_ESIGN` / `MANDATE_PHYSICAL` |
| Mandate status | `GET /v3/client/mandate/{mandate_id}`; `GET /v3/client/mandate/by_crn/{crn}` | — | `state`, `umrn`, `bank_details.state`, `npci_auth_reject_reason`, `npci_auth_failed_error` | — |
| Live banks | `GET /v3/client/mandate/banks?mode=API\|ESIGN\|PHYSICAL` | — | `[{id, name, ifscPrefix, apiMandateNetBanking, apiMandateDebitCard, apiMandateAadhaarCard, esignMandate, physicalMandate}]` | — |
| Physical NACH | `POST /v3/client/mandate/{id}/generate_file`; `POST /v3/client/mandate/submit_signed_mandate` (multipart) | `file`, `request.id` | pdf; `success` | `MANDATE_PHYSICAL` |
| Mandate cancel (03 §5) | `POST /v3/client/mandate/registration_cancel` | `umrn` or `digio_mandate_id`, `reason C001–C004` | `state` | — |
| Debit presentation (03 §2) | `POST /v3/client/nach_debit/scheduled/register` | `umrn*`, `corporate_account_number*`, `frequency*` (`onetime`), `amount_in_paise`, `settlement_date`, `corporate_config_id`, **`unique_key`**, `narration` | plan `id`, `upcoming_transaction.state` | `MANDATE_DEBIT` |
| Debit plan / txn | `GET /v3/client/nach_debit/scheduled/{planId}`; `GET …/transaction/{txnId}`; `POST …/scheduled/action` (`cancel\|pause\|resume`); `POST …/transaction/action` (`cancel\|reattempt`) | — | `details.status NEW…PAYMENT_SUCCESS\|PAYMENT_FAILED`, `failure_code`, `failure_reason`, `utr` | — |
| Settlement | `GET /v3/client/nach_debit/requester_settlement/{id}` | — | consolidated settlement | — |
| CKYC upload (03 §6) | `POST /v3/client/kyc/ckyc/register` (multipart) | `register_ckyc_request` + `image1..3` | `reference_id`, `status` | `CKYC_UPLOAD` (new) |

**Not in Digio's public docs (ask Digio):** the KYB 360 / B2B report endpoint, the TAN endpoint path, the CKYC
search/download API (Google Doc), the electricity-bill and geocode endpoint paths, and billing per DigiStudio action.

## 3. Standard DigiStudio templates named in the docs
`DIGILOCKER_AADHAAR_PAN` (DigiLocker Aadhaar + PAN) · `OfflineKyc` (Aadhaar offline) · `2WAY-VIDEO-KYC` (agent
V-CIP) · `REVERSE_PENNY_DROP`. Custom workflows use lower_snake_case names. Confirm which ones exist on our account.

## 4. Webhook envelope
```json
{ "id": "WHN…", "created_at": 1528370034000, "event": "<event>", "entities": ["…"], "payload": { "<entity>": { … } } }
```
`id` is the de-duplication key (at-least-once delivery). Unknown fields must be ignored. Respond `200` within
10 s, or Digio retries (5× in the first hour, then hourly up to 24 h; 53 attempts in sandbox) and then disables the webhooks.
Webhook *groups* can route each product to its own URL and secret. We use one URL per company, `/webhooks/digio/{companyId}`.

## 5. Events → our handling

| Product | Event | Our transition |
|---|---|---|
| DigiSign | `doc.signed` | if `agreement_status=="completed"` → **Signed** (+ download); else update the signer row only |
| | `doc.sign.failed` / `doc.sign.rejected` | Failed / Rejected (+ `error_code`, `validation_results`) |
| DigiStudio | `KYC_REQUEST_CREATED` | Sent |
| | `KYC_ACTION_COMPLETED`, `DOCUMENTS_PULLED_PARTIALLY` | Processing (fetch the result, show partials) |
| | `KYC_REQUEST_COMPLETED`, `KYC_REQUEST_REVIEW_READY`, `DOCUMENTS_PULLED_SUCCESSFULLY` | fetch result → Succeeded (map + evidence) |
| | `KYC_REQUEST_APPROVED` / `KYC_REQUEST_REJECTED` | Succeeded / Rejected (when the auditor flow is used) |
| | `KYC_REQUEST_EXPIRED` / `KYC_REQUEST_TERMINATED` | Expired / Failed |
| DigiCollect mandate | `APIMNDT.AUTHSUCCESS`, `MNDT.AUTHSUCCESS` | AuthSuccess (not final) |
| | `MNDT.TRANSUCCESS`, `MNDT.SPOBANKACCEPT`, `MNDT.NPCIACCEPT` | InProgress |
| | `APIMNDT.DESTACCEPT`, `MNDT.DESTBANKACCEPT` | **Registered** (UMRN final) |
| | `APIMNDT.AUTHFAIL`, `APIMNDT.DESTREJECT`, `MNDT.AUTHFAIL`, `MNDT.TRANFAIL`, `MNDT.SPOBANKREJECT`, `MNDT.NPCIREJECT`, `MNDT.DESTBANKREJECT` | Rejected (+ reason) |
| | `MNDT.CANCELINIT`, `MNDT.BANKCANCEL` | Cancelled (bank-cancel on an active loan → collections task) |
| DigiCollect debit | `NACH.DEBIT.TXN.CREATED`, `…SUBMITTEDTOBANK`, `…TRANSUCCESS`, `…SPOBANKACCEPT`, `…AUTHENTICATED`, `…DELAYED` | Presented / InProgress |
| | `NACH.DEBIT.SUCCESS` | **Paid** → Receipt |
| | `NACH.DEBIT.FAILED` | **Bounced** (+ code → RPN policy) |
| | `NACH.DEBIT.TXN.SKIPPED`, `…TRANFAIL`, `…SPOBANKREJECT` | NotPresented (fix → retry) |
| UPI | `UPI.MNDT.*` (AUTHSUCCESS, AUTHFAIL, REVOKED, PAUSED, RESUMED, REGISTER.FAILURE, AUTODEBIT.*) | as for NACH (Phase 6) |
| CKYC | `CKYC.STATUS.UPDATE` (`AWAITING_APPROVAL / APPROVED / REJECTED / FAILURE`) | CKYC status + KIN |

## 6. Error codes worth mapping to plain words (full lists in the appendix)
- **Aadhaar Offline:** 1002 KYC failed · 1004/10016/10018/10019 UIDAI down → "UIDAI is down, try later" · 1005
  OTP attempts exceeded · **10012 mobile not linked to Aadhaar** → suggest DigiLocker or another route · 403 bad credentials.
- **DigiLocker:** DLC002–005 / DGV001 → retry later · `DIGILOCKER_DATA_MISMATCH`, "Full Name did not match with
  PAN Data", "PAN DOB not matched with Aadhaar data" → ask the customer to recheck · `user_cancelled`, `consent_denied`.
- **e-Sign (gateway):** `OTP_INVALID`, `AADHAAR_HAS_NO_MOBILE`, `INCORRECT_AADHAAR_ID`, `AUTHENTICATION_FAILED`,
  `ESP_UIDAI_DOWN`, `SIGNING_TIMED_OUT_ON_ESP`, `SIGNING_CANCELLED_ON_ESP`; API: "Not Sufficient credits" →
  alert the vendor (buy credits), "This sign request has been expired" → re-send.
- **e-Mandate (NPCI ONMAGS):** AP01–AP33, e.g. AP08 account-holder name mismatch, AP10/AP12 amount over limit,
  AP13 full loan amount used instead of EMI, AP30 browser closed by the customer.
- **NACH debit returns:** per code, whether re-presentation is allowed (e.g. *Balance insufficient* = yes, *Account closed* = no) ·
  Digio-side DG101–DG104.
- **Penny drop:** invalid account/IFSC, NRE account, account blocked/frozen/closed, bank unreachable (retry).
