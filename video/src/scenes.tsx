import React from 'react';
import {theme} from './theme';
import {Scene, Eyebrow, Title, StatTile, StackedBar, IssueChip, Rise} from './components';

export const TitleScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.series1}>ClearMeasureLabs / bootcamp-palermo-workorders</Eyebrow>
    </Rise>
    <Rise delay={8}>
      <Title size={104}>Qodana Remediation</Title>
    </Rise>
    <Rise delay={16}>
      <div style={{fontSize: 46, color: theme.inkSecondary, marginTop: 18, fontWeight: 500}}>
        A day on the board &mdash; 18 August 2026
      </div>
    </Rise>
    <Rise delay={26}>
      <div style={{marginTop: 46, height: 4, width: 340, background: theme.series1, borderRadius: 4}} />
    </Rise>
    <Rise delay={34}>
      <div style={{fontSize: 27, color: theme.inkMuted, marginTop: 34, maxWidth: 1040, lineHeight: 1.5}}>
        23 work items filed, 11 driven to Functional Testing, 2 defects found and fixed
        along the way.
      </div>
    </Rise>
  </Scene>
);

const PHASES = [
  {name: 'Phase 1 — Correctness', epic: 8314, children: 8, done: true},
  {name: 'Phase 2 — Async hygiene', epic: 8323, children: 2, done: false},
  {name: 'Phase 3 — Mechanical cleanup', epic: 8326, children: 2, done: false},
  {name: 'Phase 4 — API tightening', epic: 8329, children: 2, done: false},
  {name: 'Phase 5 — Modernization', epic: 8332, children: 2, done: false},
];

export const BatchScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow>Filed in one batch &middot; 2:38 PM</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title>Five phase epics, sixteen children</Title>
    </Rise>
    <div style={{display: 'flex', flexDirection: 'column', gap: 15, marginTop: 46}}>
      {PHASES.map((p, i) => (
        <Rise key={p.epic} delay={18 + i * 9} distance={16}>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 24,
              background: theme.surface,
              border: `1px solid ${theme.border}`,
              borderLeft: `4px solid ${p.done ? theme.good : theme.inkMuted}`,
              borderRadius: 14,
              padding: '22px 30px',
            }}
          >
            <span
              style={{
                fontSize: 27,
                fontWeight: 700,
                color: theme.inkSecondary,
                fontVariantNumeric: 'tabular-nums',
                width: 90,
              }}
            >
              #{p.epic}
            </span>
            <span style={{fontSize: 31, fontWeight: 600, flex: 1}}>{p.name}</span>
            <span style={{fontSize: 25, color: theme.inkMuted}}>
              {p.children} child {p.children === 1 ? 'item' : 'items'}
            </span>
            <span
              style={{
                fontSize: 23,
                fontWeight: 600,
                color: p.done ? theme.good : theme.inkMuted,
                width: 310,
                textAlign: 'right',
                whiteSpace: 'nowrap',
              }}
            >
              {p.done ? '✓ Functional Testing' : '○ Conceptual Definition'}
            </span>
          </div>
        </Rise>
      ))}
    </div>
  </Scene>
);

export const BoardScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow>Where the 23 items sit</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title>Board state</Title>
    </Rise>
    <div style={{display: 'flex', gap: 26, marginTop: 48}}>
      <StatTile value={11} label="Functional Testing" sub="worked, verified" accent={theme.good} delay={16} />
      <StatTile value={12} label="Conceptual Definition" sub="not yet started" accent={theme.inkMuted} delay={24} />
      <StatTile value={23} label="Still open" sub="none closed" accent={theme.ink} delay={32} />
      <StatTile value={2} label="Defects found" sub="both fixed" accent={theme.critical} delay={40} />
    </div>
    <Rise delay={54} style={{marginTop: 62}}>
      <StackedBar
        total={23}
        delay={58}
        segments={[
          {label: 'Functional Testing', value: 11, color: theme.good},
          {label: 'Conceptual Definition', value: 12, color: theme.inkMuted},
        ]}
      />
    </Rise>
  </Scene>
);

const PHASE1 = [
  {number: 8315, title: 'Value-equality bugs (14)'},
  {number: 8316, title: 'HttpClient to IHttpClientFactory (6)'},
  {number: 8317, title: 'Multiple enumeration of IEnumerable (6)'},
  {number: 8318, title: 'Empty catch clauses (3)'},
  {number: 8319, title: 'Async / CancellationToken misuse (7)'},
  {number: 8320, title: 'Nullable API contract findings (17)'},
  {number: 8321, title: 'Obsolete API usage (1)'},
  {number: 8322, title: 'Lock fields to System.Threading.Lock (6)'},
];

export const Phase1Scene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.good}>Epic #8314 &middot; complete</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title>Phase 1 &mdash; Correctness</Title>
    </Rise>
    <div style={{display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 15, marginTop: 44}}>
      {PHASE1.map((c, i) => (
        <IssueChip key={c.number} number={c.number} title={c.title} state="done" delay={16 + i * 6} />
      ))}
    </div>
    <Rise delay={76}>
      <div style={{fontSize: 27, color: theme.inkMuted, marginTop: 40, lineHeight: 1.5, maxWidth: 1300}}>
        Each one walked the full board &mdash; UX and Technical Design recorded as explicit
        no-ops, then Test Design, Development against a named commit, then verification.
      </div>
    </Rise>
  </Scene>
);

