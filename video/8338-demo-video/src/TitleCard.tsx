import React from 'react';
import {AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig} from 'remotion';

export const TitleCard: React.FC<{
	eyebrow: string;
	title: string;
}> = ({eyebrow, title}) => {
	const frame = useCurrentFrame();
	const {fps} = useVideoConfig();
	const scale = spring({frame, fps, config: {damping: 12}});
	const opacity = interpolate(frame, [0, 15], [0, 1], {extrapolateRight: 'clamp'});

	return (
		<AbsoluteFill
			style={{
				background: 'linear-gradient(135deg, #16233b 0%, #24344f 100%)',
				justifyContent: 'center',
				alignItems: 'center'
			}}
		>
			<div style={{transform: `scale(${scale})`, opacity, textAlign: 'center'}}>
				<div
					style={{
						fontFamily: 'Segoe UI, Arial, sans-serif',
						fontSize: 28,
						letterSpacing: 6,
						color: '#7db7f0',
						fontWeight: 700,
						textTransform: 'uppercase',
						marginBottom: 18
					}}
				>
					{eyebrow}
				</div>
				<div
					style={{
						fontFamily: 'Segoe UI, Arial, sans-serif',
						fontSize: 72,
						fontWeight: 800,
						color: 'white'
					}}
				>
					{title}
				</div>
			</div>
		</AbsoluteFill>
	);
};
