import React from 'react';
import {AbsoluteFill, Audio, Sequence, staticFile} from 'remotion';
import {TransitionSeries, linearTiming} from '@remotion/transitions';
import {fade} from '@remotion/transitions/fade';
import {Intro} from './Intro';
import {TitleCard} from './TitleCard';
import {FootageScene, LowerThird} from './FootageScene';
import {StillScene} from './StillScene';
import {Outro} from './Outro';

const TRANSITION_FRAMES = 12;

const INTRO_FRAMES = 510; // ~17s — narration ~15.3s
const TITLE_CARD_FRAMES = 72;
const OUTRO_FRAMES = 280; // ~9.3s — narration ~8.4s

// Footage + still durations sized for Emma narration buffers.
// Captured webm lengths (25fps native): mcp-batch ~6s, portal-batch ~11s,
// mcp-ops ~94s, portal-ops ~108s — composition uses a readable head portion.
const MCP_BATCH_FRAMES = 180; // ~6s captured
const MCP_BATCH_STILL = 360; // narration ~15.3s
const PORTAL_BATCH_FRAMES = 300; // ~10s of ~11s clip
const PORTAL_BATCH_STILL = 270; // narration ~16.8s
const MCP_OPS_FRAMES = 720; // ~24s of long ops tour
const MCP_OPS_STILL = 240; // narration ~28.2s
const PORTAL_OPS_FRAMES = 540; // ~18s — search + manage + lifecycle head
const PORTAL_OPS_STILL = 150; // narration ~16.1s

export const TOTAL_DURATION_IN_FRAMES =
	INTRO_FRAMES +
	TITLE_CARD_FRAMES +
	MCP_BATCH_FRAMES +
	MCP_BATCH_STILL +
	TITLE_CARD_FRAMES +
	PORTAL_BATCH_FRAMES +
	PORTAL_BATCH_STILL +
	TITLE_CARD_FRAMES +
	MCP_OPS_FRAMES +
	MCP_OPS_STILL +
	TITLE_CARD_FRAMES +
	PORTAL_OPS_FRAMES +
	PORTAL_OPS_STILL +
	OUTRO_FRAMES -
	TRANSITION_FRAMES * 13;

const INTRO_NARRATION_START = 0;
const MCP_BATCH_NARRATION_START =
	INTRO_FRAMES - TRANSITION_FRAMES + (TITLE_CARD_FRAMES - TRANSITION_FRAMES);
const PORTAL_BATCH_NARRATION_START =
	MCP_BATCH_NARRATION_START +
	(MCP_BATCH_FRAMES - TRANSITION_FRAMES) +
	(MCP_BATCH_STILL - TRANSITION_FRAMES) +
	(TITLE_CARD_FRAMES - TRANSITION_FRAMES);
const MCP_OPS_NARRATION_START =
	PORTAL_BATCH_NARRATION_START +
	(PORTAL_BATCH_FRAMES - TRANSITION_FRAMES) +
	(PORTAL_BATCH_STILL - TRANSITION_FRAMES) +
	(TITLE_CARD_FRAMES - TRANSITION_FRAMES);
const PORTAL_OPS_NARRATION_START =
	MCP_OPS_NARRATION_START +
	(MCP_OPS_FRAMES - TRANSITION_FRAMES) +
	(MCP_OPS_STILL - TRANSITION_FRAMES) +
	(TITLE_CARD_FRAMES - TRANSITION_FRAMES);
const OUTRO_NARRATION_START =
	PORTAL_OPS_NARRATION_START +
	(PORTAL_OPS_FRAMES - TRANSITION_FRAMES) +
	(PORTAL_OPS_STILL - TRANSITION_FRAMES);

const fadeTransition = (
	<TransitionSeries.Transition
		presentation={fade()}
		timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
	/>
);

