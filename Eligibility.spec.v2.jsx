/*
  ELIGIBILITY — STAGE 6 OF 8 — REFERENCE SPEC v2
  =====================================================================
  Target stack: Blazor Server on .NET 8 + MySQL. Comments flag only what
  needs real translation — state, events, browser-only APIs.

  *** DECIDED: Banking tab is context-only ***
  Per direction, Banking (avg balance, cheque bounces, inward/outward
  txn %) does NOT feed the eligibility calculation — it's informational
  context for the officer's judgment, not a formula input. The tab now
  says so explicitly rather than leaving it ambiguous. If that's ever
  wrong, wiring it in later means touching computeEligibility() only —
  the UI already renders whatever that function returns.

  *** STILL THE MOST IMPORTANT WARNING IN THIS FILE (unchanged from v1) ***
  The eligible-amount formula (FOIR cap at 50%, LTV cap at 85%, take the
  minimum) is INVENTED, not your underwriting policy. It exists so this
  screen has something real to compute and react to. Confirm the actual
  formula with Arun/client before anything downstream depends on it.

  Dummy data note: everything below is placeholder for visual reference
  only — wire to real Application / Party / ExistingLoan / Banking records.
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
const CURRENT_STEP = 6;

// Pulled from earlier stages — [Blazor] real Application/Party lookups
const REQUESTED_AMOUNT = 1850000, ROI = 13.25, TENURE = 48;
const TOTAL_INCOME = 88000;
const ON_ROAD_COST = 2260000;
const LTV_PCT = (REQUESTED_AMOUNT / ON_ROAD_COST) * 100;

// Placeholder underwriting policy thresholds — NOT confirmed business rules
const FOIR_CAP_PCT = 50;
const LTV_CAP_PCT = 85;

const ALL_PARTIES = ['Applicant', 'Co-Applicant', 'Guarantor'];

function fmtINR(n) { return '₹' + Math.round(n).toLocaleString('en-IN'); }
function calcEMI(P, annualRatePct, n) {
  if (!P || !annualRatePct || !n) return 0;
  const r = annualRatePct / 12 / 100, factor = Math.pow(1 + r, n);
  if (factor === 1) return 0;
  return (P * r * factor) / (factor - 1);
}
function inverseEMI(emi, annualRatePct, n) {
  if (!emi || !annualRatePct || !n) return 0;
  const r = annualRatePct / 12 / 100, factor = Math.pow(1 + r, n);
  if (factor === 1) return 0;
  return (emi * (factor - 1)) / (r * factor);
}

function initialExistingLoans() {
  return [
    { key: 'l1', party: 'Applicant', lender: 'HDFC Bank', loanType: 'Personal loan', sanctioned: 500000, roi: 14, pos: 180000, emi: 12500, maxDpd: 0, bounces: 0, rtr: 'Regular' },
    { key: 'l2', party: 'Co-Applicant', lender: 'Bajaj Finance', loanType: 'Two-wheeler loan', sanctioned: 80000, roi: 16, pos: 22000, emi: 3200, maxDpd: 15, bounces: 1, rtr: 'Irregular' },
  ];
}
function initialBankingRows() {
  return [
    { key: 'b1', party: 'Applicant', bank: 'HDFC Bank', months: 6, avgBalance: 145000, bounces: 0, inwardPct: 62, outwardPct: 58 },
    { key: 'b2', party: 'Co-Applicant', bank: 'ICICI Bank', months: 6, avgBalance: 38000, bounces: 1, inwardPct: 45, outwardPct: 50 },
  ];
}

export default function EligibilitySpec() {
  const [activeTab, setActiveTab] = useState('Borrower Classification');
  const [hasGuarantor, setHasGuarantor] = useState(true);
  const [classification, setClassification] = useState({ psl: 'Yes - Priority Sector', pslSub: 'Transport Sector - CV', riskSharing: 20, coLendingPartner: 'HDFC Bank', endUse: 'Fleet expansion', prioritySectorAmount: 1850000 });
  const [existingLoans, setExistingLoans] = useState(initialExistingLoans);
  const [bankingRows, setBankingRows] = useState(initialBankingRows);
  const [approverNote, setApproverNote] = useState('');
  const [noteWrittenAtDeviation, setNoteWrittenAtDeviation] = useState(null); // [IMPROVEMENT #5]

  const visibleParties = ALL_PARTIES.filter((p) => p !== 'Guarantor' || hasGuarantor);

  // ------------------------------------------------------------------
  // Eligibility calculation
  // ------------------------------------------------------------------
  const existingEmiSum = existingLoans.reduce((s, r) => s + r.emi, 0);
  const maxTotalEmi = (TOTAL_INCOME * FOIR_CAP_PCT) / 100;
  const maxNewEmi = Math.max(0, maxTotalEmi - existingEmiSum);
  const foirCapAmount = inverseEMI(maxNewEmi, ROI, TENURE);
  const ltvCapAmount = (ON_ROAD_COST * LTV_CAP_PCT) / 100;
  const eligible = Math.min(foirCapAmount, ltvCapAmount, REQUESTED_AMOUNT);
  const deviationPct = ((eligible - REQUESTED_AMOUNT) / REQUESTED_AMOUNT) * 100;
  const proposedEmiAtEligible = calcEMI(eligible, ROI, TENURE);
  const foirAtEligible = TOTAL_INCOME ? ((existingEmiSum + proposedEmiAtEligible) / TOTAL_INCOME) * 100 : 0;

  // [IMPROVEMENT #1] which cap actually bound the result
  const bindingIsFoir = foirCapAmount <= ltvCapAmount;
  const bindingLabel = eligible < REQUESTED_AMOUNT ? (bindingIsFoir ? 'FOIR' : 'LTV') : null;

  const completeBlocked = deviationPct < 0 && !approverNote.trim();
  const noteStale = noteWrittenAtDeviation !== null && Math.abs(noteWrittenAtDeviation - deviationPct) > 0.5; // [IMPROVEMENT #5]

  function handleNoteChange(text) {
    setApproverNote(text);
    setNoteWrittenAtDeviation(deviationPct);
  }

  function setClassificationField(key, value) { setClassification((prev) => ({ ...prev, [key]: value })); }
  function addExistingLoan() { setExistingLoans((prev) => [...prev, { key: 'l' + Date.now(), party: 'Applicant', lender: '', loanType: '', sanctioned: 0, roi: 0, pos: 0, emi: 0, maxDpd: 0, bounces: 0, rtr: 'Regular' }]); }
  function removeExistingLoan(key) { setExistingLoans((prev) => prev.filter((r) => r.key !== key)); }
  function setLoanField(key, field, value) { setExistingLoans((prev) => prev.map((r) => (r.key === key ? { ...r, [field]: value } : r))); }
  function addBankingRow() { setBankingRows((prev) => [...prev, { key: 'b' + Date.now(), party: 'Applicant', bank: '', months: 0, avgBalance: 0, bounces: 0, inwardPct: 0, outwardPct: 0 }]); }
  function removeBankingRow(key) { setBankingRows((prev) => prev.filter((r) => r.key !== key)); }
  function setBankField(key, field, value) { setBankingRows((prev) => prev.map((r) => (r.key === key ? { ...r, [field]: value } : r))); }

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
            <button style={ghostBtnStyle}>Save draft</button>
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
              <span style={{ fontSize: 13, fontWeight: 700, letterSpacing: '.08em' }}>07</span>
              <span style={{ fontSize: 16, fontWeight: 700 }}>STAGE 6 — ELIGIBILITY</span>
              <span style={{ fontSize: 10.5, fontWeight: 500, letterSpacing: '.09em', textTransform: 'uppercase', color: COLORS.textMuted }}>Stage 6 of 8</span>
            </div>

            <div role="tablist" style={{ display: 'flex', alignItems: 'flex-end', borderBottom: '1px solid #c2c2c2', width: '100%', flexWrap: 'wrap' }}>
              {['Borrower Classification', 'Existing Loan', 'Banking', 'CAM'].map((t) => {
                const active = activeTab === t;
                return (
                  <button key={t} role="tab" aria-selected={active} onClick={() => setActiveTab(t)}
                    style={active
                      ? { padding: '8px 13px', border: '1px solid #9e9e9e', borderBottom: '1px solid #fff', background: '#fff', marginBottom: -1, fontSize: 11.5, fontWeight: 700, color: '#1c1c1c', cursor: 'pointer' }
                      : { padding: '8px 13px', border: '1px solid #dcdcdc', borderBottom: 'none', background: '#f4f4f4', fontSize: 11.5, fontWeight: 500, color: '#8b8b8b', cursor: 'pointer' }}>
                    {t}
                  </button>
                );
              })}
            </div>

            <div style={{ display: 'flex', gap: 14, alignItems: 'flex-start', flexWrap: 'wrap' }}>
              <div style={{ flex: 1, minWidth: 300 }}>
                {activeTab === 'Borrower Classification' && <ClassificationTab data={classification} onChange={setClassificationField} />}
                {activeTab === 'Existing Loan' && <ExistingLoanTab rows={existingLoans} parties={visibleParties} onAdd={addExistingLoan} onRemove={removeExistingLoan} onChange={setLoanField} />}
                {activeTab === 'Banking' && <BankingTab rows={bankingRows} parties={visibleParties} onAdd={addBankingRow} onRemove={removeBankingRow} onChange={setBankField} />}
                {activeTab === 'CAM' && <CamTab />}
              </div>

              <EligiblePanel
                eligible={eligible} deviationPct={deviationPct} foir={foirAtEligible} ltv={LTV_PCT}
                foirCapAmount={foirCapAmount} ltvCapAmount={ltvCapAmount} bindingLabel={bindingLabel}
                approverNote={approverNote} onNoteChange={handleNoteChange} noteStale={noteStale}
              />
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

function Panel({ title, right, children }) {
  return (
    <div style={{ border: '1px solid #d4d4d4', background: '#fff' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 11px', borderBottom: '1px solid #d4d4d4', background: '#f2f2f2', flexWrap: 'wrap', gap: 8 }}>
        <span style={sectionLabelStyle}>{title}</span>
        {right}
      </div>
      <div style={{ padding: '13px 12px' }}>{children}</div>
    </div>
  );
}

function ClassificationTab({ data, onChange }) {
  const pslIsYes = data.psl.indexOf('Yes') === 0;
  function field(label, inner) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 4, minWidth: 0 }}>
        <span style={fieldLabelStyle}>{label}</span>
        {inner}
      </div>
    );
  }
  return (
    <Panel title="Borrower classification">
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))', gap: '12px 14px' }}>
        {field('PSL classification', (
          <select value={data.psl} onChange={(e) => onChange('psl', e.target.value)} style={selectStyle}>
            {['Yes - Priority Sector', 'No - Non-Priority Sector'].map((o) => <option key={o}>{o}</option>)}
          </select>
        ))}
        {pslIsYes && field('PSL sub-category', (
          <select value={data.pslSub} onChange={(e) => onChange('pslSub', e.target.value)} style={selectStyle}>
            {['Transport Sector - CV', 'Agriculture', 'MSME', 'Other'].map((o) => <option key={o}>{o}</option>)}
          </select>
        ))}
        {field('Risk sharing %', <input type="number" value={data.riskSharing} onChange={(e) => onChange('riskSharing', Number(e.target.value) || 0)} style={inputStyle} />)}
        {field('Co-lending partner', (
          <select value={data.coLendingPartner} onChange={(e) => onChange('coLendingPartner', e.target.value)} style={selectStyle}>
            {['None', 'HDFC Bank', 'ICICI Bank', 'State Bank of India', 'Axis Bank'].map((o) => <option key={o}>{o}</option>)}
          </select>
        ))}
        {field('End use of funds', (
          <select value={data.endUse} onChange={(e) => onChange('endUse', e.target.value)} style={selectStyle}>
            {['Fleet expansion', 'Vehicle replacement', 'New business setup', 'Working capital', 'Other'].map((o) => <option key={o}>{o}</option>)}
          </select>
        ))}
        {pslIsYes && field('Priority sector amount', <input type="number" value={data.prioritySectorAmount} onChange={(e) => onChange('prioritySectorAmount', Number(e.target.value) || 0)} style={inputStyle} />)}
      </div>
    </Panel>
  );
}

// [IMPROVEMENT #4, #6] rows grouped under a party sub-header instead of one
// flat mixed table — this is what actually solves "whose row is whose" (#6),
// rather than a decorative per-party color scheme, which the project's own
// "functional color only" rule argues against for something this cosmetic.
function groupByParty(rows, parties) {
  return parties.map((p) => ({ party: p, rows: rows.filter((r) => r.party === p) })).filter((g) => g.rows.length > 0 || true);
}

function ExistingLoanTab({ rows, parties, onAdd, onRemove, onChange }) {
  const cols = ['Lender', 'Loan type', 'Sanctioned', 'ROI %', 'POS', 'EMI', 'Max DPD', 'Bounces (12m)', 'RTR', ''];
  const groups = groupByParty(rows, parties);
  return (
    <Panel title="Existing loan" right={<button onClick={onAdd} style={smallGhostBtnStyle}>+ Add existing loan</button>}>
      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: 820 }}>
          <thead><tr style={{ background: '#f7f7f7' }}><th style={thStyle}>Applicant</th>{cols.map((c) => <th key={c} style={thStyle}>{c}</th>)}</tr></thead>
          <tbody>
            {groups.map((g) => (
              <React.Fragment key={g.party}>
                {g.rows.length > 0 && (
                  <tr style={{ background: '#fafbfc' }}>
                    <td colSpan={cols.length + 1} style={{ padding: '5px 7px', fontSize: 9.5, fontWeight: 700, letterSpacing: '.06em', textTransform: 'uppercase', color: COLORS.textSecondary, borderBottom: '1px solid #e4e4e4', borderTop: '1px solid #e4e4e4' }}>{g.party}</td>
                  </tr>
                )}
                {g.rows.map((r) => (
                  <tr key={r.key} style={{ borderBottom: '1px solid #eceef1' }} onMouseEnter={(e) => (e.currentTarget.style.background = COLORS.rowHover)} onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}>
                    <td style={{ padding: '5px 7px' }}>
                      <select aria-label={'Applicant for loan at ' + (r.lender || 'unnamed lender')} value={r.party} onChange={(e) => onChange(r.key, 'party', e.target.value)} style={selectStyleSm}>
                        {parties.map((p) => <option key={p}>{p}</option>)}
                      </select>
                    </td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Lender for ' + r.party + "'s loan"} value={r.lender} onChange={(e) => onChange(r.key, 'lender', e.target.value)} style={{ ...cellInputStyle, width: 90 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Loan type for ' + r.party + "'s loan"} value={r.loanType} onChange={(e) => onChange(r.key, 'loanType', e.target.value)} style={{ ...cellInputStyle, width: 100 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Sanctioned amount for ' + r.party + "'s loan"} type="number" value={r.sanctioned} onChange={(e) => onChange(r.key, 'sanctioned', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 80 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'ROI for ' + r.party + "'s loan"} type="number" value={r.roi} onChange={(e) => onChange(r.key, 'roi', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 50 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Principal outstanding for ' + r.party + "'s loan"} type="number" value={r.pos} onChange={(e) => onChange(r.key, 'pos', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 80 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'EMI for ' + r.party + "'s loan"} type="number" value={r.emi} onChange={(e) => onChange(r.key, 'emi', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 75, fontWeight: 600 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Max DPD for ' + r.party + "'s loan"} type="number" value={r.maxDpd} onChange={(e) => onChange(r.key, 'maxDpd', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 55 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Bounces in last 12 months for ' + r.party + "'s loan"} type="number" value={r.bounces} onChange={(e) => onChange(r.key, 'bounces', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 55 }} /></td>
                    <td style={{ padding: '5px 7px' }}>
                      <select aria-label={'Repayment track record for ' + r.party + "'s loan"} value={r.rtr} onChange={(e) => onChange(r.key, 'rtr', e.target.value)} style={selectStyleSm}>
                        {['Regular', 'Irregular', 'Overdue'].map((o) => <option key={o}>{o}</option>)}
                      </select>
                    </td>
                    <td style={{ padding: '5px 7px' }}><button aria-label={'Remove ' + r.party + "'s loan at " + (r.lender || 'unnamed lender')} onClick={() => onRemove(r.key)} style={removeBtnStyle}>×</button></td>
                  </tr>
                ))}
                {g.rows.length > 0 && (
                  <tr style={{ borderBottom: '2px solid #d4d4d4' }}>
                    <td colSpan={6} style={{ padding: '5px 7px', fontSize: 10.5, color: COLORS.textMuted, textAlign: 'right' }}>Subtotal EMI</td>
                    <td style={{ padding: '5px 7px', fontSize: 10.5, fontWeight: 700, color: COLORS.textPrimary }}>{fmtINR(g.rows.reduce((s, r) => s + r.emi, 0))}</td>
                    <td colSpan={4}></td>
                  </tr>
                )}
              </React.Fragment>
            ))}
          </tbody>
        </table>
      </div>
    </Panel>
  );
}

function BankingTab({ rows, parties, onAdd, onRemove, onChange }) {
  const cols = ['Bank', 'Months reviewed', 'Avg. balance', 'Cheque bounces', 'Inward txn %', 'Outward txn %', ''];
  const groups = groupByParty(rows, parties);
  return (
    <Panel title="Banking" right={<button onClick={onAdd} style={smallGhostBtnStyle}>+ Add bank</button>}>
      {/* [IMPROVEMENT #2] explicit — this data does not feed the eligible-amount calculation */}
      <div style={{ fontSize: 10.5, color: COLORS.textMuted, marginBottom: 10, fontStyle: 'italic' }}>Informational only — does not affect the eligible amount calculation.</div>
      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: 640 }}>
          <thead><tr style={{ background: '#f7f7f7' }}><th style={thStyle}>Applicant</th>{cols.map((c) => <th key={c} style={thStyle}>{c}</th>)}</tr></thead>
          <tbody>
            {groups.map((g) => (
              <React.Fragment key={g.party}>
                {g.rows.length > 0 && (
                  <tr style={{ background: '#fafbfc' }}>
                    <td colSpan={cols.length + 1} style={{ padding: '5px 7px', fontSize: 9.5, fontWeight: 700, letterSpacing: '.06em', textTransform: 'uppercase', color: COLORS.textSecondary, borderBottom: '1px solid #e4e4e4', borderTop: '1px solid #e4e4e4' }}>{g.party}</td>
                  </tr>
                )}
                {g.rows.map((r) => (
                  <tr key={r.key} style={{ borderBottom: '1px solid #eceef1' }} onMouseEnter={(e) => (e.currentTarget.style.background = COLORS.rowHover)} onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}>
                    <td style={{ padding: '5px 7px' }}>
                      <select aria-label={'Applicant for banking record at ' + (r.bank || 'unnamed bank')} value={r.party} onChange={(e) => onChange(r.key, 'party', e.target.value)} style={selectStyleSm}>
                        {parties.map((p) => <option key={p}>{p}</option>)}
                      </select>
                    </td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Bank for ' + r.party} value={r.bank} onChange={(e) => onChange(r.key, 'bank', e.target.value)} style={{ ...cellInputStyle, width: 100 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Months reviewed for ' + r.party} type="number" value={r.months} onChange={(e) => onChange(r.key, 'months', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 60 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Average balance for ' + r.party} type="number" value={r.avgBalance} onChange={(e) => onChange(r.key, 'avgBalance', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 85 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Cheque bounces for ' + r.party} type="number" value={r.bounces} onChange={(e) => onChange(r.key, 'bounces', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 60 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Inward transaction percentage for ' + r.party} type="number" value={r.inwardPct} onChange={(e) => onChange(r.key, 'inwardPct', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 60 }} /></td>
                    <td style={{ padding: '5px 7px' }}><input aria-label={'Outward transaction percentage for ' + r.party} type="number" value={r.outwardPct} onChange={(e) => onChange(r.key, 'outwardPct', Number(e.target.value) || 0)} style={{ ...cellInputStyle, width: 60 }} /></td>
                    <td style={{ padding: '5px 7px' }}><button aria-label={'Remove banking record for ' + r.party + ' at ' + (r.bank || 'unnamed bank')} onClick={() => onRemove(r.key)} style={removeBtnStyle}>×</button></td>
                  </tr>
                ))}
              </React.Fragment>
            ))}
          </tbody>
        </table>
      </div>
    </Panel>
  );
}

