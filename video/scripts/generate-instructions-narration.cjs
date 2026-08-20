const { MsEdgeTTS, OUTPUT_FORMAT } = require('msedge-tts');
const fs = require('fs');
const path = require('path');

const VOICE = 'en-US-AndrewNeural';
const outDir = path.join(__dirname, '..', 'public', 'audio');

const segments = [
  {
    name: 'instructions-intro',
    text: 'Work item eighty-two eighty-seven adds an optional Instructions field to the work-order edit screen, directly below Description, with a four thousand character limit.'
  },
  {
    name: 'instructions-before',
    text: 'Before the change, the edit screen ended at Description. Anyone who needed step-by-step fulfillment notes had to cram them into the description, or leave them out of the system entirely.'
  },
  {
    name: 'instructions-after',
    text: 'After the change, an Instructions text area sits immediately below Description. It is optional, so work orders still save with it empty, and the value persists and reloads across edits.'
  },
  {
    name: 'instructions-validation',
    text: 'The four thousand character limit is now enforced in the browser and validated on the model, so anything longer is refused with a clear message instead of being silently truncated. Existing work orders with no instructions load and save without error.'
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
