import path from 'path';
import {
	ChatTurn,
	extractStill,
	finishRecording,
	launchRecordedContext,
	pause,
	writeGrokSessionHtml
} from './helpers';
import {McpHttpClient, extractWorkOrderNumbers} from './mcp-client';

export type OpsCaptureResult = {
	lifecycleNumber: string | null;
	instructionNumber: string | null;
	sessionNotes: string[];
};

/**
 * Films remaining live MCP tools (honest no-op captions when tools/list omits a name).
 */
export async function captureMcpOps(client?: McpHttpClient): Promise<OpsCaptureResult> {
	const mcp = client ?? new McpHttpClient();
	if (!client) {
		await mcp.connect();
	}

	const turns: ChatTurn[] = [];
	const notes: string[] = [];
	let lifecycleNumber: string | null = null;
	let instructionNumber: string | null = null;

	async function tool(
		name: string,
		args: Record<string, unknown>,
		userPrompt?: string
	): Promise<string> {
		if (userPrompt) {
			turns.push({kind: 'user', text: userPrompt});
		}
		const missing = !mcp.hasTool(name);
		const result = missing
			? `${name} is not on TDD yet.`
			: await mcp.callTool(name, args);
		turns.push({
			kind: 'tool',
			name,
			args: JSON.stringify(args, null, 2),
			result,
			missing
		});
		if (missing) {
			notes.push(`${name}: not on TDD yet`);
			turns.push({
				kind: 'assistant',
				text: `That tool is not on TDD yet — skipping ${name}.`
			});
		}
		return result;
	}

	await tool(
		'list-work-orders',
		{status: 'Assigned', creatorUsername: 'tlovejoy', assigneeUsername: 'gwillie'},
		'List assigned work orders Lovejoy created for Willie.'
	);

	const getProbe = await tool(
		'list-work-orders',
		{status: 'Assigned', assigneeUsername: 'gwillie'},
		'Pick one of Willie\'s assigned orders and show full details, including instructions.'
	);
	const probeNumbers = extractWorkOrderNumbers(getProbe);
	const sampleNumber = probeNumbers[0];
	if (sampleNumber) {
		const detail = await tool('get-work-order', {workOrderNumber: sampleNumber});
		turns.push({
			kind: 'assistant',
			text: detail.includes('Instructions')
				? `Here is ${sampleNumber}. Instructions field is included even when blank.`
				: `Here is ${sampleNumber}.`
		});
	}

	const created = await tool(
		'create-work-order',
		{
			title: 'Replace sanctuary light bulbs',
			description: 'Replace burned-out bulbs in the sanctuary chandelier before Sunday.',
			creatorUsername: 'tlovejoy',
			roomNumber: 'Sanctuary',
			dueDate: nextDue(),
			instructions: 'Use the ladder from the utility closet. Dispose of old bulbs safely.'
		},
		'Create a draft for sanctuary light bulbs with instructions for Willie.'
	);
	const createdNumbers = extractWorkOrderNumbers(created);
	instructionNumber = createdNumbers[0] ?? null;

	if (instructionNumber) {
		await tool(
			'save-work-order',
			{
				workOrderNumber: instructionNumber,
				executingUsername: 'tlovejoy',
				title: 'Replace sanctuary light bulbs — balcony too',
				description: 'Replace burned-out bulbs in the sanctuary chandelier and balcony fixtures.',
				instructions: 'Use the ladder from the utility closet. Check balcony sconces as well.',
				roomNumber: 'Sanctuary balcony',
				dueDate: ''
			},
			'Save updates on that draft — clear the due date, keep it as Draft.'
		);
	}

	const draftCreate = await tool(
		'create-work-order',
		{
			title: 'Edge the fellowship hall walkway',
			description: 'Edge and blow the walkway from the fellowship hall to the parking lot.',
			creatorUsername: 'tlovejoy',
			roomNumber: 'Fellowship hall walk'
		},
		'Create another draft, then assign it to Willie, begin it, and complete it.'
	);
	lifecycleNumber = extractWorkOrderNumbers(draftCreate)[0] ?? null;

	if (lifecycleNumber) {
		await tool('execute-work-order-command', {
			workOrderNumber: lifecycleNumber,
			commandName: 'DraftToAssignedCommand',
			executingUsername: 'tlovejoy',
			assigneeUsername: 'gwillie'
		});
		await tool('execute-work-order-command', {
			workOrderNumber: lifecycleNumber,
			commandName: 'AssignedToInProgressCommand',
			executingUsername: 'gwillie'
		});
		await tool('execute-work-order-command', {
			workOrderNumber: lifecycleNumber,
			commandName: 'InProgressToCompleteCommand',
			executingUsername: 'gwillie'
		});
		const after = await tool('get-work-order', {workOrderNumber: lifecycleNumber});
		turns.push({
			kind: 'assistant',
			text: `Lifecycle finished for ${lifecycleNumber}.\n${after.slice(0, 500)}`
		});
	}

	await tool('list-employees', {}, 'List the church employees available for assignment.');
	await tool('get-employee', {username: 'gwillie'}, 'Show Willie\'s employee record.');

	if (sampleNumber) {
		await tool(
			'list-work-order-attachments',
			{workOrderNumber: sampleNumber},
			`List attachments on ${sampleNumber}. Remember: list only — no file upload on TDD yet.`
		);
		turns.push({
			kind: 'assistant',
			text:
				'Attachments are list-only on TDD. There is no file picker yet, so no upload is attempted.'
		});
	} else if (!mcp.hasTool('list-work-order-attachments')) {
		await tool('list-work-order-attachments', {workOrderNumber: 'UNKNOWN'}, undefined);
	}

	const htmlPath = path.join(__dirname, 'assets', 'grok-ops.html');
	writeGrokSessionHtml(turns, htmlPath);

	const {browser, context, page} = await launchRecordedContext('mcp-ops');
	await page.goto(`file://${htmlPath}`);
	await pause(2000);
	// Slow scroll through the tool tour
	await page.evaluate(async () => {
		const chat = document.getElementById('chat');
		if (!chat) return;
		const max = chat.scrollHeight - chat.clientHeight;
		for (let y = 0; y <= max; y += 40) {
			chat.scrollTop = y;
			await new Promise((r) => setTimeout(r, 80));
		}
		chat.scrollTop = max;
	});
	await pause(2500);
	const webm = await finishRecording(context, browser, page, 'mcp-ops');
	await extractStill(webm, 'mcp-ops-final');

	return {lifecycleNumber, instructionNumber, sessionNotes: notes};
}

function nextDue(): string {
	const d = new Date();
	d.setUTCDate(d.getUTCDate() + 10);
	return d.toISOString().slice(0, 10);
}
