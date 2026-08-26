/**
 * Minimal Streamable HTTP MCP client for the live TDD /mcp endpoint.
 * Never mocks tool results — every call goes to the real server.
 */
export const MCP_URL =
	process.env.TDD_MCP_URL ??
	'https://ui-gh.icywave-25720613.southcentralus.azurecontainerapps.io/mcp';

type JsonRpc = {
	jsonrpc: '2.0';
	id?: number;
	method?: string;
	params?: unknown;
	result?: unknown;
	error?: {message?: string};
};

function parseSseJson(body: string): JsonRpc {
	const match = body.match(/data:\s*(\{[\s\S]*\})/);
	if (!match) {
		throw new Error(`No SSE data in MCP response: ${body.slice(0, 400)}`);
	}
	return JSON.parse(match[1]) as JsonRpc;
}

export class McpHttpClient {
	private sessionId: string | null = null;
	private nextId = 1;
	private toolNames: string[] = [];

	async connect(): Promise<string[]> {
		const init = await this.post(
			{
				jsonrpc: '2.0',
				id: this.nextId++,
				method: 'initialize',
				params: {
					protocolVersion: '2024-11-05',
					capabilities: {},
					clientInfo: {name: '9105-tdd-mcp-video', version: '1.0'}
				}
			},
			true
		);
		if (init.error) {
			throw new Error(`MCP initialize failed: ${JSON.stringify(init.error)}`);
		}
		await this.post({jsonrpc: '2.0', method: 'notifications/initialized'}, false);
		const listed = await this.post(
			{jsonrpc: '2.0', id: this.nextId++, method: 'tools/list', params: {}},
			true
		);
		const tools = (listed.result as {tools?: {name: string}[]})?.tools ?? [];
		this.toolNames = tools.map((t) => t.name).sort();
		return this.toolNames;
	}

	hasTool(name: string): boolean {
		return this.toolNames.includes(name);
	}

	getToolNames(): string[] {
		return [...this.toolNames];
	}

	async callTool(name: string, args: Record<string, unknown>): Promise<string> {
		if (!this.hasTool(name)) {
			return `NOT_ON_TDD: ${name} is not on TDD yet.`;
		}
		const response = await this.post(
			{
				jsonrpc: '2.0',
				id: this.nextId++,
				method: 'tools/call',
				params: {name, arguments: args}
			},
			true
		);
		if (response.error) {
			return `ERROR: ${response.error.message ?? JSON.stringify(response.error)}`;
		}
		const content = (response.result as {content?: {type: string; text?: string}[]})?.content ?? [];
		return content
			.filter((c) => c.type === 'text' && c.text)
			.map((c) => c.text as string)
			.join('\n');
	}

	private async post(payload: JsonRpc, expectBody: boolean): Promise<JsonRpc> {
		const headers: Record<string, string> = {
			'Content-Type': 'application/json',
			Accept: 'application/json, text/event-stream'
		};
		if (this.sessionId) {
			headers['Mcp-Session-Id'] = this.sessionId;
		}
		const res = await fetch(MCP_URL, {
			method: 'POST',
			headers,
			body: JSON.stringify(payload)
		});
		const sid = res.headers.get('mcp-session-id');
		if (sid) {
			this.sessionId = sid;
		}
		const text = await res.text();
		if (!expectBody || !text.trim()) {
			return {jsonrpc: '2.0'};
		}
		if (text.trim().startsWith('{')) {
			return JSON.parse(text) as JsonRpc;
		}
		return parseSseJson(text);
	}
}

export function nextSaturdays(count: number): string[] {
	const today = new Date();
	const day = today.getUTCDay();
	let add = (6 - day + 7) % 7;
	if (add === 0) {
		add = 7;
	}
	const first = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate() + add));
	const dates: string[] = [];
	for (let i = 0; i < count; i++) {
		const d = new Date(first);
		d.setUTCDate(first.getUTCDate() + i * 7);
		dates.push(d.toISOString().slice(0, 10));
	}
	return dates;
}

export function extractWorkOrderNumbers(text: string): string[] {
	const matches = text.match(/\b[0-9A-F]{7}\b/gi) ?? [];
	return [...new Set(matches.map((m) => m.toUpperCase()))];
}
