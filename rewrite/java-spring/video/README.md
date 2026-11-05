# Acceptance recording render

The Playwright JUnit acceptance test records a Chromium session to `../target/playwright-recordings/acceptance.webm`. Copy the WebM into `public/acceptance.webm`, then run `npm install --no-audit --no-fund && npm run render` here after the Maven acceptance suite. Remotion probes the source duration and renders `out/acceptance.mp4` (H.264).
