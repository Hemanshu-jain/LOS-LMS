# 05 — Prompts for the local build session

How to use: paste **3.0 Kickoff** once at the start of a session. Then paste the prompt for the phase you are
on. Run **R. Review** before every PR. Each phase ends with its **sandbox test script**, and the results go in the PR.

Ground truth for the session: `docs/digio/00`–`04`. API schemas: `docs/digio/appendix/DIGIO-API-SPEC-SUMMARY.md`.
Page text: `docs/digio/appendix/DIGIO-DOCS-TEXT.md`. Live docs (if the network allows): `https://documentation.digio.in`
(the site is client-rendered; use `curl https://documentation.digio.in/swagger/<operationId>.json` for a spec).

---

## 3.0 Kickoff (paste first, every new session)
```
You are building the Digio integration for the LOS/LMS repo (.NET 8 Blazor Server, EF Core MySQL, multi-tenant NBFC
loan origination, staff-only app). Read, in order: docs/digio/00-MASTER-PLAN.md, 01-FOUNDATION.md,
02-FEATURES-BY-STAGE.md, 03-LMS-COLLECTIONS.md, 04-DIGIO-API-REFERENCE.md. Then read the current code you will touch:
LosLms/Services/Integrations/DigioClient.cs, DigioRates.cs, EsignService.cs, DigiKycService.cs, BusinessKycService.cs,
ApiBillingService.cs, LosLms/Program.cs (webhook ~480-535, CSP ~395-470), Data/LosDbContext.cs (query filters ~1000-1080),
Pages/SuperAdmin/DigioKeys.razor, and LosLms.Tests/DigioTests.cs.

Hard rules (from 00 §2): never invent data; one door to Digio (DigioClient); server is the source of truth; webhooks
always answer 200 and never block Digio's queue; idempotency id on every create; sandbox evidence never passes a gate
on a production company; evidence goes stale when inputs change; no PII or secrets in logs; one phase = one PR with
tests; additive MySQL-safe migrations with [MaxLength] on every string; no live network in tests.

Do not write code yet. Reply with: (1) the current architecture in 10 lines, (2) anything in the plan that
contradicts the code (file:line), (3) the exact files you will create or change for the phase I name next,
(4) open questions. Then wait for me.
```

## 3.1 Phase 0 — Readiness: sandbox smoke + sample capture (no feature code)
```
Phase 0 (docs/digio/00 §5, §9). Build a throwaway, SuperAdmin-only, Development-only page or a console harness that
uses the vault credentials for ONE company (never print the secret) and calls the sandbox once for each product our
account has enabled: PAN verify (fetch_id_data/PAN) with a Digio sandbox test PAN; GST verify; Aadhaar mask with a
sample image; a DigiStudio with_template request for DIGILOCKER_AADHAAR_PAN (notify_customer=false) plus its
/response; vehicle_rc with a test registration; AML screen with a test name; GET /v3/client/mandate/banks; an e-Sign
uploadpdf of a 1-page test PDF to my own mobile; and the NACH sandbox test in docs/digio/06-NACH.md §9 using the sandbox
NACH config id (I will enter it; read it from the vault or a Development-only user-secret, never hard-code or commit it).
For each one, save the raw JSON response with ALL PII masked (names → "TEST NAME", numbers → X except the last 4) into
docs/digio/samples/<operation>.json, and record in docs/digio/DECISIONS.md: HTTP status, the error code if any, the
date format Digio accepted (dob / settlement_date), whether the gateway link format
{gateway}{id}/{txn}/{identifier}?redirect_url=… works for KID and ENA as well as DID, and which products returned 403
(not enabled). Then test webhook reachability: register our URL in the sandbox enterprise dashboard (Profile → Webhooks),
trigger one event, and confirm it arrives (sandbox delay is ~5 min). Delete the harness afterwards, or keep it behind
#if DEBUG. Output: the samples and DECISIONS.md filled in. List what is still unknown and must be asked of Digio.
```
**Test script:** every enabled product has a sample file; DECISIONS.md lists the 10 decisions from 00 §9 (answered or owner named).

