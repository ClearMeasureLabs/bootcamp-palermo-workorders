import React from 'react';
import {AbsoluteFill, Series, Audio, staticFile, interpolate, useCurrentFrame} from 'remotion';
import {theme} from './theme';
import {Scene, Eyebrow, Title, StatTile, Rise} from './components';

const INSTRUCTIONS_MAX = 4000;

const FADE = 10;

const Fade: React.FC<{durationInFrames: number; children: React.ReactNode}> = ({
  durationInFrames,
  children,
}) => {
  const frame = useCurrentFrame();
  const opacity = interpolate(
    frame,
    [0, FADE, durationInFrames - FADE, durationInFrames],
    [0, 1, 1, 0],
    {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'}
  );
  return <AbsoluteFill style={{opacity}}>{children}</AbsoluteFill>;
};

const Caption: React.FC<{text: string}> = ({text}) => (
  <div
    style={{
      position: 'absolute',
      left: 110,
      right: 110,
      bottom: 72,
      fontSize: 28,
      lineHeight: 1.45,
      color: theme.inkSecondary,
      background: 'rgba(13,13,13,0.72)',
      border: `1px solid ${theme.border}`,
      borderRadius: 12,
      padding: '18px 24px',
    }}
  >
    {text}
  </div>
);

/** One labelled field on the mock work-order edit form. */
const Field: React.FC<{
  label: string;
  value: string;
  rows?: number;
  accent?: string;
  note?: React.ReactNode;
}> = ({label, value, rows = 1, accent, note}) => (
  <div style={{marginTop: 18}}>
    <div style={{fontSize: 22, color: theme.inkMuted, marginBottom: 8}}>{label}</div>
    <div
      style={{
        minHeight: rows * 34 + 20,
        background: theme.surface,
        border: `1px solid ${accent ?? theme.border}`,
        borderRadius: 10,
        padding: '12px 16px',
        fontSize: 20,
        lineHeight: 1.45,
        color: value ? theme.ink : theme.inkMuted,
        whiteSpace: 'pre-wrap',
        wordBreak: 'break-word',
        overflow: 'hidden',
      }}
    >
      {value || '(empty — optional)'}
    </div>
    {note ? <div style={{marginTop: 10, fontSize: 21}}>{note}</div> : null}
  </div>
);

const FormCard: React.FC<{children: React.ReactNode}> = ({children}) => (
  <div
    style={{
      background: theme.plane,
      border: `1px solid ${theme.border}`,
      borderRadius: 16,
      padding: 28,
      marginTop: 30,
    }}
  >
    <div style={{fontSize: 22, color: theme.inkMuted}}>Work Order manage · edit screen</div>
    {children}
  </div>
);

const DESCRIPTION = 'Replace the failed ballast in the fellowship hall lighting.';

export const IntroScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.series1}>Issue #8287 · Work Order Manager</Eyebrow>
    </Rise>
    <Rise delay={8}>
      <Title size={88}>Instructions field</Title>
    </Rise>
    <Rise delay={18}>
      <div style={{fontSize: 32, color: theme.inkSecondary, marginTop: 24, maxWidth: 1150, lineHeight: 1.4}}>
        An optional 4,000-character Instructions field on the work-order edit screen,
        directly below Description.
      </div>
    </Rise>
    <Caption text="Narration: Work item 8287 adds an optional Instructions field to the work-order edit screen, directly below Description, with a 4,000-character limit." />
  </Scene>
);

export const BeforeScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.critical}>Before</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title size={64}>Nowhere to put fulfillment steps</Title>
    </Rise>
    <FormCard>
      <Field label="Description:" value={DESCRIPTION} rows={2} />
      <div style={{marginTop: 22, fontSize: 22, color: theme.critical}}>
        No Instructions field — step-by-step notes had to be crammed into Description
      </div>
    </FormCard>
    <Caption text="Narration: Before the change, the edit screen ended at Description. Step-by-step fulfillment notes had to be crammed in, or left out of the system entirely." />
  </Scene>
);

export const AfterScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.good}>After</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title size={64}>Instructions sits below Description</Title>
    </Rise>
    <div style={{display: 'flex', gap: 24, marginTop: 24}}>
      <StatTile value="4,000" label="Character limit" accent={theme.series1} />
      <StatTile value="Optional" label="Saves when empty" accent={theme.good} delay={8} />
      <StatTile value="Round-trip" label="Persists and reloads" accent={theme.series3} delay={16} />
    </div>
    <FormCard>
      <Field label="Description:" value={DESCRIPTION} rows={2} />
      <Field
        label="Instructions:"
        value={
          'Shut off breaker panel B before starting. Ballast part number 40-B22 is on the second shelf of the maintenance closet. Log the replacement in the fixture card and photograph the panel before closing it up.'
        }
        rows={3}
        accent={theme.good}
        note={<span style={{color: theme.good}}>Saved and reloaded on the next edit</span>}
      />
    </FormCard>
    <Caption text="Narration: After the change, an Instructions text area sits immediately below Description. It is optional, and the value persists and reloads across edits." />
  </Scene>
);

export const ValidationScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.warning}>Validation</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title size={64}>Over 4,000 is refused, not truncated</Title>
    </Rise>
    <FormCard>
      <Field
        label="Instructions:"
        value={'Shut off breaker panel B before starting. ' + '·'.repeat(600)}
        rows={3}
        accent={theme.critical}
        note={
          <span style={{color: theme.critical}}>
            Instructions cannot exceed 4,000 characters.
          </span>
        }
      />
      <div style={{marginTop: 16, fontSize: 21, color: theme.inkMuted}}>
        {INSTRUCTIONS_MAX} / {INSTRUCTIONS_MAX} characters · browser maxlength + model validation
      </div>
    </FormCard>
    <Caption text="Narration: The 4,000-character limit is enforced in the browser and validated on the model, so longer input is refused with a clear message instead of being silently truncated." />
  </Scene>
);

export const INSTRUCTIONS_SCENES: {component: React.FC; durationInFrames: number; audio?: string}[] = [
  {component: IntroScene, durationInFrames: 195, audio: 'audio/instructions-intro.mp3'},
  {component: BeforeScene, durationInFrames: 225, audio: 'audio/instructions-before.mp3'},
  {component: AfterScene, durationInFrames: 240, audio: 'audio/instructions-after.mp3'},
  {component: ValidationScene, durationInFrames: 270, audio: 'audio/instructions-validation.mp3'},
];

export const INSTRUCTIONS_TOTAL_FRAMES = INSTRUCTIONS_SCENES.reduce(
  (sum, s) => sum + s.durationInFrames,
  0
);

export const InstructionsFieldVideo: React.FC = () => (
  <AbsoluteFill style={{backgroundColor: theme.plane}}>
    <Series>
      {INSTRUCTIONS_SCENES.map(({component: Component, durationInFrames, audio}) => (
        <Series.Sequence key={Component.name} durationInFrames={durationInFrames}>
          <Fade durationInFrames={durationInFrames}>
            <Component />
            {audio ? <Audio src={staticFile(audio)} /> : null}
          </Fade>
        </Series.Sequence>
      ))}
    </Series>
  </AbsoluteFill>
);
