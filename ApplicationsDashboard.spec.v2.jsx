/*
  APPLICATIONS DASHBOARD — REFERENCE SPEC v2
  =====================================================================
  CORRECTED FROM v1: the actual build target is Blazor Server on .NET 8
  + MySQL — not WinForms. That changes how this file should be read.

  Because Blazor Server renders real HTML/CSS in the browser (same as
  this React file), almost everything below ports nearly 1:1 into a
  .razor component: the markup structure, the inline styles, the colors,
  the spacing. You will NOT find a per-element "[Blazor: ...]" comment
  on every div the way v1 had "[WinForms: ...]" on every control — that
  translation gap doesn't exist here, so repeating it everywhere would
  just be noise. Instead, comments are placed only where something
  genuinely needs translating:
    - React state (useState) -> Razor @code fields
    - Event handlers (onClick, onChange) -> @onclick / @bind
    - .map() rendering -> @foreach
    - Conditional rendering -> @if
    - Anything that touches a browser-only API (CSV download, keyboard
      shortcuts) -> needs a small JS interop call, since C# can't reach
      those directly. These are flagged explicitly below.
    - Anywhere Blazor Server's architecture (every interaction is a
      round trip over SignalR to the server, not local client state)
      changes a design decision — e.g. debouncing the search input.

  Traceability: comments tagged [IMPROVEMENT #n] map back to the
  numbered improvements list. #1-11 were in v1. #12-17 are new this
  version. Cut any that aren't a confirmed real workflow need.

  Dummy data note: ALL_APPLICATIONS and TOTAL_APPLICATIONS (128) are
  placeholder values for visual reference only — wire to the real
  Applications table / MySQL query in the actual build.
*/

import React, { useState, useMemo, useRef, useEffect } from 'react';

// ---------------------------------------------------------------------------
// DESIGN TOKENS — raw hex/px, ported 1:1 from the approved mockup.
// These are plain CSS values, so they carry over unchanged into Razor
// inline styles or a shared .css file — no translation needed here.
// ---------------------------------------------------------------------------
const COLORS = {
  page: '#f4f5f7',
  card: '#ffffff',
  border: '#d8dbe1',
  borderStrong: '#b9bfc9',
  textPrimary: '#1a1f29',
  textSecondary: '#5b6472',
  textMuted: '#93999a',
  accent: '#1f3a5f',
  accentHover: '#16304f',
  rowHover: '#fafbfc',
  status: {
    New: { bg: '#e8e9ec', text: '#3b4453' },
    'In progress': { bg: '#fbe8c9', text: '#7a4b06' },
    Sanctioned: { bg: '#d9ecd7', text: '#1f5c2a' },
    Rejected: { bg: '#f8d7d7', text: '#8a1f1f' },
  },
  warning: { bg: '#fbe8c9', text: '#7a4b06' },
};

const FONT = "'IBM Plex Sans', Arial, sans-serif";
// Make sure the Google Fonts <link> (or a self-hosted woff2) is added to the
// Blazor app's shared layout / _Host.cshtml <head> — same requirement as any
// web page, nothing Blazor-specific about it.

const STATUS_PRIORITY = { New: 0, 'In progress': 1, Sanctioned: 2, Rejected: 3 };
const REFERENCE_TODAY = new Date('2026-07-13'); // spec-only anchor; use server date in production
const TOTAL_APPLICATIONS = 128; // dummy — replace with a real COUNT query

