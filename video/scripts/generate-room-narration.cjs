const { MsEdgeTTS, OUTPUT_FORMAT } = require('msedge-tts');
const fs = require('fs');
const path = require('path');

const VOICE = 'en-US-AndrewNeural';
const outDir = path.join(__dirname, '..', 'public', 'audio');

const segments = [
  {
    name: 'intro',
    text: 'Lengthen the work-order Room field from fifty characters to nine hundred so detailed locations can be stored.'
  },
  {
    name: 'before',
    text: 'This is the live Work Order manage screen from the Playwright test. Room is now a wrapping text area on the same form staff already use.'
  },
  {
    name: 'after',
    text: 'The test typed nine hundred characters into Room, saved the draft, opened the work order again, and the full value was still on this screen.'
  },
  {
    name: 'reject',
    text: 'The same Playwright test then entered nine hundred one characters. Save stayed on this form with the message Room cannot exceed nine hundred characters.'
  }
];

(async () => {
  fs.mkdirSync(outDir, { recursive: true });
  for (const seg of segments) {
    const tts = new MsEdgeTTS();
    await tts.setMetadata(VOICE, OUTPUT_FORMAT.AUDIO_24KHZ_48KBITRATE_MONO_MP3);
    const { audioFilePath } = await tts.toFile(outDir, seg.text);
    const target = path.join(outDir, `${seg.name}.mp3`);
    fs.renameSync(audioFilePath, target);
    console.log('wrote', target);
  }
})().catch((e) => {
  console.error('FAIL', e);
  process.exit(1);
});
