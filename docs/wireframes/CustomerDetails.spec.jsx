/*
  CUSTOMER DETAILS — STAGE 1 OF 8 — REFERENCE SPEC v1
  =====================================================================
  Target stack: Blazor Server on .NET 8 + MySQL (same correction as the
  Applications Dashboard spec). Markup/styles port ~1:1 into a .razor
  component; comments below flag only what genuinely needs translating.

  [Blazor] This becomes a nested route under the application shell, e.g.
  Pages/Applications/Stages/CustomerDetails.razor
  (@page "/applications/{ApplicationId}/customer-details").
  Screens 03-09 will reuse the same stepper/summary-rail/sub-header shell —
  strongly consider extracting those three into their own .razor components
  now (StageStepper, ApplicationSummaryRail, StageSubHeader) rather than
  copy-pasting them into all 9 stage pages. This file keeps them inline
  for readability as a spec, but the real build should not.

  *** IMPORTANT DATA-MODEL NOTE — read before wiring this to real data ***
  The three party tabs (Applicant / Co-Applicant / Guarantor) are drawn
  from ONE shared field state below purely because this is a visual spec
  and gap #2 confirmed all three tabs use identical section structure.
  That does NOT mean the real implementation should use one shared set of
  fields with a tab switch — each party needs their OWN stored KYC record.
  The backing model needs three distinct data objects (e.g. a `PartyDetails`
  table/class keyed by PartyType: Applicant/CoApplicant/Guarantor), and
  switching tabs should load/save the record for whichever party is active.
  Treating this literally as "one form, three tab buttons" would silently
  overwrite one party's data with another's. Flagging this explicitly
  because it's the kind of thing that's easy to build wrong by mechanically
  copying the mockup's behavior.

  Dummy data note: SAMPLE_APPLICATION and all field values are placeholders
  for visual reference only — wire to the real Applications / Parties
  tables in the actual build.
*/

import React, { useState } from 'react';

// ---------------------------------------------------------------------------
// DESIGN TOKENS — same palette as the Applications Dashboard spec.
// ---------------------------------------------------------------------------
const COLORS = {
  border: '#d8dbe1',
  borderStrong: '#b9bfc9',
  textPrimary: '#1a1f29',
  textSecondary: '#5b6472',
  textMuted: '#93999a',
  accent: '#1f3a5f',
  accentHover: '#16304f',
  pass: { bg: '#d9ecd7', text: '#1f5c2a' },
  fail: { bg: '#f8d7d7', text: '#8a1f1f' },
};

const FONT = "'IBM Plex Sans', Arial, sans-serif";

const STEP_LABELS = ['Customer Details', 'Loan & Security', 'Bank & Financial', 'Document Checklist', 'Reports', 'Eligibility', 'Approvals', 'Post Sanction'];
const CURRENT_STEP = 1; // this screen is always step 1; screens 03-09 pass their own step number into the shared stepper

const SAMPLE_APPLICATION = { // dummy — replace with the real application record, keyed by ApplicationId from the route
  id: 'LN-2026-004871',
  customerType: 'Individual · CV',
  branch: 'Nashik West',
  loanProduct: 'Commercial Vehicle',
  scheme: 'CV-STD-2026',
  loanAmount: '₹18,50,000',
  tenure: '48 mo',
  roi: '13.25%',
};

// [IMPROVEMENT — flag for Arun/client] Per the brief, only the Dedupe check
// gates "Complete stage". Video KYC has no such note. Worth confirming that's
// intentional — in most lending compliance flows, an unfinished Video KYC
// would also block sanction somewhere downstream, even if not at this exact
// button. Built exactly as specified (dedupe-only gate) pending confirmation.
const PARTY_TABS = ['Applicant', 'Co-Applicant', 'Guarantor'];

