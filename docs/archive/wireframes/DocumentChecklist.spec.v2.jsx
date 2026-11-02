/*
  DOCUMENT CHECKLIST — STAGE 4 OF 8 — REFERENCE SPEC v2
  =====================================================================
  Target stack: Blazor Server on .NET 8 + MySQL. Comments flag only what
  needs real translation — state, events, browser-only APIs.

  *** GUARANTOR VISIBILITY — explicitly requested, built as a flag ***
  Per direction: if no Guarantor was added earlier in the application
  (Stage 1 / Stage 2), the Guarantor tab — and its checklist, and its
  contribution to the aggregate totals — should not appear here at all.
  This file implements that as a single `hasGuarantor` boolean (see the
  spec-only toggle at the top) rather than hardcoding 3 tabs. Right now
  it's a local boolean for demo purposes; in the real build it needs to
  come from whatever Stage 1/2 record determines whether a guarantor
  exists on this application (e.g. Application.HasGuarantor or a count
  of Party records where PartyType = Guarantor) — NOT re-derived here.

  *** DATA MODEL — each party now has genuinely separate data ***
  Unlike Stage 1's mockup (which used one shared field set across party
  tabs for simplicity), this screen needs real per-party data for the
  aggregate-across-parties feature to mean anything — three copies of
  the same numbers wouldn't be an aggregate, they'd just be misleading.
  PARTY_DATA below is keyed by party name; production should key by a
  real PartyId, not the party-type string.

  Dummy data note: everything in PARTY_DATA is placeholder for visual
  reference only.
*/

import React, { useState, useRef } from 'react';

const COLORS = {
  border: '#d8dbe1', borderStrong: '#b9bfc9', textPrimary: '#1a1f29', textSecondary: '#5b6472', textMuted: '#93999a',
  accent: '#1f3a5f', rowHover: '#fafbfc',
  healthy: { bg: '#d9ecd7', text: '#1f5c2a' },
  caution: { bg: '#fbe8c9', text: '#7a4b06' },
  risk: { bg: '#f8d7d7', text: '#8a1f1f' },
};
const FONT = "'IBM Plex Sans', Arial, sans-serif";
const STEP_LABELS = ['Customer Details', 'Loan & Security', 'Bank & Financial', 'Document Checklist', 'Reports', 'Eligibility', 'Approvals', 'Post Sanction'];
const CURRENT_STEP = 4;
const REFERENCE_TODAY = new Date('2026-07-13'); // spec-only anchor; use server date in production

const ALL_PARTIES = ['Applicant', 'Co-Applicant', 'Guarantor'];

const OTHER_DOC_TYPES = [
  { key: 'income', label: 'Income proof — ITR / GST' },
  { key: 'stability', label: 'Stability proof' },
  { key: 'utility', label: 'Utility proof' },
  { key: 'ownership', label: 'Ownership proof' },
  { key: 'existingLoan', label: 'Existing loan statement' },
  { key: 'tradeLicence', label: 'Trade licence / permit' },
  { key: 'routeSheets', label: 'Route / trip sheets' },
  { key: 'guarantorDocs', label: 'Guarantor documents' },
];

function daysBetween(dateStr) { return Math.floor((REFERENCE_TODAY - new Date(dateStr)) / 86400000); }

