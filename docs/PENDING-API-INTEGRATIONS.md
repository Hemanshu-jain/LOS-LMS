# Pending API integrations — activates when provider keys arrive

Everything in this file is **built and dormant**: the UI, the data model and the fallback all exist and
work today; only the outbound provider call is missing. When a key/credential lands, wire it in at the
named drop-in point and set the config value — no schema changes, no UI rebuild.

Two providers are pending:

- **Gemini** — one API key. Fast; no onboarding.
- **Digio** — production credentials issued after their onboarding (MSA + ₹11,800 onboarding fee +
  documents). Several features below map to different Digio products, each metered separately.

Golden rule already enforced in code: **no provider ever fabricates a result.** Every stub returns a
"not configured" state and leaves the human-entered value authoritative.

---

## Gemini (`Gemini__ApiKey`)

Config section `Gemini` in `appsettings.json` (`ApiKey` blank, `Model` = `gemini-2.5-flash`). Set the
real key via env var `Gemini__ApiKey` or user-secrets — never commit it.

| # | Feature | Drop-in point | Now (no key) | When key set |
|---|---------|---------------|--------------|--------------|
| 6 | Scanned-document **signature detection** | `Services/Integrations/SignatureDetectionService.cs` → `DetectAsync`; triggered by the **Auto-check** button in `Pages/Applications/Stages/DocumentChecklist.razor` | "Signed auto-check unavailable" — staff tick the **Signed** box manually | Auto-check reads the scan, pre-ticks **Signed** on a confident yes (officer can override; persists on Save) |

Possible later extension (not built): Gemini document **OCR / auto-fill** (read a PAN/Aadhaar/bank scan
and pre-fill fields). Same key. Flag before building — needs its own UI.

---

## Digio — what is BUILT, switches on by entering keys

- **Keys:** SuperAdmin → **Digio keys** (`/system/digio`) — per company: client id, client secret, webhook
  secret, sandbox/production. Stored encrypted in the credential vault (`App_Data`), never in the DB.
- **One gate for every call:** `Services/Integrations/DigioClient.cs` → `PostAsync`. No keys → "not configured",
  nothing sent. Overdue bill → paused, nothing sent. Otherwise HTTP Basic call + usage logged.
  Sandbox `https://ext.digio.in:444`, production `https://api.digio.in`.
- **Penny drop — LIVE when keys set:** Bank & Financial → Run penny-drop check →
  `POST /v4/client/verify/bank_account` (PENNY_DROP, name vs Stage 1 KYC name). Billed ₹2.50.
