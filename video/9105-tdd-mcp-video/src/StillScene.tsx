import React from 'react';
import {AbsoluteFill, Img, interpolate, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';

/**
 * Still PNG with Ken Burns zoom + caption. Letterboxed full-page captures
 * sit on a dark matte so 1280×800 composition stays consistent.
 */
export const StillScene: React.FC<{
	src: string;
	heading: string;
	detail: string;
}> = ({src, heading, detail}) => {
	const frame = useCurrentFrame();
	const {durationInFrames} = useVideoConfig();

	const scale = interpolate(frame, [0, durationInFrames], [1, 1.06], {
		extrapolateLeft: 'clamp',
		extrapolateRight: 'clamp'
	});

	const captionOpacity = interpolate(
		frame,
		[8, 24, durationInFrames - 16, durationInFrames],
		[0, 1, 1, 0],
		{extrapolateLeft: 'clamp', extrapolateRight: 'clamp'}
	);

	return (
		<AbsoluteFill style={{backgroundColor: '#0a1610', overflow: 'hidden'}}>
			<AbsoluteFill style={{transform: `scale(${scale})`, justifyContent: 'center', alignItems: 'center'}}>
				<Img
					src={staticFile(src)}
					style={{width: '100%', height: '100%', objectFit: 'contain', backgroundColor: '#0a1610'}}
				/>
			</AbsoluteFill>

			<AbsoluteFill style={{justifyContent: 'flex-end', alignItems: 'flex-start'}}>
				<div
					style={{
						margin: '0 0 56px 48px',
						opacity: captionOpacity,
						background: 'rgba(10, 28, 20, 0.88)',
						borderLeft: '5px solid #4caf7a',
						padding: '14px 22px',
						borderRadius: 6,
						maxWidth: 720
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
		</AbsoluteFill>
	);
};
