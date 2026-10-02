import React from 'react';
import {Composition, staticFile} from 'remotion';
import {AcceptanceRecording} from './video';

export const Root: React.FC = () => <Composition
  id="AcceptanceRecording"
  component={AcceptanceRecording}
  durationInFrames={180}
  fps={30}
  width={1280}
  height={720}
  calculateMetadata={async ({props}) => {
    const {getVideoMetadata} = await import('@remotion/media-utils');
    // Resolve through Remotion's configured public directory. A root-relative URL
    // bypasses staticFile's public asset handling during metadata probing.
    const metadata = await getVideoMetadata(staticFile('acceptance.webm'));
    return {durationInFrames: Math.max(1, Math.ceil(metadata.durationInSeconds * 30)), props};
  }}
/>;