function initialPartyData() {
  return {
    Applicant: {
      kyc: [
        { key: 'pan', name: 'PAN card', file: 'PAN_ramesh.pdf', uploadDate: '2026-07-02', status: 'Collected', targetDate: '', remarksLog: [] },
        { key: 'aadhaar', name: 'Aadhaar (masked)', file: 'Aadhaar_masked_ramesh.pdf', uploadDate: '2026-07-02', status: 'Collected', targetDate: '', remarksLog: [] },
        { key: 'photo', name: 'Photograph', file: 'photo_ramesh.jpg', uploadDate: '2026-07-02', status: 'Collected', targetDate: '', remarksLog: [] },
        { key: 'signature', name: 'Signature proof', file: '', uploadDate: '', status: 'Pending', targetDate: '2026-07-08', remarksLog: [{ date: '2026-07-09', text: 'Called applicant — no answer' }, { date: '2026-07-11', text: 'Followed up via SMS, promised by Friday' }] },
        { key: 'address', name: 'Address proof', file: '', uploadDate: '', status: 'Pending', targetDate: '2026-07-20', remarksLog: [{ date: '2026-07-10', text: 'Awaiting latest utility bill' }] },
      ],
      other: { income: 'Collected', stability: 'Collected', utility: 'Collected', ownership: 'Collected', existingLoan: 'Collected', tradeLicence: 'Collected', routeSheets: 'Pending', guarantorDocs: 'Pending' },
      otherFiles: { income: 'ITR_2025-26.pdf', stability: 'stability_letter.pdf', utility: 'electricity_bill.pdf', ownership: 'RC_ownership.pdf', existingLoan: 'loan_stmt_hdfc.pdf', tradeLicence: 'trade_licence.pdf' },
    },
    'Co-Applicant': {
      kyc: [
        { key: 'pan', name: 'PAN card', file: 'PAN_sunita.pdf', uploadDate: '2026-07-03', status: 'Collected', targetDate: '', remarksLog: [] },
        { key: 'aadhaar', name: 'Aadhaar (masked)', file: 'Aadhaar_masked_sunita.pdf', uploadDate: '2026-07-03', status: 'Collected', targetDate: '', remarksLog: [] },
        { key: 'photo', name: 'Photograph', file: '', uploadDate: '', status: 'Pending', targetDate: '2026-07-05', remarksLog: [{ date: '2026-07-06', text: 'Requested passport-size photo via WhatsApp' }] },
        { key: 'signature', name: 'Signature proof', file: '', uploadDate: '', status: 'Pending', targetDate: '2026-07-25', remarksLog: [] },
        // [IMPROVEMENT #5] validityDays demonstrates staleness — this doc was collected
        // but is old enough (>90 days) to no longer be considered valid.
        { key: 'address', name: 'Address proof', file: 'address_sunita_old.pdf', uploadDate: '2026-03-01', status: 'Collected', targetDate: '', remarksLog: [], validityDays: 90 },
      ],
      other: { income: 'Collected', stability: 'Pending', utility: 'Pending', ownership: 'Pending', existingLoan: 'Pending', tradeLicence: 'Pending', routeSheets: 'Pending', guarantorDocs: 'Pending' },
      otherFiles: { income: 'ITR_sunita.pdf' },
    },
    Guarantor: {
      kyc: [
        { key: 'pan', name: 'PAN card', file: 'PAN_guarantor.pdf', uploadDate: '2026-07-04', status: 'Collected', targetDate: '', remarksLog: [] },
        { key: 'aadhaar', name: 'Aadhaar (masked)', file: '', uploadDate: '', status: 'Pending', targetDate: '2026-07-10', remarksLog: [] },
        { key: 'photo', name: 'Photograph', file: '', uploadDate: '', status: 'Pending', targetDate: '2026-07-15', remarksLog: [] },
        { key: 'signature', name: 'Signature proof', file: '', uploadDate: '', status: 'Pending', targetDate: '2026-07-18', remarksLog: [] },
        { key: 'address', name: 'Address proof', file: '', uploadDate: '', status: 'Pending', targetDate: '2026-07-22', remarksLog: [] },
      ],
      other: { income: 'Pending', stability: 'Pending', utility: 'Pending', ownership: 'Pending', existingLoan: 'Pending', tradeLicence: 'Pending', routeSheets: 'Pending', guarantorDocs: 'Pending' },
      otherFiles: {},
    },
  };
}

// Displayed status accounts for staleness — computed, not stored, so it can
// never drift out of sync with "today".
function displayStatus(row) {
  if (row.status === 'Collected' && row.validityDays && daysBetween(row.uploadDate) > row.validityDays) return 'Stale';
  return row.status;
}
function isChaseable(row) { return displayStatus(row) !== 'Collected'; } // Pending or Stale — both need a target date + remarks
function isOverdue(row) { return isChaseable(row) && row.targetDate && daysBetween(row.targetDate) > 0; }

