import { BadRequestException, Injectable, NotFoundException, OnModuleInit } from '@nestjs/common';
import initSqlJs, { Database } from 'sql.js';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';

export type CreateWorkOrder = { title: string; description?: string; instructions?: string; roomNumber?: string; dueDate?: string; creator?: string };
type WorkOrder = { id: string; number: string; title: string; description: string; instructions: string; roomNumber: string|null; dueDate: string|null; urgency: string; dueDateBadge: string|null; status: string; creator: string; assignee: string|null; createdAt: string; updatedAt: string; assignedAt: string|null; completedAt: string|null };
type StoredWorkOrder = Omit<WorkOrder, 'urgency'|'dueDateBadge'>;

const transitions: Record<string, string[]> = {
  Draft: ['Assigned', 'Cancelled'], Assigned: ['InProgress', 'Cancelled'],
  InProgress: ['Complete', 'Cancelled'], Complete: [], Cancelled: [],
};

@Injectable()
export class WorkOrdersService implements OnModuleInit {
  private db!: Database;
  private readonly filename = (process.env.DATABASE_URL ?? `file:${path.resolve('workorders.db')}`).replace(/^file:/, '');

  async onModuleInit(): Promise<void> {
    const SQL = await initSqlJs();
    this.db = new SQL.Database(existsSync(this.filename) ? new Uint8Array(readFileSync(this.filename)) : undefined);
    this.db.run('CREATE TABLE IF NOT EXISTS SchemaMigration (version INTEGER PRIMARY KEY, appliedAt TEXT NOT NULL)');
    this.db.run(`CREATE TABLE IF NOT EXISTS WorkOrder (
      id TEXT PRIMARY KEY, number TEXT NOT NULL UNIQUE, title TEXT NOT NULL, description TEXT NOT NULL,
      roomNumber TEXT, status TEXT NOT NULL DEFAULT 'Draft', creator TEXT NOT NULL, assignee TEXT,
      createdAt TEXT NOT NULL, updatedAt TEXT NOT NULL, assignedAt TEXT, completedAt TEXT
    )`);
    this.applyDetailsMigration();
    this.persist();
  }

  async list(filters: { status?: string; assignee?: string; q?: string } = {}): Promise<WorkOrder[]> {
    const predicates: string[] = [];
    const params: string[] = [];
    if (filters.status) { predicates.push('status = ?'); params.push(filters.status); }
    if (filters.assignee) { predicates.push('LOWER(COALESCE(assignee,\'\')) = LOWER(?)'); params.push(filters.assignee); }
    if (filters.q?.trim()) {
      predicates.push("(number LIKE ? OR title LIKE ? OR description LIKE ? OR instructions LIKE ? OR roomNumber LIKE ? OR assignee LIKE ?)");
      const term = `%${filters.q.trim()}%`;
      params.push(term, term, term, term, term, term);
    }
    const where = predicates.length ? `WHERE ${predicates.join(' AND ')}` : '';
    return this.all(`SELECT * FROM WorkOrder ${where} ORDER BY createdAt DESC`, ...params).map(order => this.present(order));
  }

  async get(id: string): Promise<WorkOrder> {
    const order = this.one('SELECT * FROM WorkOrder WHERE id = ?', id);
    if (!order) throw new NotFoundException(`Work order ${id} was not found`);
    return this.present(order);
  }

  async create(input: CreateWorkOrder): Promise<WorkOrder> {
    if (!input.title?.trim()) throw new BadRequestException('Title is required');
    this.validateDueDate(input.dueDate);
    const next = (this.one("SELECT COALESCE(MAX(CAST(SUBSTR(number,4) AS INTEGER)),0)+1 AS next FROM WorkOrder") as unknown as { next: number }).next;
    const now = new Date().toISOString();
    const order: StoredWorkOrder = {
      id: crypto.randomUUID(), number: `WO-${String(next).padStart(5, '0')}`, title: input.title.trim(),
      description: (input.description ?? '').slice(0, 4000), instructions: (input.instructions ?? '').slice(0, 4000),
      roomNumber: input.roomNumber?.trim() || null, dueDate: input.dueDate || null, status: 'Draft',
      creator: input.creator?.trim() || 'demo.user', assignee: null, createdAt: now, updatedAt: now, assignedAt: null, completedAt: null,
    };
    this.db.run(`INSERT INTO WorkOrder(id,number,title,description,instructions,roomNumber,dueDate,status,creator,assignee,createdAt,updatedAt,assignedAt,completedAt)
      VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?)`, [order.id, order.number, order.title, order.description, order.instructions, order.roomNumber, order.dueDate, order.status, order.creator, order.assignee, order.createdAt, order.updatedAt, order.assignedAt, order.completedAt]);
    this.persist();
    return this.present(order);
  }

