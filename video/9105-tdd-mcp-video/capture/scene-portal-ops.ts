import {
	extractStill,
	finishRecording,
	launchRecordedContext,
	loginAs,
	openSearch,
	pause,
	selectById
} from './helpers';
import type {OpsCaptureResult} from './scene-mcp-ops';

/**
 * Portal proof for MCP ops: search filters, manage fields, lifecycle heading, attachments list.
 */
export async function capturePortalOps(ops: OpsCaptureResult, batchNumbers: string[]): Promise<void> {
	const {browser, context, page} = await launchRecordedContext('portal-ops');
	await loginAs(page, 'tlovejoy');
	await pause(600);

	await openSearch(page);
	await selectById(page, 'CreatorSelect', 'tlovejoy');
	await selectById(page, 'AssigneeSelect', 'gwillie');
	await selectById(page, 'StatusSelect', 'Assigned');
	await page.locator('#SearchButton').click();
	await page.waitForLoadState('networkidle');
	await pause(1800);

	const draftNumber = ops.instructionNumber;
	if (draftNumber) {
		await selectById(page, 'StatusSelect', 'Draft');
		await selectById(page, 'AssigneeSelect', '');
		await page.locator('#SearchButton').click();
		await page.waitForLoadState('networkidle');
		await pause(1000);
		const link = page.getByTestId(`WorkOrderLink${draftNumber}`);
		if ((await link.count()) > 0) {
			await link.first().click();
			await page.waitForLoadState('networkidle');
			await pause(2200);
			await page.getByTestId('Instructions').scrollIntoViewIfNeeded().catch(() => undefined);
			await pause(1500);
			await page.getByTestId('AttachmentsSection').scrollIntoViewIfNeeded().catch(() => undefined);
			await pause(1500);
		}
	}

	if (ops.lifecycleNumber) {
		await openSearch(page);
		await selectById(page, 'StatusSelect', 'Complete');
		await selectById(page, 'AssigneeSelect', 'gwillie');
		await selectById(page, 'CreatorSelect', 'tlovejoy');
		await page.locator('#SearchButton').click();
		await page.waitForLoadState('networkidle');
		await pause(1200);
		const link = page.getByTestId(`WorkOrderLink${ops.lifecycleNumber}`);
		if ((await link.count()) > 0) {
			await link.first().click();
			await page.waitForLoadState('networkidle');
			await pause(2500);
		}
	} else if (batchNumbers[0]) {
		await openSearch(page);
		await pause(2000);
	}

	const webm = await finishRecording(context, browser, page, 'portal-ops');
	await extractStill(webm, 'portal-ops-final');
}
