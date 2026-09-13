import React from 'react';
import {AbsoluteFill, Audio, Sequence, staticFile} from 'remotion';
import {TransitionSeries, linearTiming} from '@remotion/transitions';
import {fade} from '@remotion/transitions/fade';
import {StillScene} from './StillScene';

const FPS = 30;
const TRANSITION_FRAMES = 15;

// Scene durations (all at 30fps) — sized against measured audio lengths:
//   intro:  6.05s → pad to 210 frames (7s)
//   before: 10.13s → pad to 330 frames (11s)
//   after:  14.71s → pad to 480 frames (16s)

const INTRO_FRAMES = 210;   // 7s  — "Work item 9275 makes a small but meaningful change…"
const BEFORE_FRAMES = 330;  // 11s — "Previously the button read Enter Church Portal…"
const AFTER_FRAMES = 480;   // 16s — "The button now reads Open Church Portal…"

export const TOTAL_DURATION_IN_FRAMES =
	INTRO_FRAMES +
	BEFORE_FRAMES +
	AFTER_FRAMES -
	TRANSITION_FRAMES * 2;

const INTRO_AUDIO_START = 0;
const BEFORE_AUDIO_START = INTRO_FRAMES - TRANSITION_FRAMES;
const AFTER_AUDIO_START = BEFORE_AUDIO_START + (BEFORE_FRAMES - TRANSITION_FRAMES);

export const DemoVideo: React.FC = () => (
	<AbsoluteFill style={{backgroundColor: 'black'}}>
		<TransitionSeries>
			<TransitionSeries.Sequence durationInFrames={INTRO_FRAMES}>
				<StillScene
					src="screenshots/before.png"
					heading="Issue #9275 — Login Button Wording"
					detail={'The sign-in button label changes from \u201cEnter Church Portal\u201d to \u201cOpen Church Portal\u201d'}
				/>
			</TransitionSeries.Sequence>

			<TransitionSeries.Transition
				presentation={fade()}
				timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
			/>

			<TransitionSeries.Sequence durationInFrames={BEFORE_FRAMES}>
				<StillScene
					src="screenshots/before.png"
					heading="Before: Enter Church Portal"
					detail="The original button label — functional but formal"
				/>
			</TransitionSeries.Sequence>

			<TransitionSeries.Transition
				presentation={fade()}
				timing={linearTiming({durationInFrames: TRANSITION_FRAMES})}
			/>

			<TransitionSeries.Sequence durationInFrames={AFTER_FRAMES}>
				<StillScene
					src="screenshots/after.png"
					heading="After: Open Church Portal"
					detail="One word changed — warmer, more inviting tone. No layout or behaviour changes."
				/>
			</TransitionSeries.Sequence>
		</TransitionSeries>

		<Sequence from={INTRO_AUDIO_START} layout="none">
			<Audio src={staticFile('audio/intro.mp3')} />
		</Sequence>
		<Sequence from={BEFORE_AUDIO_START} layout="none">
			<Audio src={staticFile('audio/before.mp3')} />
		</Sequence>
		<Sequence from={AFTER_AUDIO_START} layout="none">
			<Audio src={staticFile('audio/after.mp3')} />
		</Sequence>
	</AbsoluteFill>
);
