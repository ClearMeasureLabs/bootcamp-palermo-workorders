import React from 'react';
import {AbsoluteFill, OffthreadVideo, interpolate, staticFile, useCurrentFrame} from 'remotion';

export const LowerThird: React.FC<{
	heading: string;
	detail: string;
	startFrame: number;
	holdFrames: number;
}> = ({heading, detail, startFrame, holdFrames}) => {
	const frame = useCurrentFrame();
	const local = frame - startFrame;
	const opacity = interpolate(local, [0, 12, holdFrames - 12, holdFrames], [0, 1, 1, 0], {
		extrapolateLeft: 'clamp',
		extrapolateRight: 'clamp'
	});
	const x = interpolate(local, [0, 16], [-28, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});

	if (local < 0 || local > holdFrames) return null;

	return (
		<AbsoluteFill style={{justifyContent: 'flex-end', alignItems: 'flex-start'}}>
			<div
				style={{
					margin: '0 0 56px 48px',
					opacity,
					transform: `translateX(${x}px)`,
					background: 'rgba(10, 28, 20, 0.88)',
					borderLeft: '5px solid #4caf7a',
					padding: '14px 22px',
					borderRadius: 6,
					maxWidth: 640
				}}
			>
				<div
					style={{
						fontFamily: 'Segoe UI, Arial, sans-serif',
						fontWeight: 800,
						fontSize: 26,
						color: 'white'
					}}
				>
					{heading}
				</div>
				<div
					style={{
						fontFamily: 'Segoe UI, Arial, sans-serif',
						fontWeight: 400,
						fontSize: 18,
						color: 'rgba(255,255,255,0.88)',
						marginTop: 4
					}}
				>
					{detail}
				</div>
			</div>
		</AbsoluteFill>
	);
};

/**
 * Letterboxed full-page capture playback for the 1280×800 composition.
 */
export const FootageScene: React.FC<{
	src: string;
}> = ({src}) => {
	return (
		<AbsoluteFill style={{backgroundColor: '#0a1610', justifyContent: 'center', alignItems: 'center'}}>
			<OffthreadVideo
				src={staticFile(src)}
				style={{width: '100%', height: '100%', objectFit: 'contain'}}
			/>
		</AbsoluteFill>
	);
};
