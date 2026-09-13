import React from 'react';
import {AbsoluteFill, Series, Audio, staticFile, interpolate, useCurrentFrame} from 'remotion';
import {theme} from './theme';
import {Scene, Eyebrow, Title, Rise} from './components';

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

const CodeBlock: React.FC<{lines: string[]}> = ({lines}) => (
  <div
    style={{
      background: theme.plane,
      border: `1px solid ${theme.border}`,
      borderRadius: 14,
      padding: '28px 34px',
      marginTop: 28,
      fontFamily: '"Cascadia Code", "Fira Mono", "Consolas", monospace',
      fontSize: 28,
      lineHeight: 1.7,
    }}
  >
    {lines.map((line, i) => (
      <div
        key={i}
        style={{
          color: line.startsWith('#') ? theme.series1 : theme.inkSecondary,
          fontWeight: line.startsWith('#') ? 700 : 400,
        }}
      >
        {line || '\u00A0'}
      </div>
    ))}
  </div>
);

export const MergeProbeDIntroScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.series1}>Issue #9228 · Documentation</Eyebrow>
    </Rise>
    <Rise delay={8}>
      <Title size={88}>docs/mergeprobe-d.md</Title>
    </Rise>
    <Rise delay={18}>
      <div style={{fontSize: 32, color: theme.inkSecondary, marginTop: 24, maxWidth: 1150, lineHeight: 1.4}}>
        A single Markdown documentation file added to the repository.
        Docs-only change — no code, no tests, no config.
      </div>
    </Rise>
    <Caption text="Narration: Work item 9228 adds docs/mergeprobe-d.md to the repository. This is a docs-only change — no code, no tests, no config — just a single Markdown file." />
  </Scene>
);

export const MergeProbeDContentScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.good}>File Contents</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title size={64}>One heading, one sentence</Title>
    </Rise>
    <Rise delay={14}>
      <CodeBlock lines={[
        '# Merge Probe D',
        '',
        'This file documents the mergeprobe-d probe, which is used to',
        'verify merge pipeline behavior for branch D.',
      ]} />
    </Rise>
    <Caption text="Narration: The file contains a single H1 heading — Merge Probe D — followed by one descriptive sentence explaining that this file documents the mergeprobe-d probe, used to verify merge pipeline behavior for branch D." />
  </Scene>
);

export const MergeProbeDPurposeScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.series3}>Pattern &amp; Purpose</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title size={64}>Follows existing docs convention</Title>
    </Rise>
    <Rise delay={14}>
      <div style={{display: 'flex', flexDirection: 'column', gap: 18, marginTop: 28}}>
        {[
          {file: 'docs/automerge-a.md', label: 'Existing — automerge probe A'},
          {file: 'docs/automerge-b.md', label: 'Existing — automerge probe B'},
          {file: 'docs/mergeprobe-d.md', label: 'New — mergeprobe D (this PR)', highlight: true},
        ].map(({file, label, highlight}) => (
          <div
            key={file}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 20,
              background: highlight ? 'rgba(25,158,112,0.12)' : theme.surface,
              border: `1px solid ${highlight ? theme.series3 : theme.border}`,
              borderLeft: `4px solid ${highlight ? theme.series3 : theme.border}`,
              borderRadius: 12,
              padding: '16px 24px',
            }}
          >
            <span style={{fontFamily: 'monospace', fontSize: 26, color: highlight ? theme.series3 : theme.inkSecondary}}>{file}</span>
            <span style={{fontSize: 22, color: theme.inkMuted}}>— {label}</span>
          </div>
        ))}
      </div>
    </Rise>
    <Caption text="Narration: This probe file follows the same structure as existing docs like automerge-a.md and automerge-b.md. It gives the merge pipeline a lightweight, verifiable artifact to confirm the branch D automation path is working end to end." />
  </Scene>
);

export const MERGEPROBE_D_SCENES: {component: React.FC; durationInFrames: number; audio?: string}[] = [
  {component: MergeProbeDIntroScene, durationInFrames: 210, audio: 'audio/mergeprobe-d-intro.mp3'},
  {component: MergeProbeDContentScene, durationInFrames: 240, audio: 'audio/mergeprobe-d-content.mp3'},
  {component: MergeProbeDPurposeScene, durationInFrames: 255, audio: 'audio/mergeprobe-d-purpose.mp3'},
];

export const MERGEPROBE_D_TOTAL_FRAMES = MERGEPROBE_D_SCENES.reduce(
  (sum, s) => sum + s.durationInFrames,
  0
);

export const MergeProbeDVideo: React.FC = () => (
  <AbsoluteFill style={{backgroundColor: theme.plane}}>
    <Series>
      {MERGEPROBE_D_SCENES.map(({component: Component, durationInFrames, audio}) => (
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
