import { BadRequestException, ForbiddenException, Injectable, NotFoundException, OnModuleInit, UnauthorizedException } from '@nestjs/common';
import initSqlJs, { Database } from 'sql.js';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';

export type CreateWorkOrder = { title: string; description?: string; instructions?: string; roomNumber?: string; dueDate?: string };
type Employee = { username: string; firstName: string; lastName: string; canCreate: number; canFulfill: number };
type Attachment = { id: string; workOrderId: string; fileName: string; contentType: string; fileSize: number; uploadedBy: string; uploadedByName: string; uploadedDate: string };
type WorkOrder = { id: string; number: string; title: string; description: string; instructions: string; roomNumber: string|null; dueDate: string|null; urgency: string; dueDateBadge: string|null; status: string; creator: string; assignee: string|null; createdAt: string; updatedAt: string; assignedAt: string|null; completedAt: string|null };
type StoredWorkOrder = Omit<WorkOrder, 'urgency'|'dueDateBadge'>;

const transitions: Record<string, string[]> = {
  Draft: ['Assigned'], Assigned: ['InProgress', 'Cancelled'],
  InProgress: ['Assigned', 'Complete'], Complete: [], Cancelled: [],
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
    this.applyIdentityMigration();
    this.applyAttachmentMigration();
    this.persist();
  }

  async list(token: string | undefined, filters: { status?: string; assignee?: string; q?: string } = {}): Promise<WorkOrder[]> {
    this.requireSession(token);
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

  async get(id: string, token?: string): Promise<WorkOrder> {
    this.requireSession(token);
    const order = this.one('SELECT * FROM WorkOrder WHERE id = ?', id);
    if (!order) throw new NotFoundException(`Work order ${id} was not found`);
    return this.present(order);
  }

  attachments(id: string, token?: string): Attachment[] {
    this.requireSession(token);
    if (!this.one('SELECT id FROM WorkOrder WHERE id=?', id)) throw new NotFoundException(`Work order ${id} was not found`);
    return this.all<Attachment>(`SELECT a.id,a.workOrderId,a.fileName,a.contentType,a.fileSize,a.uploadedBy,
      e.firstName || ' ' || e.lastName AS uploadedByName,a.uploadedDate
      FROM WorkOrderAttachment a JOIN Employee e ON e.username=a.uploadedBy WHERE a.workOrderId=? ORDER BY a.uploadedDate,a.id`, id);
  }

  addAttachment(id: string, input: { fileName: string; contentType?: string; fileSize: number }, token?: string): Attachment {
    const actor = this.requireSession(token);
    if (!this.one('SELECT id FROM WorkOrder WHERE id=?', id)) throw new NotFoundException(`Work order ${id} was not found`);
    if (!input.fileName?.trim()) throw new BadRequestException('File name is required');
    if (input.fileName.length > 500) throw new BadRequestException('File name must be 500 characters or fewer');
    const contentType = input.contentType ?? '';
    if (contentType.length > 200) throw new BadRequestException('Content type must be 200 characters or fewer');
    if (!Number.isSafeInteger(input.fileSize) || input.fileSize < 0) throw new BadRequestException('File size must be a non-negative integer');
    const attachment: Attachment = { id: crypto.randomUUID(), workOrderId: id, fileName: input.fileName, contentType,
      fileSize: input.fileSize, uploadedBy: actor.username, uploadedByName: `${actor.firstName} ${actor.lastName}`, uploadedDate: new Date().toISOString() };
    this.db.run('INSERT INTO WorkOrderAttachment(id,workOrderId,fileName,contentType,fileSize,uploadedBy,uploadedDate) VALUES (?,?,?,?,?,?,?)',
      [attachment.id, id, attachment.fileName, contentType, input.fileSize, actor.username, attachment.uploadedDate]);
    this.persist();
    return attachment;
  }

  async create(input: CreateWorkOrder, token?: string): Promise<WorkOrder> {
    const actor = this.requireSession(token);
    if (!actor.canCreate) throw new ForbiddenException('Your role cannot create work orders');
    if (!input.title?.trim()) throw new BadRequestException('Title is required');
    this.validateDueDate(input.dueDate);
    const next = (this.one("SELECT COALESCE(MAX(CAST(SUBSTR(number,4) AS INTEGER)),0)+1 AS next FROM WorkOrder") as unknown as { next: number }).next;
    const now = new Date().toISOString();
    const order: StoredWorkOrder = {
      id: crypto.randomUUID(), number: `WO-${String(next).padStart(5, '0')}`, title: input.title.trim(),
      description: (input.description ?? '').slice(0, 4000), instructions: (input.instructions ?? '').slice(0, 4000),
      roomNumber: input.roomNumber?.trim() || null, dueDate: input.dueDate || null, status: 'Draft',
      creator: actor.username, assignee: null, createdAt: now, updatedAt: now, assignedAt: null, completedAt: null,
    };
    this.db.run(`INSERT INTO WorkOrder(id,number,title,description,instructions,roomNumber,dueDate,status,creator,assignee,createdAt,updatedAt,assignedAt,completedAt)
      VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?)`, [order.id, order.number, order.title, order.description, order.instructions, order.roomNumber, order.dueDate, order.status, order.creator, order.assignee, order.createdAt, order.updatedAt, order.assignedAt, order.completedAt]);
    this.persist();
    return this.present(order);
  }

  async transition(id: string, target: string, assignee?: string, token?: string): Promise<WorkOrder> {
    const actor = this.requireSession(token);
    const order = await this.get(id, token);
    if (!transitions[order.status]?.includes(target)) throw new BadRequestException(`Invalid transition ${order.status} -> ${target}`);
    const isCreator = order.creator.toLowerCase() === actor.username.toLowerCase();
    const isAssignee = order.assignee?.toLowerCase() === actor.username.toLowerCase();
    const isInitialAssignment = order.status === 'Draft' && target === 'Assigned';
    if (isInitialAssignment && !isCreator) throw new ForbiddenException('Only the work-order creator can assign it');
    if (target === 'Cancelled' && !isCreator) throw new ForbiddenException('Only the work-order creator can cancel it');
    if ((target === 'InProgress' || target === 'Complete' || (order.status === 'InProgress' && target === 'Assigned')) && !isAssignee) {
      throw new ForbiddenException('Only the assigned employee can perform this transition');
    }
    if (isInitialAssignment && !assignee?.trim()) throw new BadRequestException('Assignee is required when assigning');
    const requestedAssignee = isInitialAssignment ? this.employee(assignee!.trim()) : undefined;
    if (isInitialAssignment && !requestedAssignee?.canFulfill) throw new BadRequestException('Assignee must be an employee who can fulfill work orders');
    const now = new Date().toISOString();
    this.db.run(`UPDATE WorkOrder SET status=?, updatedAt=?,
      assignee=CASE WHEN ?='Assigned' AND ?='Draft' THEN ? WHEN ?='Cancelled' THEN NULL ELSE assignee END,
      assignedAt=CASE WHEN ?='Assigned' AND ?='Draft' THEN ? WHEN ?='Cancelled' THEN NULL ELSE assignedAt END,
      completedAt=CASE WHEN ?='Complete' THEN ? ELSE completedAt END WHERE id=? AND status=?`,
      [target, now, target, order.status, assignee?.trim() ?? null, target, target, order.status, now, target, target, now, id, order.status]);
    if (this.db.getRowsModified() !== 1) {
      const current = await this.get(id, token);
      throw new BadRequestException(`Invalid transition ${current.status} -> ${target}`);
    }
    this.persist();
    return this.get(id, token);
  }

  employees(canFulfill?: boolean): Array<{ username: string; displayName: string; canCreate: boolean; canFulfill: boolean }> {
    const employees = this.all<Employee>(`SELECT e.username,e.firstName,e.lastName,
      MAX(r.canCreate) AS canCreate,MAX(r.canFulfill) AS canFulfill
      FROM Employee e JOIN EmployeeRole er ON er.username=e.username JOIN Role r ON r.name=er.roleName
      GROUP BY e.username,e.firstName,e.lastName ORDER BY e.lastName,e.firstName`);
    return employees.filter(employee => canFulfill === undefined || Boolean(employee.canFulfill) === canFulfill)
      .map(employee => ({ username: employee.username, displayName: `${employee.firstName} ${employee.lastName}`.toUpperCase(), canCreate: Boolean(employee.canCreate), canFulfill: Boolean(employee.canFulfill) }));
  }

  login(username: string): { token: string; user: ReturnType<WorkOrdersService['profile']> } {
    const employee = this.employee(username);
    if (!employee) throw new UnauthorizedException('Select a valid employee');
    const token = crypto.randomUUID();
    this.db.run('INSERT INTO AuthSession(token,username,createdAt) VALUES (?,?,?)', [token, employee.username, new Date().toISOString()]);
    this.persist();
    return { token, user: this.profile(employee) };
  }

  me(token: string | undefined): ReturnType<WorkOrdersService['profile']> {
    return this.profile(this.requireSession(token));
  }

  logout(token: string | undefined): { loggedOut: true } {
    if (!token) throw new UnauthorizedException('Sign in is required');
    this.db.run('DELETE FROM AuthSession WHERE token=?', [token]);
    this.persist();
    return { loggedOut: true };
  }

  private applyDetailsMigration(): void {
    const columns = this.all<{ name: string }>('PRAGMA table_info(WorkOrder)').map(column => column.name);
    if (!columns.includes('instructions')) this.db.run("ALTER TABLE WorkOrder ADD COLUMN instructions TEXT NOT NULL DEFAULT ''");
    if (!columns.includes('dueDate')) this.db.run('ALTER TABLE WorkOrder ADD COLUMN dueDate TEXT');
    this.db.run("INSERT OR IGNORE INTO SchemaMigration(version, appliedAt) VALUES (2, datetime('now'))");
  }

  private applyIdentityMigration(): void {
    this.db.run('CREATE TABLE IF NOT EXISTS Role (name TEXT PRIMARY KEY, canCreate INTEGER NOT NULL, canFulfill INTEGER NOT NULL)');
    this.db.run('CREATE TABLE IF NOT EXISTS Employee (username TEXT PRIMARY KEY, firstName TEXT NOT NULL, lastName TEXT NOT NULL)');
    this.db.run('CREATE TABLE IF NOT EXISTS EmployeeRole (username TEXT NOT NULL, roleName TEXT NOT NULL, PRIMARY KEY(username,roleName))');
    this.db.run('CREATE TABLE IF NOT EXISTS AuthSession (token TEXT PRIMARY KEY, username TEXT NOT NULL, createdAt TEXT NOT NULL)');
    const roles = [
      ['Manager', 1, 0], ['Minister', 1, 1], ['Deacon', 0, 1], ['Groundskeeper', 0, 1], ['Fulfillment', 0, 1], ['Parishioner', 0, 0],
    ] as const;
    for (const role of roles) this.db.run('INSERT OR IGNORE INTO Role(name,canCreate,canFulfill) VALUES (?,?,?)', [...role]);
    const employees: Array<[string,string,string,string[]]> = [
      ['hsimpson', 'Homer', 'Simpson', ['Manager']],
      ['tlovejoy', 'Timothy', 'Lovejoy Jr', ['Minister']],
      ['nflanders', 'Ned', 'Flanders', ['Deacon', 'Parishioner']],
      ['hlovejoy', 'Helen', 'Lovejoy', ['Parishioner']],
      ['gwillie', 'Groundskeeper Willie', 'MacDougal', ['Groundskeeper']],
      ['demo.tech', 'Alex', 'Technician', ['Fulfillment']],
      ['demo.user', 'Demo', 'User', ['Manager']],
    ];
    for (const [username, firstName, lastName, roleNames] of employees) {
      this.db.run('INSERT OR IGNORE INTO Employee(username,firstName,lastName) VALUES (?,?,?)', [username, firstName, lastName]);
      for (const roleName of roleNames) this.db.run('INSERT OR IGNORE INTO EmployeeRole(username,roleName) VALUES (?,?)', [username, roleName]);
    }
    this.db.run("INSERT OR IGNORE INTO SchemaMigration(version, appliedAt) VALUES (3, datetime('now'))");
  }

  private applyAttachmentMigration(): void {
    this.db.run(`CREATE TABLE IF NOT EXISTS WorkOrderAttachment (
      id TEXT PRIMARY KEY, workOrderId TEXT NOT NULL REFERENCES WorkOrder(id) ON DELETE CASCADE,
      fileName TEXT NOT NULL CHECK(length(fileName)<=500), contentType TEXT NOT NULL CHECK(length(contentType)<=200),
      fileSize INTEGER NOT NULL CHECK(fileSize>=0), uploadedBy TEXT NOT NULL REFERENCES Employee(username), uploadedDate TEXT NOT NULL
    )`);
    this.db.run('CREATE INDEX IF NOT EXISTS IX_WorkOrderAttachment_WorkOrderId ON WorkOrderAttachment(workOrderId,uploadedDate)');
    this.db.run("INSERT OR IGNORE INTO SchemaMigration(version, appliedAt) VALUES (4, datetime('now'))");
  }

  private employee(username: string): Employee | undefined {
    return this.one<Employee>(`SELECT e.username,e.firstName,e.lastName,
      MAX(r.canCreate) AS canCreate,MAX(r.canFulfill) AS canFulfill
      FROM Employee e JOIN EmployeeRole er ON er.username=e.username JOIN Role r ON r.name=er.roleName
      WHERE LOWER(e.username)=LOWER(?) GROUP BY e.username,e.firstName,e.lastName`, username);
  }

  private requireSession(token?: string): Employee {
    if (!token) throw new UnauthorizedException('Sign in is required');
    const session = this.one<{ username: string }>("SELECT username FROM AuthSession WHERE token=? AND julianday(createdAt) >= julianday('now','-12 hours')", token);
    const employee = session ? this.employee(session.username) : undefined;
    if (!employee) throw new UnauthorizedException('Session is invalid or expired');
    return employee;
  }

  private profile(employee: Employee): { username: string; fullName: string; canCreate: boolean; canFulfill: boolean } {
    return { username: employee.username, fullName: `${employee.firstName} ${employee.lastName}`, canCreate: Boolean(employee.canCreate), canFulfill: Boolean(employee.canFulfill) };
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