// Field definitions — options marked (proposed) are placeholders pending
// your confirmation (see the brief's "gap #3" / Customer category note).
const PERSONAL_FIELDS = [
  { key: 'fullName', label: 'Full name (as per PAN)', type: 'text' },
  { key: 'dob', label: 'Date of birth', type: 'date' },
  { key: 'gender', label: 'Gender', type: 'select', options: ['Male', 'Female', 'Other'] },
  { key: 'maritalStatus', label: 'Marital status', type: 'select', options: ['Single', 'Married', 'Other'] },
  { key: 'fatherSpouseName', label: 'Father / spouse name', type: 'text' },
  { key: 'customerCategory', label: 'Customer category', type: 'select', options: ['Individual', 'Proprietorship', 'Partnership firm', 'Private limited company', 'HUF'] }, // (proposed — confirm)
  { key: 'nationality', label: 'Nationality', type: 'select', options: ['Indian', 'Other'] },
  { key: 'motherTongue', label: 'Mother tongue', type: 'select', options: ['Marathi', 'Hindi', 'English', 'Other'] },
];

const CONTACT_FIELDS = [
  { key: 'mobile', label: 'Mobile', type: 'tel', verified: true },
  { key: 'altMobile', label: 'Alternate mobile', type: 'tel' },
  { key: 'email', label: 'Email', type: 'email' },
  { key: 'address1', label: 'Address line 1', type: 'text' },
  { key: 'address2', label: 'Address line 2', type: 'text' },
  { key: 'city', label: 'City', type: 'text' },
  { key: 'state', label: 'State', type: 'select', options: ['Maharashtra', 'Other'] },
  { key: 'pinCode', label: 'PIN code', type: 'text' },
  { key: 'residenceType', label: 'Residence type', type: 'select', options: ['Owned', 'Rented', 'Company provided'] },
  { key: 'yearsAtAddress', label: 'Years at address', type: 'number' },
];

const EMPLOYMENT_FIELDS = [ // count matches the approved wireframe's "6 fields" label — treating as confirmed
  { key: 'employmentType', label: 'Employment type', type: 'select', options: ['Salaried', 'Self-employed', 'Business owner'] },
  { key: 'employerName', label: 'Employer / Business name', type: 'text' },
  { key: 'designation', label: 'Designation / Nature of business', type: 'text' },
  { key: 'monthlyIncome', label: 'Monthly income', type: 'number' },
  { key: 'yearsInJob', label: 'Years in current job / business', type: 'number' },
  { key: 'officeAddress', label: 'Office / business address', type: 'text' },
];

