import {captureAssignScene} from './scene-assign';
import {captureBeginCompleteScene} from './scene-begin-complete';
import {captureFeatureTourScene} from './scene-feature-tour';

/**
 * Drives the real running Work Order app end-to-end via Playwright and records
 * .webm footage for each demo-video scene into video/footage/.
 *
 * Prerequisites (see video/README.md):
 *  - UI.Server running and healthy at DEMO_BASE_URL (default https://localhost:7175)
 *  - Database seeded with tlovejoy and jcuevas (ZDataLoader / ChurchBulletinVideo)
 */
async function main(): Promise<void> {
	await captureAssignScene();
	await captureBeginCompleteScene();
	await captureFeatureTourScene();
	console.log('All scenes captured.');
}

main().catch((err) => {
	console.error(err);
	process.exit(1);
});
