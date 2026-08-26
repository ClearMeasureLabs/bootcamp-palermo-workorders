import React from 'react';
import {AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig} from 'remotion';

const brandGradient = 'linear-gradient(145deg, #0f2a1f 0%, #1a4d3a 42%, #2d7a5a 100%)';

export const Intro: React.FC = () => {
	const frame = useCurrentFrame();
	const {fps} = useVideoConfig();

	const titleSpring = spring({frame, fps, config: {damping: 14, mass: 0.7}});
	const titleY = interpolate(titleSpring, [0, 1], [48, 0]);
	const titleOpacity = interpolate(frame, [0, 18], [0, 1], {extrapolateRight: 'clamp'});
	const subtitleOpacity = interpolate(frame, [28, 48], [0, 1], {
		extrapolateRight: 'clamp',
		extrapolateLeft: 'clamp'
	});
	const subtitleY = interpolate(frame, [28, 48], [16, 0], {
		extrapolateRight: 'clamp',
		extrapolateLeft: 'clamp'
	});

	return (
		<AbsoluteFill style={{background: brandGradient, overflow: 'hidden'}}>
			<AbsoluteFill
				style={{
					background:
						'radial-gradient(ellipse at 20% 30%, rgba(255,255,255,0.10) 0%, transparent 55%), radial-gradient(ellipse at 80% 70%, rgba(180,220,160,0.12) 0%, transparent 50%)'
				}}
			/>
			<AbsoluteFill style={{justifyContent: 'center', alignItems: 'center', padding: 48}}>
				<div
					style={{
						fontFamily: 'Georgia, "Times New Roman", serif',
						fontWeight: 700,
						fontSize: 64,
						color: 'white',
						opacity: titleOpacity,
						transform: `translateY(${titleY}px)`,
						textAlign: 'center',
						letterSpacing: -0.5,
						textShadow: '0 6px 28px rgba(0,0,0,0.35)'
					}}
				>
					Church Work Orders with Grok
				</div>
				<div
					style={{
						marginTop: 22,
						fontFamily: 'Segoe UI, Arial, sans-serif',
						fontWeight: 400,
						fontSize: 28,
						color: 'rgba(255,255,255,0.92)',
						opacity: subtitleOpacity,
						transform: `translateY(${subtitleY}px)`,
						textAlign: 'center',
						maxWidth: 900,
						lineHeight: 1.35
					}}
				>
					Watch Grok schedule Saturday lawn care for Willie — then see it on the church portal
				</div>
			</AbsoluteFill>
		</AbsoluteFill>
	);
};
