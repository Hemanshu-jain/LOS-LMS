// Password show/hide toggle for the statically rendered /account pages (Login, Setup). Kept in a served
// file rather than an inline <script> so the Content-Security-Policy can stay strict (script-src 'self',
// no 'unsafe-inline'). These pages have no Blazor circuit, so this plain global is the whole mechanism.
function losTogglePassword(inputId, btn) {
    var input = document.getElementById(inputId);
    if (!input) { return; }
    var reveal = input.type === 'password';
    input.type = reveal ? 'text' : 'password';
    btn.textContent = reveal ? 'Hide' : 'Show';
    btn.setAttribute('aria-label', reveal ? 'Hide password' : 'Show password');
    btn.setAttribute('aria-pressed', reveal ? 'true' : 'false');
}