  async transition(id: string, target: string, assignee?: string): Promise<WorkOrder> {
    const order = await this.get(id);
    if (!transitions[order.status]?.includes(target)) throw new BadRequestException(`Invalid transition ${order.status} -> ${target}`);
    if (target === 'Assigned' && !assignee?.trim()) throw new BadRequestException('Assignee is required when assigning');
    const now = new Date().toISOString();
    this.db.run(`UPDATE WorkOrder SET status=?, updatedAt=?,
      assignee=CASE WHEN ?='Assigned' THEN ? ELSE assignee END,
      assignedAt=CASE WHEN ?='Assigned' THEN ? ELSE assignedAt END,
      completedAt=CASE WHEN ?='Complete' THEN ? ELSE completedAt END WHERE id=? AND status=?`,
      [target, now, target, assignee?.trim() ?? null, target, now, target, now, id, order.status]);
    if (this.db.getRowsModified() !== 1) {
      const current = await this.get(id);
      throw new BadRequestException(`Invalid transition ${current.status} -> ${target}`);
    }
    this.persist();
    return this.get(id);
  }

  private applyDetailsMigration(): void {
    const columns = this.all<{ name: string }>('PRAGMA table_info(WorkOrder)').map(column => column.name);
    if (!columns.includes('instructions')) this.db.run("ALTER TABLE WorkOrder ADD COLUMN instructions TEXT NOT NULL DEFAULT ''");
    if (!columns.includes('dueDate')) this.db.run('ALTER TABLE WorkOrder ADD COLUMN dueDate TEXT');
    this.db.run("INSERT OR IGNORE INTO SchemaMigration(version, appliedAt) VALUES (2, datetime('now'))");
  }

  private validateDueDate(dueDate?: string): void {
    if (!dueDate) return;
    const date = new Date(`${dueDate}T00:00:00.000Z`);
    if (!/^\d{4}-\d{2}-\d{2}$/.test(dueDate) || Number.isNaN(date.valueOf()) || date.toISOString().slice(0, 10) !== dueDate) {
      throw new BadRequestException('Due date must be a valid calendar date in YYYY-MM-DD format');
    }
  }

  private present(order: StoredWorkOrder): WorkOrder {
    let urgency = 'None';
    if (order.dueDate && ['Draft', 'Assigned', 'InProgress'].includes(order.status)) {
      const parts = new Intl.DateTimeFormat('en-US', { timeZone: 'America/Chicago', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
      const today = `${parts.find(p => p.type === 'year')!.value}-${parts.find(p => p.type === 'month')!.value}-${parts.find(p => p.type === 'day')!.value}`;
      urgency = order.dueDate === today ? 'DueToday' : order.dueDate < today ? 'Overdue' : 'None';
    }
    return { ...order, urgency, dueDateBadge: order.dueDate ? (urgency === 'DueToday' ? 'Due Today' : urgency === 'Overdue' ? 'Overdue' : 'On Track') : null };
  }

  private one<T = StoredWorkOrder>(sql: string, ...params: unknown[]): T|undefined { return this.all<T>(sql, ...params)[0]; }
  private all<T = StoredWorkOrder>(sql: string, ...params: unknown[]): T[] {
    const statement = this.db.prepare(sql, params as (string|number|null|Uint8Array)[]);
    const rows: T[] = [];
    while (statement.step()) rows.push(statement.getAsObject() as unknown as T);
    statement.free();
    return rows;
  }
  private persist(): void { writeFileSync(this.filename, Buffer.from(this.db.export())); }
}
