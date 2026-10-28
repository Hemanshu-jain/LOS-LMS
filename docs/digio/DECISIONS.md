# Digio integration — decisions & Phase 0 findings

Filled in during Phase 0 (`05-SESSION-PROMPTS.md` §3.1). Each answer changes what gets built: see `00-MASTER-PLAN.md` §9.
Write "TBD — owner: <name>" rather than leaving a line blank.

## A. Business decisions

| # | Decision | Answer | Decided by / date |
|---|---|---|---|
| 1 | Products enabled on our Digio account (sandbox / production) + DigiStudio template names | | |
| 2 | Default customer channel (link via SMS / WhatsApp / email, or on the officer's device) | Offer all: SMS / WhatsApp / email link and 'open on this device'; the officer picks per request. Default = SMS link. | Owner via head, 2026-10-28 |
| 3 | Minimum KYC evidence per party to complete Customer Details (per product) | | |
| 4 | Aadhaar at rest: keep last 4 + keyed hash + masked image only (plan R3)? Compliance sign-off | No. Full Aadhaar stays stored unmasked; plan R3 (last 4 + keyed hash migration) dropped. | Owner via head, 2026-10-28 |
| 5 | Loan agreement signers (applicant / co-applicants / guarantors / lender signatory: name and email), sequential or parallel | Borrowers (applicant, co-applicants, guarantors) sign IN ORDER (sequential). The lender side is signed by the company owner: the signature image is configured in the system and applied by us, not a Digio signer. To confirm with Digio that this is acceptable. | Owner via head, 2026-10-28 |
| 6 | e-Stamp: states, denominations, inventory owner | | |
| 7 | NACH: e-NACH vs physical default; frequency (Adhoc / Monthly); max amount = EMI × ?; category codes (EMI, security); corporate_config_id(s); corporate account no. (record only "sandbox id received on <date>", never the value; see 06-NACH.md §1) | TBD, defaults set by head (owner unsure): per-company configurable; e-NACH default with physical fallback; frequency Adhoc; max amount = EMI x 2; corporate_config_id from Digio (owner to fetch). | Owner via head, 2026-10-28 |
| 8 | Release-funds gate: agreement Signed? mandate Registered? Admin bypass allowed? | Release funds requires agreement Signed AND mandate Registered. Admin bypass allowed, and it is logged. | Owner via head, 2026-10-28 |
| 9 | Collections: auto-present EMIs? bounce charge; re-presentation attempts / gap; receipt policy | | |
| 10 | Compliance owner for CKYC, sanctions screening, V-CIP, retention period | | |

## B. Phase 0 technical findings

| Item | Finding |
|---|---|
| Date format accepted (`dob`, `settlement_date`, `first_collection_date`) | PAN: both `1990-01-01` and `01/01/1990` returned 200 with identical results, so Digio accepts either; we send `yyyy-MM-dd`. In sandbox the canned reply is `name_as_per_pan_match: false`, `date_of_birth_match: false` for any input, so the match logic can only be proven in production. `settlement_date` and `first_collection_date` not probed (NACH needs the owner's corporate_config_id). Vehicle RC dates come back `yyyy-MM-dd`. |
| Gateway link `{gateway}{id}/{txn}/{identifier}?redirect_url=` works for DID / KID / ENA? | Not tested end to end (no signer opened a link). DigiLocker request returns `access_token.{entity_id,id}` (GWT…) with `generate_access_token: true`; the customer link is built from KID + token. e-Sign (DID) and mandate (ENA) not run: need `esign=1&mobile=` / `nachConfigId=`. Still to verify. |
| Products returning 403 (not enabled) | None returned 403 on sandbox. AML (DigiShield) returns **404, code 1024 "Requested resource not found"** on `POST /client/v1/user/screen` (path and body match the spec): treated as **DigiShield not enabled on sandbox; owner to ask Digio**. The app shows "Not enabled on your Digio account" on 403/404. |
| Webhook reached our URL? (host: desktop + tunnel / cloud), observed delay | Not tested in Phase 0 (webhooks are registered by the owner, never by us). Digio's sandbox delay seen earlier: about 5 minutes; the reconciler covers a missed webhook. |
| DigiStudio result: where DigiLocker fields live in `/response` | Request (`/client/kyc/v2/request/with_template`, template DIGILOCKER_AADHAAR_PAN) 200: `id` KID…, `status: requested`, `expire_in_days`, `access_token`, `template_id`, no `actions` yet. `POST /client/kyc/v2/{id}/response?detail_response=true` 200 while the customer has not acted: `actions[0]` = `{id, type: "digilocker", status: "requested", details: {}, ...}`. The filled location of name/dob/address is **unknown until a customer completes a sandbox journey**; map from `actions[].details` / `id_card_data_response` per the spec only after capturing one. |
| `signature_verification` accepted shape (from a real call) | Not run (needs a signed document). |
| NACH `unique_key` idempotency confirmed? | Not run (needs nachConfigId). |
| Billing per DigiStudio action / per e-Sign signer (from Digio) | Unknown, ask Digio. |
| Still unknown, to ask Digio (KYB 360 endpoint, TAN path, CKYC doc, geocode, electricity, sandbox debit simulation) | Ask Digio: enable DigiShield (AML) on sandbox and production; billing per DigiStudio action and per e-Sign signer; KYB 360, TAN, CKYC doc, geocode, electricity, sandbox debit simulation. Aadhaar mask: sandbox replies 200 with `error_message: "No ID card detected in the image"`, counts 0 for our 1x1 test PNG (expected; a real card image is needed to see `masked_output`). GST: 200 with `error_message: "No records found"` for the spec's sample GSTIN. Mandate banks: 200, each bank has `esign_mandate`, `api_mandate`, `physical_mandate`, `allowed_auth_sub_type`, `penny_less`. |
