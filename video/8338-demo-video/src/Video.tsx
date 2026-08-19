import React from 'react';
import {AbsoluteFill, Audio, Sequence, staticFile} from 'remotion';
import {TransitionSeries, linearTiming} from '@remotion/transitions';
import {fade} from '@remotion/transitions/fade';
import {Intro} from './Intro';
import {TitleCard} from './TitleCard';
import {FootageScene, LowerThird, StatusBadge} from './FootageScene';
import {StillScene} from './StillScene';
import {Outro} from './Outro';

const TRANSITION_FRAMES = 15;

// Real Playwright-captured footage durations (see video/capture/*.ts and
// video/footage/*.webm, native 25fps VP8) expressed in this composition's 30fps.
// Kept a couple of frames under the true clip length as a safety margin.
const ASSIGN_SCENE_FRAMES = 270; // ~9.0s of the captured 9.2s clip
const BEGIN_COMPLETE_SCENE_FRAMES = 265; // ~8.8s of the captured 9.0s clip
const FEATURE_TOUR_SCENE_FRAMES = 288; // ~9.6s of the captured 9.76s clip

// Freeze-frame stills (extracted via ffmpeg from the tail of each captured
// clip, see video/stills/) held after their scene so narration has room to
// finish and the key screen (e.g. the completed work order) lingers on
// screen. Durations sized against the narration clip lengths in video/audio/
// (see video/scripts/generate-narration.cjs), each with a few seconds of
// buffer: assign ~19.0s, complete ~19.4s, features ~23.5s of narration.
const ASSIGN_STILL_FRAMES = 330; // 11.0s
const COMPLETE_STILL_FRAMES = 345; // 11.5s
const FEATURE_STILL_FRAMES = 450; // 15.0s

const INTRO_FRAMES = 690; // 23.0s — narration ~22.2s
const TITLE_CARD_FRAMES = 90; // 3s
const OUTRO_FRAMES = 330; // 11.0s — narration ~9.8s

export const TOTAL_DURATION_IN_FRAMES =
	INTRO_FRAMES +
	TITLE_CARD_FRAMES +
	ASSIGN_SCENE_FRAMES +
	ASSIGN_STILL_FRAMES +
	TITLE_CARD_FRAMES +
	BEGIN_COMPLETE_SCENE_FRAMES +
	COMPLETE_STILL_FRAMES +
	TITLE_CARD_FRAMES +
	FEATURE_TOUR_SCENE_FRAMES +
	FEATURE_STILL_FRAMES +
	OUTRO_FRAMES -
	TRANSITION_FRAMES * 10;

// Absolute start frames for each narration clip, computed from the
// TransitionSeries layout above (each crossfade overlaps the next sequence
// by TRANSITION_FRAMES). Narration for a scene begins when that scene's
// footage starts and runs into its trailing still, which is sized with
// enough buffer to let the narration finish before the next title card.
const INTRO_NARRATION_START = 0;
const ASSIGN_NARRATION_START = INTRO_FRAMES - TRANSITION_FRAMES + (TITLE_CARD_FRAMES - TRANSITION_FRAMES);
const COMPLETE_NARRATION_START =
	ASSIGN_NARRATION_START +
	(ASSIGN_SCENE_FRAMES - TRANSITION_FRAMES) +
	(ASSIGN_STILL_FRAMES - TRANSITION_FRAMES) +
	(TITLE_CARD_FRAMES - TRANSITION_FRAMES);
const FEATURES_NARRATION_START =
	COMPLETE_NARRATION_START +
	(BEGIN_COMPLETE_SCENE_FRAMES - TRANSITION_FRAMES) +
	(COMPLETE_STILL_FRAMES - TRANSITION_FRAMES) +
	(TITLE_CARD_FRAMES - TRANSITION_FRAMES);
const OUTRO_NARRATION_START =
	FEATURES_NARRATION_START +
	(FEATURE_TOUR_SCENE_FRAMES - TRANSITION_FRAMES) +
	(FEATURE_STILL_FRAMES - TRANSITION_FRAMES);

