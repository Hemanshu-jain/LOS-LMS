// Resizable table columns. Any <table data-resizable="name"> whose header cells carry data-col="key" and a .col-resizer handle gets
// drag-to-resize borders; the widths are kept in localStorage (per browser, per table name) until changed or reset.
// Phones and narrow windows (<= 720px) keep the natural responsive layout: saved widths are ignored there and the handles are hidden.
(function () {
    'use strict';

    var MIN = 56;
    var suppressClick = false;

    function storageKey(name) { return 'lms.cols.' + name; }
    function narrow() { return window.matchMedia && window.matchMedia('(max-width: 720px)').matches; }

    function load(name) {
        try { return JSON.parse(localStorage.getItem(storageKey(name)) || '{}') || {}; } catch (e) { return {}; }
    }

    function save(name, widths) {
        try { localStorage.setItem(storageKey(name), JSON.stringify(widths)); } catch (e) { /* private window: widths just won't persist */ }
    }

    function headers(table) { return Array.prototype.slice.call(table.querySelectorAll('thead th[data-col]')); }

    function lay(table, widths) {
        var total = 0;
        var all = Array.prototype.slice.call(table.querySelectorAll('thead th'));
        all.forEach(function (th) {
            var key = th.getAttribute('data-col');
            var w = key ? widths[key] : null;
            if (w) { th.style.width = w + 'px'; th.style.minWidth = w + 'px'; }
            total += th.offsetWidth;
        });
        table.style.tableLayout = 'fixed';
        table.style.minWidth = '100%';
        table.style.width = Math.max(total, 0) + 'px';
    }

    function clear(table) {
        table.style.tableLayout = '';
        table.style.minWidth = '';
        table.style.width = '';
        Array.prototype.slice.call(table.querySelectorAll('thead th')).forEach(function (th) {
            th.style.width = '';
            th.style.minWidth = '';
        });
    }

    function current(table) {
        var widths = {};
        headers(table).forEach(function (th) { widths[th.getAttribute('data-col')] = Math.round(th.getBoundingClientRect().width); });
        return widths;
    }

    function apply(table) {
        var name = table.getAttribute('data-resizable');
        if (!name) { return; }
        if (narrow()) { if (table.style.tableLayout) { clear(table); } return; }

        var saved = load(name);
        var cols = headers(table);
        if (cols.length === 0 || Object.keys(saved).length === 0) { return; }

        // Re-render check: only lay out again when a header lost its width (columns shown/hidden, table redrawn).
        var stale = cols.some(function (th) { var w = saved[th.getAttribute('data-col')]; return w && th.style.width !== w + 'px'; })
            || !table.style.tableLayout;
        if (!stale) { return; }

        // Columns without a saved width (newly shown) take a sensible default instead of squeezing.
        var widths = {};
        cols.forEach(function (th) {
            var key = th.getAttribute('data-col');
            widths[key] = saved[key] || Math.max(MIN, Math.round(th.getBoundingClientRect().width));
        });
        lay(table, widths);
    }

    var scheduled = false;
    function scheduleApply() {
        if (scheduled) { return; }
        scheduled = true;
        window.requestAnimationFrame(function () {
            scheduled = false;
            Array.prototype.slice.call(document.querySelectorAll('table[data-resizable]')).forEach(apply);
        });
    }

    document.addEventListener('pointerdown', function (e) {
        var handle = e.target && e.target.closest ? e.target.closest('.col-resizer') : null;
        if (!handle || narrow()) { return; }
        var th = handle.closest('th');
        var table = th && th.closest('table');
        if (!table) { return; }

        e.preventDefault();
        e.stopPropagation();

        // Freeze every column at its present width first, so nothing jumps when the layout becomes fixed.
        var widths = current(table);
        lay(table, widths);

        var key = th.getAttribute('data-col');
        var startX = e.clientX;
        var startWidth = widths[key];
        var moved = false;
        document.body.classList.add('col-resizing');

        function move(ev) {
            moved = true;
            widths[key] = Math.max(MIN, Math.round(startWidth + (ev.clientX - startX)));
            lay(table, widths);
        }

        function up() {
            document.removeEventListener('pointermove', move);
            document.removeEventListener('pointerup', up);
            document.removeEventListener('pointercancel', up);
            document.body.classList.remove('col-resizing');
            save(table.getAttribute('data-resizable'), widths);
            if (moved) { suppressClick = true; window.setTimeout(function () { suppressClick = false; }, 50); }
        }

        document.addEventListener('pointermove', move);
        document.addEventListener('pointerup', up);
        document.addEventListener('pointercancel', up);
    }, true);

    // The click that ends a drag must not sort the column.
    window.addEventListener('click', function (e) {
        if (suppressClick || (e.target && e.target.closest && e.target.closest('.col-resizer'))) {
            e.stopPropagation();
            e.preventDefault();
        }
    }, true);

    window.addEventListener('resize', scheduleApply);
    new MutationObserver(scheduleApply).observe(document.documentElement, { childList: true, subtree: true });
    document.addEventListener('DOMContentLoaded', scheduleApply);

    window.lmsTableResize = {
        reset: function (name) {
            try { localStorage.removeItem(storageKey(name)); } catch (e) { /* ignore */ }
            Array.prototype.slice.call(document.querySelectorAll('table[data-resizable="' + name + '"]')).forEach(clear);
        },
    };
})();