// ---------------------------------------------------------------------------
// MAIN COMPONENT
// ---------------------------------------------------------------------------
export default function CustomerDetailsSpec() {
  const [devDedupeState, setDevDedupeState] = useState('pass'); // SPEC-ONLY — see note below, do not build this control
  const [activeTab, setActiveTab] = useState('Applicant');
  const [personalCollapsed, setPersonalCollapsed] = useState(false);
  const [contactCollapsed, setContactCollapsed] = useState(false);
  const [employmentCollapsed, setEmploymentCollapsed] = useState(true); // matches the approved wireframe's default state

  const completeBlocked = devDedupeState === 'fail';

  return (
    <div style={{ fontFamily: FONT, fontSize: 12, color: COLORS.textPrimary }}>

      {/* ===================================================================
          SPEC-ONLY CONTROL — lets you see both dedupe outcomes in one file.
          [Blazor] In the real build, DedupeState comes from a backend check
          result (e.g. Task<DedupeResult> run in OnInitializedAsync or
          triggered on save), never from a UI toggle. Delete this control.
      =================================================================== */}
      <div style={{ display: 'flex', gap: 8, padding: '10px 12px', border: '1px dashed ' + COLORS.borderStrong, marginBottom: 16, background: '#fff' }}>
        <span style={{ fontSize: 11, color: COLORS.textMuted, alignSelf: 'center' }}>Spec preview — dedupe gate:</span>
        {['pass', 'fail'].map((s) => (
          <button
            key={s}
            onClick={() => setDevDedupeState(s)}
            style={{ fontSize: 11, padding: '4px 10px', border: '1px solid ' + COLORS.border, borderRadius: 0, cursor: 'pointer', background: devDedupeState === s ? COLORS.accent : '#fff', color: devDedupeState === s ? '#fff' : COLORS.textSecondary }}
          >
            {s}
          </button>
        ))}
      </div>

      {/* ===================================================================
          APP SHELL — everything below this line is the real UI.
      =================================================================== */}
      <div style={{ border: '1px solid ' + COLORS.border, background: '#fff' }}>

        {/* Sub-header — [Blazor] extract as StageSubHeader.razor, reused on screens 03-09 */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '10px 14px', borderBottom: '1px solid ' + COLORS.border, background: '#f4f4f4', flexWrap: 'wrap', gap: 8 }}>
          <span style={{ fontSize: 11, fontWeight: 600, letterSpacing: '.1em', textTransform: 'uppercase', color: COLORS.textSecondary }}>
            ← Applications · APP #{SAMPLE_APPLICATION.id}
          </span>
          <div style={{ display: 'flex', gap: 8 }}>
            <button style={{ height: 28, border: '1px solid ' + COLORS.borderStrong, background: '#ebebeb', borderRadius: 0, padding: '0 12px', fontSize: 11, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase', color: '#3d3d3d', cursor: 'pointer' }}>
              Save draft
            </button>
            {/* [Blazor] disabled="@CompleteBlocked" where CompleteBlocked reads the real dedupe
                result, not this spec-only toggle. Keep the aria-disabled — the visual "blocked"
                look plus a real disabled attribute together, not styling alone, is what actually
                prevents a keyboard/screen-reader user from activating it. */}
            <button
              disabled={completeBlocked}
              aria-disabled={completeBlocked}
              style={{
                height: 28, border: '1px solid ' + (completeBlocked ? COLORS.borderStrong : '#16304f'),
                background: completeBlocked ? '#e4e6e9' : COLORS.accent, borderRadius: 0, padding: '0 12px',
                fontSize: 11, fontWeight: 600, letterSpacing: '.05em', textTransform: 'uppercase',
                color: completeBlocked ? '#a7adb5' : '#fff', cursor: completeBlocked ? 'not-allowed' : 'pointer',
              }}
            >
              Complete stage →
            </button>
          </div>
        </div>

        {/* Stepper — [Blazor] extract as StageStepper.razor, param Current (int) */}
        <div style={{ padding: '12px 14px', borderBottom: '1px solid #dcdcdc', background: '#fbfbfb' }}>
          <Stepper current={CURRENT_STEP} />
        </div>

        <div style={{ display: 'flex', alignItems: 'stretch', flexWrap: 'wrap' }}>
          {/* Left rail — [Blazor] extract as ApplicationSummaryRail.razor, param ApplicationId */}
          <div style={{ width: 230, flex: 'none', padding: 14, borderRight: '1px solid #dcdcdc', background: '#f7f7f7' }}>
            <SummaryRail app={SAMPLE_APPLICATION} />
          </div>

          <div style={{ flex: 1, minWidth: 280, padding: 16, display: 'flex', flexDirection: 'column', gap: 15 }}>

            <div style={{ display: 'flex', alignItems: 'baseline', gap: 12, borderBottom: '2px solid ' + COLORS.textPrimary, paddingBottom: 7, flexWrap: 'wrap' }}>
              <span style={{ fontSize: 13, fontWeight: 700, letterSpacing: '.08em' }}>02</span>
              <span style={{ fontSize: 16, fontWeight: 700 }}>STAGE 1 — CUSTOMER DETAILS</span>
              <span style={{ fontSize: 10.5, fontWeight: 500, letterSpacing: '.09em', textTransform: 'uppercase', color: COLORS.textMuted }}>Stage 1 of 8</span>
            </div>

            {/* Party tabs — [Blazor] @onclick sets ActiveParty; see the data-model note at
                the top of this file before wiring this to real per-party records. */}
            <div role="tablist" aria-label="Party" style={{ display: 'flex', alignItems: 'flex-end', borderBottom: '1px solid #c2c2c2', width: '100%' }}>
              {PARTY_TABS.map((t) => {
                const active = activeTab === t;
                return (
                  <button
                    key={t}
                    role="tab"
                    aria-selected={active}
                    onClick={() => setActiveTab(t)}
                    style={active
                      ? { padding: '8px 13px', border: '1px solid #9e9e9e', borderBottom: '1px solid #fff', background: '#fff', marginBottom: -1, fontSize: 11.5, fontWeight: 700, color: '#1c1c1c', whiteSpace: 'nowrap', cursor: 'pointer' }
                      : { padding: '8px 13px', border: '1px solid #dcdcdc', borderBottom: 'none', background: '#f4f4f4', fontSize: 11.5, fontWeight: 500, color: '#8b8b8b', whiteSpace: 'nowrap', cursor: 'pointer' }}
                  >
                    {t}
                  </button>
                );
              })}
            </div>

            {/* Personal information */}
            <CollapsibleSection
              title="Personal information"
              collapsed={personalCollapsed}
              onToggle={() => setPersonalCollapsed((v) => !v)}
            >
              <div style={{ display: 'flex', gap: 16, alignItems: 'flex-start', flexWrap: 'wrap' }}>
                <div style={{ flex: 'none', display: 'flex', flexDirection: 'column', gap: 5 }}>
                  {/* [Blazor] <InputFile OnChange="OnPhotoSelected" /> — or a camera-capture
                      component if live capture (not just upload) is actually required; the
                      brief says "capture/upload" so confirm which one(s) are truly needed. */}
                  <div style={{ width: 88, height: 104, border: '1px dashed #bdbdbd', background: '#f7f7f7', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 10, color: '#9a9a9a' }}>Photo</div>
                  <button style={{ height: 24, border: '1px solid #c4c4c4', background: '#f1f1f1', fontSize: 9.5, fontWeight: 500, color: '#6f6f6f', cursor: 'pointer' }}>Capture / upload</button>
                </div>
                <div style={{ flex: 1, minWidth: 220 }}>
                  <FieldGrid fields={PERSONAL_FIELDS} idPrefix="personal" />
                </div>
              </div>
            </CollapsibleSection>

            {/* Identity + Gates */}
            <div style={{ display: 'flex', gap: 14, alignItems: 'stretch', flexWrap: 'wrap' }}>
              <IdentityPanel />
              <GatesPanel dedupeState={devDedupeState} />
            </div>

            {/* Contact details */}
            <CollapsibleSection
              title="Contact details"
              collapsed={contactCollapsed}
              onToggle={() => setContactCollapsed((v) => !v)}
            >
              <FieldGrid fields={CONTACT_FIELDS} idPrefix="contact" />
            </CollapsibleSection>

            {/* Employment details */}
            <CollapsibleSection
              title="Employment details"
              collapsed={employmentCollapsed}
              onToggle={() => setEmploymentCollapsed((v) => !v)}
              summaryText={employmentCollapsed ? EMPLOYMENT_FIELDS.length + ' fields · complete' : null}
            >
              <FieldGrid fields={EMPLOYMENT_FIELDS} idPrefix="employment" />
            </CollapsibleSection>

          </div>
        </div>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// SUB-COMPONENTS
// ---------------------------------------------------------------------------

function Stepper({ current }) {
  return (
    <div style={{ display: 'flex', gap: 5, alignItems: 'stretch', width: '100%', flexWrap: 'wrap' }}>
      {STEP_LABELS.map((label, i) => {
        const num = i + 1;
        const numStr = (num < 10 ? '0' : '') + num;
        const done = num < current;
        const isCurrent = num === current;
        // [Blazor] aria-current="step" only applies to the current segment — carry this
        // through, it's the one accessibility hook a screen reader has for "where am I".
        if (done) {
          return (
            <div key={label} style={{ flex: 1, minWidth: 110, height: 40, border: '1px solid #b9bfc9', background: '#eceef1', padding: '0 9px', display: 'flex', flexDirection: 'column', justifyContent: 'center', gap: 2 }}>
              <span style={{ fontSize: 8.5, fontWeight: 600, letterSpacing: '.1em', color: '#5b6472' }}>✓ {numStr} DONE</span>
              <span style={{ fontSize: 10.5, fontWeight: 600, color: '#3b4453' }}>{label}</span>
            </div>
          );
        }
        if (isCurrent) {
          return (
            <div key={label} aria-current="step" style={{ flex: 1, minWidth: 110, height: 40, border: '1px solid #16304f', background: '#1f3a5f', padding: '0 9px', display: 'flex', flexDirection: 'column', justifyContent: 'center', gap: 2 }}>
              <span style={{ fontSize: 8.5, fontWeight: 600, letterSpacing: '.1em', color: '#cdd6e0' }}>● {numStr} CURRENT</span>
              <span style={{ fontSize: 10.5, fontWeight: 700, color: '#fff' }}>{label}</span>
            </div>
          );
        }
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

function SummaryRail({ app }) {
  return (
    <div style={{ width: '100%', border: '1px solid #d0d0d0', background: '#fafafa' }}>
      <div style={{ padding: '8px 11px', borderBottom: '1px solid #d0d0d0', background: '#efefef' }}>
        <span style={{ fontSize: 9.5, fontWeight: 600, letterSpacing: '.12em', textTransform: 'uppercase', color: '#5c5c5c' }}>Application summary</span>
      </div>
      <div style={{ padding: 11, display: 'flex', flexDirection: 'column', gap: 9 }}>
        <SummaryRow label="Customer type" value={app.customerType} />
        <SummaryRow label="Branch" value={app.branch} />
        <SummaryRow label="Loan product" value={app.loanProduct} />
        <SummaryRow label="Scheme" value={app.scheme} />
        <div style={{ height: 1, background: '#dedede' }} />
        <SummaryRow label="Loan amount" value={app.loanAmount} big />
        <div style={{ display: 'flex', gap: 10 }}>
          <div style={{ flex: 1 }}><SummaryRow label="Tenure" value={app.tenure} /></div>
          <div style={{ flex: 1 }}><SummaryRow label="ROI" value={app.roi} /></div>
        </div>
        <div style={{ height: 1, background: '#dedede' }} />
        <div style={{ display: 'flex', flexDirection: 'column', gap: 5 }}>
          <span style={{ fontSize: 9, fontWeight: 600, letterSpacing: '.09em', textTransform: 'uppercase', color: '#8d8d8d' }}>Quick actions</span>
          {/* [Blazor] "View CAM.pdf" — CAM = Credit Appraisal Memo; this almost certainly needs
              to open/download a generated PDF, so this button needs a real href/handler once
              CAM generation exists, not just a placeholder click target. */}
          <button style={{ height: 26, border: '1px solid #c4c4c4', background: '#f1f1f1', display: 'flex', alignItems: 'center', padding: '0 9px', fontSize: 10.5, fontWeight: 500, color: '#555', textAlign: 'left', cursor: 'pointer' }}>View CAM.pdf</button>
          <button style={{ height: 26, border: '1px solid #c4c4c4', background: '#f1f1f1', display: 'flex', alignItems: 'center', padding: '0 9px', fontSize: 10.5, fontWeight: 500, color: '#555', textAlign: 'left', cursor: 'pointer' }}>Activity log</button>
        </div>
      </div>
    </div>
  );
}

function SummaryRow({ label, value, big }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <span style={{ fontSize: 9, fontWeight: 600, letterSpacing: '.09em', textTransform: 'uppercase', color: '#8d8d8d' }}>{label}</span>
      <span style={{ fontSize: big ? 15 : 11.5, fontWeight: big ? 600 : 500, color: '#333' }}>{value}</span>
    </div>
  );
}

function CollapsibleSection({ title, collapsed, onToggle, summaryText, children }) {
  // [Blazor] a private bool per section (PersonalCollapsed, ContactCollapsed,
  // EmploymentCollapsed) + @onclick="() => Toggle(...)" on the header, @if around the body.
  return (
    <div style={{ border: '1px solid #d4d4d4', background: '#fff' }}>
      <button
        onClick={onToggle}
        aria-expanded={!collapsed}
        style={{ width: '100%', display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 11px', border: 'none', borderBottom: collapsed ? 'none' : '1px solid #d4d4d4', background: '#f2f2f2', cursor: 'pointer', textAlign: 'left' }}
      >
        <span style={{ fontSize: 10.5, fontWeight: 600, letterSpacing: '.1em', textTransform: 'uppercase', color: '#4d4d4d' }}>{title}</span>
        <span style={{ fontSize: 11, color: '#93999a' }}>
          {collapsed ? '▸ expand' + (summaryText ? ' · ' + summaryText : '') : '▾ collapse'}
        </span>
      </button>
      {!collapsed && <div style={{ padding: '13px 12px' }}>{children}</div>}
    </div>
  );
}

function FieldGrid({ fields, idPrefix }) {
  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))', gap: '12px 14px', width: '100%' }}>
      {fields.map((f) => {
        const inputId = idPrefix + '-' + f.key;
        return (
          <div key={f.key} style={{ display: 'flex', flexDirection: 'column', gap: 4, minWidth: 0 }}>
            <label htmlFor={inputId} style={{ fontSize: 10.5, fontWeight: 600, letterSpacing: '.08em', textTransform: 'uppercase', color: '#7d7d7d', display: 'flex', alignItems: 'center', gap: 5 }}>
              {f.label}
              {f.verified && (
                <span style={{ fontSize: 9, fontWeight: 600, color: '#1f5c2a', background: '#d9ecd7', padding: '1px 5px', textTransform: 'none', letterSpacing: 0 }}>Verified</span>
              )}
            </label>
            {f.type === 'select' ? (
              <select id={inputId} style={{ height: 29, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 12, background: '#fafbfc', padding: '0 6px', width: '100%' }}>
                {f.options.map((o) => <option key={o}>{o}</option>)}
              </select>
            ) : (
              <input id={inputId} type={f.type} style={{ height: 29, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 12, background: '#fafbfc', padding: '0 8px', width: '100%' }} />
            )}
          </div>
        );
      })}
    </div>
  );
}

function IdentityPanel() {
  return (
    <div style={{ flex: '1 1 260px', border: '1px solid #d4d4d4', background: '#fff' }}>
      <div style={{ padding: '8px 11px', borderBottom: '1px solid #d4d4d4', background: '#f2f2f2' }}>
        <span style={{ fontSize: 10, fontWeight: 600, letterSpacing: '.1em', textTransform: 'uppercase', color: '#4d4d4d' }}>Identity · OCR verify</span>
      </div>
      <div style={{ padding: '13px 12px', display: 'flex', flexDirection: 'column', gap: 11 }}>
        <IdentityField label="PAN" />
        <IdentityField label="Aadhaar" />
        <div style={{ height: 1, background: '#e4e4e4' }} />
        <div style={{ display: 'flex', gap: 9, flexWrap: 'wrap' }}>
          {/* [Blazor] <InputFile> x2. Brief calls these "scan" uploads — confirm whether
              that means a photo/scan file upload (most likely) or an actual scanner-device
              integration; built here as file upload, which is almost certainly right. */}
          <div style={{ flex: '1 1 100px', height: 30, border: '1px dashed #bdbdbd', background: '#f7f7f7', display: 'flex', alignItems: 'center', padding: '0 10px', fontSize: 10.5, color: '#8d8d8d' }}>PAN card scan</div>
          <div style={{ flex: '1 1 100px', height: 30, border: '1px dashed #bdbdbd', background: '#f7f7f7', display: 'flex', alignItems: 'center', padding: '0 10px', fontSize: 10.5, color: '#8d8d8d' }}>Aadhaar scan</div>
        </div>
      </div>
    </div>
  );
}

function IdentityField({ label }) {
  const inputId = 'identity-' + label.toLowerCase();
  return (
    <div style={{ display: 'flex', gap: 9, alignItems: 'flex-end' }}>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 4 }}>
        <label htmlFor={inputId} style={{ fontSize: 10.5, fontWeight: 600, letterSpacing: '.08em', textTransform: 'uppercase', color: '#7d7d7d' }}>{label}</label>
        <input id={inputId} type="text" style={{ height: 29, border: '1px solid ' + COLORS.border, borderRadius: 0, fontSize: 12, background: '#fafbfc', padding: '0 8px' }} />
      </div>
      {/* [Blazor] per the brief, this call is expected to pre-fill the Personal section
          fields on success — no need to build that wiring here, just don't build this as
          a dead-end button; the real handler needs to write into the Personal field state. */}
      <button style={{ height: 29, border: '1px solid #9e9e9e', background: '#ebebeb', padding: '0 11px', fontSize: 10, fontWeight: 600, letterSpacing: '.07em', textTransform: 'uppercase', color: '#3d3d3d', whiteSpace: 'nowrap', cursor: 'pointer' }}>
        Verify OCR
      </button>
    </div>
  );
}

function GatesPanel({ dedupeState }) {
  const dc = dedupeState === 'pass' ? COLORS.pass : COLORS.fail;
  const dedupeText = dedupeState === 'pass' ? '✓ No match found — clear to proceed' : '✕ Potential match found — review required';
  return (
    <div style={{ flex: '1 1 240px', border: '1px solid #d4d4d4', background: '#fff' }}>
      <div style={{ padding: '8px 11px', borderBottom: '1px solid #d4d4d4', background: '#f2f2f2' }}>
        <span style={{ fontSize: 10, fontWeight: 600, letterSpacing: '.1em', textTransform: 'uppercase', color: '#4d4d4d' }}>Gates</span>
      </div>
      <div style={{ padding: '13px 12px', display: 'flex', flexDirection: 'column', gap: 10 }}>
        {/* [Blazor] system-generated per the brief — render from a DedupeResult the server
            already computed, never from a form input. */}
        <div style={{ border: '1px solid ' + dc.text, background: dc.bg, padding: '9px 10px', display: 'flex', flexDirection: 'column', gap: 3 }}>
          <span style={{ fontSize: 9, fontWeight: 600, letterSpacing: '.09em', textTransform: 'uppercase', color: dc.text }}>Dedupe check</span>
          <span style={{ fontSize: 12, fontWeight: 600, color: dc.text }}>{dedupeText}</span>
          <span style={{ fontSize: 10, color: '#8a8a8a' }}>Blocks stage completion when a match exists</span>
        </div>
        <div style={{ border: '1px dashed #bdbdbd', background: '#f7f7f7', padding: '9px 10px', display: 'flex', flexDirection: 'column', gap: 5 }}>
          <span style={{ fontSize: 9, fontWeight: 600, letterSpacing: '.09em', textTransform: 'uppercase', color: '#6f6f6f' }}>Video KYC</span>
          <span style={{ fontSize: 10.5, color: '#7a7a7a' }}>Not initiated · agent-assisted call</span>
          {/* [Blazor / browser-API note] "Start video KYC" almost certainly needs to launch
              a real-time video call (WebRTC or a third-party SDK like Agora/Twilio). That's
              a browser-native capability C# can't drive directly — plan on JS interop or an
              embedded iframe to whatever video vendor gets picked, not a server round trip. */}
          <button style={{ height: 26, border: '1px solid #9e9e9e', background: '#ebebeb', fontSize: 10, fontWeight: 600, letterSpacing: '.07em', textTransform: 'uppercase', color: '#3d3d3d', cursor: 'pointer' }}>
            Start video KYC
          </button>
        </div>
      </div>
    </div>
  );
}