function CamTab() {
  function row(label, val) {
    return <tr style={{ borderBottom: '1px solid #eceef1' }}><td style={{ padding: '6px 8px', fontSize: 11.5, color: '#3b4453' }}>{label}</td><td style={{ padding: '6px 8px', textAlign: 'right', fontSize: 11.5, fontWeight: 600, color: COLORS.textPrimary }}>{val}</td></tr>;
  }
  const tiles = [
    ['EMI', fmtINR(calcEMI(REQUESTED_AMOUNT, ROI, TENURE))],
    ['ROI', ROI + '%'],
    ['Term', TENURE + ' mo'],
    ['LTV', LTV_PCT.toFixed(0) + '%'],
  ];
  return (
    <Panel title="CAM recap (read-only)" right={<span style={linkStyle}>Open CAM</span>}>
      <div style={{ display: 'flex', gap: 14, flexWrap: 'wrap' }}>
        <table style={{ flex: 1, minWidth: 260, borderCollapse: 'collapse' }}>
          <tbody>
            {row('On-road cost', fmtINR(ON_ROAD_COST))}
            {row('Loan amount', fmtINR(REQUESTED_AMOUNT))}
            {row('LTV on on-road cost', LTV_PCT.toFixed(0) + '%')}
          </tbody>
        </table>
        <div style={{ flex: '0 0 220px', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 9 }}>
          {tiles.map(([label, value]) => (
            <div key={label} style={{ border: '1px solid #b6b6b6', background: '#efefef', padding: 8 }}>
              <div style={{ fontSize: 8.5, fontWeight: 600, letterSpacing: '.08em', textTransform: 'uppercase', color: '#7d7d7d' }}>{label}</div>
              <div style={{ fontSize: 14, fontWeight: 700, color: '#1f1f1f' }}>{value}</div>
            </div>
          ))}
        </div>
      </div>
    </Panel>
  );
}

