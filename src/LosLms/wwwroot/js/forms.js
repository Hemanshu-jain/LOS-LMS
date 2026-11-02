// Google-Forms-style "jump to the field that needs attention". Called from a stage screen when the
// user tries to advance with a required field still blank: scroll it into view, focus it, and flash a
// brief highlight so the eye lands on it. A served file (not inline) so the strict CSP stays intact.
window.losFocusField = function (id) {
    var el = document.getElementById(id);
    if (!el) { return; }

    el.scrollIntoView({ behavior: 'smooth', block: 'center' });

    // Focus after the smooth scroll starts; preventScroll so focus doesn't fight the scrollIntoView.
    try { el.focus({ preventScroll: true }); } catch (e) { /* some inputs refuse focus — the flash still lands */ }

    el.classList.remove('field-flash');
    // Force reflow so re-adding the class restarts the animation even on a repeat click.
    void el.offsetWidth;
    el.classList.add('field-flash');
    window.setTimeout(function () { el.classList.remove('field-flash'); }, 1600);
};
