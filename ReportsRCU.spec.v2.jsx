/*
  REPORTS (RCU) — STAGE 5 OF 8 — REFERENCE SPEC v2
  =====================================================================
  Target stack: Blazor Server on .NET 8 + MySQL. Comments flag only what
  needs real translation — state, events, browser-only APIs.

  *** OPEN QUESTION FROM v1, DECIDED FOR THIS BUILD ***
  Screened vs Sampled has no defined behavioral effect anywhere in the
  brief. Built as classification metadata only — it's recorded and
  displayed, but doesn't touch the outcome table or the completion gate.
  If it's actually meant to change behavior (e.g. Sampled cases skip the
  gate, or route to a different reviewer), that's a real decision still
  needed, not something to infer from a toggle's label.

  Dummy data note: everything below is placeholder for visual reference
  only — wire to real Application / Party / RCU records.
*/

import React, { useState } from 'react';

const COLORS = {
  border: '#d8dbe1', borderStrong: '#b9bfc9', textPrimary: '#1a1f29', textSecondary: '#5b6472', textMuted: '#93999a',
  accent: '#1f3a5f', rowHover: '#fafbfc',
  healthy: { bg: '#d9ecd7', text: '#1f5c2a' },
  caution: { bg: '#fbe8c9', text: '#7a4b06' },
  risk: { bg: '#f8d7d7', text: '#8a1f1f' },
};
const FONT = "'IBM Plex Sans', Arial, sans-serif";
const STEP_LABELS = ['Customer Details', 'Loan & Security', 'Bank & Financial', 'Document Checklist', 'Reports', 'Eligibility', 'Approvals', 'Post Sanction'];
const CURRENT_STEP = 5;
const REFERENCE_TODAY = new Date('2026-07-13'); // spec-only anchor; use server date in production

const ALL_PARTIES = ['Applicant', 'Co-Applicant', 'Guarantor'];
const OFFICER_OPTIONS = ['', 'R. Kulkarni', 'S. Deshpande', 'A. Rao']; // reused from the Applications Dashboard filter list, for continuity

function daysBetween(a, b) { return Math.floor((a - b) / 86400000); }
function addDays(dateStr, days) { const d = new Date(dateStr); d.setDate(d.getDate() + days); return d; }
function fmtDate(d) { return d.toISOString().slice(0, 10); }

function initialOutcomes() {
  return {
    Applicant: { name: 'Ramesh Pawar', status: 'Recommended', verifiedOn: '2026-07-11', verifiedBy: 'R. Kulkarni', remarks: 'Clean verification, no adverse findings' },
    'Co-Applicant': { name: 'Sunita Pawar', status: 'Recommended', verifiedOn: '2026-07-12', verifiedBy: 'R. Kulkarni', remarks: 'Address confirmed by neighbor' },
    Guarantor: { name: 'Ganesh Shinde', status: 'Pending', verifiedOn: '', verifiedBy: '', remarks: 'Field visit scheduled' },
  };
}
function initialReports() {
  // [IMPROVEMENT #5] history — the last entry is "current"; everything before
  // it is prior submissions. Seeded with one prior round so the history list
  // has something to show without requiring a click first.
  return [
    { date: '2026-07-08', file: 'RCU_report_v1.pdf', note: 'Initial submission — incomplete, missing Guarantor verification' },
    { date: '2026-07-10', file: 'RCU_report_v2.pdf', note: 'Re-submitted after guarantor field visit scheduled' },
  ];
}