export const DemoVideo: React.FC = () => {
	return (
		<AbsoluteFill style={{backgroundColor: 'black'}}>
			<TransitionSeries>
				<TransitionSeries.Sequence durationInFrames={INTRO_FRAMES}>
					<Intro />
				</TransitionSeries.Sequence>

				<TransitionSeries.Transition
					presentation={fade()}
					timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
				/>

				<TransitionSeries.Sequence durationInFrames={TITLE_CARD_FRAMES}>
					<TitleCard eyebrow="Step One" title="Create & Assign a Work Order" />
				</TransitionSeries.Sequence>

				<TransitionSeries.Transition
					presentation={fade()}
					timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
				/>

				<TransitionSeries.Sequence durationInFrames={ASSIGN_SCENE_FRAMES}>
					<AbsoluteFill>
						<FootageScene src="footage/assign-scene.webm" />
						<LowerThird
							heading="Reverend Timothy Lovejoy"
							detail="Creates a new work order and assigns it to Joe Cuevas (Fulfillment)"
							startFrame={20}
							holdFrames={ASSIGN_SCENE_FRAMES - 30}
						/>
						<StatusBadge label="Assigned" startFrame={ASSIGN_SCENE_FRAMES - 60} holdFrames={55} />
					</AbsoluteFill>
				</TransitionSeries.Sequence>

				<TransitionSeries.Transition
					presentation={fade()}
					timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
				/>

				<TransitionSeries.Sequence durationInFrames={ASSIGN_STILL_FRAMES}>
					<StillScene
						src="stills/assign-final.png"
						heading="Status: Assigned"
						detail="The work order is now on Joe Cuevas's queue, ready to begin"
					/>
				</TransitionSeries.Sequence>

				<TransitionSeries.Transition
					presentation={fade()}
					timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
				/>

				<TransitionSeries.Sequence durationInFrames={TITLE_CARD_FRAMES}>
					<TitleCard eyebrow="Step Two" title="Begin & Complete the Work" />
				</TransitionSeries.Sequence>

				<TransitionSeries.Transition
					presentation={fade()}
					timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
				/>

				<TransitionSeries.Sequence durationInFrames={BEGIN_COMPLETE_SCENE_FRAMES}>
					<AbsoluteFill>
						<FootageScene src="footage/begin-complete-scene.webm" />
						<LowerThird
							heading="Joe Cuevas"
							detail="Begins the assigned work order, then marks it complete"
							startFrame={20}
							holdFrames={BEGIN_COMPLETE_SCENE_FRAMES - 30}
						/>
						<StatusBadge label="In Progress" startFrame={90} holdFrames={80} />
						<StatusBadge
							label="Complete"
							startFrame={BEGIN_COMPLETE_SCENE_FRAMES - 55}
							holdFrames={50}
						/>
					</AbsoluteFill>
				</TransitionSeries.Sequence>

				<TransitionSeries.Transition
					presentation={fade()}
					timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
				/>

				<TransitionSeries.Sequence durationInFrames={COMPLETE_STILL_FRAMES}>
					<StillScene
						src="stills/complete-final.png"
						heading="Status: Complete"
						detail="Joe closes the loop — the work order shows who fixed it and when"
					/>
				</TransitionSeries.Sequence>

				<TransitionSeries.Transition
					presentation={fade()}
					timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
				/>

				<TransitionSeries.Sequence durationInFrames={TITLE_CARD_FRAMES}>
					<TitleCard eyebrow="Feature Tour" title="Search, Themes & the AI Assistant" />
				</TransitionSeries.Sequence>

				<TransitionSeries.Transition
					presentation={fade()}
					timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
				/>

				<TransitionSeries.Sequence durationInFrames={FEATURE_TOUR_SCENE_FRAMES}>
					<AbsoluteFill>
						<FootageScene src="footage/feature-tour-scene.webm" />
						<LowerThird
							heading="Search, Dark Mode & AI Chat"
							detail="Filter work orders by assignee and status, switch themes, and ask the built-in AI assistant"
							startFrame={20}
							holdFrames={FEATURE_TOUR_SCENE_FRAMES - 30}
						/>
					</AbsoluteFill>
				</TransitionSeries.Sequence>

				<TransitionSeries.Transition
					presentation={fade()}
					timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
				/>

				<TransitionSeries.Sequence durationInFrames={FEATURE_STILL_FRAMES}>
					<StillScene
						src="stills/feature-final.png"
						heading="More Than the Basics"
						detail="Attachments, dictation, dark mode, and a built-in AI assistant round out the toolkit"
					/>
				</TransitionSeries.Sequence>

				<TransitionSeries.Transition
					presentation={fade()}
					timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
				/>

				<TransitionSeries.Sequence durationInFrames={OUTRO_FRAMES}>
					<Outro />
				</TransitionSeries.Sequence>
			</TransitionSeries>

			<Sequence from={INTRO_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/intro.mp3')} />
			</Sequence>
			<Sequence from={ASSIGN_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/assign.mp3')} />
			</Sequence>
			<Sequence from={COMPLETE_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/complete.mp3')} />
			</Sequence>
			<Sequence from={FEATURES_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/features.mp3')} />
			</Sequence>
			<Sequence from={OUTRO_NARRATION_START} layout="none">
				<Audio src={staticFile('audio/outro.mp3')} />
			</Sequence>
		</AbsoluteFill>
	);
};