function partyCounts(data) {
  let collected = 0, pending = 0, overdue = 0;
  data.kyc.forEach((r) => { displayStatus(r) === 'Collected' ? collected++ : pending++; if (isOverdue(r)) overdue++; });
  Object.values(data.other).forEach((s) => (s === 'Collected' ? collected++ : pending++));
  return { collected, pending, overdue };
}
function partyComplete(data) {
  return data.kyc.every((r) => displayStatus(r) === 'Collected') && Object.values(data.other).every((s) => s === 'Collected');
}

export default function DocumentChecklistSpec() {
  const [hasGuarantor, setHasGuarantor] = useState(true); // spec-only for now — see file header note
  const visibleParties = ALL_PARTIES.filter((p) => p !== 'Guarantor' || hasGuarantor);

  const [activeTab, setActiveTab] = useState('Applicant');
  const [partyData, setPartyData] = useState(initialPartyData);
  const [previewFile, setPreviewFile] = useState(null); // [IMPROVEMENT #9]
  const bulkInputRef = useRef(null);

  const effectiveTab = visibleParties.includes(activeTab) ? activeTab : visibleParties[0];
  const current = partyData[effectiveTab];

  function updateParty(updater) {
    setPartyData((prev) => ({ ...prev, [effectiveTab]: updater(prev[effectiveTab]) }));
  }

  function collectKyc(idx) {
    updateParty((d) => {
      const kyc = d.kyc.map((r, i) => (i === idx
        ? { ...r, status: 'Collected', file: r.name.replace(/\s+/g, '_').toLowerCase() + '.pdf', uploadDate: '2026-07-13', targetDate: '', remarksLog: [], validityDays: undefined }
        : r));
      return { ...d, kyc };
    });
  }
  function toggleOther(key) {
    updateParty((d) => {
      const nextStatus = d.other[key] === 'Pending' ? 'Collected' : 'Pending';
      const nextFiles = { ...d.otherFiles };
      if (nextStatus === 'Collected') nextFiles[key] = key + '_doc.pdf'; else delete nextFiles[key];
      return { ...d, other: { ...d.other, [key]: nextStatus }, otherFiles: nextFiles };
    });
  }
  function setKycField(idx, field, value) {
    updateParty((d) => ({ ...d, kyc: d.kyc.map((r, i) => (i === idx ? { ...r, [field]: value } : r)) }));
  }
  function addRemark(idx, text) {
    if (!text.trim()) return;
    updateParty((d) => ({ ...d, kyc: d.kyc.map((r, i) => (i === idx ? { ...r, remarksLog: [...r.remarksLog, { date: '2026-07-13', text }] } : r)) }));
  }

  // [IMPROVEMENT #4] Bulk upload — real File API, sequentially assigns picked
  // files to whatever's currently outstanding for this party (KYC first, then
  // other docs). [Blazor] swap for <InputFile multiple>; Blazor Server streams
  // file bytes over the circuit, so batch-uploading many/large files should
  // show real progress rather than assuming an instant round trip.
  function handleBulkFiles(fileList) {
    const files = Array.from(fileList);
    if (!files.length) return;
    updateParty((d) => {
      let d2 = { ...d, kyc: d.kyc.map((r) => ({ ...r })), other: { ...d.other }, otherFiles: { ...d.otherFiles } };
      let fi = 0;
      d2.kyc.forEach((r) => {
        if (fi >= files.length) return;
        if (displayStatus(r) !== 'Collected') { r.status = 'Collected'; r.file = files[fi].name; r.uploadDate = '2026-07-13'; r.targetDate = ''; r.remarksLog = []; r.validityDays = undefined; fi++; }
      });
      Object.keys(d2.other).forEach((key) => {
        if (fi >= files.length) return;
        if (d2.other[key] === 'Pending') { d2.other[key] = 'Collected'; d2.otherFiles[key] = files[fi].name; fi++; }
      });
      return d2;
    });
  }

  const agg = visibleParties.reduce((acc, p) => {
    const c = partyCounts(partyData[p]);
    return { collected: acc.collected + c.collected, pending: acc.pending + c.pending, overdue: acc.overdue + c.overdue };
  }, { collected: 0, pending: 0, overdue: 0 });

  return (
    <div style={{ fontFamily: FONT, fontSize: 12, color: COLORS.textPrimary }}>

      {/* SPEC-ONLY — forces guarantor presence for review. [Blazor] delete this;
          hasGuarantor comes from real application data, never a toggle. */}
      <div style={{ display: 'flex', gap: 8, padding: '10px 12px', border: '1px dashed ' + COLORS.borderStrong, marginBottom: 16, background: '#fff' }}>
        <span style={{ fontSize: 11, color: COLORS.textMuted, alignSelf: 'center' }}>Spec preview — guarantor on this application:</span>
        {[true, false].map((v) => (
          <button key={String(v)} onClick={() => setHasGuarantor(v)} style={{ fontSize: 11, padding: '4px 10px', border: '1px solid ' + COLORS.border, borderRadius: 0, cursor: 'pointer', background: hasGuarantor === v ? COLORS.accent : '#fff', color: hasGuarantor === v ? '#fff' : COLORS.textSecondary }}>
            {v ? 'yes' : 'no'}
          </button>
        ))}
      </div>

      <div style={{ border: '1px solid ' + COLORS.border, background: '#fff' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '10px 14px', borderBottom: '1px solid ' + COLORS.border, background: '#f4f4f4', flexWrap: 'wrap', gap: 8 }}>
          <span style={{ fontSize: 11, fontWeight: 600, letterSpacing: '.1em', textTransform: 'uppercase', color: COLORS.textSecondary }}>← Applications · APP #LN-2026-004871</span>
          <div style={{ display: 'flex', gap: 8 }}>
            <button style={ghostBtnStyle}>Save draft</button>
            <button style={primaryBtnStyle}>Complete stage →</button>
          </div>
        </div>

        <div style={{ padding: '12px 14px', borderBottom: '1px solid #dcdcdc', background: '#fbfbfb' }}>
          <Stepper current={CURRENT_STEP} />
        </div>

        <div style={{ display: 'flex', alignItems: 'stretch', flexWrap: 'wrap' }}>
          <div style={{ width: 230, flex: 'none', padding: 14, borderRight: '1px solid #dcdcdc', background: '#f7f7f7' }}>
            <SummaryRail />
          </div>

          <div style={{ flex: 1, minWidth: 280, padding: 16, display: 'flex', flexDirection: 'column', gap: 15 }}>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: 12, borderBottom: '2px solid ' + COLORS.textPrimary, paddingBottom: 7, flexWrap: 'wrap' }}>
              <span style={{ fontSize: 13, fontWeight: 700, letterSpacing: '.08em' }}>05</span>
              <span style={{ fontSize: 16, fontWeight: 700 }}>STAGE 4 — DOCUMENT CHECKLIST</span>
              <span style={{ fontSize: 10.5, fontWeight: 500, letterSpacing: '.09em', textTransform: 'uppercase', color: COLORS.textMuted }}>Stage 4 of 8</span>
            </div>

            {/* [IMPROVEMENT #2] aggregate across all visible parties — always shown,
                independent of which tab is active. */}
            <div style={{ fontSize: 11, color: COLORS.textSecondary, padding: '6px 0' }}>
              Across all parties: <strong>{agg.collected} collected</strong> · <strong>{agg.pending} pending</strong>
              {agg.overdue > 0 && <> · <strong style={{ color: COLORS.risk.text }}>{agg.overdue} overdue</strong></>}
            </div>

            {/* [IMPROVEMENT #1] completion badge per tab; Guarantor tab simply
                doesn't render when hasGuarantor is false. */}
            <div role="tablist" style={{ display: 'flex', alignItems: 'flex-end', borderBottom: '1px solid #c2c2c2', width: '100%', flexWrap: 'wrap' }}>
              {visibleParties.map((p) => {
                const active = effectiveTab === p;
                const complete = partyComplete(partyData[p]);
                return (
                  <button key={p} role="tab" aria-selected={active} onClick={() => setActiveTab(p)}
                    style={active
                      ? { padding: '8px 13px', border: '1px solid #9e9e9e', borderBottom: '1px solid #fff', background: '#fff', marginBottom: -1, fontSize: 11.5, fontWeight: 700, color: '#1c1c1c', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 5 }
                      : { padding: '8px 13px', border: '1px solid #dcdcdc', borderBottom: 'none', background: '#f4f4f4', fontSize: 11.5, fontWeight: 500, color: '#8b8b8b', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 5 }}>
                    {p}
                    {complete && <span title="All documents collected" style={{ color: COLORS.healthy.text, fontSize: 11 }}>✓</span>}
                  </button>
                );
              })}
            </div>

            <CounterStrip counts={partyCounts(current)} />

            <KycPanel
              party={effectiveTab}
              rows={current.kyc}
              onCollect={collectKyc}
              onFieldChange={setKycField}
              onAddRemark={addRemark}
              onView={(row) => setPreviewFile(row)}
              onBulkClick={() => bulkInputRef.current && bulkInputRef.current.click()}
              onPrint={() => window.print()}
            />
            <input ref={bulkInputRef} type="file" multiple style={{ display: 'none' }} onChange={(e) => { handleBulkFiles(e.target.files); e.target.value = ''; }} />

            <OtherDocsPanel docs={current.other} files={current.otherFiles} onToggle={toggleOther} />
          </div>
        </div>
      </div>

      {previewFile && <PreviewModal file={previewFile} onClose={() => setPreviewFile(null)} />}
    </div>
  );
}