## 3.2 Phase 1a — Hotfixes to what is live
```
Phase 1a (docs/digio/01 §1). (1) Change the /webhooks/digio/{companyId} handler so it ALWAYS returns 200: unknown
company, oversize, bad checksum and bad JSON are logged and ignored (bad checksum also raises a rate-limited admin
notice), never processed. (2) doc.signed sets AgreementEsignStatus=Signed only when payload document.agreement_status ==
"completed"; per-signer signed events do not. (3) On completion, download the signed PDF via
GET /v2/client/document/download?document_id=… through DigioClient, store it with IFileStore under
App_Data/uploads/{applicationId}/agreement/signed-{DID}.pdf, add Disbursement.SignedAgreementFilePath (additive
migration) and a download link on Post Sanction. Tests: the 4 response cases are each 200 and only the valid one is
processed; the two-signer sample with one signer pending is not Signed; completed → Signed + file saved (fake
handler). Keep DigioTests green. Build + test, summarise the diff.
```
**Test script:** send a 1-signer agreement in sandbox, sign it, and within about 5 min the status is Signed and the signed PDF opens from Post Sanction. Post a webhook with a wrong checksum using curl: it gets 200 and nothing changes.

## 3.3 Phase 1 — Foundation (may be split into 2–3 PRs: client+tables / inbox+processor+reconciler / UI kit+pages)
```
Phase 1 (docs/digio/01 §2–§12). Implement: DigioClient.SendAsync/DownloadAsync (environment, idempotency key, error
mapping, timeouts, request log before send, no-PII logging) keeping PostAsync working; DigioEndpoints constants;
tables DigioRequest, DigioWebhookInbox, VerificationEvidence, DigioSettings with tenant query filters; webhook →
inbox insert (unique CompanyId+DigioEventId) → 200; DigioEventProcessor and DigioReconciler BackgroundServices with
backoff; DigioStatusNotifier (AdminRequestNotifier pattern); the shared UI kit in Components/Shared/Digio
(VerifyBadge, VerifyButton, CustomerActionPanel, DigioDiff, EvidenceList, ConsentCheckbox); /digio/return page;
DigioKeys page additions (product toggles, Test connection, webhook URL from PublicBaseUrl, last webhook time);
/admin/integrations health page; FakeDigioHandler test harness using the Phase 0 samples. Migrate the existing
penny-drop and e-Sign calls onto SendAsync without changing user-visible behaviour. Tests per 01 §11. Do it as
small PRs in the order: client+tables, pipeline, UI+pages.
```
**Test script:** replay the same webhook twice → one state change. Stop the app, let a sandbox e-Sign complete, start the app → the reconciler marks it Signed. Open the same application in two browsers → a status change appears in both. The keys page Test connection shows OK, and shows 401 with a deliberately wrong secret.

## 3.4 Phase 2 — KYC core (Customer Details)
```
Phase 2 (docs/digio/02 §1.1, §1.2, §1.4, §1.5, §1.8, Stage 1 gate; 01 §3.5). Implement PAN verify, DigiLocker
Aadhaar+PAN (link + on-device), Aadhaar masking on upload, AML screening with Admin review of alerts, the fuzzy-match
helper, the consent register, and the KYC evidence gate in GateCheckService with an Admin bypass via AdminRequest.
Map ONLY from docs/digio/samples (no guessed fields). DigiKycService.Apply stays fill-blanks-only; conflicts use
DigioDiff. BEFORE the Aadhaar-at-rest migration (AadhaarLast4 + AadhaarHash, null the full number, dedupe on the hash),
STOP and ask me to confirm the compliance decision; ship that migration as its own PR. Tests: mapping from samples,
staleness, sandbox-vs-production gate, dedupe on hash, no PII in logs.
```
**Test script:** party with a sandbox PAN → Verified. Change one letter → Stale and the gate is blocked. DigiLocker by link to my phone → fields fill blanks only, and a conflicting DOB shows the diff. An Aadhaar upload is stored masked. An AML test name that hits → completion blocked → the Admin marks a false positive → unblocked, with a note.

## 3.5 Phase 3 — e-Sign hardening, e-stamp, e-NACH
```
Phase 3 (docs/digio/02 §8.1–8.4 and docs/digio/06-NACH.md, which is the authoritative NACH spec). Multi-party e-Sign: all parties with verified KYC (+ lender signatory from
settings), signature_verification rules (NAME fuzzy, YOB, AADHAAR last 4, GENDER) built from evidence, callback =
our request id, the agreement hash as fingerprint, cancel + re-send when the agreement changes, per-signer status rows with
Resend/Open on this device. e-stamp via estamp_request tags from settings (behind its toggle). e-NACH: replace the
MandateFlow stub with live banks lookup, create_form (settings: corporate_config_id, category, frequency, max
multiplier), the mandate webhook lifecycle with GET confirmation, UMRN capture, and physical NACH generate_file +
submit_signed_mandate. Release-funds gate per DECISIONS.md §8 (signed + registered, Admin bypass logged).
Tests: two-signer flow, rejection rule display, idempotent create, mandate state machine incl. out-of-order events.
```
**Test script:** 2 signers (my two numbers): sign one → "1 of 2 signed"; sign the other → Signed + PDF saved. Regenerate the agreement after sending → the old request is cancelled and a new one sent. A sandbox e-mandate by net banking → AuthSuccess → Registered with a UMRN (sandbox may need Digio to simulate the bank response; ask).