const ALL_APPLICATIONS = [
  { id: 'LN-2026-004871', product: 'Commercial vehicle', branch: 'Nashik West', customer: 'Ramesh Pawar', amount: 1850000, status: 'In progress', stage: 'Stage 3 · Bank & financial', queueDate: '2026-07-12' },
  { id: 'LN-2026-004868', product: 'Loan against property', branch: 'Pune Camp', customer: 'Sunita Deshmukh', amount: 4200000, status: 'In progress', stage: 'Stage 6 · Eligibility', queueDate: '2026-07-11' },
  { id: 'LN-2026-004859', product: 'Commercial vehicle', branch: 'Aurangabad', customer: 'Iqbal Transport LLP', amount: 2675000, status: 'In progress', stage: 'Stage 4 · Documents', queueDate: '2026-07-10' },
  { id: 'LN-2026-004844', product: 'Loan against property', branch: 'Nashik West', customer: 'Anil Jadhav', amount: 1500000, status: 'In progress', stage: 'Stage 7 · Approvals', queueDate: '2026-07-08' },
  { id: 'LN-2026-004831', product: 'Commercial vehicle', branch: 'Jalgaon', customer: 'Shree Roadlines', amount: 3120000, status: 'Sanctioned', stage: 'Stage 8 · Post sanction', queueDate: '2026-07-05' },
  { id: 'LN-2026-004820', product: 'Loan against property', branch: 'Pune Camp', customer: 'Meena Kulkarni', amount: 2240000, status: 'In progress', stage: 'Stage 2 · Loan & security', queueDate: '2026-07-04' },
  { id: 'LN-2026-004811', product: 'Commercial vehicle', branch: 'Nashik East', customer: 'Balaji Carriers', amount: 1990000, status: 'New', stage: 'Stage 1 · Customer details', queueDate: '2026-07-02' },
  { id: 'LN-2026-004802', product: 'Loan against property', branch: 'Aurangabad', customer: 'Farhan Shaikh', amount: 3750000, status: 'In progress', stage: 'Stage 5 · Reports (RCU)', queueDate: '2026-07-01' },
  { id: 'LN-2026-004795', product: 'Commercial vehicle', branch: 'Pune Camp', customer: 'Vikram Rao', amount: 1420000, status: 'Rejected', stage: 'Stage 5 · Reports (RCU)', queueDate: '2026-06-28' },
];

const COLUMN_DEFS = [
  { key: 'product', label: 'Product' },
  { key: 'branch', label: 'Branch' },
  { key: 'customer', label: 'Customer name' },
  { key: 'amount', label: 'Loan amount' },
  { key: 'status', label: 'Status · stage' },
  { key: 'queueDate', label: 'Queue date' },
];

// ---------------------------------------------------------------------------
// HELPERS
// ---------------------------------------------------------------------------
function formatINR(n) { return '₹' + n.toLocaleString('en-IN'); }
function daysAgo(dateStr) { return Math.floor((REFERENCE_TODAY - new Date(dateStr)) / 86400000); }

function compareValues(a, b, key) {
  if (key === 'amount') return a.amount - b.amount;
  if (key === 'queueDate') return new Date(a.queueDate) - new Date(b.queueDate);
  if (key === 'status') return STATUS_PRIORITY[a.status] - STATUS_PRIORITY[b.status];
  const av = String(a[key]).toLowerCase();
  const bv = String(b[key]).toLowerCase();
  return av < bv ? -1 : av > bv ? 1 : 0;
}

function getPageNumbers(current, total) {
  const pages = new Set([1, total, current - 1, current, current + 1]);
  return [...pages].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);
}

