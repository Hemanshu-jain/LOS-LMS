# Captured Digio sandbox responses

One file per operation, `<operation>.json` (e.g. `pan-verify.json`, `digilocker-response.json`, `esign-uploadpdf.json`,
`webhook-doc-signed-2-signers.json`). These are the **source of truth for field mapping** and the fixtures for
`LosLms.Tests` (`FakeDigioHandler`).

Rules:
- Sandbox only. Never a production response.
- **Mask all PII before committing:** names → `TEST NAME`; PAN / Aadhaar / account / mobile → `X` except the last 4;
  emails → `test@example.com`; addresses → `TEST ADDRESS, CITY, STATE 000000`; photos / base64 files → `"<base64 omitted>"`.
- Keep every key and the structure exactly as received (including nulls and unknown fields). Change values only.
- Add a header comment field `"_captured": "YYYY-MM-DD, sandbox, <endpoint>"`.
