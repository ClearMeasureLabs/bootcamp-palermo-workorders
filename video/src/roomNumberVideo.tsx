import React from 'react';
import {AbsoluteFill, Series, Audio, staticFile, interpolate, useCurrentFrame} from 'remotion';
import {theme} from './theme';
import {Scene, Eyebrow, Title, StatTile, Rise} from './components';

const ROOM_MAX = 900;

function buildSampleRoom(prefix: string, totalLength: number): string {
  if (prefix.length >= totalLength) {
    return prefix.slice(0, totalLength);
  }

  return prefix + '·'.repeat(totalLength - prefix.length);
}

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

const FakeForm: React.FC<{
  room: string;
  status: 'blocked' | 'saved' | 'invalid';
  limit: number;
}> = ({room, status, limit}) => (
  <div
    style={{
      background: theme.plane,
      border: `1px solid ${theme.border}`,
      borderRadius: 16,
      padding: 28,
      marginTop: 36,
    }}
  >
    <div style={{fontSize: 22, color: theme.inkMuted, marginBottom: 10}}>Work Order manage · Room</div>
    <div
      style={{
        minHeight: 120,
        maxHeight: 180,
        overflowY: 'auto',
        overflowX: 'hidden',
        whiteSpace: 'pre-wrap',
        background: theme.surface,
        border: `1px solid ${status === 'saved' ? theme.good : status === 'blocked' || status === 'invalid' ? theme.critical : theme.border}`,
        borderRadius: 10,
        padding: 16,
        fontSize: 20,
        lineHeight: 1.4,
        color: theme.ink,
        wordBreak: 'break-word',
      }}
    >
      {room}
    </div>
    <div style={{marginTop: 14, fontSize: 22, color: theme.inkMuted}}>
      {room.length} / {limit} characters
      {status === 'blocked' ? (
        <span style={{color: theme.critical, marginLeft: 16}}>Save blocked — over the 50-character limit</span>
      ) : null}
      {status === 'invalid' ? (
        <span style={{color: theme.critical, marginLeft: 16}}>Rejected — Room cannot exceed 900 characters</span>
      ) : null}
      {status === 'saved' ? (
        <span style={{color: theme.good, marginLeft: 16}}>Saved and displayed</span>
      ) : null}
    </div>
  </div>
);

export const IntroScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.series1}>Issue #8423 · Work Order Manager</Eyebrow>
    </Rise>
    <Rise delay={8}>
      <Title size={88}>Room field: 50 → 900</Title>
    </Rise>
    <Rise delay={18}>
      <div style={{fontSize: 32, color: theme.inkSecondary, marginTop: 24, maxWidth: 1100, lineHeight: 1.4}}>
        Staff need longer location descriptions. This recap shows the old 50-character
        failure and the new 900-character save and display.
      </div>
    </Rise>
    <Caption text="Narration: Lengthen the work-order Room field from fifty characters to nine hundred so detailed locations can be stored." />
  </Scene>
);

export const BeforeScene: React.FC = () => {
  const longRoom =
    'Sanctuary balcony, north stairwell, third landing, west alcove beside the organ loft HVAC return — plus overflow chairs stored against the plaster wall.';
  return (
    <Scene>
      <Rise>
        <Eyebrow color={theme.critical}>Before</Eyebrow>
      </Rise>
      <Rise delay={6}>
        <Title size={64}>Fifty characters was not enough</Title>
      </Rise>
      <FakeForm room={longRoom} status="blocked" limit={50} />
      <Caption text="Narration: Before the change, a detailed room identifier longer than fifty characters could not be saved. The database and mapping rejected it." />
    </Scene>
  );
};

export const AfterScene: React.FC = () => {
  const room = buildSampleRoom(
    'Sanctuary balcony, north stairwell, third landing, west alcove beside the organ loft HVAC return, overflow chairs, plaster wall, and accessible route notes for fulfillment staff.',
    ROOM_MAX
  );
  return (
    <Scene>
      <Rise>
        <Eyebrow color={theme.good}>After</Eyebrow>
      </Rise>
      <Rise delay={6}>
        <Title size={64}>Nine hundred characters save and display</Title>
      </Rise>
      <div style={{display: 'flex', gap: 24, marginTop: 28}}>
        <StatTile value="900" label="Accepted" accent={theme.good} />
        <StatTile value="901" label="Rejected" accent={theme.critical} delay={8} />
        <StatTile value="Optional" label="Empty still valid" accent={theme.series1} delay={16} />
      </div>
      <FakeForm room={room} status="saved" limit={ROOM_MAX} />
      <Caption text="Narration: After the change, a nine-hundred-character Room value saves and comes back on the form, wrapping and scrolling so the full value stays readable." />
    </Scene>
  );
};

export const RejectScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.warning}>Validation</Eyebrow>
    </Rise>
    <Rise delay={6}>
      <Title size={64}>Nine hundred one is still rejected</Title>
    </Rise>
    <FakeForm room={'X'.repeat(ROOM_MAX + 1)} status="invalid" limit={ROOM_MAX} />
    <Caption text="Narration: Values of nine hundred one characters or more are rejected and are not stored. Existing shorter rooms remain valid." />
  </Scene>
);

export const ROOM_SCENES: {component: React.FC; durationInFrames: number; audio?: string}[] = [
  {component: IntroScene, durationInFrames: 150, audio: 'audio/intro.mp3'},
  {component: BeforeScene, durationInFrames: 180, audio: 'audio/before.mp3'},
  {component: AfterScene, durationInFrames: 210, audio: 'audio/after.mp3'},
  {component: RejectScene, durationInFrames: 165, audio: 'audio/reject.mp3'},
];

export const ROOM_TOTAL_FRAMES = ROOM_SCENES.reduce((sum, s) => sum + s.durationInFrames, 0);

export const RoomNumber900Video: React.FC = () => (
  <AbsoluteFill style={{backgroundColor: theme.plane}}>
    <Series>
      {ROOM_SCENES.map(({component: Component, durationInFrames, audio}) => (
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
