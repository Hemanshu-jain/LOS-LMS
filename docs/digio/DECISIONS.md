# Digio integration — decisions & Phase 0 findings

Filled in during Phase 0 (`05-SESSION-PROMPTS.md` §3.1). Each answer changes what gets built: see `00-MASTER-PLAN.md` §9.
Write "TBD — owner: <name>" rather than leaving a line blank.

## A. Business decisions

| # | Decision | Answer | Decided by / date |
|---|---|---|---|
| 1 | Products enabled on our Digio account (sandbox / production) + DigiStudio template names | | |
| 2 | Default customer channel (link via SMS / WhatsApp / email, or on the officer's device) | | |
| 3 | Minimum KYC evidence per party to complete Customer Details (per product) | | |
| 4 | Aadhaar at rest: keep last 4 + keyed hash + masked image only (plan R3)? Compliance sign-off | | |
| 5 | Loan agreement signers (applicant / co-applicants / guarantors / lender signatory: name and email), sequential or parallel | | |
| 6 | e-Stamp: states, denominations, inventory owner | | |
| 7 | NACH: e-NACH vs physical default; frequency (Adhoc / Monthly); max amount = EMI × ?; category codes (EMI, security); corporate_config_id(s); corporate account no. (record only "sandbox id received on <date>", never the value; see 06-NACH.md §1) | | |
| 8 | Release-funds gate: agreement Signed? mandate Registered? Admin bypass allowed? | | |
| 9 | Collections: auto-present EMIs? bounce charge; re-presentation attempts / gap; receipt policy | | |
| 10 | Compliance owner for CKYC, sanctions screening, V-CIP, retention period | | |

## B. Phase 0 technical findings

| Item | Finding |
|---|---|
| Date format accepted (`dob`, `settlement_date`, `first_collection_date`) | |
| Gateway link `{gateway}{id}/{txn}/{identifier}?redirect_url=` works for DID / KID / ENA? | |
| Products returning 403 (not enabled) | |
| Webhook reached our URL? (host: desktop + tunnel / cloud), observed delay | |
| DigiStudio result: where DigiLocker fields live in `/response` | |
| `signature_verification` accepted shape (from a real call) | |
| NACH `unique_key` idempotency confirmed? | |
| Billing per DigiStudio action / per e-Sign signer (from Digio) | |
| Still unknown, to ask Digio (KYB 360 endpoint, TAN path, CKYC doc, geocode, electricity, sandbox debit simulation) | |
