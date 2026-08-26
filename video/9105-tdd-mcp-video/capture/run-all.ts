import fs from 'fs';
import path from 'path';
import {captureMcpBatch} from './scene-mcp-batch';
import {capturePortalBatch} from './scene-portal-batch';
import {captureMcpOps} from './scene-mcp-ops';
import {capturePortalOps} from './scene-portal-ops';

/**
 * Captures live TDD MCP + portal footage for the #9105 customer-training video.
 *
 * Env:
 *  - TDD_PORTAL_URL (default: https://ui-gh.icywave-25720613.southcentralus.azurecontainerapps.io)
 *  - TDD_MCP_URL    (default: …/mcp)
 */
async function main(): Promise<void> {
	const assetsDir = path.join(__dirname, 'assets');
	fs.mkdirSync(assetsDir, {recursive: true});

	console.log('=== MCP batch (create-dated-work-orders) ===');
	const batch = await captureMcpBatch();
	console.log('create-dated-work-orders present:', batch.hasCreateDated);
	console.log('numbers:', batch.numbers.join(', ') || '(none)');

	const sessionMeta = {
		capturedAt: new Date().toISOString(),
		toolNames: batch.toolNames,
		hasCreateDatedWorkOrders: batch.hasCreateDated,
		batchNumbers: batch.numbers,
		batchResult: batch.batchResult
	};
	fs.writeFileSync(
		path.join(__dirname, '..', 'artifacts', 'capture-session.json'),
		JSON.stringify(sessionMeta, null, 2)
	);

	console.log('=== Portal batch proof ===');
	await capturePortalBatch(batch.numbers);

	console.log('=== MCP ops tour ===');
	const ops = await captureMcpOps();
	console.log('instruction draft:', ops.instructionNumber);
	console.log('lifecycle:', ops.lifecycleNumber);
	if (ops.sessionNotes.length) {
		console.log('honest no-ops:', ops.sessionNotes.join('; '));
	}

	console.log('=== Portal ops proof ===');
	await capturePortalOps(ops, batch.numbers);

	console.log('All scenes captured.');
}

main().catch((err) => {
	console.error(err);
	process.exit(1);
});
