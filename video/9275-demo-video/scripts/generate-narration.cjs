/**
 * Generates narration audio for the #9275 demo video.
 * Voice: en-AU-WilliamNeural (Australian male)
 */
const { MsEdgeTTS, OUTPUT_FORMAT } = require('msedge-tts');
const fs = require('fs');
const path = require('path');

const VOICE = 'en-AU-WilliamNeural';
const outDir = path.join(__dirname, '..', 'public', 'audio');

const segments = [
  {
    name: 'intro',
    text: "Work item ninety-two seventy-five makes a small but meaningful change to the Church Staff Portal login screen."
  },
  {
    name: 'before',
    text: "Previously, the primary sign-in button read Enter Church Portal. Clear enough, but the word Enter can feel a bit formal and transactional for a community-focused application."
  },
  {
    name: 'after',
    text: "The button now reads Open Church Portal. One word changed, but the tone is warmer and more inviting — you're opening a door to serve your community, not just entering a system. No other text, layout, colour, or behaviour has changed."
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
