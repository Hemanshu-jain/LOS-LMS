# What's new in LOS-LMS v1.2.3

Installs silently in the background.

## Verification services (Digio)
- **Digio keys** (SuperAdmin): each company's Digio credentials are entered once and stored encrypted. Nothing is sent to Digio until keys are entered.
- **Bank penny-drop check** on Bank & Financial, and **Aadhaar e-Sign** of the loan agreement on Post-Sanction, switch on as soon as keys are set.
- Monthly usage bills: if a bill stays unpaid past its due date, verification services pause until it is marked paid.

## Connect partner software
- Partner software (such as Paavan Software) can now link to your company with a **Connect** button: an Admin enters the short code on the **Connect an app** page and clicks Allow. No API keys to copy.
- **Connected apps** (Admin) shows every linked app, when it was last used, and a **Disconnect** button.
- Developer guide for partners at `/docs/`.

## Reliability
- Updates download more reliably (no more "file in use" errors).
- The database is backed up automatically before each update.
