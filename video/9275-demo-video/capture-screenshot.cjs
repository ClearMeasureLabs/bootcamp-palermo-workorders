/**
 * Captures 1920×1080 screenshots of the login screen
 * showing the "Open Church Portal" button.
 * Uses a local HTML file — no running app required.
 */
const { chromium } = require('/workspace/video/8338-demo-video/node_modules/playwright');
const path = require('path');
const fs = require('fs');

const HTML = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8"/>
<meta name="viewport" content="width=device-width, initial-scale=1.0"/>
<title>Login — Church Staff Portal</title>
<style>
  *, *::before, *::after { box-sizing: border-box; }
  body {
    margin: 0;
    background: #f4f5f7;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 100vh;
  }
  .login-container {
    width: 100%;
    max-width: 440px;
    padding: 0 16px;
  }
  .card {
    background: #fff;
    border-radius: 8px;
    box-shadow: 0 4px 24px rgba(0,0,0,0.12);
    overflow: hidden;
  }
  .login-header {
    background: #2c5f9e;
    color: #fff;
    text-align: center;
    padding: 32px 24px 24px;
  }
  .login-header h3 {
    margin: 0 0 6px;
    font-size: 1.5rem;
    font-weight: 700;
  }
  .login-subtitle {
    font-size: 0.9rem;
    opacity: 0.85;
  }
  .login-body {
    padding: 32px 32px 24px;
  }
  .form-group {
    margin-bottom: 20px;
  }
  .form-group label {
    display: block;
    margin-bottom: 6px;
    font-weight: 500;
    font-size: 0.9rem;
    color: #444;
  }
  .form-control {
    width: 100%;
    padding: 10px 12px;
    border: 1px solid #ccc;
    border-radius: 4px;
    font-size: 0.95rem;
    color: #333;
    background: #fff;
  }
  .form-control:focus {
    outline: none;
    border-color: #2c5f9e;
    box-shadow: 0 0 0 3px rgba(44,95,158,0.2);
  }
  .text-center { text-align: center; }
  .btn {
    display: inline-block;
    padding: 10px 28px;
    border: none;
    border-radius: 4px;
    font-size: 1rem;
    font-weight: 600;
    cursor: pointer;
    transition: background 0.15s;
  }
  .btn-primary {
    background: #2c5f9e;
    color: #fff;
  }
  .btn-primary:hover { background: #244e87; }
  .btn-link {
    background: none;
    color: #2c5f9e;
    font-size: 0.85rem;
    padding: 4px 0;
    text-decoration: underline;
    border: none;
    cursor: pointer;
  }
  .lovejoy-shortcut {
    margin-top: 18px;
  }
  .church-blessing {
    margin-top: 20px;
    font-size: 0.8rem;
    color: #888;
    font-style: italic;
    text-align: center;
  }
</style>
</head>
<body>
<div class="login-container">
  <div class="card">
    <div class="login-header">
      <h3>Church Staff Portal</h3>
      <div class="login-subtitle">First Church of Shelbyville</div>
    </div>
    <div class="login-body">
      <form>
        <div class="form-group">
          <label for="User">Select Church Member</label>
          <select id="User" data-testid="User" class="form-control">
            <option value="">-- Select a parishioner or staff member --</option>
            <option value="hsimpson">HOMER SIMPSON</option>
            <option value="tlovejoy">TIMOTHY LOVEJOY</option>
          </select>
        </div>
        <div class="text-center">
          <button data-testid="LoginButton" type="submit" class="btn btn-primary">Open Church Portal</button>
        </div>
      </form>
      <div class="text-center lovejoy-shortcut">
        <button type="button" class="btn btn-link">Log in as Timothy Lovejoy</button>
      </div>
      <div class="church-blessing">"May your service to the church be blessed and your work orders be few."</div>
    </div>
  </div>
</div>
</body>
</html>`;

const BEFORE_HTML = HTML.replace('Open Church Portal', 'Enter Church Portal');

async function main() {
  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1920, height: 1080 } });
  const page = await context.newPage();

  const outDir = path.join(__dirname, 'public', 'screenshots');
  fs.mkdirSync(outDir, { recursive: true });

  // Screenshot "before" (Enter Church Portal)
  await page.setContent(BEFORE_HTML, { waitUntil: 'networkidle' });
  await page.screenshot({ path: path.join(outDir, 'before.png'), fullPage: false });
  console.log('Captured before.png');

  // Screenshot "after" (Open Church Portal)  
  await page.setContent(HTML, { waitUntil: 'networkidle' });
  await page.screenshot({ path: path.join(outDir, 'after.png'), fullPage: false });
  console.log('Captured after.png');

  await browser.close();
  console.log('Done.');
}

main().catch(e => { console.error(e); process.exit(1); });