const CLAMP_ROWS: {number: number; title: string; depth: number; state: 'done' | 'defect'}[] = [
  {number: 8316, title: 'HttpClient to IHttpClientFactory', depth: 0, state: 'done'},
  {number: 8336, title: 'operator== treats uninitialized instances as equal', depth: 1, state: 'defect'},
  {number: 8337, title: 'Equals / GetHashCode still fail for a null code', depth: 2, state: 'defect'},
];

export const ClampScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.warning}>&#9888; Discovered while working</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title>Two defects, filed as children</Title>
    </Rise>
    <div style={{marginTop: 44, display: 'flex', flexDirection: 'column', gap: 17}}>
      {CLAMP_ROWS.map((r, i) => (
        <div key={r.number} style={{marginLeft: r.depth * 76}}>
          <IssueChip
            number={r.number}
            title={r.title}
            state={r.state}
            delay={16 + i * 20}
            width={1200 - r.depth * 76}
          />
        </div>
      ))}
    </div>
    <Rise delay={92}>
      <div
        style={{
          marginTop: 52,
          background: theme.surface,
          border: `1px solid ${theme.border}`,
          borderLeft: `4px solid ${theme.warning}`,
          borderRadius: 14,
          padding: '28px 34px',
          maxWidth: 1200,
        }}
      >
        <div style={{fontSize: 29, fontWeight: 700, color: theme.warning}}>
          &#9888; Parent clamp applied twice
        </div>
        <div style={{fontSize: 26, color: theme.inkSecondary, marginTop: 14, lineHeight: 1.5}}>
          #8316 was pulled back to Conceptual Definition each time a defect was found
          beneath it, then restored to Functional Testing once the child was verified.
          A parent never outranks its least-advanced open child.
        </div>
      </div>
    </Rise>
  </Scene>
);

const COMMITS = [
  {sha: '93696f67', text: '== / != value-equality operators on WorkOrderStatus'},
  {sha: '9908564e', text: 'Named exception types for 3 empty catch clauses in AcceptanceTestBase'},
  {sha: 'cf910791', text: 'Equals delegates to operator==; sentinel hash for a null code'},
  {sha: 'new file', text: 'src/AcceptanceTests/TestHttpClientFactory.cs pools one named handler'},
];

export const CommitsScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.series1}>Branch joe-cuevas-cm/qodana-remediation-v2</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title>What actually landed</Title>
    </Rise>
    <div style={{marginTop: 48, display: 'flex', flexDirection: 'column', gap: 17}}>
      {COMMITS.map((c, i) => (
        <Rise key={c.sha} delay={18 + i * 12} distance={16}>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 26,
              background: theme.surface,
              border: `1px solid ${theme.border}`,
              borderRadius: 14,
              padding: '24px 32px',
            }}
          >
            <span
              style={{
                fontFamily: 'ui-monospace, Consolas, monospace',
                fontSize: 25,
                color: theme.series1,
                width: 160,
                flexShrink: 0,
              }}
            >
              {c.sha}
            </span>
            <span style={{fontSize: 28, color: theme.ink}}>{c.text}</span>
          </div>
        </Rise>
      ))}
    </div>
    <Rise delay={78}>
      <div style={{fontSize: 27, color: theme.inkMuted, marginTop: 40}}>
        CI verified green on the head commit &mdash; all 15 check runs success or skipped.
      </div>
    </Rise>
  </Scene>
);

export const StatusScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.critical}>Not merged</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title>Where it stands</Title>
    </Rise>
    <Rise delay={16}>
      <div
        style={{
          marginTop: 44,
          background: theme.surface,
          border: `1px solid ${theme.border}`,
          borderLeft: `4px solid ${theme.critical}`,
          borderRadius: 16,
          padding: '34px 40px',
          maxWidth: 1300,
        }}
      >
        <div style={{fontSize: 33, fontWeight: 700, color: theme.critical}}>
          &#9888; PR #8335 &mdash; &ldquo;Qodana remediation (phases 1-5) &mdash; DO NOT MERGE&rdquo;
        </div>
        <div style={{fontSize: 27, color: theme.inkSecondary, marginTop: 16, lineHeight: 1.5}}>
          Open and mergeable, but held by title. Every commit above sits on that one
          shared branch, so none of this work has reached master yet.
        </div>
      </div>
    </Rise>
    <div style={{display: 'flex', gap: 26, marginTop: 48}}>
      <StatTile value="1 / 5" label="Phases complete" sub="Phase 1 only" accent={theme.good} delay={40} />
      <StatTile value={12} label="Items untouched" sub="Phases 2 - 5" accent={theme.inkMuted} delay={48} />
      <StatTile value={0} label="Items closed" sub="all 23 still open" accent={theme.warning} delay={56} />
    </div>
  </Scene>
);
