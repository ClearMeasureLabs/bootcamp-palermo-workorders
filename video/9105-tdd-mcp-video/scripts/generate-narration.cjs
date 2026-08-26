const {MsEdgeTTS, OUTPUT_FORMAT} = require('msedge-tts');
const fs = require('fs');
const path = require('path');

const VOICE = 'en-US-EmmaMultilingualNeural';
const outDir = path.join(__dirname, '..', 'public', 'audio');

const segments = [
	{
		name: 'intro',
		text:
			'Welcome to customer training for church work orders. Grok is already connected to the church portal tools. Watch Lovejoy schedule Saturday lawn care for Groundskeeper Willie, then see the same work appear on the portal — due dates, manage screens, and all.'
	},
	{
		name: 'mcp-batch',
		text:
			'With one command, Grok creates several dated work orders. Lovejoy is the creator. Willie is the assignee. The job is Saturday lawn mowing on the sanctuary grounds — a real church task with real due dates, not a test title.'
	},
	{
		name: 'portal-batch',
		text:
			'On the church portal, search shows those work order numbers. Due dates that are today appear in yellow. Overdue dates appear in red. Empty due dates stay uncolored. Opening one manage screen shows the heading, title, Willie as assignee, and the due date Grok set.'
	},
	{
		name: 'mcp-ops',
		text:
			'Grok then uses the rest of the live tools: list work orders by status, creator, and assignee; get a work order including blank instructions; create a draft with optional instructions; save title, description, instructions, room, and due date — including clearing a due date without changing status; assign, begin, and complete; list employees; and list attachments only. There is no file upload yet, so Grok does not pretend to attach a file.'
	},
	{
		name: 'portal-ops',
		text:
			'Each of those calls is checked on the portal. Search dropdowns match the list filters. The manage screen shows instructions, room, and due date. After assign, begin, and complete, the heading follows the real status. Attachment lists stay list-only.'
	},
	{
		name: 'outro',
		text:
			'From Grok to the portal, Lovejoy schedules the Saturdays and Willie sees the work. That is how the church keeps grounds ready for Sunday.'
	}
];

(async () => {
	fs.mkdirSync(outDir, {recursive: true});
	for (const seg of segments) {
		const tts = new MsEdgeTTS();
		await tts.setMetadata(VOICE, OUTPUT_FORMAT.AUDIO_24KHZ_48KBITRATE_MONO_MP3);
		const {audioFilePath} = await tts.toFile(outDir, seg.text);
		const target = path.join(outDir, `${seg.name}.mp3`);
		fs.renameSync(audioFilePath, target);
		console.log('wrote', target);
	}
})().catch((e) => {
	console.error('FAIL', e);
	process.exit(1);
});
