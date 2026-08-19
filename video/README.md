# Work-order demo videos (Remotion)

This folder is an independent npm/Remotion project. It does not change the .NET solution.

## Room field 50 → 900 (#8423)

Narrated recap of the Room field length change. Screen footage comes from the
Playwright acceptance test `WorkOrderRoomNumberLengthTests` (the same test that
verified the manage-form change).

```
# 1. Drive the live Work Order manage screen and write stills + .webm
$env:DEMO_CAPTURE = "1"
dotnet test src/AcceptanceTests --configuration Debug --filter "FullyQualifiedName~WorkOrderRoomNumberLengthTests"

# 2. Render the Remotion composition that plays those captures
cd video
npm install
npm run narration   # optional TTS into public/audio/*.mp3
npm run render:room # writes out/room-number-900.mp4
```

Stills land in `public/footage/` (`after-new-form.png`, `after-room-filled.png`,
`after-room-reopened.png`, `reject-validation.png`). Composition id:
`RoomNumber900`. 1920×1080, 30 fps. Scenes live in `src/roomNumberVideo.tsx`.

## Qodana recap

```
npm start     # Remotion Studio
npm run render  # writes out/qodana-remediation.mp4
```
