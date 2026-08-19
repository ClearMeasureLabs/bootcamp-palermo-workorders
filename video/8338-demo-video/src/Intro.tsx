import React from 'react';
import {AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig} from 'remotion';

const brandGradient = 'linear-gradient(135deg, #1e3a5f 0%, #2d5a8c 45%, #4a90d9 100%)';

export const Intro: React.FC = () => {
	const frame = useCurrentFrame();
	const {fps} = useVideoConfig();

	const titleSpring = spring({frame, fps, config: {damping: 14, mass: 0.7}});
	const titleY = interpolate(titleSpring, [0, 1], [60, 0]);
	const titleOpacity = interpolate(frame, [0, 20], [0, 1], {extrapolateRight: 'clamp'});

	const subtitleOpacity = interpolate(frame, [35, 55], [0, 1], {extrapolateRight: 'clamp', extrapolateLeft: 'clamp'});
	const subtitleY = interpolate(frame, [35, 55], [20, 0], {extrapolateRight: 'clamp', extrapolateLeft: 'clamp'});

	const badgeScale = spring({frame: frame - 70, fps, config: {damping: 10}});

	const orbitAngle = (frame / 300) * Math.PI * 2;
	const orbX = Math.cos(orbitAngle) * 420;
	const orbY = Math.sin(orbitAngle) * 220;

	return (
		<AbsoluteFill style={{background: brandGradient, overflow: 'hidden'}}>
			<AbsoluteFill
				style={{
					transform: `translate(${orbX}px, ${orbY}px)`,
					justifyContent: 'center',
					alignItems: 'center'
				}}
			>
				<div
					style={{
						width: 900,
						height: 900,
						borderRadius: '50%',
						background: 'radial-gradient(circle, rgba(255,255,255,0.12) 0%, rgba(255,255,255,0) 70%)'
					}}
				/>
			</AbsoluteFill>

			<AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
				<div
					style={{
						fontFamily: 'Segoe UI, Arial, sans-serif',
						fontWeight: 800,
						fontSize: 108,
						color: 'white',
						opacity: titleOpacity,
						transform: `translateY(${titleY}px)`,
						textShadow: '0 8px 40px rgba(0,0,0,0.35)',
						letterSpacing: -2
					}}
				>
					Work Order Manager
				</div>
				<div
					style={{
						marginTop: 28,
						fontFamily: 'Segoe UI, Arial, sans-serif',
						fontWeight: 400,
						fontSize: 40,
						color: 'rgba(255,255,255,0.92)',
						opacity: subtitleOpacity,
						transform: `translateY(${subtitleY}px)`
					}}
				>
					From request to completion — track every facility work order in one place
				</div>

				<div
					style={{
						marginTop: 60,
						display: 'flex',
						gap: 24,
						transform: `scale(${badgeScale})`
					}}
				>
					{['Draft', 'Assigned', 'In Progress', 'Complete'].map((label, i) => (
						<div
							key={label}
							style={{
								padding: '14px 26px',
								borderRadius: 999,
								background: 'rgba(255,255,255,0.16)',
								border: '1px solid rgba(255,255,255,0.4)',
								color: 'white',
								fontFamily: 'Segoe UI, Arial, sans-serif',
								fontSize: 26,
								fontWeight: 600,
								display: 'flex',
								alignItems: 'center',
								gap: 12
							}}
						>
							<span>{label}</span>
							{i < 3 && <span style={{opacity: 0.7}}>&rarr;</span>}
						</div>
					))}
				</div>
			</AbsoluteFill>
		</AbsoluteFill>
	);
};
