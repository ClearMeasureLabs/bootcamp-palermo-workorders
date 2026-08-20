# Demo Video (Remotion)

Independent Node/npm project that builds the app demo video for issue #8338. Does not
touch the .NET solution, NuGet packages, `global.json`, or `.octopus/`.

## Layout

- `capture/` — Playwright (TypeScript) scripts that drive the **real, running**
  Work Order app and record `.webm` footage into `public/footage/` (gitignored —
  regenerate with `npm run capture`). Helpers in `capture/helpers.ts` mirror the
  data-testid based Click/Input/Select conventions from
  `src/AcceptanceTests/AcceptanceTestBase.cs`.
- `public/audio/` — narration voiceover (`.mp3`, committed) generated locally via
  Microsoft Edge's online TTS service (`msedge-tts` devDependency, no API key) by
  `scripts/generate-narration.cjs`. One clip per section: intro (purpose), assign
  (happy path), complete (begin/complete work), features (tour), outro.
- `public/stills/` — freeze-frame PNGs (committed) extracted from the tail of each
  captured `.webm` clip via `ffmpeg -sseof -1.0 -i clip.webm -update 1 -q:v 2 out.png`.
  Shown in the composition as `<Img>` + Ken Burns zoom (`StillScene.tsx`) instead of
  `<Freeze>` + `OffthreadVideo`, which crashed the Remotion compositor when combined
  with `TransitionSeries` premounting — see the comment in `FootageScene.tsx`.
- `src/` — Remotion composition (`Root.tsx`, `Video.tsx`, per-scene components) that
  sequences an animated intro, the assign scene + freeze still, the begin/complete
  scene + freeze still, a feature tour + freeze still, and an outro, with narration
  `<Audio>` sequences timed to each section, using `@remotion/transitions` fades
  between visual scenes.
- `out/` — rendered output (gitignored).
- `artifacts/demo-video.mp4` — the final compressed deliverable, committed to the repo.

## Regenerating narration

```bash
npm install
node scripts/generate-narration.cjs   # -> public/audio/*.mp3 via Microsoft Edge TTS
```

## Reproducing the capture + render

1. Migrate and seed a local database (LocalDB shown; SQLite also works, see
   `src/AcceptanceTests/ServerFixture.cs` for the fallback path):

   ```powershell
   dotnet build src/ChurchBulletin.sln --configuration Release
   dotnet src/Database/bin/Release/net10.0/ClearMeasure.Bootcamp.Database.dll Update "(LocalDb)\MSSQLLocalDB" ChurchBulletinVideo src/Database/scripts
   $env:ConnectionStrings__SqlConnectionString = 'server=(LocalDb)\MSSQLLocalDB;database=ChurchBulletinVideo;Integrated Security=true;TrustServerCertificate=true;'
   dotnet test src/IntegrationTests --configuration Release --filter "FullyQualifiedName~ZDataLoader" --no-build
   ```

2. Start the real app against that database (seeds `tlovejoy` and `jcuevas`):

   ```powershell
   cd src/UI/Server
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   $env:APPLICATIONINSIGHTS_CONNECTION_STRING = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
   $env:ApiKeyAuthentication__Enabled = "false"
   $env:ConnectionStrings__SqlConnectionString = 'server=(LocalDb)\MSSQLLocalDB;database=ChurchBulletinVideo;Integrated Security=true;TrustServerCertificate=true;'
   dotnet run --no-build --configuration Release --no-launch-profile --urls=https://localhost:7175
   ```

3. From `video/`:

   ```bash
   npm install
   npx playwright install chromium
   DEMO_BASE_URL=https://localhost:7175 npm run capture   # real Playwright capture -> public/footage/*.webm
   npm run render                                          # Remotion render -> out/demo-video.mp4
   ```

4. Copy/compress `out/demo-video.mp4` to `artifacts/demo-video.mp4` (ffmpeg optional —
   the h264/CRF 28 output from `remotion.config.ts` is already small; see that file).

## Testing note

This is Node tooling, not C#/application logic — no `.NET` unit/integration tests apply.
The capture pipeline **is** the full-system test: `npm run capture` drives Playwright
end-to-end against a real running app instance (Chromium, headless, real DOM
interactions via data-testid selectors); `npm run render` produces the final artifact.
Both were run successfully as part of this change.
