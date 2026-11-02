## What and why
<!-- What does this change, and why? Link the issue or decision if there is one. -->

## How it was tested
<!-- Tests added or run; anything you checked by hand. `dotnet test tests/LosLms.Tests` result. -->

## Checklist
- [ ] Tests added or updated, and `dotnet test tests/LosLms.Tests` passes
- [ ] No secrets, keys, tokens, real customer data or PII added (code, config, docs, tests, fixtures)
- [ ] Database changes are **additive** migrations; every new string has `[MaxLength]`
- [ ] Docs updated (and `CHANGELOG.md` if staff or operators will notice the change)
- [ ] If files moved or were renamed, I searched for the old path (docs, `publish.ps1`, `Dockerfile`, CI, tests)

## Production impact
<!-- Merging to `main` triggers a Render deploy for anything but docs-only changes.
     Say: does this need a migration, new configuration, a new secret, or a quiet moment to deploy? -->