export default function ReportsRcuSpec() {
  const [mode, setMode] = useState('Screened');
  const [hasGuarantor, setHasGuarantor] = useState(true);
  const [initiation, setInitiation] = useState({ branch: 'Nashik West', vendor: 'Verified Field Services', initiationDate: '2026-07-08', completionDate: '', tat: 3, caseRef: 'RCU-2026-08842' });
  const [outcomes, setOutcomes] = useState(initialOutcomes);
  const [reports, setReports] = useState(initialReports);
  const [reportSaved, setReportSaved] = useState(false);
  const [overrideActive, setOverrideActive] = useState(false); // [IMPROVEMENT #4]
  const [overrideReason, setOverrideReason] = useState('');
  const [overrideApprover, setOverrideApprover] = useState('');

  const visibleParties = ALL_PARTIES.filter((p) => p !== 'Guarantor' || hasGuarantor);

  const statusCounts = visibleParties.reduce((acc, p) => {
    acc[outcomes[p].status] = (acc[outcomes[p].status] || 0) + 1;
    return acc;
  }, {});
  const overallStatus = statusCounts.Pending ? 'Pending' : statusCounts['Not recommended'] ? 'Not recommended' : 'Recommended';
  const overallColor = overallStatus === 'Recommended' ? COLORS.healthy : overallStatus === 'Not recommended' ? COLORS.risk : COLORS.caution;

  // [IMPROVEMENT #2] TAT breach — only meaningful before completion is recorded.
  const expectedCompletion = addDays(initiation.initiationDate, Number(initiation.tat) || 0);
  const tatDaysOver = daysBetween(REFERENCE_TODAY, expectedCompletion);
  const tatBreached = !initiation.completionDate && tatDaysOver > 0;

  // [IMPROVEMENT #4] override only applies to a completed-but-negative outcome —
  // it can't override "Pending", since there's nothing yet to override.
  const overrideValid = overrideActive && overrideReason.trim() && overrideApprover.trim();
  const completeBlocked = overallStatus === 'Pending' || (overallStatus === 'Not recommended' && !overrideValid);

  const currentReport = reports[reports.length - 1];
  const reportHistory = reports.slice(0, -1);

  function setInitiationField(key, value) { setInitiation((prev) => ({ ...prev, [key]: value })); }
  function setOutcomeField(party, field, value) {
    setOutcomes((prev) => {
      const next = { ...prev, [party]: { ...prev[party], [field]: value } };
      if (field === 'status') {
        if (value !== 'Pending' && !next[party].verifiedOn) next[party].verifiedOn = fmtDate(REFERENCE_TODAY);
        if (value === 'Pending') { next[party].verifiedOn = ''; next[party].verifiedBy = ''; }
      }
      return next;
    });
  }

  // [IMPROVEMENT #3] "Re-submit to vendor" now does something real: archives
  // the current report into history, opens a fresh submission cycle (new
  // initiation date, cleared completion date), and resets the save flag.
  // [Blazor] the confirm() below is a browser-native stand-in — the real
  // build needs a proper confirmation dialog component, not window.confirm.
  function resubmitToVendor() {
    if (!window.confirm('Re-submit this case to the RCU vendor? This starts a new verification cycle.')) return;
    const n = reports.length + 1;
    setReports((prev) => [...prev, { date: fmtDate(REFERENCE_TODAY), file: `RCU_report_v${n}.pdf`, note: 'Re-submitted to vendor' }]);
    setInitiation((prev) => ({ ...prev, initiationDate: fmtDate(REFERENCE_TODAY), completionDate: '' }));
    setReportSaved(false);
  }

  return (
    <div style={{ fontFamily: FONT, fontSize: 12, color: COLORS.textPrimary }}>

      <div style={{ display: 'flex', gap: 8, padding: '10px 12px', border: '1px dashed ' + COLORS.borderStrong, marginBottom: 16, background: '#fff' }}>
        <span style={{ fontSize: 11, color: COLORS.textMuted, alignSelf: 'center' }}>Spec preview — guarantor on this application:</span>
        {[true, false].map((v) => (
          <button key={String(v)} onClick={() => setHasGuarantor(v)} style={{ fontSize: 11, padding: '4px 10px', border: '1px solid ' + COLORS.border, borderRadius: 0, cursor: 'pointer', background: hasGuarantor === v ? COLORS.accent : '#fff', color: hasGuarantor === v ? '#fff' : COLORS.textSecondary }}>{v ? 'yes' : 'no'}</button>
        ))}
      </div>

      <div style={{ border: '1px solid ' + COLORS.border, background: '#fff' }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '10px 14px', borderBottom: '1px solid ' + COLORS.border, background: '#f4f4f4', flexWrap: 'wrap', gap: 8 }}>
          <span style={{ fontSize: 11, fontWeight: 600, letterSpacing: '.1em', textTransform: 'uppercase', color: COLORS.textSecondary }}>← Applications · APP #LN-2026-004871</span>
          <div style={{ display: 'flex', gap: 8 }}>
            <button onClick={resubmitToVendor} style={ghostBtnStyle}>Re-submit to vendor</button>
            <button disabled={completeBlocked} style={completeBlocked ? primaryBtnStyleBlocked : primaryBtnStyle}>Complete stage →</button>
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
              <span style={{ fontSize: 13, fontWeight: 700, letterSpacing: '.08em' }}>06</span>
              <span style={{ fontSize: 16, fontWeight: 700 }}>STAGE 5 — REPORTS (RCU)</span>
              <span style={{ fontSize: 10.5, fontWeight: 500, letterSpacing: '.09em', textTransform: 'uppercase', color: COLORS.textMuted }}>Stage 5 of 8</span>
            </div>

            <InitiationPanel
              mode={mode} onModeChange={setMode}
              initiation={initiation} onFieldChange={setInitiationField}
              overallStatus={overallStatus} overallColor={overallColor} statusCounts={statusCounts}
              tatBreached={tatBreached} tatDaysOver={tatDaysOver}
            />

            {/* [IMPROVEMENT #4] only appears when there's actually something to override */}
            {overallStatus === 'Not recommended' && (
              <OverridePanel
                active={overrideActive} onToggle={setOverrideActive}
                reason={overrideReason} onReasonChange={setOverrideReason}
                approver={overrideApprover} onApproverChange={setOverrideApprover}
              />
            )}

            <div style={{ display: 'flex', gap: 14, alignItems: 'stretch', flexWrap: 'wrap' }}>
              <OutcomePanel parties={visibleParties} outcomes={outcomes} onFieldChange={setOutcomeField} />
              <ReportPanel current={currentReport} history={reportHistory} saved={reportSaved} onSave={() => setReportSaved(true)} />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// SUB-COMPONENTS
// ---------------------------------------------------------------------------

function InitiationPanel({ mode, onModeChange, initiation, onFieldChange, overallStatus, overallColor, statusCounts, tatBreached, tatDaysOver }) {
  function field(label, inner) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 4, minWidth: 0 }}>
        <span style={{ fontSize: 10.5, fontWeight: 600, letterSpacing: '.08em', textTransform: 'uppercase', color: '#7d7d7d' }}>{label}</span>
        {inner}
      </div>
    );
  }
  // [IMPROVEMENT #1] breakdown line — no more one-word verdict with no explanation
  const breakdown = Object.entries(statusCounts).map(([s, n]) => `${n} ${s}`).join(' · ');

  return (
    <div style={{ border: '1px solid #d4d4d4', background: '#fff' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 11px', borderBottom: '1px solid #d4d4d4', background: '#f2f2f2', flexWrap: 'wrap', gap: 8 }}>
        <span style={sectionLabelStyle}>RCU initiation</span>
        <div style={{ display: 'flex' }}>
          {['Screened', 'Sampled'].map((m) => {
            const active = mode === m;
            return (
              <button key={m} onClick={() => onModeChange(m)}
                style={{ padding: '5px 12px', border: '1px solid ' + (active ? '#1f1f1f' : '#c6c6c6'), borderLeft: m === 'Sampled' ? 'none' : undefined, background: active ? '#2b2b2b' : '#f4f4f4', fontSize: 9.5, fontWeight: 600, letterSpacing: '.07em', textTransform: 'uppercase', color: active ? '#fff' : '#8b8b8b', cursor: 'pointer' }}>
                {m}
              </button>
            );
          })}
        </div>
      </div>
      <div style={{ padding: '13px 12px', display: 'flex', gap: 14, alignItems: 'flex-start', flexWrap: 'wrap' }}>
        <div style={{ flex: 1, minWidth: 260, display: 'flex', flexDirection: 'column', gap: 12 }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', gap: '12px 14px' }}>
            {field('Branch name', <select value={initiation.branch} onChange={(e) => onFieldChange('branch', e.target.value)} style={selectStyle}>{['Nashik West', 'Nashik East', 'Pune Camp', 'Aurangabad', 'Jalgaon'].map((o) => <option key={o}>{o}</option>)}</select>)}
            {field('Vendor name', <select value={initiation.vendor} onChange={(e) => onFieldChange('vendor', e.target.value)} style={selectStyle}>{['Verified Field Services', 'TransUnion CIBIL RCU', 'CRISIL Risk Solutions', 'SecureCheck Verifications', 'Other'].map((o) => <option key={o}>{o}</option>)}</select>)}
            {field('Initiation date', <input type="date" value={initiation.initiationDate} onChange={(e) => onFieldChange('initiationDate', e.target.value)} style={inputStyle} />)}
            {field('Completion date', <input type="date" value={initiation.completionDate} onChange={(e) => onFieldChange('completionDate', e.target.value)} style={inputStyle} />)}
            {field('TAT (days)', <input type="number" value={initiation.tat} onChange={(e) => onFieldChange('tat', Number(e.target.value) || 0)} style={inputStyle} />)}
            {field('Case reference no.', <input type="text" value={initiation.caseRef} onChange={(e) => onFieldChange('caseRef', e.target.value)} style={inputStyle} />)}
          </div>
          {/* [IMPROVEMENT #2] TAT breach signal */}
          {tatBreached && (
            <div style={{ fontSize: 11, color: COLORS.risk.text, fontWeight: 500 }}>⚠ TAT breached by {tatDaysOver} day{tatDaysOver === 1 ? '' : 's'} — vendor is past their committed turnaround</div>
          )}
        </div>
        <div style={{ flex: '0 0 220px', border: '1px solid ' + overallColor.text, background: overallColor.bg, padding: 12, display: 'flex', flexDirection: 'column', gap: 4 }}>
          <span style={{ fontSize: 9, fontWeight: 600, letterSpacing: '.1em', textTransform: 'uppercase', color: overallColor.text }}>Overall status</span>
          <span style={{ fontSize: 17, fontWeight: 700, color: overallColor.text }}>{overallStatus}</span>
          <span style={{ fontSize: 10, color: '#8a8a8a' }}>Derived from all applicant outcomes</span>
          <span style={{ fontSize: 10.5, color: overallColor.text, marginTop: 2 }}>{breakdown}</span>
        </div>
      </div>
    </div>
  );
}

function OverridePanel({ active, onToggle, reason, onReasonChange, approver, onApproverChange }) {
  return (
    <div style={{ border: '1px solid ' + COLORS.risk.text, background: COLORS.risk.bg, padding: '11px 13px', display: 'flex', flexDirection: 'column', gap: 8 }}>
      <label style={{ display: 'flex', alignItems: 'center', gap: 7, fontSize: 11.5, fontWeight: 600, color: COLORS.risk.text, cursor: 'pointer' }}>
        <input type="checkbox" checked={active} onChange={(e) => onToggle(e.target.checked)} />
        Override and proceed despite "Not recommended"
      </label>
      {active && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: 10 }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            <span style={{ fontSize: 10, fontWeight: 600, color: COLORS.risk.text }}>Reason (required)</span>
            <input type="text" value={reason} onChange={(e) => onReasonChange(e.target.value)} placeholder="Why is this proceeding despite the outcome?" style={inputStyle} />
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            <span style={{ fontSize: 10, fontWeight: 600, color: COLORS.risk.text }}>Approved by (required)</span>
            <select value={approver} onChange={(e) => onApproverChange(e.target.value)} style={selectStyle}>
              {OFFICER_OPTIONS.map((o) => <option key={o || '(blank)'} value={o}>{o || '— Select —'}</option>)}
            </select>
          </div>
        </div>
      )}
    </div>
  );
}

