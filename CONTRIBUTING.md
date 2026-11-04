# Contributing

Thanks for working on LOS/LMS. This system handles customers' identity documents and lending decisions, and
**`main` is deployed to production automatically**, so the process below is deliberately strict.

## Before you start

1. Read [`docs/development/ARCHITECTURE.md`](docs/development/ARCHITECTURE.md) and set up locally with
   [`docs/development/SETUP.md`](docs/development/SETUP.md).
2. For anything touching Digio, read [`docs/digio/00-MASTER-PLAN.md`](docs/digio/00-MASTER-PLAN.md) first (its principles
   are binding: one door to Digio, never fabricate a result, idempotent webhooks).

## The workflow

1. Branch from `main`: `feat/short-name`, `fix/short-name`, `docs/…`, `chore/…`.
2. Make a focused change. Match the style of the surrounding code (naming, comment density, idiom).
3. Run the tests: `dotnet test tests/LosLms.Tests`. They need no database and no network.
4. Commit using [Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `refactor:`, `docs:`, `test:`,
   `chore:`. Explain *why* in the body when it is not obvious.
5. Open a pull request using the template. CI must be green (build, dependency vulnerability scan, tests).
6. Squash or merge as the maintainers prefer. **Merging to `main` triggers a production deploy** (unless the change is
   documentation only), so do not merge in the middle of a customer's working day without a reason.

## Rules that are not negotiable

| Rule | Why |
|---|---|
| **No secrets in git**: no keys, tokens, passwords, connection strings, licence or signing keys, real customer data | The repository's history is permanent. Use user-secrets, environment variables or the git-ignored files at the repo root |
| **Migrations are additive** and every string property has `[MaxLength]` | Production is live; MySQL cannot index `LONGTEXT`. Never edit a migration that has shipped |
| **Provider calls go through their client** (`DigioClient` for Digio) and declare how they are billed | Metering and the bill-overdue pause live there. An architecture test enforces it |
| **Never invent a verification result** | A fabricated PAN, bureau or mandate status is the worst failure this system can have |
| **No PII in logs** | Log ids and statuses, never PAN, Aadhaar, account numbers or names |
| **Tenant data is filtered by the database context** | Do not bypass query filters except in background jobs and webhooks, and scope those by company explicitly |

## Tests

- Put a new test file in the `tests/LosLms.Tests/` folder for its feature area (`Digio/`, `Origination/`,
  `Tenancy/`, …), the same area as the code under `src/LosLms/Services/`. Keep the `LosLms.Tests` namespace.
- Add or update tests with every behaviour change. Prefer testing real behaviour over mocks; the Digio fake returns
  captured sandbox shapes from `docs/digio/samples/`.
- Some tests scan the source tree (billing declarations, who may mark an instalment paid). They expect the `src/LosLms`
  layout, so do not move folders casually.

## Releasing (maintainers)

Desktop releases and cloud deploys are different. See [`docs/operations/`](docs/operations/):

- **Cloud:** merge to `main`; Render builds the Docker image. Watch the deploy; `/health` reports the running version.
- **Desktop:** bump `<Version>` in **both** `src/LosLms.Server/LosLms.Server.csproj` and `src/LosLms/LosLms.csproj`, add the
  entry to [`CHANGELOG.md`](CHANGELOG.md), run `publish.ps1`, and attach the signed update zip and its `.sig` to a GitHub
  Release. Details: [`DESKTOP-RELEASE-AND-UPDATES.md`](docs/operations/DESKTOP-RELEASE-AND-UPDATES.md).

## Documentation

Update the docs in the same pull request as the change. User-visible changes get a line in `CHANGELOG.md`. If you move or
rename a file, search the repository for its old path (`docs/`, `publish.ps1`, `Dockerfile`, CI) before you push.

## Reporting a security problem

Do not open a public issue. Contact the maintainers privately.
