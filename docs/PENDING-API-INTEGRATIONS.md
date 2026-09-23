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
