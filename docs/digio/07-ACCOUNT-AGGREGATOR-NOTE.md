# 07 — Account Aggregator bank statements (design note only, no code)

Status: **design note.** Nothing is built and nothing is billed until the NBFC is an FIU.

## Why it waits
- Digio acts as the FIU-TSP (technology service provider). The **NBFC itself must be registered as an FIU** with an Account Aggregator
  through Sahamati. Digio quotes about **30–35 days** for that onboarding. No sandbox journey is possible before it.
- The Digio quote has no Account Aggregator price. Under the billing guard (`docs/digio` billing rules: no price, no call) the product stays
  blocked until the owner has a price in writing.

## What it would replace
`BankStatementAnalysisService` (the dormant "Fetch & Analyze (last 6 months)" button on Bank & Financial) already knows how to take a
fetched statement and fill the banking analysis. AA gives it real data instead of an upload.

## Flow to build once onboarded (from Digio's AA docs, to be confirmed against the live API)
1. Officer clicks **Fetch via Account Aggregator** (consent text stored with the request, same rule as the other Digio checks: recorded automatically).
2. Consent request created from a template → customer opens the AA link on their phone and approves.
3. Digio FI request → fetch FI data (statement, last 6 months) or its analytics.
4. Map into `BankStatementAnalysisService.ApplyAsync`; evidence subject `BANK_STATEMENT` (sandbox evidence never counts for production).
5. Webhook plus the existing reconciler complete a consent the customer finishes after the officer closes the page.

## Open questions for Digio (ask before any code)
- Price per consent / per FI fetch, and per analytics call.
- Exact endpoints and the consent template fields (none are in the public spec summary).
- Webhook event names and payload shape for consent approved / FI data ready.
- Whether the NBFC FIU registration is done by Digio's team or by us; the documents needed.

## Rule when this is built
Every call goes through `DigioClient` with an `ApiCode` and a seeded active rate, like everything else.
