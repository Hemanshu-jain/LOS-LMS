// Starts Blazor with patient reconnection, makes the "Reload" buttons work, and reloads by itself once the server answers again.
// No inline script anywhere (the Content-Security-Policy is 'self' only), so this lives in a file.
(function () {
    'use strict';

    // A short network blip should reconnect silently: keep trying for several minutes (1 s, 2 s ... up to 10 s apart) before giving up.
    if (window.Blazor && typeof window.Blazor.start === 'function') {
        window.Blazor.start({
            circuit: {
                reconnectionOptions: {
                    maxRetries: 60,
                    retryIntervalMilliseconds: function (previousAttempts) { return Math.min(1000 + previousAttempts * 1000, 10000); }
                }
            }
        });
    }

    // The "Reload LOS/LMS" button and the error bar's "Reload" link.
    document.addEventListener('click', function (event) {
        var target = event.target;
        if (target && target.closest && target.closest('[data-reload]')) {
            event.preventDefault();
            window.location.reload();
        }
    });

    // When the connection is lost for good (failed, or the server restarted and forgot this circuit), wait until the server answers and reload once.
    var modal = document.getElementById('components-reconnect-modal');
    var polling = false;

    function giveUp() {
        return modal && (modal.classList.contains('components-reconnect-failed') || modal.classList.contains('components-reconnect-rejected'));
    }

    function poll() {
        if (polling) { return; }
        polling = true;
        var timer = window.setInterval(function () {
            fetch('/health', { cache: 'no-store', credentials: 'same-origin' })
                .then(function (response) {
                    if (response.ok) {
                        window.clearInterval(timer);
                        window.location.reload();
                    }
                })
                .catch(function () { /* still down: keep waiting */ });
        }, 3000);
    }

    if (modal && 'MutationObserver' in window) {
        new MutationObserver(function () { if (giveUp()) { poll(); } }).observe(modal, { attributes: true, attributeFilter: ['class'] });
    }
})();
