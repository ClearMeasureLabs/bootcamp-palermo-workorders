import React from 'react';
import {AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig} from 'remotion';

export const TitleCard: React.FC<{
	eyebrow: string;
	title: string;
}> = ({eyebrow, title}) => {
	const frame = useCurrentFrame();
	const {fps} = useVideoConfig();
	const scale = spring({frame, fps, config: {damping: 12}});
	const opacity = interpolate(frame, [0, 12], [0, 1], {extrapolateRight: 'clamp'});

	return (
		<AbsoluteFill
			style={{
				background: 'linear-gradient(145deg, #102418 0%, #1c3d2e 100%)',
				justifyContent: 'center',
				alignItems: 'center',
				padding: 40
			}}
		>
			<div style={{transform: `scale(${scale})`, opacity, textAlign: 'center'}}>
				<div
					style={{
						fontFamily: 'Segoe UI, Arial, sans-serif',
						fontSize: 22,
						letterSpacing: 5,
						color: '#9fd4b5',
						fontWeight: 700,
						textTransform: 'uppercase',
						marginBottom: 14
					}}
				>
					{eyebrow}
				</div>
				<div
					style={{
						fontFamily: 'Georgia, "Times New Roman", serif',
						fontSize: 48,
						fontWeight: 700,
						color: 'white',
						lineHeight: 1.2
					}}
				>
					{title}
				</div>
			</div>
		</AbsoluteFill>
	);
};
