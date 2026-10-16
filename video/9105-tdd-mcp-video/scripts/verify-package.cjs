/**
 * Package gates for #9105 (Node-only; no ChurchBulletin.sln tests).
 * - Composition 1280×800 @ 30fps
 * - Narration church language (no factory / Counter / Fetch / health / connector jargon)
 * - Artifact present when committed
 */
const fs = require('fs');
const path = require('path');

const root = path.join(__dirname, '..');
let failed = false;

function fail(msg) {
	console.error('FAIL:', msg);
	failed = true;
}

function ok(msg) {
	console.log('OK:', msg);
}

const rootTsx = fs.readFileSync(path.join(root, 'src', 'Root.tsx'), 'utf8');
if (!/export const WIDTH = 1280;/.test(rootTsx)) fail('Root.tsx WIDTH must be 1280');
else ok('WIDTH 1280');
if (!/export const HEIGHT = 800;/.test(rootTsx)) fail('Root.tsx HEIGHT must be 800');
else ok('HEIGHT 800');
if (!/export const FPS = 30;/.test(rootTsx)) fail('Root.tsx FPS must be 30');
else ok('FPS 30');
if (!/id="TddMcpCustomerTraining"/.test(rootTsx)) fail('Composition id TddMcpCustomerTraining missing');
else ok('Composition id TddMcpCustomerTraining');

const narration = fs.readFileSync(path.join(root, 'scripts', 'generate-narration.cjs'), 'utf8');
const banned = [
	/\bfactory\b/i,
	/\bCounter\b/,
	/\bFetch data\b/i,
	/\bhealth check/i,
	/\bconnector setup\b/i,
	/\bprompt-setup\b/i
];
for (const re of banned) {
	if (re.test(narration)) fail(`Narration contains banned jargon matching ${re}`);
}
if (!failed) ok('Narration church language (no banned jargon)');

const requiredAudio = ['intro', 'mcp-batch', 'portal-batch', 'mcp-ops', 'portal-ops', 'outro'];
for (const name of requiredAudio) {
	const p = path.join(root, 'public', 'audio', `${name}.mp3`);
	if (!fs.existsSync(p) || fs.statSync(p).size < 1000) fail(`Missing/short audio ${name}.mp3`);
	else ok(`audio/${name}.mp3`);
}

const artifact = path.join(root, 'artifacts', 'customer-training-grok-tdd-mcp.mp4');
if (!fs.existsSync(artifact) || fs.statSync(artifact).size < 100_000) {
	fail('artifacts/customer-training-grok-tdd-mcp.mp4 missing or too small');
} else {
	ok(`artifact ${(fs.statSync(artifact).size / (1024 * 1024)).toFixed(1)} MiB`);
}

const session = path.join(root, 'artifacts', 'capture-session.json');
if (fs.existsSync(session)) {
	const meta = JSON.parse(fs.readFileSync(session, 'utf8'));
	if (!meta.hasCreateDatedWorkOrders) fail('capture-session missing create-dated-work-orders');
	else ok('create-dated-work-orders was on TDD at capture');
} else {
	ok('capture-session.json optional until capture runs');
}

if (failed) process.exit(1);
console.log('verify-package: all checks passed');
