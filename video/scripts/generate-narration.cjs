const { MsEdgeTTS, OUTPUT_FORMAT } = require('msedge-tts');
const path = require('path');

const VOICE = 'en-US-AndrewNeural';

const segments = [
	{
		name: 'intro',
		text:
			"Every day, buildings need repairs, replacements, and maintenance. The Work Order Manager gives your team one place to create, assign, and track every facility request, from the moment it's reported to the moment it's fixed. Requests move through a clear lifecycle: Draft, Assigned, In Progress, and Complete, so nothing falls through the cracks."
	},
	{
		name: 'assign',
		text:
			"Reverend Timothy Lovejoy needs a maintenance issue resolved. He logs in, creates a new work order describing the problem and the room number, and assigns it directly to Joe Cuevas on the fulfillment team. The moment he saves, the work order's status changes to Assigned, and Joe is notified there's work waiting for him."
	},
	{
		name: 'complete',
		text:
			"Joe logs in and sees the work order waiting in his queue. He opens it, reviews the details Reverend Lovejoy entered, and marks it In Progress as he starts the repair. Once the job is done, Joe marks the work order Complete, closing the loop and giving everyone visibility into what was fixed, when, and by whom."
	},
	{
		name: 'features',
		text:
			"Beyond the basic workflow, the Work Order Manager includes a full set of everyday tools: search and filter work orders by assignee or status, attach photos and documents directly to a request, switch between light and dark themes, and even use voice dictation to describe an issue hands free. A built in A I assistant can also answer questions about your work orders in plain language."
	},
	{
		name: 'outro',
		text:
			'From request to resolution, the Work Order Manager keeps your facility team organized, accountable, and fast. Start tracking your work orders today.'
	}
];

(async () => {
	for (const seg of segments) {
		const tts = new MsEdgeTTS();
		await tts.setMetadata(VOICE, OUTPUT_FORMAT.AUDIO_24KHZ_48KBITRATE_MONO_MP3);
		const { audioFilePath } = await tts.toFile(path.join(__dirname, '..', 'audio'), seg.text);
		const fs = require('fs');
		const target = path.join(__dirname, '..', 'audio', `${seg.name}.mp3`);
		fs.renameSync(audioFilePath, target);
		console.log('wrote', target);
	}
})().catch((e) => {
	console.error('FAIL', e);
	process.exit(1);
});
