import React from 'react';
import {
  AbsoluteFill,
  Series,
  Audio,
  Img,
  staticFile,
  interpolate,
  useCurrentFrame,
} from 'remotion';
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
      left: 48,
      right: 48,
      bottom: 36,
      fontSize: 26,
      lineHeight: 1.4,
      color: '#f7fafc',
      background: 'rgba(45, 55, 72, 0.92)',
      border: '1px solid rgba(255,255,255,0.18)',
      borderRadius: 12,
      padding: '16px 22px',
    }}
  >
    {text}
  </div>
);

const Banner: React.FC<{eyebrow: string; heading: string; color: string}> = ({
  eyebrow,
  heading,
  color,
}) => (
  <div
    style={{
      position: 'absolute',
      top: 28,
      left: 48,
      right: 48,
      background: 'rgba(247, 250, 252, 0.94)',
      borderLeft: `8px solid ${color}`,
      borderRadius: 12,
      padding: '16px 22px',
      boxShadow: '0 10px 30px rgba(0,0,0,0.18)',
    }}
  >
    <div
      style={{
        fontSize: 18,
        letterSpacing: 2.4,
        textTransform: 'uppercase',
        fontWeight: 700,
        color,
      }}
    >
      {eyebrow}
    </div>
    <div
      style={{
        fontSize: 36,
        fontWeight: 700,
        color: '#2d3748',
        marginTop: 4,
        fontFamily: 'Georgia, serif',
      }}
    >
      {heading}
    </div>
  </div>
);

/** Full-frame still captured by WorkOrderRoomNumberLengthTests. */
const ScreenStill: React.FC<{
  src: string;
  eyebrow: string;
  heading: string;
  caption: string;
  accent: string;
}> = ({src, eyebrow, heading, caption, accent}) => (
  <AbsoluteFill style={{backgroundColor: '#e2e8f0'}}>
    <Img
      src={staticFile(src)}
      style={{width: '100%', height: '100%', objectFit: 'contain'}}
    />
    <Banner eyebrow={eyebrow} heading={heading} color={accent} />
    <Caption text={caption} />
  </AbsoluteFill>
);

export const IntroScene: React.FC = () => (
  <Scene>
    <Rise>
      <Eyebrow color={theme.series1}>Issue #8423 · Playwright acceptance test</Eyebrow>
    </Rise>
    <Rise delay={8}>
      <Title size={80}>Room field: 50 → 900</Title>
    </Rise>
    <Rise delay={18}>
      <div style={{fontSize: 32, color: theme.inkSecondary, marginTop: 24, maxWidth: 1200, lineHeight: 1.4}}>
        Footage is the live Work Order manage screen driven by
        WorkOrderRoomNumberLengthTests — the same Playwright test that verified the change.
      </div>
    </Rise>
    <Caption text="Narration: Lengthen the work-order Room field from fifty characters to nine hundred so detailed locations can be stored." />
  </Scene>
);

export const BeforeScene: React.FC = () => (
  <ScreenStill
    src="footage/after-new-form.png"
    eyebrow="The changed screen"
    heading="Work Order manage · Room is now a wrapping text area"
    caption="Narration: This is the live Work Order manage screen from the Playwright test. Room is now a wrapping text area on the same form staff already use."
    accent="#7b68ee"
  />
);

export const AfterScene: React.FC = () => {
  const frame = useCurrentFrame();
  const src = frame < 150 ? 'footage/after-room-filled.png' : 'footage/after-room-reopened.png';
  return (
    <ScreenStill
      src={src}
      eyebrow="ShouldSaveWorkOrderWith900CharacterRoom"
      heading="Nine hundred characters saved and displayed"
      caption="Narration: The test typed nine hundred characters into Room, saved the draft, opened the work order again, and the full value was still on this screen."
      accent="#0ca30c"
    />
  );
};

export const RejectScene: React.FC = () => (
  <ScreenStill
    src="footage/reject-validation.png"
    eyebrow="ShouldRejectRoomLongerThan900Characters"
    heading="Nine hundred one is rejected on this form"
    caption="Narration: The same Playwright test then entered nine hundred one characters. Save stayed on this form with the message Room cannot exceed 900 characters."
    accent="#d03b3b"
  />
);

export const ROOM_SCENES: {component: React.FC; durationInFrames: number; audio?: string}[] = [
  {component: IntroScene, durationInFrames: 150, audio: 'audio/intro.mp3'},
  {component: BeforeScene, durationInFrames: 180, audio: 'audio/before.mp3'},
  {component: AfterScene, durationInFrames: 240, audio: 'audio/after.mp3'},
  {component: RejectScene, durationInFrames: 180, audio: 'audio/reject.mp3'},
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
