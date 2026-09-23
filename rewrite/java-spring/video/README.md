# Acceptance recording render

The Playwright JUnit acceptance test records a Chromium session to `../target/playwright-recordings/acceptance.webm`. Run `npm ci && npm run render` here after the Maven acceptance suite. Remotion probes the source duration and renders `out/acceptance.mp4` (H.264).
