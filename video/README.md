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