export const TddMcpCustomerTraining: React.FC = () => {
	return (
		<AbsoluteFill style={{backgroundColor: 'black'}}>
			<TransitionSeries>
				<TransitionSeries.Sequence durationInFrames={INTRO_FRAMES}>
					<Intro />
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={TITLE_CARD_FRAMES}>
					<TitleCard eyebrow="Grok on TDD" title="One command, several Saturdays" />
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={MCP_BATCH_FRAMES}>
					<AbsoluteFill>
						<FootageScene src="footage/mcp-batch.webm" />
						<LowerThird
							heading="Grok · create-dated-work-orders"
							detail="Lovejoy schedules Saturday lawn mows for Willie in one shot"
							startFrame={18}
							holdFrames={MCP_BATCH_FRAMES - 28}
						/>
					</AbsoluteFill>
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={MCP_BATCH_STILL}>
					<StillScene
						src="stills/mcp-batch-final.png"
						heading="Batch created"
						detail="Several dated work orders — Lovejoy for Willie — Saturday lawn care"
					/>
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={TITLE_CARD_FRAMES}>
					<TitleCard eyebrow="Church portal" title="Search shows the new Saturdays" />
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={PORTAL_BATCH_FRAMES}>
					<AbsoluteFill>
						<FootageScene src="footage/portal-batch.webm" />
						<LowerThird
							heading="Portal search"
							detail="Due dates colored — today yellow, overdue red — Willie as assignee"
							startFrame={18}
							holdFrames={PORTAL_BATCH_FRAMES - 28}
						/>
					</AbsoluteFill>
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={PORTAL_BATCH_STILL}>
					<StillScene
						src="stills/portal-batch-final.png"
						heading="Manage screen"
						detail="Title, Willie assignee, and due date match what Grok created"
					/>
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={TITLE_CARD_FRAMES}>
					<TitleCard eyebrow="More tools" title="List, get, create, save, assign" />
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={MCP_OPS_FRAMES}>
					<AbsoluteFill>
						<FootageScene src="footage/mcp-ops.webm" />
						<LowerThird
							heading="Grok · live TDD tools"
							detail="List, get, create with instructions, save, assign / begin / complete, employees, attachments list"
							startFrame={18}
							holdFrames={MCP_OPS_FRAMES - 28}
						/>
					</AbsoluteFill>
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={MCP_OPS_STILL}>
					<StillScene
						src="stills/mcp-ops-final.png"
						heading="Honest about attachments"
						detail="list-work-order-attachments is list-only — no file upload on TDD yet"
					/>
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={TITLE_CARD_FRAMES}>
					<TitleCard eyebrow="Portal proof" title="Same numbers on manage and search" />
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={PORTAL_OPS_FRAMES}>
					<AbsoluteFill>
						<FootageScene src="footage/portal-ops.webm" />
						<LowerThird
							heading="Portal matches MCP"
							detail="Instructions, room, due date, and status follow each Grok call"
							startFrame={18}
							holdFrames={PORTAL_OPS_FRAMES - 28}
						/>
					</AbsoluteFill>
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={PORTAL_OPS_STILL}>
					<StillScene
						src="stills/portal-ops-final.png"
						heading="Status follows the work"
						detail="Assign, begin, and complete update the manage heading for Willie"
					/>
				</TransitionSeries.Sequence>
				{fadeTransition}
				<TransitionSeries.Sequence durationInFrames={OUTRO_FRAMES}>
					<Outro />
				</TransitionSeries.Sequence>
			</TransitionSeries>

			<Sequence from={INTRO_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/intro.mp3')} />
			</Sequence>
			<Sequence from={MCP_BATCH_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/mcp-batch.mp3')} />
			</Sequence>
			<Sequence from={PORTAL_BATCH_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/portal-batch.mp3')} />
			</Sequence>
			<Sequence from={MCP_OPS_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/mcp-ops.mp3')} />
			</Sequence>
			<Sequence from={PORTAL_OPS_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/portal-ops.mp3')} />
			</Sequence>
			<Sequence from={OUTRO_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/outro.mp3')} />
			</Sequence>
		</AbsoluteFill>
	);
};