function EligiblePanel({ eligible, deviationPct, foir, ltv, foirCapAmount, ltvCapAmount, bindingLabel, approverNote, onNoteChange, noteStale }) {
  function drow(label, val, warn) {
    return <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 11 }}><span style={{ color: '#8d8d8d' }}>{label}</span><span style={{ fontWeight: 600, color: warn ? COLORS.risk.text : '#333' }}>{val}</span></div>;
  }
  return (
    <div style={{ flex: '0 0 250px', border: '1px solid #d4d4d4', background: '#fff', alignSelf: 'flex-start' }}>
      <div style={{ padding: '8px 11px', borderBottom: '1px solid #d4d4d4', background: '#f2f2f2' }}><span style={sectionLabelStyle}>Eligible amount</span></div>
      <div style={{ padding: '13px 12px' }}>
        <div style={{ fontSize: 22, fontWeight: 700, color: '#1f1f1f' }}>{fmtINR(eligible)}</div>
        {/* [IMPROVEMENT #1] which cap actually bound the result */}
        {bindingLabel && <div style={{ fontSize: 10, color: COLORS.risk.text, marginTop: 2 }}>Capped by {bindingLabel}</div>}
        <div style={{ height: 1, background: '#dedede', margin: '10px 0' }} />
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {drow('Requested amount', fmtINR(REQUESTED_AMOUNT))}
          {drow('Deviation %', deviationPct.toFixed(1) + '%', deviationPct < 0)}
          {drow('FOIR %', foir.toFixed(0) + '%')}
          {drow('LTV %', ltv.toFixed(0) + '%')}
        </div>
        {/* [IMPROVEMENT #3] intermediate caps, not just the winning number */}
        <div style={{ marginTop: 8, paddingTop: 8, borderTop: '1px solid #eceef1', display: 'flex', flexDirection: 'column', gap: 4 }}>
          <span style={{ fontSize: 9.5, fontWeight: 600, letterSpacing: '.08em', textTransform: 'uppercase', color: '#8d8d8d' }}>How this was capped</span>
          {drow('FOIR allows up to', fmtINR(foirCapAmount))}
          {drow('LTV allows up to', fmtINR(ltvCapAmount))}
        </div>

        {deviationPct < 0 ? (
          <div style={{ marginTop: 8, paddingTop: 8, borderTop: '1px solid #eceef1', display: 'flex', flexDirection: 'column', gap: 4 }}>
            <span style={{ fontSize: 9.5, fontWeight: 600, color: COLORS.risk.text }}>Approver note (required — negative deviation)</span>
            <textarea value={approverNote} onChange={(e) => onNoteChange(e.target.value)} rows={2} style={{ fontSize: 10.5, border: '1px solid ' + COLORS.border, padding: 5, resize: 'vertical' }} />
            {/* [IMPROVEMENT #5] flags when the note was written against a different deviation than the current one */}
            {noteStale && <span style={{ fontSize: 10, color: COLORS.caution.text }}>⚠ Written at a different deviation than now — review before relying on it</span>}
          </div>
        ) : (
          // [IMPROVEMENT #8] note doesn't just silently vanish — a trace stays visible
          approverNote.trim() && (
            <div style={{ marginTop: 8, paddingTop: 8, borderTop: '1px solid #eceef1' }}>
              <span style={{ fontSize: 10, color: COLORS.textMuted, fontStyle: 'italic' }}>Approver note on file (not required while eligible ≥ requested)</span>
            </div>
          )
        )}
      </div>
    </div>
  );
}

