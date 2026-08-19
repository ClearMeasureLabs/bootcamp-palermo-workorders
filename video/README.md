# Work-order demo videos (Remotion)

This folder is an independent npm/Remotion project. It does not change the .NET solution.

## Room field 50 → 900 (#8423)

Narrated before/after of the Room field length change.

```
npm install
npm run narration   # optional TTS into public/audio/*.mp3
npm run render:room # writes out/room-number-900.mp4
```

Composition id: `RoomNumber900`. 1920×1080, 30 fps. Scenes live in `src/roomNumberVideo.tsx`.

## Qodana recap

```
npm start     # Remotion Studio
npm run render  # writes out/qodana-remediation.mp4
```

## Instructions field (#8287)

Narrated before/after of the optional 4,000-character Instructions field added to the
Work Order edit screen below Description.

```
npm install
npm run narration:instructions   # TTS into public/audio/instructions-*.mp3
npm run render:instructions      # writes out/instructions-field.mp4
```

Composition id: `InstructionsField`. 1920×1080, 30 fps. Scenes live in
`src/instructionsFieldVideo.tsx`; narration text lives in
`scripts/generate-instructions-narration.cjs`.