## 3.6 Phase 4 — Asset, business, documents
```
Phase 4 (docs/digio/02 §2.1, §7.1, §1.6, §1.7, Stage 4). Vehicle RC verify with the 6 check rows and the PDD re-check
after disbursal; KYB via fetch_id_data (GST/GST_ADVANCED/CIN/DIN/PAN_TO_GST/UDYAM) through BusinessKycService with AML
for directors; ID OCR as the DigiLocker fallback (OCR is never verification on its own); liveness + face match on
Party.PhotoPath; checklist items showing evidence and refusing a tick against contrary evidence without an Admin note;
the optional KYC report PDF. Tests from samples for each.
```
**Test script:** a used-vehicle RC with an existing financier → red flag shown. GSTIN → firm name filled if blank. The live photo against the DigiLocker photo → a face score shown. A checklist tick against Mismatch evidence → blocked for Staff and allowed for an Admin with a note.

## 3.7 Phase 5 — LMS collections
```
Phase 5 (docs/digio/03). Build the minimal servicing ledger (LoanAccount, Instalment snapshot at Release funds,
Presentation, Receipt), CollectionScheduler (daily 18:00 IST, T-1 cut-off, unique_key idempotency), debit webhooks +
GET confirmation, the bounce / re-presentation policy from settings (RPN-allowed codes only, ≥3 days, max attempts),
settlement reconciliation, mandate cancel on closure, the CKYC upload queue (behind its toggle), the loan account page
and the collections dashboard. Ask me for the bounce-charge policy before coding it. Tests per 03 §8.
```
**Test script:** per 03 §8 in sandbox (ask Digio how to simulate debit success and failure in sandbox).

## 3.8 Phase 6 — optional products (one prompt per product, only when chosen)
```
Phase 6: implement <product> per docs/digio/02 <section>. First capture its sandbox sample (Phase 0 rules), then
build on the foundation (DigioRequest, evidence, UI kit). Behind its product toggle. Tests from the sample.
```

## 3.9 Phase 7 — Production cutover (per company)
```
Phase 7 for company <code>. Checklist only — do not change code unless something is broken:
1) production keys entered (Production ticked, typed confirmation); 2) production webhook group URL + secret set in
https://enterprise.digio.in (Profile → Webhooks), events enabled for every product in use; 3) Test connection = OK;
4) product toggles match the signed Digio order; 5) DigiStudio templates exist in production with the same names;
6) NACH corporate_config_id(s) + sponsor account are production values; 7) e-stamp inventory is available; 8) rate
card = the Digio invoice; 9) pilot: one branch, 5 real files, end to end, with the evidence list reviewed by credit;
10) health page clean for 48 h; then enable for all branches. Write the result into docs/digio/DECISIONS.md.
```

---

## R. Review prompt (before every PR)
```
Review this phase adversarially against docs/digio/00 §2 and §6:
- any path that sets a field/flag/gate without a Digio response or an audited human decision?
- any Digio call not via DigioClient, or a create without an idempotency id?
- can a webhook replay, out-of-order event, reconciler race or double-click double-apply, double-bill or go backwards?
- can sandbox evidence pass a gate for a production company? can stale evidence pass?
- tenant isolation: can company A's webhook or request touch company B's rows?
- PII/secrets in logs, exceptions, URLs, ResultJson unencrypted, or the /digio/return page?
- does any autosave overwrite a Digio-owned field?
- every UI state honest and reachable (not configured / paused / waiting / verified / mismatch / failed / expired / stale / sandbox)?
- migrations additive, MySQL-safe, [MaxLength] everywhere, and tests pass on SQLite?
Fix everything you find, re-run dotnet build + dotnet test, and list what you fixed.
```

## When stuck
- **401** → wrong keys or the wrong environment toggle. **403** → product not enabled: ask Digio, do not code around it.
- **Webhook not arriving** → sandbox delay (~5 min), webhook group disabled in the dashboard ("Click to Activate"),
  desktop without a tunnel, Render asleep. The reconciler should still converge. Check `/admin/integrations`.
- **Field not where the plan says** → trust `docs/digio/samples/*.json` over this plan, and update the plan in the same PR.
- **Plan is wrong or impossible** → stop and say so with evidence. Do not improvise around a principle in 00 §2.