function Stepper({ current }) {
  return (
    <div style={{ display: 'flex', gap: 5, alignItems: 'stretch', width: '100%', flexWrap: 'wrap' }}>
      {STEP_LABELS.map((label, i) => {
        const num = i + 1, numStr = (num < 10 ? '0' : '') + num;
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
        {row('Loan amount', fmtINR(REQUESTED_AMOUNT), true)}
        <div style={{ display: 'flex', gap: 10 }}><div style={{ flex: 1 }}>{row('Tenure', TENURE + ' mo')}</div><div style={{ flex: 1 }}>{row('ROI', ROI + '%')}</div></div>
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
const fieldLabelStyle = { fontSize: 10.5, fontWeight: 600, letterSpacing: '.08em', textTransform: 'uppercase', color: '#7d7d7d' };
const sectionLabelStyle = { fontSize: 10, fontWeight: 600, letterSpacing: '.13em', textTransform: 'uppercase', color: '#4d4d4d' };
const inputStyle = { height: 29, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 12, background: '#fafbfc', padding: '0 8px', width: '100%' };
const selectStyle = { height: 29, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 12, background: '#fafbfc', padding: '0 6px', width: '100%' };
const selectStyleSm = { height: 26, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 10.5, background: '#fafbfc', padding: '0 4px' };
const cellInputStyle = { height: 26, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 10.5, padding: '0 5px' };
const removeBtnStyle = { height: 24, width: 24, border: '1px solid ' + COLORS.border, background: '#fff', color: COLORS.risk.text, fontSize: 12, cursor: 'pointer' };
const ghostBtnStyle = { height: 28, border: '1px solid ' + COLORS.borderStrong, background: '#ebebeb', borderRadius: 0, padding: '0 12px', fontSize: 11, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#3d3d3d', cursor: 'pointer' };
const smallGhostBtnStyle = { fontSize: 10, fontWeight: 600, color: '#3d3d3d', background: '#ebebeb', border: '1px solid #9e9e9e', padding: '4px 9px', cursor: 'pointer' };
const primaryBtnStyle = { height: 28, border: '1px solid #16304f', background: COLORS.accent, borderRadius: 0, padding: '0 12px', fontSize: 11, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#fff', cursor: 'pointer' };
const primaryBtnStyleBlocked = { height: 28, border: '1px solid ' + COLORS.borderStrong, background: '#e4e6e9', borderRadius: 0, padding: '0 12px', fontSize: 11, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#a7adb5', cursor: 'not-allowed' };
const thStyle = { textAlign: 'left', padding: '6px 7px', fontSize: 9, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#8d8d8d', borderBottom: '1px solid #d4d4d4', whiteSpace: 'nowrap' };
const linkStyle = { fontSize: 10, color: '#6f6f6f', textDecoration: 'underline', cursor: 'pointer' };
