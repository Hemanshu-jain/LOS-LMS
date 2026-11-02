// Password reveal for the statically rendered /account pages (Login, Setup, Change password). Wired
// here with addEventListener rather than an inline onclick so the Content-Security-Policy can stay
// strict (script-src 'self', no 'unsafe-inline'). These pages have no Blazor circuit, so this plain
// script is the whole mechanism. Each reveal button carries data-reveal-target="<input id>".
function losWireReveals() {
    var buttons = document.querySelectorAll('.auth-reveal[data-reveal-target]');
    for (var i = 0; i < buttons.length; i++) {
        var btn = buttons[i];
        if (btn.dataset.revealWired) { continue; }
        btn.dataset.revealWired = '1';
        btn.addEventListener('click', function () {
            var input = document.getElementById(this.getAttribute('data-reveal-target'));
            if (!input) { return; }
            var reveal = input.type === 'password';
            input.type = reveal ? 'text' : 'password';
            this.classList.toggle('is-revealed', reveal);
            this.setAttribute('aria-label', reveal ? 'Hide password' : 'Show password');
            this.setAttribute('aria-pressed', reveal ? 'true' : 'false');
        });
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', losWireReveals);
} else {
    losWireReveals();
}
