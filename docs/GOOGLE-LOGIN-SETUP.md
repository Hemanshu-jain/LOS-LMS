# Google sign-in: setup

"Continue with Google" is **off until you give the server a Google client id and secret**. With nothing set, the Google buttons are hidden and
the `/signin-google` callback does not exist. Nothing about passwords or the Brevo confirmation email changes.

## What it does

| Where | Button | Rule |
|---|---|---|
| Sign in | Continue with Google | Signs in an **existing** user whose email equals the verified Google email. It never creates a user inside an existing company. |
| Create your company | Sign up with Google | Creates a **new company** whose Admin is the Google user (email already verified by Google). The company's details are asked on the next page. |
| Account page (change password) | Link Google account | Links the signed-in user's own Google account; the Google email must equal the user's email. |

- Google's own `email_verified` must be true, otherwise the sign-in or sign-up is refused.
- A user who signs in or links with a verified Google email is marked **email confirmed**: they do not need the Brevo confirmation link.
  Password sign-ups still use it exactly as before.
- Deactivated users and unknown emails get the same message ("No account uses that Google email"), so nobody can probe who has an account.
- Linking is stored in Identity's own `AspNetUserLogins` table (provider `Google`). Every link and every Google sign-up writes an `AuditEntry` row.

## 1. Create the OAuth client (Google Cloud Console)

1. Go to <https://console.cloud.google.com/> and pick (or create) a project, e.g. `los-lms`.
2. **APIs & Services > OAuth consent screen**
   - User type: **External**. Create.
   - App name: `LOS/LMS` (what users see), user support email: yours, developer contact email: yours.
   - Authorized domains: `bhodhix.com`. App home page: `https://los-lms.bhodhix.com`.
   - **Scopes**: add **`openid`**, **`.../auth/userinfo.email`** and **`.../auth/userinfo.profile`** (these are the "email", "profile" and "openid" scopes; no sensitive scopes are needed, so no Google verification review is required).
   - Test users: while the app is in *Testing* only listed Google accounts can sign in. When you are ready for everyone, press **Publish app** (moves it to *In production*; with only these basic scopes it does not need a security assessment).
3. **APIs & Services > Credentials > Create credentials > OAuth client ID**
   - Application type: **Web application**. Name: `LOS/LMS web`.
   - **Authorized JavaScript origins:** `https://los-lms.bhodhix.com`
   - **Authorized redirect URIs:** `https://los-lms.bhodhix.com/signin-google` (exactly this: scheme, host, path, no trailing slash).
   - Create. Copy the **Client ID** and **Client secret**.
4. For local testing add a second redirect URI, e.g. `https://localhost:5001/signin-google` (use your local https port). Do not add it to production if you do not need it.

## 2. Give the secrets to the server (never in code)

On Render, **Environment**:

| Variable | Value |
|---|---|
| `Authentication__Google__ClientId` | the Client ID |
| `Authentication__Google__ClientSecret` | the Client secret |

Redeploy. The Google buttons appear on Sign in and Create your company, and "Link Google account" appears on the account page.
(Double underscore `__` is how ASP.NET reads `Authentication:Google:ClientId` from the environment.)

Self sign-up (`Web__AllowSelfRegistration=true`) must be on for the "Sign up with Google" button to work; Sign in and Link do not need it.

## 3. Checks after deploy

- Open `https://los-lms.bhodhix.com/account/login`: the "Continue with Google" button is there.
- With a Google account whose email is a user in the system: signs in, lands on the dashboard (the vendor SuperAdmin lands on Companies).
- With a Google account that is not a user: "No account uses that Google email."
- Create your company > Sign up with Google > add company details > you are signed in as that company's Admin.

## Troubleshooting

- `redirect_uri_mismatch`: the redirect URI in the console is not exactly `https://los-lms.bhodhix.com/signin-google`. Behind Render's proxy the app already honours `X-Forwarded-Proto`, so it builds `https://`.
- "Google sign-in did not complete": the user cancelled, or the consent screen is still in *Testing* and their account is not a listed test user.
- Rotating the secret: create a new secret in the console, update `Authentication__Google__ClientSecret`, redeploy, then delete the old one.
