import React from 'react';
import {AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig} from 'remotion';

export const Outro: React.FC = () => {
	const frame = useCurrentFrame();
	const {fps} = useVideoConfig();
	const logoScale = spring({frame, fps, config: {damping: 12}});
	const textOpacity = interpolate(frame, [20, 45], [0, 1], {
		extrapolateLeft: 'clamp',
		extrapolateRight: 'clamp'
	});

	return (
		<AbsoluteFill
			style={{
				background: 'linear-gradient(135deg, #1e3a5f 0%, #2d5a8c 45%, #4a90d9 100%)',
				justifyContent: 'center',
				alignItems: 'center',
				flexDirection: 'column'
			}}
		>
			<div
				style={{
					transform: `scale(${logoScale})`,
					fontFamily: 'Segoe UI, Arial, sans-serif',
					fontWeight: 800,
					fontSize: 92,
					color: 'white',
					textShadow: '0 8px 40px rgba(0,0,0,0.35)'
				}}
			>
				Work Order Manager
			</div>
			<div
				style={{
					marginTop: 24,
					opacity: textOpacity,
					fontFamily: 'Segoe UI, Arial, sans-serif',
					fontSize: 32,
					color: 'rgba(255,255,255,0.92)'
				}}
			>
				Draft &rarr; Assigned &rarr; In Progress &rarr; Complete — start tracking your work orders today
			</div>
		</AbsoluteFill>
	);
};
