import React from 'react';
import {AbsoluteFill, Img, interpolate, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';

/**
 * Displays a still PNG (extracted from captured footage) held for the full
 * duration of the enclosing Sequence, with a subtle Ken Burns zoom and a
 * caption overlay. Used instead of <Freeze> + OffthreadVideo, which crashed
 * the Remotion compositor when combined with TransitionSeries premounting.
 */
export const StillScene: React.FC<{
	src: string;
	heading: string;
	detail: string;
}> = ({src, heading, detail}) => {
	const frame = useCurrentFrame();
	const {durationInFrames} = useVideoConfig();

	const scale = interpolate(frame, [0, durationInFrames], [1, 1.08], {
		extrapolateLeft: 'clamp',
		extrapolateRight: 'clamp'
	});

	const captionOpacity = interpolate(frame, [10, 30, durationInFrames - 20, durationInFrames], [0, 1, 1, 0], {
		extrapolateLeft: 'clamp',
		extrapolateRight: 'clamp'
	});

	return (
		<AbsoluteFill style={{backgroundColor: '#0b1420', overflow: 'hidden'}}>
			<AbsoluteFill style={{transform: `scale(${scale})`}}>
				<Img src={staticFile(src)} style={{width: '100%', height: '100%', objectFit: 'cover'}} />
			</AbsoluteFill>

			<AbsoluteFill style={{justifyContent: 'flex-end', alignItems: 'flex-start'}}>
				<div
					style={{
						margin: '0 0 90px 90px',
						opacity: captionOpacity,
						background: 'rgba(15, 26, 42, 0.82)',
						borderLeft: '6px solid #4a90d9',
						padding: '20px 32px',
						borderRadius: 8,
						maxWidth: 900
					}}
				>
					<div
						style={{
							fontFamily: 'Segoe UI, Arial, sans-serif',
							fontWeight: 800,
							fontSize: 34,
							color: 'white'
						}}
					>
						{heading}
					</div>
					<div
						style={{
							fontFamily: 'Segoe UI, Arial, sans-serif',
							fontWeight: 400,
							fontSize: 22,
							color: 'rgba(255,255,255,0.85)',
							marginTop: 6
						}}
					>
						{detail}
					</div>
				</div>
			</AbsoluteFill>
		</AbsoluteFill>
	);
};
