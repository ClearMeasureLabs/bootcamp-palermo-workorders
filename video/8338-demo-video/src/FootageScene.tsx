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
	const opacity = interpolate(local, [0, 15, holdFrames - 15, holdFrames], [0, 1, 1, 0], {
		extrapolateLeft: 'clamp',
		extrapolateRight: 'clamp'
	});
	const x = interpolate(local, [0, 20], [-40, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});

	if (local < 0 || local > holdFrames) return null;

	return (
		<AbsoluteFill style={{justifyContent: 'flex-end', alignItems: 'flex-start'}}>
			<div
				style={{
					margin: '0 0 90px 90px',
					opacity,
					transform: `translateX(${x}px)`,
					background: 'rgba(15, 26, 42, 0.82)',
					borderLeft: '6px solid #4a90d9',
					padding: '20px 32px',
					borderRadius: 8,
					maxWidth: 760
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
	);
};

export const StatusBadge: React.FC<{label: string; startFrame: number; holdFrames: number}> = ({
	label,
	startFrame,
	holdFrames
}) => {
	const frame = useCurrentFrame();
	const local = frame - startFrame;
	if (local < 0 || local > holdFrames) return null;
	const scale = interpolate(local, [0, 10], [0.6, 1], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
	const opacity = interpolate(local, [0, 10, holdFrames - 10, holdFrames], [0, 1, 1, 0], {
		extrapolateLeft: 'clamp',
		extrapolateRight: 'clamp'
	});

	return (
		<AbsoluteFill style={{justifyContent: 'flex-start', alignItems: 'flex-end'}}>
			<div
				style={{
					margin: '90px 90px 0 0',
					transform: `scale(${scale})`,
					opacity,
					background: '#2e9e5b',
					color: 'white',
					fontFamily: 'Segoe UI, Arial, sans-serif',
					fontWeight: 800,
					fontSize: 28,
					padding: '14px 30px',
					borderRadius: 999,
					boxShadow: '0 8px 24px rgba(0,0,0,0.35)'
				}}
			>
				{label}
			</div>
		</AbsoluteFill>
	);
};

/**
 * Plays a captured .webm clip for the full duration of its enclosing Sequence.
 * Deliberately does not nest an inner <Sequence>/<Freeze> pair around the video —
 * TransitionSeries premounts neighboring scenes a few frames ahead of their active
 * window for the crossfade, and OffthreadVideo does not gracefully handle being
 * asked for a negative timestamp during that premount when wrapped in extra
 * Sequence/Freeze layers. A single top-level OffthreadVideo sized to match the
 * parent TransitionSeries.Sequence avoids that failure mode.
 */
export const FootageScene: React.FC<{
	src: string;
}> = ({src}) => {
	return (
		<AbsoluteFill style={{backgroundColor: '#0b1420'}}>
			<OffthreadVideo src={staticFile(src)} />
		</AbsoluteFill>
	);
};
