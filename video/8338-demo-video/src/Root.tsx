import React from 'react';
import {Composition} from 'remotion';
import {DemoVideo, TOTAL_DURATION_IN_FRAMES} from './Video';

export const FPS = 30;
export const WIDTH = 1920;
export const HEIGHT = 1080;

export const RemotionRoot: React.FC = () => {
	return (
		<Composition
			id="DemoVideo"
			component={DemoVideo}
			durationInFrames={TOTAL_DURATION_IN_FRAMES}
			fps={FPS}
			width={WIDTH}
			height={HEIGHT}
		/>
	);
};
