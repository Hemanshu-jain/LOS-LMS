// "/" focuses the applications search box.
//
// Handled entirely in the browser on purpose: routing this through a DotNetObjectReference
// callback would send the keystroke to the server over SignalR and wait for the round trip to
// come back before the caret moved. Focusing an element needs no server involvement.
//
// Harmless no-op on pages that have no #app-search.
document.addEventListener('keydown', (e) => {
    if (e.key !== '/') return;

    const target = e.target;
    const tag = target && target.tagName;
    if (tag === 'INPUT' || tag === 'SELECT' || tag === 'TEXTAREA') return;
    if (target && target.isContentEditable) return;

    const search = document.getElementById('app-search');
    if (!search) return;

    e.preventDefault();
    search.focus();
    search.select();
});

// Selects the text in the currently focused field. Called from Blazor (System Updates → copy URL)
// as a named function rather than eval(), which the app's CSP (script-src 'self') forbids.
window.selectActiveElement = function () {
    const el = document.activeElement;
    if (el && typeof el.select === 'function') {
        el.select();
    }
};
