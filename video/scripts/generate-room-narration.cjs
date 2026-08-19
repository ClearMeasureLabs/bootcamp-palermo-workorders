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
    text: 'Before the change, a detailed room identifier longer than fifty characters could not be saved. The database and mapping rejected it.'
  },
  {
    name: 'after',
    text: 'After the change, a nine-hundred-character Room value saves and comes back on the form, wrapping and scrolling so the full value stays readable.'
  },
  {
    name: 'reject',
    text: 'Values of nine hundred one characters or more are rejected and are not stored. Existing shorter rooms remain valid.'
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
