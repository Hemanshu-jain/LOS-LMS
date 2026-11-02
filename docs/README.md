# Documentation

Start here. Everything is grouped by who needs it.

| I want to… | Read |
|---|---|
| Understand what the system is and how it is organised | [`development/ARCHITECTURE.md`](development/ARCHITECTURE.md) |
| Run it on my machine, run the tests, add a migration | [`development/SETUP.md`](development/SETUP.md) |
| Contribute a change | [`../CONTRIBUTING.md`](../CONTRIBUTING.md) |
| See what changed in each version | [`../CHANGELOG.md`](../CHANGELOG.md) |
| Deploy or operate the **cloud** edition (Render) | [`operations/CLOUD-DEPLOY.md`](operations/CLOUD-DEPLOY.md) |
| Back up or restore the production database | [`operations/BACKUP-RESTORE.md`](operations/BACKUP-RESTORE.md) |
| Build, install and update the **desktop** edition | [`operations/DESKTOP-RELEASE-AND-UPDATES.md`](operations/DESKTOP-RELEASE-AND-UPDATES.md) |
| Onboard a new client, issue or renew a licence | [`operations/NEW-CLIENT-ONBOARDING.md`](operations/NEW-CLIENT-ONBOARDING.md) |
| Work on the Digio integration (KYC, e-Sign, e-NACH, collections) | [`digio/00-MASTER-PLAN.md`](digio/00-MASTER-PLAN.md), then `01`…`06` in that folder |
| See which provider integrations are live or waiting for keys | [`integrations/PENDING-API-INTEGRATIONS.md`](integrations/PENDING-API-INTEGRATIONS.md) |
| Know which business assumptions are still unconfirmed | [`project/OPEN-QUESTIONS.md`](project/OPEN-QUESTIONS.md) |
| Get test logins and sample data for manual testing | [`testing/`](testing/) |
| Look at the original screen designs or the first walkthroughs | [`archive/`](archive/) (historical) |

## Folder map

```
docs/
  development/    how to build on the code            ARCHITECTURE, SETUP
  operations/     how to run and ship it              CLOUD-DEPLOY, BACKUP-RESTORE, DESKTOP-RELEASE-AND-UPDATES, NEW-CLIENT-ONBOARDING
  digio/          the Digio integration plan          00-MASTER-PLAN … 06-NACH, DECISIONS, samples/ (captured sandbox responses), appendix/
  integrations/   status of third-party integrations
  project/        decisions and open questions
  testing/        manual-test logins and data
  archive/        historical material, kept for reference
```

`docs/digio/samples/` is read by the automated tests and by the Development-only Digio harness. Keep it where it is.