// [Blazor / browser-API note] Building and downloading a CSV file is a
// browser API (Blob + URL.createObjectURL), which C# cannot call directly.
// Two ways to port this:
//   (a) Keep the CSV string built in C#, then call a tiny JS interop
//       function via IJSRuntime.InvokeVoidAsync("downloadFile", filename, csv)
//       that does exactly what this function does.
//   (b) Generate the CSV server-side and expose it as a minimal API endpoint
//       returning a FileStreamResult, then navigate the browser to that URL.
// (a) is simpler if the export logic is staying this close to the UI.
function exportCSV(rows) {
  const header = ['App ID', 'Product', 'Branch', 'Customer name', 'Loan amount', 'Status', 'Stage', 'Queue date'];
  const lines = [header.join(',')];
  rows.forEach((r) => {
    lines.push([r.id, r.product, r.branch, r.customer, r.amount, r.status, r.stage, r.queueDate]
      .map((v) => '"' + String(v).replace(/"/g, '""') + '"').join(','));
  });
  const blob = new Blob([lines.join('\n')], { type: 'text/csv' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url; a.download = 'applications.csv';
  document.body.appendChild(a); a.click(); document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

// ---------------------------------------------------------------------------
// ICONS — minimal inline SVGs, zero external dependencies.
// ---------------------------------------------------------------------------
const IconBase = ({ children, size = 14 }) => (
  <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">{children}</svg>
);
const IconSearch = (p) => <IconBase {...p}><circle cx="11" cy="11" r="7" /><line x1="21" y1="21" x2="16.65" y2="16.65" /></IconBase>;
const IconChevronDown = (p) => <IconBase {...p}><polyline points="6 9 12 15 18 9" /></IconBase>;
const IconChevronUp = (p) => <IconBase {...p}><polyline points="18 15 12 9 6 15" /></IconBase>;
const IconPlus = (p) => <IconBase {...p}><line x1="12" y1="5" x2="12" y2="19" /><line x1="5" y1="12" x2="19" y2="12" /></IconBase>;
const IconChevronLeft = (p) => <IconBase {...p}><polyline points="15 18 9 12 15 6" /></IconBase>;
const IconChevronRight = (p) => <IconBase {...p}><polyline points="9 18 15 12 9 6" /></IconBase>;
const IconUser = (p) => <IconBase {...p}><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" /><circle cx="12" cy="7" r="4" /></IconBase>;
const IconAlert = (p) => <IconBase {...p}><path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" /><path d="M12 9v4" /><path d="M12 17h.01" /></IconBase>;
const IconColumns = (p) => <IconBase {...p}><rect x="3" y="4" width="18" height="16" rx="0" /><line x1="9" y1="4" x2="9" y2="20" /><line x1="15" y1="4" x2="15" y2="20" /></IconBase>;
const IconDownload = (p) => <IconBase {...p}><path d="M12 3v12" /><polyline points="7 11 12 16 17 11" /><path d="M5 20h14" /></IconBase>;
const IconHistory = (p) => <IconBase {...p}><path d="M3 12a9 9 0 1 0 3-6.7" /><path d="M3 4v5h5" /><path d="M12 7v5l3 3" /></IconBase>;
const IconX = (p) => <IconBase {...p}><line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" /></IconBase>;

// ---------------------------------------------------------------------------
// MAIN COMPONENT
// [Blazor] This whole function becomes the @code block + markup of
// Pages/Applications/ApplicationsDashboard.razor (@page "/applications").
// Every useState below becomes a private field in that @code block.
// Blazor Server re-renders the component automatically after a UI event
// handler finishes — there's no separate "setState" call to translate,
// just assign the field and the framework re-renders.
// ---------------------------------------------------------------------------
export default function ApplicationsDashboardSpec() {
  const [showFilterPanel, setShowFilterPanel] = useState(false);
  const [showColumnPanel, setShowColumnPanel] = useState(false);
  const [activeTab, setActiveTab] = useState('Assigned to me');
  const [statusFilter, setStatusFilter] = useState(null); // [IMPROVEMENT #5]
  const [sortKey, setSortKey] = useState('queueDate'); // [IMPROVEMENT #3]
  const [sortDir, setSortDir] = useState('desc');
  const [selected, setSelected] = useState(new Set()); // [IMPROVEMENT #7]
  const [visibleColumns, setVisibleColumns] = useState(new Set(COLUMN_DEFS.map((c) => c.key))); // [IMPROVEMENT #13]
  const [density, setDensity] = useState('compact'); // [IMPROVEMENT #15]
  const [recentIds, setRecentIds] = useState([]); // [IMPROVEMENT #18]
  const [drawerId, setDrawerId] = useState(null); // [IMPROVEMENT #12]
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25); // [IMPROVEMENT #4]

  const searchRef = useRef(null);

  // [IMPROVEMENT #14] Keyboard shortcuts.
  // [Blazor / browser-API note] A global document keydown listener is a
  // browser API, same problem as the CSV export above. Port this via a tiny
  // wwwroot/keyshortcuts.js file that calls back into C# with
  // DotNetObjectReference, or just handle @onkeydown on the root element for
  // the parts that don't need to be truly global (Escape-to-close, for
  // example, can usually be scoped to the drawer element itself instead of
  // the whole document, which avoids interop entirely).
  useEffect(() => {
    function onKeyDown(e) {
      if (e.key === '/' && document.activeElement.tagName !== 'INPUT') {
        e.preventDefault();
        searchRef.current && searchRef.current.focus();
      }
      if (e.key === 'Escape' && drawerId) setDrawerId(null);
    }
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [drawerId]);

  // Counts for the triage strip — computed from this 9-row sample only.
  // Production needs a backend aggregate query across ALL applications,
  // not just the current page, or these numbers will be wrong.
  const statusCounts = useMemo(() => {
    const counts = { New: 0, 'In progress': 0, Sanctioned: 0, Rejected: 0 };
    ALL_APPLICATIONS.forEach((a) => { counts[a.status] += 1; });
    return counts;
  }, []);

  const visibleRows = useMemo(() => {
    let rows = [...ALL_APPLICATIONS];
    if (statusFilter) rows = rows.filter((r) => r.status === statusFilter);
    rows.sort((a, b) => (sortDir === 'asc' ? 1 : -1) * -compareValues(a, b, sortKey));
    return rows;
    // [Blazor] Same as v1: this becomes a computed C# property (a getter),
    // not a memoized value — with a dataset this small there's no reason to
    // hand-roll caching. If the real query ever returns thousands of rows,
    // do the filter/sort in the MySQL query itself, not in-memory in Razor.
  }, [statusFilter, sortKey, sortDir]);

  function handleSort(key) {
    if (sortKey === key) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    else { setSortKey(key); setSortDir('asc'); }
  }

  function toggleRow(id) {
    setSelected((prev) => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  function toggleAll() {
    setSelected((prev) => (prev.size === visibleRows.length ? new Set() : new Set(visibleRows.map((r) => r.id))));
  }

  function toggleColumn(key) {
    setVisibleColumns((prev) => {
      const next = new Set(prev);
      next.has(key) ? next.delete(key) : next.add(key);
      return next;
    });
  }

  function openDrawer(id) {
    setDrawerId(id);
    setRecentIds((prev) => [id, ...prev.filter((x) => x !== id)].slice(0, 5)); // [IMPROVEMENT #18], cap at 5
  }

  const totalPages = Math.ceil(TOTAL_APPLICATIONS / pageSize);
  const pageNumbers = getPageNumbers(page, totalPages);
  const drawerApp = ALL_APPLICATIONS.find((a) => a.id === drawerId);

  const SAVED_VIEWS = [ // [IMPROVEMENT #8] — demo only; real persistence needs a table or local storage
    { label: 'My open cases', apply: () => { setStatusFilter(null); setActiveTab('Assigned to me'); } },
    { label: 'Rejected only', apply: () => setStatusFilter('Rejected') },
  ];

  return (
    <div style={{ fontFamily: FONT, background: COLORS.page, color: COLORS.textPrimary, fontSize: 12 }}>
      <div style={{ border: '1px solid ' + COLORS.border, background: COLORS.card, position: 'relative' }}>

        {/* Top bar */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '9px 16px', borderBottom: '1px solid ' + COLORS.border }}>
          <span style={{ fontSize: 11, fontWeight: 600, letterSpacing: '.12em', textTransform: 'uppercase', color: COLORS.textSecondary }}>LOS/LMS · Applications</span>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ fontSize: 12, color: COLORS.textSecondary }}>Credit Officer · Nashik West</span>
            <div role="img" aria-label="Logged-in user avatar" style={{ width: 24, height: 24, border: '1px solid ' + COLORS.border, background: '#e8e9ec', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <IconUser size={14} />
            </div>
          </div>
        </div>

        {/* Toolbar: search, filters, columns, density, export, new application */}
        <div style={{ padding: '14px 16px', display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', gap: 14, flexWrap: 'wrap' }}>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
            <div style={{ position: 'relative', width: 300, maxWidth: '100%' }}>
              <span style={{ position: 'absolute', left: 9, top: '50%', transform: 'translateY(-50%)', color: COLORS.textMuted, display: 'flex' }}><IconSearch /></span>
              {/* [Blazor] value/onChange here becomes @bind="SearchText". IMPORTANT: because
                  Blazor Server sends every keystroke over SignalR to the server, bind this with
                  @bind:event="oninput" @bind:after="..." using a debounce (~300ms), not a raw
                  per-keystroke round trip — otherwise search will feel laggy on a slow connection. */}
              <input ref={searchRef} type="text" placeholder="Application number, PAN or customer name (press / to focus)" aria-label="Search applications"
                style={{ width: '100%', height: 32, border: '1px solid ' + COLORS.border, background: '#fafbfc', borderRadius: 0, padding: '0 10px 0 30px', fontSize: 12, color: COLORS.textPrimary }} />
            </div>
            <button onClick={() => setShowFilterPanel((v) => !v)} aria-expanded={showFilterPanel}
              style={ghostButtonStyle}>
              Filters {showFilterPanel ? <IconChevronUp size={13} /> : <IconChevronDown size={13} />}
            </button>
            <button onClick={() => setShowColumnPanel((v) => !v)} aria-expanded={showColumnPanel} style={ghostButtonStyle}>
              <IconColumns size={14} /> Columns
            </button>
            <div style={{ display: 'flex', border: '1px solid ' + COLORS.border }}>
              {['compact', 'comfortable'].map((d, i) => (
                <button key={d} onClick={() => setDensity(d)} style={{ height: 30, border: 'none', borderLeft: i ? '1px solid ' + COLORS.border : 'none', background: density === d ? COLORS.accent : '#fff', color: density === d ? '#fff' : '#3b4453', padding: '0 9px', fontSize: 11 }}>
                  {d === 'compact' ? 'Compact' : 'Comfortable'}
                </button>
              ))}
            </div>
            <button onClick={() => exportCSV(visibleRows)} style={ghostButtonStyle}>
              <IconDownload size={14} /> Export
            </button>
          </div>
          <button style={{ height: 32, border: 'none', background: COLORS.accent, borderRadius: 0, padding: '0 14px', fontSize: 12, fontWeight: 500, color: '#fff', display: 'flex', alignItems: 'center', gap: 6, whiteSpace: 'nowrap' }}>
            <IconPlus size={14} /> New application
          </button>
        </div>

        {showColumnPanel && (
          <div style={{ padding: '10px 16px', borderTop: '1px solid ' + COLORS.border, borderBottom: '1px solid ' + COLORS.border, background: '#fafbfc', display: 'flex', gap: 14, flexWrap: 'wrap' }}>
            {COLUMN_DEFS.map((c) => (
              <label key={c.key} style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, color: '#3b4453' }}>
                <input type="checkbox" checked={visibleColumns.has(c.key)} onChange={() => toggleColumn(c.key)} /> {c.label}
              </label>
            ))}
          </div>
        )}

        {showFilterPanel && (
          <div style={{ padding: '14px 16px', borderTop: '1px solid ' + COLORS.border, borderBottom: '1px solid ' + COLORS.border, background: '#fafbfc' }}>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 12 }}>
              <FilterField label="Product" options={['All products', 'Commercial vehicle', 'Loan against property']} />
              <FilterField label="Branch" options={['All branches', 'Nashik West', 'Nashik East', 'Pune Camp', 'Aurangabad', 'Jalgaon']} />
              <FilterField label="Stage / status" options={['All stages', 'New', 'In progress', 'Sanctioned', 'Rejected']} />
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <span style={fieldLabelStyle}>Queued between</span>
                <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
                  <input type="date" style={dateInputStyle} aria-label="Queued from" />
                  <span style={{ color: COLORS.textMuted }}>–</span>
                  <input type="date" style={dateInputStyle} aria-label="Queued to" />
                </div>
              </div>
              <FilterField label="Sourcing channel" options={['All channels', 'DSA', 'Branch walk-in', 'Digital']} />
              <FilterField label="Assigned officer" options={['All officers', 'R. Kulkarni', 'S. Deshpande', 'A. Rao']} />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'center', gap: 14, marginTop: 12 }}>
              <button style={{ background: 'none', border: 'none', fontSize: 12, color: COLORS.textSecondary, cursor: 'pointer' }}>Clear all</button>
              <button style={{ height: 28, border: 'none', background: COLORS.accent, borderRadius: 0, padding: '0 12px', fontSize: 12, fontWeight: 500, color: '#fff', cursor: 'pointer' }}>Apply filters</button>
            </div>
          </div>
        )}

        {/* Saved views — [IMPROVEMENT #8] */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '10px 16px', borderBottom: '1px solid ' + COLORS.border, flexWrap: 'wrap' }}>
          <span style={{ fontSize: 11, color: COLORS.textMuted }}>Saved views:</span>
          {SAVED_VIEWS.map((v) => (
            <button key={v.label} onClick={v.apply} style={chipButtonStyle}>{v.label}</button>
          ))}
        </div>

        {/* Recently viewed — [IMPROVEMENT #18] */}
        {recentIds.length > 0 && (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '10px 16px', borderBottom: '1px solid ' + COLORS.border, flexWrap: 'wrap' }}>
            <IconHistory size={13} />
            <span style={{ fontSize: 11, color: COLORS.textMuted }}>Recently viewed:</span>
            {recentIds.map((id) => (
              <button key={id} onClick={() => openDrawer(id)} style={chipButtonStyle}>{id}</button>
            ))}
          </div>
        )}

        {/* Triage strip — [IMPROVEMENT #5, #6] */}
        <div style={{ display: 'flex', gap: 10, padding: '12px 16px', flexWrap: 'wrap' }}>
          {Object.entries(statusCounts).map(([status, count]) => {
            const c = COLORS.status[status];
            const active = statusFilter === status;
            return (
              <button key={status} onClick={() => setStatusFilter(active ? null : status)} aria-pressed={active}
                style={{ display: 'flex', flexDirection: 'column', gap: 2, padding: '8px 12px', minWidth: 88, border: '1px solid ' + (active ? COLORS.accent : COLORS.border), borderRadius: 0, cursor: 'pointer', background: active ? c.bg : '#fff' }}>
                <span style={{ fontSize: 16, fontWeight: 600, color: c.text }}>{count}</span>
                <span style={{ fontSize: 10.5, color: COLORS.textSecondary }}>{status}</span>
              </button>
            );
          })}
          {statusFilter && <button onClick={() => setStatusFilter(null)} style={{ fontSize: 11, color: COLORS.textSecondary, background: 'none', border: 'none', cursor: 'pointer', alignSelf: 'center' }}>Clear filter</button>}
        </div>

        {/* Tabs */}
        <div style={{ display: 'flex', gap: 18, padding: '0 16px', borderBottom: '1px solid ' + COLORS.border }}>
          {['Assigned to me', 'All applications', 'Migrated data'].map((tab) => (
            <span key={tab} onClick={() => setActiveTab(tab)} role="tab" aria-selected={activeTab === tab}
              style={{ padding: '10px 2px', fontSize: 12.5, cursor: 'pointer', fontWeight: activeTab === tab ? 500 : 400, color: activeTab === tab ? COLORS.textPrimary : COLORS.textSecondary, borderBottom: '2px solid ' + (activeTab === tab ? COLORS.accent : 'transparent') }}>
              {tab}
            </span>
          ))}
        </div>
        {/* NOTE: tab switching is visual only — "All applications" / "Migrated data" need
            their own real queries server-side; this file only has one sample dataset. */}

        {/* Bulk action bar — [IMPROVEMENT #7] */}
        {selected.size > 0 && (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 16px', background: '#eef2f6', borderBottom: '1px solid ' + COLORS.border }}>
            <span style={{ fontSize: 12 }}>{selected.size} selected</span>
            <div style={{ display: 'flex', gap: 10 }}>
              <button style={chipButtonStyle}>Reassign</button>
              <button style={chipButtonStyle}>Export</button>
              <button onClick={() => setSelected(new Set())} style={chipButtonStyle}>Clear selection</button>
            </div>
          </div>
        )}

        {/* Table */}
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ background: '#f4f5f7' }}>
                <th style={{ ...thStyle, width: 32 }}>
                  <input type="checkbox" aria-label="Select all rows" checked={selected.size > 0 && selected.size === visibleRows.length} onChange={toggleAll} />
                </th>
                <SortableHeader label="App ID" sortKeyName="id" currentKey={sortKey} sortDir={sortDir} onSort={handleSort} />
                {COLUMN_DEFS.filter((c) => visibleColumns.has(c.key)).map((c) => (
                  <SortableHeader key={c.key} label={c.label} sortKeyName={c.key} align={c.key === 'amount' ? 'right' : 'left'} currentKey={sortKey} sortDir={sortDir} onSort={handleSort} />
                ))}
              </tr>
            </thead>
            <tbody>
              {visibleRows.map((r) => {
                const s = COLORS.status[r.status];
                const age = daysAgo(r.queueDate);
                const isOverdue = age > 5; // [IMPROVEMENT #6] — confirm the real SLA threshold with Arun/client
                const pad = density === 'compact' ? '9px 10px' : '14px 10px';
                return (
                  <tr key={r.id} onClick={() => openDrawer(r.id)} style={{ borderBottom: '1px solid #eceef1', cursor: 'pointer' }}
                    onMouseEnter={(e) => (e.currentTarget.style.background = COLORS.rowHover)} onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}>
                    <td style={{ padding: pad }} onClick={(e) => e.stopPropagation()}>
                      <input type="checkbox" aria-label={'Select application ' + r.id} checked={selected.has(r.id)} onChange={() => toggleRow(r.id)} />
                    </td>
                    <td style={{ padding: pad, color: COLORS.accent, fontWeight: 500, whiteSpace: 'nowrap' }}>{r.id}</td>
                    {visibleColumns.has('product') && <td style={{ ...truncateCellStyle, padding: pad }} title={r.product}>{r.product}</td>}
                    {visibleColumns.has('branch') && <td style={{ ...truncateCellStyle, padding: pad }} title={r.branch}>{r.branch}</td>}
                    {visibleColumns.has('customer') && <td style={{ ...truncateCellStyle, padding: pad }} title={r.customer}>{r.customer}</td>}
                    {visibleColumns.has('amount') && <td style={{ padding: pad, textAlign: 'right', whiteSpace: 'nowrap' }}>{formatINR(r.amount)}</td>}
                    {visibleColumns.has('status') && (
                      <td style={{ padding: pad }}>
                        <span style={{ display: 'inline-block', fontSize: 11, fontWeight: 500, padding: '2px 7px', background: s.bg, color: s.text }}>{r.status}</span>
                        <div style={{ fontSize: 11, color: COLORS.textMuted, marginTop: 3, whiteSpace: 'nowrap' }}>{r.stage}</div>
                      </td>
                    )}
                    {visibleColumns.has('queueDate') && (
                      <td style={{ padding: pad, color: COLORS.textSecondary, whiteSpace: 'nowrap' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
                          {r.queueDate}
                          {isOverdue && <span title={age + ' days since queued — past 5-day SLA'} style={{ color: COLORS.warning.text, display: 'flex' }}><IconAlert size={12} /></span>}
                        </div>
                      </td>
                    )}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {/* Footer */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '10px 16px', borderTop: '1px solid ' + COLORS.border, flexWrap: 'wrap', gap: 10 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <span style={{ fontSize: 11.5, color: COLORS.textSecondary }}>
              Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, TOTAL_APPLICATIONS)} of {TOTAL_APPLICATIONS} applications
            </span>
            <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 11.5, color: COLORS.textSecondary }}>
              Rows per page
              <select value={pageSize} onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }} style={{ height: 26, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 11.5 }}>
                {[25, 50, 100].map((n) => <option key={n} value={n}>{n}</option>)}
              </select>
            </label>
          </div>
          <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
            <button aria-label="Previous page" disabled={page === 1} onClick={() => setPage((p) => Math.max(1, p - 1))} style={pageBtnStyle}><IconChevronLeft size={13} /></button>
            {pageNumbers.map((n, i) => (
              <React.Fragment key={n}>
                {i > 0 && n - pageNumbers[i - 1] > 1 && <span style={{ fontSize: 11.5, color: COLORS.textMuted, padding: '0 4px' }}>…</span>}
                <button onClick={() => setPage(n)} style={{ ...pageBtnStyle, background: n === page ? COLORS.accent : '#fff', borderColor: n === page ? COLORS.accent : COLORS.border, color: n === page ? '#fff' : '#3b4453' }}>{n}</button>
              </React.Fragment>
            ))}
            <button aria-label="Next page" disabled={page === totalPages} onClick={() => setPage((p) => Math.min(totalPages, p + 1))} style={pageBtnStyle}><IconChevronRight size={13} /></button>
          </div>
        </div>

        {/* Quick-view drawer — [IMPROVEMENT #12] */}
        {drawerApp && (
          <div onClick={() => setDrawerId(null)} style={{ position: 'fixed', inset: 0, background: 'rgba(26,31,41,0.35)', display: 'flex', justifyContent: 'flex-end', zIndex: 50 }}>
            <div onClick={(e) => e.stopPropagation()} style={{ width: 320, maxWidth: '90%', background: '#fff', height: '100%', borderLeft: '1px solid ' + COLORS.border, padding: 18, overflowY: 'auto' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 14 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 500, color: COLORS.accent }}>{drawerApp.id}</div>
                  <span style={{ display: 'inline-block', marginTop: 6, fontSize: 11, fontWeight: 500, padding: '2px 7px', background: COLORS.status[drawerApp.status].bg, color: COLORS.status[drawerApp.status].text }}>{drawerApp.status}</span>
                </div>
                <button onClick={() => setDrawerId(null)} aria-label="Close" style={{ border: 'none', background: 'none', color: COLORS.textSecondary }}><IconX size={16} /></button>
              </div>
              <div style={{ fontSize: 11, color: COLORS.textMuted, marginBottom: 16 }}>{drawerApp.stage}</div>
              <DrawerRow label="Product" value={drawerApp.product} />
              <DrawerRow label="Branch" value={drawerApp.branch} />
              <DrawerRow label="Customer" value={drawerApp.customer} />
              <DrawerRow label="Loan amount" value={formatINR(drawerApp.amount)} />
              <DrawerRow label="Queue date" value={drawerApp.queueDate} />
              {/* "Open full application" navigates into the real 8-stage screen for this app —
                  wire to the actual route once that screen exists. */}
              <button style={{ marginTop: 18, width: '100%', height: 34, border: 'none', background: COLORS.accent, color: '#fff', fontSize: 12, fontWeight: 500, borderRadius: 0 }}>Open full application →</button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// SUB-COMPONENTS
// ---------------------------------------------------------------------------
function FilterField({ label, options }) {
  return (
    <label style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      <span style={fieldLabelStyle}>{label}</span>
      <select style={{ height: 30, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 12, background: '#fff', padding: '0 8px' }}>
        {options.map((o) => <option key={o}>{o}</option>)}
      </select>
    </label>
  );
}

function SortableHeader({ label, sortKeyName, align = 'left', currentKey, sortDir, onSort }) {
  const active = currentKey === sortKeyName;
  return (
    <th onClick={() => onSort(sortKeyName)} aria-sort={active ? (sortDir === 'asc' ? 'ascending' : 'descending') : 'none'} style={{ ...thStyle, textAlign: align, cursor: 'pointer', userSelect: 'none' }}>
      <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
        {label}
        <span style={{ fontSize: 9, color: active ? COLORS.textPrimary : COLORS.textMuted }}>{active ? (sortDir === 'asc' ? '▲' : '▼') : '⇅'}</span>
      </span>
    </th>
  );
}

function DrawerRow({ label, value }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid #eceef1', fontSize: 12 }}>
      <span style={{ color: COLORS.textSecondary }}>{label}</span><span>{value}</span>
    </div>
  );
}

// ---------------------------------------------------------------------------
// SHARED INLINE STYLE OBJECTS
// ---------------------------------------------------------------------------
const fieldLabelStyle = { fontSize: 11, fontWeight: 600, letterSpacing: '.08em', textTransform: 'uppercase', color: COLORS.textSecondary };
const dateInputStyle = { flex: 1, height: 30, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 12, padding: '0 6px' };
const thStyle = { textAlign: 'left', padding: '8px 10px', fontSize: 11, fontWeight: 600, letterSpacing: '.08em', textTransform: 'uppercase', color: COLORS.textSecondary, borderBottom: '1px solid ' + COLORS.border, whiteSpace: 'nowrap' };
const truncateCellStyle = { maxWidth: 160, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }; // [IMPROVEMENT #2]
const pageBtnStyle = { width: 26, height: 26, border: '1px solid ' + COLORS.border, background: '#fff', borderRadius: 0, fontSize: 11.5, color: '#3b4453', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center' };
const ghostButtonStyle = { height: 32, border: '1px solid ' + COLORS.border, background: '#fff', borderRadius: 0, padding: '0 11px', fontSize: 12, color: '#3b4453', display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' };
const chipButtonStyle = { fontSize: 11.5, padding: '4px 9px', border: '1px solid ' + COLORS.border, borderRadius: 0, background: '#fff', color: COLORS.textSecondary, cursor: 'pointer' };
