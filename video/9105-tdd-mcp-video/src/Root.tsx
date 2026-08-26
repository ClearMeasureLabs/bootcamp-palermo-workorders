import React from 'react';
import {Composition} from 'remotion';
import {TddMcpCustomerTraining, TOTAL_DURATION_IN_FRAMES} from './Video';

export const FPS = 30;
export const WIDTH = 1280;
export const HEIGHT = 800;

export const RemotionRoot: React.FC = () => {
	return (
		<Composition
			id="TddMcpCustomerTraining"
			component={TddMcpCustomerTraining}
			durationInFrames={TOTAL_DURATION_IN_FRAMES}
			fps={FPS}
			width={WIDTH}
			height={HEIGHT}
		/>
	);
};
