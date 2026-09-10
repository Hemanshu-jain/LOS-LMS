# Onboarding a new client (fully separate island)

Every client is completely isolated: **their own subdomain, their own tunnel, their own build, their
own computer, their own database.** Nothing is shared between clients — a client's data lives only on
that client's host PC. This is the repeatable process to add one.

Pick a short client id, e.g. `client1`, and a subdomain, e.g. `client1.bhodhix.com`.

---

## 1. Cloudflare — create the tunnel + subdomain (once per client)

In the Cloudflare dashboard → **Networking → Tunnels**:

1. **Create Tunnel** → name it after the client (e.g. `CLIENT1`) → choose **cloudflared / Windows**.
2. On the tunnel's **Routes** tab → **Add route → Published application**:
   - **Subdomain:** `client1`  **Domain:** `bhodhix.com`
   - **Service URL:** `http://localhost:5037`
   - **Add route.** (Cloudflare auto-creates the DNS record for `client1.bhodhix.com`.)
3. On the **Overview** tab, copy the install command (the **copy** icon). It contains the tunnel token
   (`cloudflared.exe service install <TOKEN>`).
4. Save that token to a per-client file (gitignored): create `tunnel-tokens\client1.txt` at the repo
   root and paste the token (the whole `service install …` line is fine — the build extracts the token).

> Each client MUST have its own tunnel + token. Never reuse one tunnel across clients — two hosts on one
> tunnel would collide, and Cloudflare would route users to the wrong client's machine.

## 2. Build that client's package (once per release)

```powershell
.\publish.ps1 -Subdomain client1.bhodhix.com -TunnelTokenFile tunnel-tokens\client1.txt -Label client1 -SkipUpdateArtifact
```

Produces `publish\LOS-LMS-client1-v<version>-win-x64.zip` with **that client's URL and token baked in**.
`-SkipUpdateArtifact` skips the public GitHub update artifact (that's built once from the default build,
not per client).

## 3. Deliver

Send that client their zip. They:
1. Extract → `LOS-LMS.exe` + `app\`.
2. On the ONE office PC that will host: run `LOS-LMS.exe` → **"Set up the SERVER"**. It sets up MySQL,
   starts the app, and auto-connects the tunnel (token baked) — `client1.bhodhix.com` goes live. No
   pasting, no config.
3. Staff PCs: run the same exe → **"Connect to the server"** → opens `client1.bhodhix.com`.

## 4. Verify

- Browse `https://client1.bhodhix.com` from anywhere → the app loads over HTTPS, first run shows the
  create-admin wizard.
- Their data (database + uploaded documents + backups) lives only under `app\` on their host PC.

---

### Isolation, in one line
`client1.bhodhix.com` → client1's tunnel → client1's PC → client1's database.
`client2.bhodhix.com` → client2's tunnel → client2's PC → client2's database.
Different URL, different machine, different data — no path between them.

### Optional: offsite backups for a client
Inside that client's `app\`, copy `server-config.example.json` → `server-config.json` and fill the
`BackupFtp*` fields (their own FTP). Nightly database dumps then upload there; otherwise backups stay
local in `app\backups\`.
