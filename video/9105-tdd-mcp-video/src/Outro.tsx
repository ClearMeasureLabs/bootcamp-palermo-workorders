import React from 'react';
import {AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig} from 'remotion';

export const Outro: React.FC = () => {
	const frame = useCurrentFrame();
	const {fps} = useVideoConfig();
	const logoScale = spring({frame, fps, config: {damping: 12}});
	const textOpacity = interpolate(frame, [18, 40], [0, 1], {
		extrapolateLeft: 'clamp',
		extrapolateRight: 'clamp'
	});

	return (
		<AbsoluteFill
			style={{
				background: 'linear-gradient(145deg, #0f2a1f 0%, #1a4d3a 42%, #2d7a5a 100%)',
				justifyContent: 'center',
				alignItems: 'center',
				flexDirection: 'column',
				padding: 48
			}}
		>
			<div
				style={{
					transform: `scale(${logoScale})`,
					fontFamily: 'Georgia, "Times New Roman", serif',
					fontWeight: 700,
					fontSize: 52,
					color: 'white',
					textAlign: 'center',
					textShadow: '0 6px 28px rgba(0,0,0,0.35)'
				}}
			>
				From Grok to the portal
			</div>
			<div
				style={{
					marginTop: 20,
					opacity: textOpacity,
					fontFamily: 'Segoe UI, Arial, sans-serif',
					fontSize: 26,
					color: 'rgba(255,255,255,0.92)',
					textAlign: 'center',
					maxWidth: 860,
					lineHeight: 1.4
				}}
			>
				Lovejoy schedules the work. Willie sees the Saturdays. The church stays ready for Sunday.
			</div>
		</AbsoluteFill>
	);
};
