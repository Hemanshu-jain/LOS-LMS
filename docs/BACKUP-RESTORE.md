# Nightly database backup and restore

The production database (Aiven MySQL 8.4) is backed up every night by the GitHub Actions workflow
[`.github/workflows/db-backup.yml`](../.github/workflows/db-backup.yml) into a **private** Supabase Storage bucket
named `db-backups`. This repository is public, so the workflow holds no values: everything comes from repository
secrets, and nothing is printed to the logs or uploaded as a GitHub artifact.

## What happens each night (03:00 IST = 21:30 UTC)

1. `mysqldump` over TLS (the server certificate must chain to our CA), consistent snapshot, with routines and triggers.
2. Compressed with gzip, then encrypted with GPG (symmetric, AES256) using the backup passphrase.
3. Checked: the dump must be at least 1 KB, contain tables, end with mysqldump's completion line, and the encrypted
   file must decrypt back to the identical bytes. Any failure fails the run (GitHub emails the repo owner).
4. Uploaded as `db-backups/los_lms-<UTC timestamp>.sql.gz.gpg`.
5. Backups older than 30 days are deleted, but never the newest 7.

## Secrets to create

GitHub: repository **Settings → Secrets and variables → Actions → New repository secret**. The names are exactly:

| Secret | What it is |
|---|---|
| `LMS_DB_HOST`, `LMS_DB_PORT` | Aiven MySQL host and port |
| `LMS_DB_USER`, `LMS_DB_PASSWORD` | A database user that can read everything (SELECT, SHOW VIEW, TRIGGER, EVENT, ROUTINE) |
| `LMS_DB_NAME` | The database to dump |
| `LMS_DB_CA_CERT` | The Aiven CA certificate, PEM text (including the BEGIN/END lines) |
| `LMS_BACKUP_PASSPHRASE` | The encryption passphrase (see below) |
| `SUPABASE_URL` | `https://<project>.supabase.co` for project `los-lms-files` |
| `SUPABASE_SERVICE_KEY` | The Supabase **service role** key (server-side only; never put it in app code) |

The Supabase bucket `db-backups` must exist and be **private**.

## If a run fails

GitHub emails the repo owner when a scheduled run fails; open **Actions → Nightly DB backup** and read the red step
(the messages say what is wrong: empty dump, TLS/CA, upload HTTP code). GitHub also pauses scheduled workflows in a
repository with no activity for 60 days, so check the Actions tab now and then that the nightly run is still green.

## Run a backup now

GitHub → **Actions → Nightly DB backup → Run workflow**. A green run shows `Uploaded db-backups/los_lms-….sql.gz.gpg`
in the log; the file name appears in the Supabase bucket.

## The passphrase

**The passphrase lives in the password manager and on paper (in the safe).** Without it a backup cannot be opened,
and there is no way to recover it. Do not change `LMS_BACKUP_PASSPHRASE` casually: older backups stay encrypted with
the passphrase they were made with, so keep every old passphrase until the backups made with it are gone.

## Restore

On a machine with `gpg` (Gpg4win) and `mysql` on the PATH, from the repo folder:

```powershell
.\tools\restore-backup.ps1 -List                       # what is available
.\tools\restore-backup.ps1                             # latest -> database los_lms_restore_scratch (local MySQL)
.\tools\restore-backup.ps1 -Name los_lms-20261011T213000Z.sql.gz.gpg -Target los_lms_check
```

It asks for the Supabase address and service key (or reads `SUPABASE_URL` / `SUPABASE_SERVICE_KEY` from the
environment) and for the backup passphrase. By default it restores into a **scratch** database on the local MySQL.
It refuses a target that looks like production (`los_lms`, `los_lms_web`, `defaultdb`, anything with `prod` in the
name), a non-local host, or a database that already has tables, unless you pass `-Force`.

**To recover production**, restore into a scratch database first, check it (table counts, a recent application),
and only then decide how to switch the app over; do not restore straight over the live database.

## Test a restore regularly

A backup you have never restored is a hope, not a backup. Once a month run the default restore above and look at
the table count and a few recent rows.