- **e-Sign — LIVE when keys set:** Post-Sanction → Send for e-Signature → `POST /v2/client/document/uploadpdf`
  (base64 agreement, Aadhaar sign, link to applicant's mobile/email). Status becomes **Sent**; only Digio's
  webhook sets **Signed** — and that is when the ₹10.60 credit is metered.
- **Webhook:** `POST /webhooks/digio/{companyId}` — verified by `X-Digio-Checksum` (HMAC-SHA256 of body with the
  company's webhook secret). Paste the URL shown on the Digio keys page into Digio dashboard → Profile → Webhooks
  and enable DOC.SIGNED / DOC.SIGN.FAILED / DOC.SIGN.REJECTED.
- **Billing:** usage report `/admin/api-usage`; postpaid bill per closed month, due month-end + 7 days
  (`ApiBilling:GraceDays`); unpaid after that the company's app is paused until SuperAdmin clicks **Mark paid**.

**Still stubbed — endpoint not in Digio's public docs (they share it at onboarding):** Aadhaar/PAN fetch
(needs a DigiStudio KYC *template* + their web SDK, `POST /client/kyc/v2/request/with_template`), Aadhaar
masking, ID OCR/PAN verify, Business KYC (GSTIN/CIN), bank statement analyzer (Account Aggregator FIU).
Wire each through `DigioClient.PostAsync` with its rate code from `DigioRates`.

## Digio (production credentials via onboarding)

Contact: Surendra D, Digio. Onboarding = send documents → sign MSA → pay onboarding fee → receive
production credentials per NBFC. API docs: https://documentation.digio.in/

| # | Feature | Digio product (indicative rate) | Drop-in point | Now (no key) |
|---|---------|--------------------------------|---------------|--------------|
| 2 | **Aadhaar fetch → auto-fill** party Personal/Contact | DigiKYC — DigiLocker fetch / Aadhaar Offline XML KYC (~₹2/fetch) | `Services/Integrations/DigiKycService.cs` → `VerifyAsync` (parse response into `DigiKycData`; `Apply` already fills blanks). "Verify OCR" buttons in `CustomerDetails.razor` | "API key not configured" notice; officer types the fields |
| 5 | **Aadhaar masking** on the uploaded Aadhaar | DigiKYC — Aadhaar masking API (~₹1) | Same service; call after the Aadhaar upload in `CustomerDetails.razor` | Manual: "Aadhaar generated/downloaded on" date + ≤5-day stale flag (already built) |
| — | **PAN / ID OCR + verification** | DigiKYC — ID OCR (~₹2) + PAN verify (~₹1.50) | `DigiKycService.VerifyAsync`; "Verify OCR" in `CustomerDetails.razor` | Not-configured notice; manual entry |
| — | **Bank account penny-drop** verification | DigiKYC — Penny drop (~₹2.50) | `Models/Banking/BankDetail.cs` `PennyDropStatus` + the verify flow in `BankFinancial.razor` | Stays `NotConfigured`; never invented |
| — | **Bank statement analyzer** (Account Aggregator) | DigiOLink AA + BSA (~₹13/statement; 30–35 day FIU onboarding) | `Services/Integrations/BankStatementAnalysisService.cs` → `RequestAsync`; `BankFinancial.razor` | Request stub; no analysis produced |
| — | **e-Sign** loan agreement (Aadhaar eSign) | DigiSign (onboarding ₹10k + ₹10.60/credit) | `Services/Integrations/EsignService.cs` → `DispatchAsync`; `PostSanction.razor` `AgreementEsignStatus` | Status never leaves `NotSent`; document still generated locally |
| — | **eNACH / NACH mandate** | DigiCollect (needs NBFC's own utility code from its bank) | `Models/Disbursal/EnachMandate.cs` + mandate flow in `PostSanction.razor` | Mandate captured but not registered with a sponsor bank |
| 12 | **Company / ROC verification** (Registrar of Companies) | DigiKYC — Business KYC: CIN / DIN / GSTIN / Udyam etc. (~₹3/check) | **Built (stub):** `Services/Integrations/BusinessKycService.cs` → `VerifyAsync` (marked `REAL PROVIDER:` boundary; `Apply` fills blank firm fields). "Verify company (ROC)" button in Approvals (Business tab) | Button reports "provider not configured"; firm details entered/pre-filled manually |
| — | **RBI-compliant Video KYC (V-CIP)** | DigiKYC — 1-way V-CIP (~₹6) | New; today's in-app camera recording (`CustomerDetails.razor` Video KYC) is **evidence only**, not V-CIP | In-app recording stored with the file |

Rates are indicative from the Digio quote (Sept 2026) and exclude taxes / NPCI / sponsor-bank charges —
confirm on the invoice.

---

## Not a provider-key item (tracked so it isn't confused with the above)

- **CIBIL / credit bureau gate** — `Party.CibilStatus` + the CIBIL gate. No bureau is wired; every
  application currently needs an **admin bypass** to pass the gate. Needs a bureau provider decision
  (separate from Digio/Gemini), then a drop-in on the CIBIL check in `CustomerDetails.razor`.

---

## How to turn each on (checklist)

1. Obtain the key/credential from the provider.
2. Set the config value out-of-file (`Gemini__ApiKey`, or the Digio credentials in the desktop app's
   config / env). Never commit a real secret.
3. Implement the outbound call at the named drop-in method (Gemini is already implemented; Digio methods
   have a marked `REAL PROVIDER:` boundary).
4. Test against one real record on the dev DB before shipping.
5. Ship via the normal signed update (`publish.ps1`).