// ---------------------------------------------------------------------------
// SUB-COMPONENTS
// ---------------------------------------------------------------------------

function CounterStrip({ counts }) {
  function Tile({ label, value, warn }) {
    return (
      <div style={{ border: '1px solid ' + (warn ? COLORS.risk.text : '#b6b6b6'), background: warn ? COLORS.risk.bg : '#efefef', padding: '9px 12px', display: 'flex', flexDirection: 'column', gap: 2 }}>
        <span style={{ fontSize: 8.5, fontWeight: 600, letterSpacing: '.1em', textTransform: 'uppercase', color: warn ? COLORS.risk.text : '#7d7d7d' }}>{label}</span>
        <span style={{ fontSize: 16, fontWeight: 700, color: warn ? COLORS.risk.text : '#1f1f1f' }}>{value}</span>
      </div>
    );
  }
  return (
    <div style={{ display: 'flex', gap: 11, flexWrap: 'wrap' }}>
      <Tile label="Collected" value={counts.collected} />
      <Tile label="Pending" value={counts.pending} />
      <Tile label="Overdue vs target" value={counts.overdue} warn={counts.overdue > 0} />
    </div>
  );
}

function KycPanel({ party, rows, onCollect, onFieldChange, onAddRemark, onView, onBulkClick, onPrint }) {
  const cols = ['Document name', 'Uploaded file', 'Upload date', 'Status', 'Target date', 'Remarks', 'Action'];
  return (
    <div style={{ border: '1px solid #d4d4d4', background: '#fff' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 11px', borderBottom: '1px solid #d4d4d4', background: '#f2f2f2', flexWrap: 'wrap', gap: 8 }}>
        <span style={sectionLabelStyle}>KYC documents · {party}</span>
        {/* [IMPROVEMENT #4] both now do something real — see handlers */}
        <div style={{ display: 'flex', gap: 11 }}>
          <span onClick={onBulkClick} style={linkStyle}>Bulk upload</span>
          <span onClick={onPrint} title="Prints the current party's checklist" style={linkStyle}>Print checklist</span>
        </div>
      </div>
      <div style={{ padding: '13px 12px', overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: 720 }}>
          <thead>
            <tr style={{ background: '#f7f7f7' }}>{cols.map((c) => <th key={c} style={thStyle}>{c}</th>)}</tr>
          </thead>
          <tbody>
            {rows.map((r, i) => {
              const ds = displayStatus(r);
              const chip = ds === 'Collected' ? COLORS.healthy : ds === 'Stale' ? COLORS.risk : COLORS.caution;
              const chaseable = isChaseable(r);
              const overdue = isOverdue(r);
              return (
                <tr key={r.key} style={{ borderBottom: '1px solid #eceef1' }} onMouseEnter={(e) => (e.currentTarget.style.background = COLORS.rowHover)} onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}>
                  <td style={{ padding: '7px 8px', fontSize: 11.5, color: '#3b4453' }}>{r.name}</td>
                  <td style={{ padding: '7px 8px', fontSize: 11.5, color: r.file ? COLORS.accent : '#c2c2c2' }}>{r.file || '—'}</td>
                  <td style={{ padding: '7px 8px', fontSize: 11.5, color: COLORS.textSecondary }}>{r.uploadDate || '—'}</td>
                  <td style={{ padding: '7px 8px' }}>
                    <span style={{ fontSize: 10.5, fontWeight: 500, padding: '2px 7px', background: chip.bg, color: chip.text }}>{ds}</span>
                    {overdue && <span title="Past target date" style={{ marginLeft: 5, color: COLORS.risk.text }}>⚠</span>}
                  </td>
                  <td style={{ padding: '7px 8px' }}>
                    {chaseable
                      ? <input type="date" value={r.targetDate} onChange={(e) => onFieldChange(i, 'targetDate', e.target.value)} style={{ height: 26, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 11, padding: '0 5px' }} />
                      : <span style={{ color: '#c2c2c2' }}>—</span>}
                  </td>
                  <td style={{ padding: '7px 8px', minWidth: 180 }}>
                    {chaseable ? <RemarksLog log={r.remarksLog} onAdd={(text) => onAddRemark(i, text)} /> : <span style={{ color: '#c2c2c2' }}>—</span>}
                  </td>
                  <td style={{ padding: '7px 8px' }}>
                    {ds === 'Collected'
                      ? <button aria-label={'View ' + r.name} onClick={() => onView(r)} style={{ fontSize: 10.5, color: COLORS.accent, background: 'none', border: 'none', cursor: 'pointer', textDecoration: 'underline' }}>View</button>
                      : <button aria-label={(ds === 'Stale' ? 'Re-upload ' : 'Upload ') + r.name} onClick={() => onCollect(i)} style={{ fontSize: 10.5, color: '#3d3d3d', background: '#ebebeb', border: '1px solid #9e9e9e', padding: '3px 8px', cursor: 'pointer' }}>{ds === 'Stale' ? 'Re-upload' : 'Upload'}</button>}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function RemarksLog({ log, onAdd }) {
  // [IMPROVEMENT #6] a running log instead of one field that gets overwritten —
  // matches how chasing a pending document actually happens (multiple contacts
  // over time), not a single freeform note.
  const [draft, setDraft] = useState('');
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      {log.map((entry, i) => (
        <div key={i} style={{ fontSize: 10, color: COLORS.textMuted }}><span style={{ color: '#b0b0b0' }}>{entry.date}</span> — {entry.text}</div>
      ))}
      <div style={{ display: 'flex', gap: 4 }}>
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter' && draft.trim()) { onAdd(draft); setDraft(''); } }}
          placeholder="Add a note…"
          style={{ height: 24, flex: 1, minWidth: 100, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 10.5, padding: '0 6px' }}
        />
        <button onClick={() => { if (draft.trim()) { onAdd(draft); setDraft(''); } }} style={{ height: 24, border: '1px solid #9e9e9e', background: '#ebebeb', fontSize: 9.5, padding: '0 7px', cursor: 'pointer' }}>Add</button>
      </div>
    </div>
  );
}

function OtherDocsPanel({ docs, files, onToggle }) {
  return (
    <div style={{ border: '1px solid #d4d4d4', background: '#fff' }}>
      <div style={{ padding: '8px 11px', borderBottom: '1px solid #d4d4d4', background: '#f2f2f2' }}><span style={sectionLabelStyle}>Other documents</span></div>
      <div style={{ padding: '13px 12px' }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(190px, 1fr))', gap: 11 }}>
          {OTHER_DOC_TYPES.map((d) => {
            const status = docs[d.key];
            const file = files[d.key];
            return (
              <div key={d.key} style={{ border: '1px solid #e0e0e0', background: '#fafafa', padding: 10, display: 'flex', flexDirection: 'column', gap: 7 }}>
                <span style={{ fontSize: 9, fontWeight: 600, letterSpacing: '.09em', textTransform: 'uppercase', color: '#6f6f6f' }}>{d.label}</span>
                {status === 'Collected' ? (
                  <button aria-label={'Replace ' + d.label} onClick={() => onToggle(d.key)} style={{ height: 27, border: '1px solid #8fae8c', background: '#eef6ed', display: 'flex', alignItems: 'center', padding: '0 9px', fontSize: 10, color: COLORS.healthy.text, cursor: 'pointer', width: '100%', textAlign: 'left' }}>✓ {file}</button>
                ) : (
                  <button aria-label={'Upload ' + d.label} onClick={() => onToggle(d.key)} style={{ height: 27, border: '1px dashed #bdbdbd', background: '#f7f7f7', display: 'flex', alignItems: 'center', padding: '0 9px', fontSize: 10, color: '#9a9a9a', cursor: 'pointer', width: '100%', textAlign: 'left' }}>Upload · PDF / JPG</button>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

function PreviewModal({ file, onClose }) {
  // [IMPROVEMENT #9] lightweight inline preview — placeholder thumbnail only,
  // since there's no real file behind this spec. [Blazor] the real version
  // needs an actual document viewer or at minimum a download link to blob
  // storage; this demonstrates the interaction pattern, not real rendering.
  return (
    <div onClick={onClose} style={{ position: 'fixed', inset: 0, background: 'rgba(26,31,41,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 60 }}>
      <div onClick={(e) => e.stopPropagation()} style={{ width: 320, background: '#fff', border: '1px solid ' + COLORS.border, padding: 18 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 14 }}>
          <span style={{ fontSize: 13, fontWeight: 600 }}>Document preview</span>
          <button onClick={onClose} aria-label="Close preview" style={{ border: 'none', background: 'none', fontSize: 16, cursor: 'pointer', color: COLORS.textSecondary }}>×</button>
        </div>
        <div style={{ height: 180, border: '1px solid #e0e0e0', background: '#f4f5f7', display: 'flex', alignItems: 'center', justifyContent: 'center', marginBottom: 12 }}>
          <IconFile />
        </div>
        <div style={{ fontSize: 12, fontWeight: 500, color: COLORS.textPrimary, marginBottom: 2 }}>{file.file || file.name}</div>
        {file.uploadDate && <div style={{ fontSize: 11, color: COLORS.textMuted }}>Uploaded {file.uploadDate}</div>}
      </div>
    </div>
  );
}

function IconFile() {
  return (
    <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="#b0b6be" strokeWidth="1.5">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
      <path d="M14 2v6h6" />
    </svg>
  );
}

function Stepper({ current }) {
  return (
    <div style={{ display: 'flex', gap: 5, alignItems: 'stretch', width: '100%', flexWrap: 'wrap' }}>
      {STEP_LABELS.map((label, i) => {
        const num = i + 1;
        const numStr = (num < 10 ? '0' : '') + num;
        if (num < current) return (
          <div key={label} style={{ flex: 1, minWidth: 110, height: 40, border: '1px solid #b9bfc9', background: '#eceef1', padding: '0 9px', display: 'flex', flexDirection: 'column', justifyContent: 'center', gap: 2 }}>
            <span style={{ fontSize: 8.5, fontWeight: 600, letterSpacing: '.1em', color: '#5b6472' }}>✓ {numStr} DONE</span>
            <span style={{ fontSize: 10.5, fontWeight: 600, color: '#3b4453' }}>{label}</span>
          </div>
        );
        if (num === current) return (
          <div key={label} aria-current="step" style={{ flex: 1, minWidth: 110, height: 40, border: '1px solid #16304f', background: '#1f3a5f', padding: '0 9px', display: 'flex', flexDirection: 'column', justifyContent: 'center', gap: 2 }}>
            <span style={{ fontSize: 8.5, fontWeight: 600, letterSpacing: '.1em', color: '#cdd6e0' }}>● {numStr} CURRENT</span>
            <span style={{ fontSize: 10.5, fontWeight: 700, color: '#fff' }}>{label}</span>
          </div>
        );
        return (
          <div key={label} aria-disabled="true" style={{ flex: 1, minWidth: 110, height: 40, border: '1px dashed #cbcbcb', background: '#fbfbfb', padding: '0 9px', display: 'flex', flexDirection: 'column', justifyContent: 'center', gap: 2 }}>
            <span style={{ fontSize: 8.5, fontWeight: 600, letterSpacing: '.1em', color: '#a8a8a8' }}>○ {numStr} LOCKED</span>
            <span style={{ fontSize: 10.5, fontWeight: 500, color: '#9a9a9a' }}>{label}</span>
          </div>
        );
      })}
    </div>
  );
}

function SummaryRail() {
  function row(label, val, big) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        <span style={{ fontSize: 9, fontWeight: 600, letterSpacing: '.09em', textTransform: 'uppercase', color: '#8d8d8d' }}>{label}</span>
        <span style={{ fontSize: big ? 15 : 11.5, fontWeight: big ? 600 : 500, color: '#333' }}>{val}</span>
      </div>
    );
  }
  return (
    <div style={{ width: '100%', border: '1px solid #d0d0d0', background: '#fafafa' }}>
      <div style={{ padding: '8px 11px', borderBottom: '1px solid #d0d0d0', background: '#efefef' }}><span style={{ fontSize: 9.5, fontWeight: 600, letterSpacing: '.12em', textTransform: 'uppercase', color: '#5c5c5c' }}>Application summary</span></div>
      <div style={{ padding: 11, display: 'flex', flexDirection: 'column', gap: 9 }}>
        {row('Customer type', 'Individual · CV')}
        {row('Branch', 'Nashik West')}
        {row('Loan product', 'Commercial Vehicle')}
        {row('Scheme', 'CV-STD-2026')}
        <div style={{ height: 1, background: '#dedede' }} />
        {row('Loan amount', '₹18,50,000', true)}
        <div style={{ display: 'flex', gap: 10 }}><div style={{ flex: 1 }}>{row('Tenure', '48 mo')}</div><div style={{ flex: 1 }}>{row('ROI', '13.25%')}</div></div>
        <div style={{ height: 1, background: '#dedede' }} />
        <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
          <span style={{ fontSize: 9, fontWeight: 600, letterSpacing: '.09em', textTransform: 'uppercase', color: '#8d8d8d' }}>Quick actions</span>
          <button style={{ height: 26, border: '1px solid #c4c4c4', background: '#f1f1f1', display: 'flex', alignItems: 'center', padding: '0 9px', fontSize: 10.5, fontWeight: 500, color: '#555', textAlign: 'left', cursor: 'pointer' }}>View CAM.pdf</button>
          <button style={{ height: 26, border: '1px solid #c4c4c4', background: '#f1f1f1', display: 'flex', alignItems: 'center', padding: '0 9px', fontSize: 10.5, fontWeight: 500, color: '#555', textAlign: 'left', cursor: 'pointer' }}>Activity log</button>
        </div>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// SHARED STYLES
// ---------------------------------------------------------------------------
const sectionLabelStyle = { fontSize: 10, fontWeight: 600, letterSpacing: '.13em', textTransform: 'uppercase', color: '#4d4d4d' };
const ghostBtnStyle = { height: 28, border: '1px solid ' + COLORS.borderStrong, background: '#ebebeb', borderRadius: 0, padding: '0 12px', fontSize: 11, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#3d3d3d', cursor: 'pointer' };
const primaryBtnStyle = { height: 28, border: '1px solid #16304f', background: COLORS.accent, borderRadius: 0, padding: '0 12px', fontSize: 11, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#fff', cursor: 'pointer' };
const linkStyle = { fontSize: 10, color: '#9a9a9a', textDecoration: 'underline', cursor: 'pointer' };
const thStyle = { textAlign: 'left', padding: '7px 8px', fontSize: 9.5, fontWeight: 600, letterSpacing: '.06em', textTransform: 'uppercase', color: '#8d8d8d', borderBottom: '1px solid #d4d4d4', whiteSpace: 'nowrap' };
