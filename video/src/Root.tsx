import React from 'react';
import {Composition} from 'remotion';
import {QodanaRemediationVideo, TOTAL_FRAMES} from './Video';
import {RoomNumber900Video, ROOM_TOTAL_FRAMES} from './roomNumberVideo';
import {InstructionsFieldVideo, INSTRUCTIONS_TOTAL_FRAMES} from './instructionsFieldVideo';
import {FPS} from './theme';

export const RemotionRoot: React.FC = () => (
  <>
    <Composition
      id="QodanaRemediation"
      component={QodanaRemediationVideo}
      durationInFrames={TOTAL_FRAMES}
      fps={FPS}
      width={1920}
      height={1080}
    />
    <Composition
      id="RoomNumber900"
      component={RoomNumber900Video}
      durationInFrames={ROOM_TOTAL_FRAMES}
      fps={FPS}
      width={1920}
      height={1080}
    />
    <Composition
      id="InstructionsField"
      component={InstructionsFieldVideo}
      durationInFrames={INSTRUCTIONS_TOTAL_FRAMES}
      fps={FPS}
      width={1920}
      height={1080}
    />
  </>
);
