const { MsEdgeTTS, OUTPUT_FORMAT } = require('msedge-tts');
const fs = require('fs');
const path = require('path');

const VOICE = 'en-AU-WilliamNeural';
const outDir = path.join(__dirname, '..', 'public', 'audio');

const segments = [
  {
    name: 'mergeprobe-d-intro',
    text: 'Work item ninety-two twenty-eight adds docs slash mergeprobe-d dot m-d to the repository. This is a docs-only change — no code, no tests, no config — just a single Markdown file.'
  },
  {
    name: 'mergeprobe-d-content',
    text: 'The file contains a single H1 heading — Merge Probe D — followed by one descriptive sentence explaining that this file documents the mergeprobe-d probe, which is used to verify merge pipeline behavior for branch D.'
  },
  {
    name: 'mergeprobe-d-purpose',
    text: 'This probe file follows the same structure as existing docs like automerge-a dot m-d and automerge-b dot m-d. It gives the merge pipeline a lightweight, verifiable artifact to confirm that the branch D automation path is working end to end.'
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
