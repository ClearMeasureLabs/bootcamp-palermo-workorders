import React from 'react';
import {AbsoluteFill, Series, useCurrentFrame, interpolate} from 'remotion';
import {theme} from './theme';
import {
  TitleScene,
  BatchScene,
  BoardScene,
  Phase1Scene,
  ClampScene,
  CommitsScene,
  StatusScene,
} from './scenes';

const FADE = 12;

/** Wraps a scene in a short cross-fade at both ends so cuts do not jar. */
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

export const SCENES: {component: React.FC; durationInFrames: number}[] = [
  {component: TitleScene, durationInFrames: 135},
  {component: BatchScene, durationInFrames: 180},
  {component: BoardScene, durationInFrames: 180},
  {component: Phase1Scene, durationInFrames: 195},
  {component: ClampScene, durationInFrames: 210},
  {component: CommitsScene, durationInFrames: 180},
  {component: StatusScene, durationInFrames: 195},
];

export const TOTAL_FRAMES = SCENES.reduce((sum, s) => sum + s.durationInFrames, 0);

export const QodanaRemediationVideo: React.FC = () => (
  <AbsoluteFill style={{backgroundColor: theme.plane}}>
    <Series>
      {SCENES.map(({component: Component, durationInFrames}) => (
        <Series.Sequence key={Component.name} durationInFrames={durationInFrames}>
          <Fade durationInFrames={durationInFrames}>
            <Component />
          </Fade>
        </Series.Sequence>
      ))}
    </Series>
  </AbsoluteFill>
);