function OutcomePanel({ parties, outcomes, onFieldChange }) {
  const cols = ['Applicant', 'Role', 'RCU status', 'Verified on', 'Verified by', 'Remarks'];
  return (
    <div style={{ flex: 1, minWidth: 280, border: '1px solid #d4d4d4', background: '#fff' }}>
      <div style={{ padding: '8px 11px', borderBottom: '1px solid #d4d4d4', background: '#f2f2f2' }}><span style={sectionLabelStyle}>Per-applicant outcome</span></div>
      <div style={{ padding: '13px 12px', overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: 620 }}>
          <thead><tr style={{ background: '#f7f7f7' }}>{cols.map((c) => <th key={c} style={thStyle}>{c}</th>)}</tr></thead>
          <tbody>
            {parties.map((p) => {
              const o = outcomes[p];
              const c = o.status === 'Recommended' ? COLORS.healthy : o.status === 'Not recommended' ? COLORS.risk : COLORS.caution;
              return (
                <tr key={p} style={{ borderBottom: '1px solid #eceef1' }} onMouseEnter={(e) => (e.currentTarget.style.background = COLORS.rowHover)} onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}>
                  <td style={{ padding: '7px 8px', fontSize: 11.5, color: COLORS.textPrimary, fontWeight: 500 }}>{o.name}</td>
                  <td style={{ padding: '7px 8px', fontSize: 11.5, color: COLORS.textSecondary }}>{p}</td>
                  <td style={{ padding: '7px 8px' }}>
                    {/* [IMPROVEMENT #7] per-applicant aria-label — otherwise three
                        identical "Pending / Recommended / Not recommended" controls
                        are indistinguishable to a screen reader. */}
                    <select aria-label={'RCU status for ' + o.name} value={o.status} onChange={(e) => onFieldChange(p, 'status', e.target.value)}
                      style={{ height: 27, border: '1.5px solid ' + c.text, borderRadius: 0, fontSize: 11, background: c.bg, color: c.text, fontWeight: 500, padding: '0 5px', cursor: 'pointer' }}>
                      {['Pending', 'Recommended', 'Not recommended'].map((s) => <option key={s}>{s}</option>)}
                    </select>
                  </td>
                  <td style={{ padding: '7px 8px' }}>
                    {o.status === 'Pending' ? <span style={{ color: '#c2c2c2' }}>—</span> : <input aria-label={'Verified-on date for ' + o.name} type="date" value={o.verifiedOn} onChange={(e) => onFieldChange(p, 'verifiedOn', e.target.value)} style={{ height: 26, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 11, padding: '0 5px' }} />}
                  </td>
                  <td style={{ padding: '7px 8px' }}>
                    {/* [IMPROVEMENT #6] compliance/audit trail — who confirmed this */}
                    {o.status === 'Pending' ? <span style={{ color: '#c2c2c2' }}>—</span> : (
                      <select aria-label={'Verified by, for ' + o.name} value={o.verifiedBy} onChange={(e) => onFieldChange(p, 'verifiedBy', e.target.value)} style={selectStyleSm}>
                        {OFFICER_OPTIONS.map((opt) => <option key={opt || '(blank)'} value={opt}>{opt || '— Select —'}</option>)}
                      </select>
                    )}
                  </td>
                  <td style={{ padding: '7px 8px' }}>
                    <input aria-label={'Remarks for ' + o.name} type="text" value={o.remarks} onChange={(e) => onFieldChange(p, 'remarks', e.target.value)} style={{ height: 26, width: 170, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 11, padding: '0 6px' }} />
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

function ReportPanel({ current, history, saved, onSave }) {
  return (
    <div style={{ flex: '0 0 300px', border: '1px solid #d4d4d4', background: '#fff' }}>
      <div style={{ padding: '8px 11px', borderBottom: '1px solid #d4d4d4', background: '#f2f2f2' }}><span style={sectionLabelStyle}>Report</span></div>
      <div style={{ padding: '13px 12px', display: 'flex', flexDirection: 'column', gap: 10 }}>
        <div style={{ height: 130, border: '1px dashed #bdbdbd', background: '#f7f7f7', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 11, color: '#9a9a9a', textAlign: 'center', padding: 10 }}>RCU report preview (PDF)</div>
        <div style={{ fontSize: 10.5, color: COLORS.textSecondary }}>{current.file} · {current.date}</div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button style={{ flex: 1, height: 28, border: '1px solid #9e9e9e', background: '#ebebeb', fontSize: 10.5, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#3d3d3d', cursor: 'pointer' }}>View</button>
          <button onClick={onSave} style={{ flex: 1, height: 28, border: '1px solid ' + (saved ? COLORS.healthy.text : '#16304f'), background: saved ? COLORS.healthy.bg : COLORS.accent, fontSize: 10.5, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: saved ? COLORS.healthy.text : '#fff', cursor: 'pointer' }}>{saved ? '✓ Saved' : 'Save'}</button>
        </div>

        {/* [IMPROVEMENT #5] report history — prior submissions, not just the latest */}
        {history.length > 0 && (
          <div style={{ borderTop: '1px solid #eceef1', paddingTop: 10, display: 'flex', flexDirection: 'column', gap: 6 }}>
            <span style={{ fontSize: 9, fontWeight: 600, letterSpacing: '.1em', textTransform: 'uppercase', color: '#8d8d8d' }}>Report history</span>
            {history.map((r, i) => (
              <div key={i} style={{ fontSize: 10.5, color: COLORS.textSecondary }}>
                <span style={{ color: COLORS.accent, textDecoration: 'underline', cursor: 'pointer' }}>{r.file}</span> · {r.date}
                <div style={{ fontSize: 10, color: COLORS.textMuted }}>{r.note}</div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
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
const inputStyle = { height: 29, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 12, background: '#fafbfc', padding: '0 8px', width: '100%' };
const selectStyle = { height: 29, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 12, background: '#fafbfc', padding: '0 6px', width: '100%' };
const selectStyleSm = { height: 26, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 11, background: '#fafbfc', padding: '0 5px' };
const ghostBtnStyle = { height: 28, border: '1px solid ' + COLORS.borderStrong, background: '#ebebeb', borderRadius: 0, padding: '0 12px', fontSize: 11, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#3d3d3d', cursor: 'pointer' };
const primaryBtnStyle = { height: 28, border: '1px solid #16304f', background: COLORS.accent, borderRadius: 0, padding: '0 12px', fontSize: 11, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#fff', cursor: 'pointer' };
const primaryBtnStyleBlocked = { height: 28, border: '1px solid ' + COLORS.borderStrong, background: '#e4e6e9', borderRadius: 0, padding: '0 12px', fontSize: 11, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#a7adb5', cursor: 'not-allowed' };
const thStyle = { textAlign: 'left', padding: '7px 8px', fontSize: 9.5, fontWeight: 600, letterSpacing: '.06em', textTransform: 'uppercase', color: '#8d8d8d', borderBottom: '1px solid #d4d4d4', whiteSpace: 'nowrap' };
