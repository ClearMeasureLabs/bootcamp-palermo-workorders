import { BadRequestException, Injectable, NotFoundException, OnModuleInit } from '@nestjs/common';
import initSqlJs, { Database } from 'sql.js';
import { existsSync, writeFileSync } from 'node:fs';
import path from 'node:path';

export type CreateWorkOrder = { title: string; description: string; roomNumber?: string; creator?: string };
type WorkOrder = { id: string; number: string; title: string; description: string; roomNumber: string|null; status: string; creator: string; assignee: string|null; createdAt: string; updatedAt: string; assignedAt: string|null; completedAt: string|null };

const transitions: Record<string, string[]> = {
  Draft: ['Assigned', 'Cancelled'],
  Assigned: ['InProgress', 'Cancelled'],
  InProgress: ['Complete', 'Cancelled'],
  Complete: [],
  Cancelled: [],
};

@Injectable()
export class WorkOrdersService implements OnModuleInit {
  private db!: Database;
  private readonly filename = (process.env.DATABASE_URL ?? `file:${path.resolve('workorders.db')}`).replace(/^file:/, '');

  async onModuleInit(): Promise<void> {
    const SQL = await initSqlJs();
    this.db = new SQL.Database(existsSync(this.filename) ? new Uint8Array(require('node:fs').readFileSync(this.filename)) : undefined);
    this.db.run(`CREATE TABLE IF NOT EXISTS WorkOrder (
      id TEXT PRIMARY KEY, number TEXT NOT NULL UNIQUE, title TEXT NOT NULL, description TEXT NOT NULL,
      roomNumber TEXT, status TEXT NOT NULL DEFAULT 'Draft', creator TEXT NOT NULL, assignee TEXT,
      createdAt TEXT NOT NULL, updatedAt TEXT NOT NULL, assignedAt TEXT, completedAt TEXT
    )`);
    this.persist();
  }

  async list(status?: string): Promise<WorkOrder[]> {
    return this.all(`SELECT * FROM WorkOrder ${status ? 'WHERE status = ?' : ''} ORDER BY createdAt DESC`, ...(status ? [status] : []));
  }

  async get(id: string): Promise<WorkOrder> {
    const order = this.one('SELECT * FROM WorkOrder WHERE id = ?', id);
    if (!order) throw new NotFoundException(`Work order ${id} was not found`);
    return order;
  }

  async create(input: CreateWorkOrder): Promise<WorkOrder> {
    if (!input.title?.trim()) throw new BadRequestException('Title is required');
    const next = (this.one("SELECT COALESCE(MAX(CAST(SUBSTR(number,4) AS INTEGER)),0)+1 AS next FROM WorkOrder") as unknown as { next: number }).next;
    const now = new Date().toISOString();
    const order = { id: crypto.randomUUID(), number: `WO-${String(next).padStart(5, '0')}`, title: input.title.trim(), description: input.description?.trim() ?? '', roomNumber: input.roomNumber?.trim() || null, status: 'Draft', creator: input.creator?.trim() || 'demo.user', assignee: null, createdAt: now, updatedAt: now, assignedAt: null, completedAt: null };
    this.db.run(`INSERT INTO WorkOrder(id,number,title,description,roomNumber,status,creator,assignee,createdAt,updatedAt,assignedAt,completedAt)
      VALUES (?,?,?,?,?,?,?,?,?,?,?,?)`, [order.id, order.number, order.title, order.description, order.roomNumber, order.status, order.creator, order.assignee, order.createdAt, order.updatedAt, order.assignedAt, order.completedAt]);
    this.persist();
    return order;
  }

  async transition(id: string, target: string, assignee?: string): Promise<WorkOrder> {
    const order = await this.get(id);
    if (!transitions[order.status]?.includes(target)) {
      throw new BadRequestException(`Invalid transition ${order.status} -> ${target}`);
    }
    if (target === 'Assigned' && !assignee?.trim()) throw new BadRequestException('Assignee is required when assigning');
    const now = new Date().toISOString();
    this.db.run(`UPDATE WorkOrder SET status=?, updatedAt=?,
      assignee=CASE WHEN ?='Assigned' THEN ? ELSE assignee END,
      assignedAt=CASE WHEN ?='Assigned' THEN ? ELSE assignedAt END,
      completedAt=CASE WHEN ?='Complete' THEN ? ELSE completedAt END WHERE id=?`,
      [target, now, target, assignee?.trim() ?? null, target, now, target, now, id]);
    this.persist();
    return this.get(id);
  }

  private one(sql: string, ...params: unknown[]): WorkOrder|undefined {
    const rows = this.all(sql, ...params);
    return rows[0];
  }

  private all(sql: string, ...params: unknown[]): WorkOrder[] {
    const statement = this.db.prepare(sql, params as (string|number|null|Uint8Array)[]);
    const rows: WorkOrder[] = [];
    while (statement.step()) rows.push(statement.getAsObject() as unknown as WorkOrder);
    statement.free();
    return rows;
  }

  private persist(): void { writeFileSync(this.filename, Buffer.from(this.db.export())); }
}
