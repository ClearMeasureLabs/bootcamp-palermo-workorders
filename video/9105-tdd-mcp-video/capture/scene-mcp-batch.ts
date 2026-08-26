import path from 'path';
import {
	ChatTurn,
	extractStill,
	finishRecording,
	launchRecordedContext,
	pause,
	writeGrokSessionHtml
} from './helpers';
import {McpHttpClient, extractWorkOrderNumbers, nextSaturdays} from './mcp-client';

export type BatchCaptureResult = {
	toolNames: string[];
	hasCreateDated: boolean;
	numbers: string[];
	batchResult: string;
};

/**
 * Films Grok calling create-dated-work-orders on live TDD MCP (or honest no-op).
 */
export async function captureMcpBatch(): Promise<BatchCaptureResult> {
	const client = new McpHttpClient();
	const toolNames = await client.connect();
	console.log('tools/list:', toolNames.join(', '));

	const dates = nextSaturdays(3);
	const turns: ChatTurn[] = [
		{
			kind: 'user',
			text:
				'Please schedule Saturday lawn mowing on the sanctuary grounds for the next three Saturdays. Create them as Lovejoy for Willie in one shot.'
		}
	];

	let batchResult = '';
	let numbers: string[] = [];
	const hasCreateDated = client.hasTool('create-dated-work-orders');

	if (!hasCreateDated) {
		batchResult = 'create-dated-work-orders is not on TDD yet.';
		turns.push({
			kind: 'tool',
			name: 'create-dated-work-orders',
			args: JSON.stringify(
				{
					creatorUsername: 'tlovejoy',
					assigneeUsername: 'gwillie',
					title: 'Saturday lawn mow — sanctuary grounds',
					description:
						'Mow the sanctuary lawn and trim the hedges along the parking lot before Sunday service.',
					dueDates: dates.join(',')
				},
				null,
				2
			),
			result: batchResult,
			missing: true
		});
		turns.push({
			kind: 'assistant',
			text: 'That tool is not on TDD yet — cannot create the Saturday batch from here.'
		});
	} else {
		const args = {
			creatorUsername: 'tlovejoy',
			assigneeUsername: 'gwillie',
			title: 'Saturday lawn mow — sanctuary grounds',
			description:
				'Mow the sanctuary lawn and trim the hedges along the parking lot before Sunday service.',
			dueDates: dates.join(',')
		};
		batchResult = await client.callTool('create-dated-work-orders', args);
		numbers = extractWorkOrderNumbers(batchResult);
		turns.push({
			kind: 'tool',
			name: 'create-dated-work-orders',
			args: JSON.stringify(args, null, 2),
			result: batchResult
		});
		turns.push({
			kind: 'assistant',
			text: `Done. Lovejoy created Saturday lawn mows for Willie:\n${batchResult}`
		});
	}

	const htmlPath = path.join(__dirname, 'assets', 'grok-batch.html');
	writeGrokSessionHtml(turns, htmlPath);

	const {browser, context, page} = await launchRecordedContext('mcp-batch');
	await page.goto(`file://${htmlPath}`);
	await pause(2500);
	await page.evaluate(() => {
		const chat = document.getElementById('chat');
		if (chat) chat.scrollTop = chat.scrollHeight;
	});
	await pause(3500);
	const webm = await finishRecording(context, browser, page, 'mcp-batch');
	await extractStill(webm, 'mcp-batch-final');

	return {toolNames, hasCreateDated, numbers, batchResult};
}
