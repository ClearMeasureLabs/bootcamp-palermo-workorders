import React from 'react';
import {Composition} from 'remotion';
import {DemoVideo, TOTAL_DURATION_IN_FRAMES} from './Video';

export const Root: React.FC = () => (
	<Composition
		id="DemoVideo"
		component={DemoVideo}
		durationInFrames={TOTAL_DURATION_IN_FRAMES}
		fps={30}
		width={1920}
		height={1080}
	/>
);
