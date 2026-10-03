import { BadRequestException, ForbiddenException, Injectable, NotFoundException, OnModuleInit, UnauthorizedException } from '@nestjs/common';
import initSqlJs, { Database } from 'sql.js';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';

export type CreateWorkOrder = { title: string; description?: string; instructions?: string; roomNumber?: string; dueDate?: string };
type UpdateWorkOrder = { title?: string; description?: string; instructions?: string; roomNumber?: string; dueDate?: string|null };
type Employee = { username: string; firstName: string; lastName: string; canCreate: number; canFulfill: number };
type Attachment = { id: string; workOrderId: string; fileName: string; contentType: string; fileSize: number; uploadedBy: string; uploadedByName: string; uploadedDate: string };
type WorkOrderEvent = { id: string; workOrderId: string; action: string; actor: string; actorName: string; fromStatus: string|null; toStatus: string; occurredAt: string };
type WorkOrder = { id: string; number: string; title: string; description: string; instructions: string; roomNumber: string|null; dueDate: string|null; urgency: string; dueDateBadge: string|null; status: string; creator: string; assignee: string|null; createdAt: string; updatedAt: string; assignedAt: string|null; completedAt: string|null; attachments?: Attachment[] };
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
    this.db.run('PRAGMA foreign_keys=ON');
    this.db.run('CREATE TABLE IF NOT EXISTS SchemaMigration (version INTEGER PRIMARY KEY, appliedAt TEXT NOT NULL)');
    this.db.run(`CREATE TABLE IF NOT EXISTS WorkOrder (
      id TEXT PRIMARY KEY, number TEXT NOT NULL UNIQUE, title TEXT NOT NULL, description TEXT NOT NULL,
      roomNumber TEXT, status TEXT NOT NULL DEFAULT 'Draft', creator TEXT NOT NULL, assignee TEXT,
      createdAt TEXT NOT NULL, updatedAt TEXT NOT NULL, assignedAt TEXT, completedAt TEXT
    )`);
    this.applyDetailsMigration();
    this.applyIdentityMigration();
    this.applyAttachmentMigration();
    this.applyWorkOrderNumberMigration();
    this.applyStatusCodeMigration();
    this.applyRelationalIdentityMigration();
    this.persist();
  }

  async list(token: string | undefined, filters: { status?: string; assignee?: string; creator?: string; q?: string; overdueOnly?: boolean } = {}): Promise<WorkOrder[]> {
    this.requireSession(token);
    const predicates: string[] = [];
    const params: string[] = [];
    if (filters.status?.trim()) { predicates.push('status = ?'); params.push(this.statusCode(this.normalizeStatus(filters.status))); }
    if (filters.assignee?.trim()) { predicates.push('LOWER(COALESCE(assignee,\'\')) = LOWER(?)'); params.push(filters.assignee.trim()); }
    if (filters.creator?.trim()) { predicates.push('LOWER(creator) = LOWER(?)'); params.push(filters.creator.trim()); }
    if (filters.overdueOnly) { predicates.push("dueDate < ? AND status IN ('DRT','ASD','IPG')"); params.push(this.chicagoToday()); }
    if (filters.q?.trim()) {
      predicates.push("(number LIKE ? OR title LIKE ? OR description LIKE ? OR instructions LIKE ? OR roomNumber LIKE ? OR assignee LIKE ?)");
      const term = `%${filters.q.trim()}%`;
      params.push(term, term, term, term, term, term);
    }
    const where = predicates.length ? `WHERE ${predicates.join(' AND ')}` : '';
    const orders = this.all(`SELECT * FROM WorkOrder ${where} ORDER BY createdAt DESC`, ...params);
    const attachments = this.attachmentsFor(orders.map(order => order.id));
    return orders.map(order => ({ ...this.present(order), attachments: attachments.get(order.id) ?? [] }));
  }

  statusCounts(token?: string): Record<string, number> {
    this.requireSession(token);
    const counts: Record<string, number> = { Draft: 0, Assigned: 0, InProgress: 0, Complete: 0, Cancelled: 0 };
    for (const row of this.all<{ status: string; count: number }>('SELECT status,COUNT(*) AS count FROM WorkOrder GROUP BY status')) {
      const status = this.displayStatus(row.status);
      if (Object.hasOwn(counts, status)) counts[status] = Number(row.count);
    }
    return counts;
  }

  history(id: string, token?: string): WorkOrderEvent[] {
    this.requireSession(token);
    if (!this.one('SELECT id FROM WorkOrder WHERE id=?', id)) throw new NotFoundException(`Work order ${id} was not found`);
    return this.all<WorkOrderEvent>(`SELECT ev.id,ev.workOrderId,ev.action,ev.actor,
      e.firstName || ' ' || e.lastName AS actorName,ev.fromStatus,ev.toStatus,ev.occurredAt
      FROM WorkOrderEvent ev JOIN Employee e ON e.username=ev.actor WHERE ev.workOrderId=? ORDER BY ev.sequence`, id);
  }

  async get(id: string, token?: string): Promise<WorkOrder> {
    this.requireSession(token);
    const order = this.one('SELECT * FROM WorkOrder WHERE id = ?', id);
    if (!order) throw new NotFoundException(`Work order ${id} was not found`);
    return this.present(order);
  }

  async update(id: string, input: UpdateWorkOrder, token?: string): Promise<WorkOrder> {
    const actor = this.requireSession(token);
    const order = await this.get(id, token);
    if (order.creator.toLowerCase() !== actor.username.toLowerCase()) throw new ForbiddenException('Only the work-order creator can edit it');
    if (order.status !== 'Draft') throw new BadRequestException('Only draft work orders can be edited');
    if (input.title !== undefined && !input.title.trim()) throw new BadRequestException('Title is required');
    if (input.dueDate) this.validateDueDate(input.dueDate);
    const now = new Date().toISOString();
    this.db.run(`UPDATE WorkOrder SET title=?,description=?,instructions=?,roomNumber=?,dueDate=?,updatedAt=? WHERE id=? AND status='DRT'`, [
      input.title?.trim() ?? order.title,
      input.description?.slice(0, 4000) ?? order.description,
      input.instructions?.slice(0, 4000) ?? order.instructions,
      input.roomNumber === undefined ? order.roomNumber : input.roomNumber.trim() || null,
      input.dueDate === undefined ? order.dueDate : input.dueDate || null,
      now, id,
    ]);
    if (this.db.getRowsModified() !== 1) throw new BadRequestException('Only draft work orders can be edited');
    this.insertEvent(id, actor.username, 'Updated', 'Draft', 'Draft', now);
    this.persist();
    return this.get(id, token);
  }

  attachments(id: string, token?: string): Attachment[] {
    this.requireSession(token);
    if (!this.one('SELECT id FROM WorkOrder WHERE id=?', id)) throw new NotFoundException(`Work order ${id} was not found`);
    return this.attachmentsFor([id]).get(id) ?? [];
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
    this.db.run('INSERT INTO WorkOrderAttachment(id,workOrderId,fileName,contentType,fileSize,uploadedBy,uploadedDate,uploadedById) VALUES (?,?,?,?,?,?,?,(SELECT id FROM Employee WHERE username=?))',
      [attachment.id, id, attachment.fileName, contentType, input.fileSize, actor.username, attachment.uploadedDate, actor.username]);
    const order = this.one<StoredWorkOrder>('SELECT * FROM WorkOrder WHERE id=?', id)!;
    const status = this.displayStatus(order.status);
    this.insertEvent(id, actor.username, 'Attachment added', status, status, attachment.uploadedDate);
    this.persist();
    return attachment;
  }

  async create(input: CreateWorkOrder, token?: string): Promise<WorkOrder> {
    const actor = this.requireSession(token);
    if (!actor.canCreate) throw new ForbiddenException('Your role cannot create work orders');
    if (!input.title?.trim()) throw new BadRequestException('Title is required');
    this.validateDueDate(input.dueDate);
    const now = new Date().toISOString();
    const order: StoredWorkOrder = {
      id: crypto.randomUUID(), number: crypto.randomUUID().replaceAll('-', '').slice(0, 7).toUpperCase(), title: input.title.trim(),
      description: (input.description ?? '').slice(0, 4000), instructions: (input.instructions ?? '').slice(0, 4000),
      roomNumber: input.roomNumber?.trim() || null, dueDate: input.dueDate || null, status: 'Draft',
      creator: actor.username, assignee: null, createdAt: now, updatedAt: now, assignedAt: null, completedAt: null,
    };
    order.status = this.statusCode(order.status);
    this.db.run(`INSERT INTO WorkOrder(id,number,title,description,instructions,roomNumber,dueDate,status,creator,assignee,createdAt,updatedAt,assignedAt,completedAt,creatorId)
      VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,(SELECT id FROM Employee WHERE username=?))`, [order.id, order.number, order.title, order.description, order.instructions, order.roomNumber, order.dueDate, order.status, order.creator, order.assignee, order.createdAt, order.updatedAt, order.assignedAt, order.completedAt, actor.username]);
    this.insertEvent(order.id, actor.username, 'Created', null, 'Draft', now);
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
      [this.statusCode(target), now, target, order.status, assignee?.trim() ?? null, target, target, order.status, now, target, target, now, id, this.statusCode(order.status)]);
    if (this.db.getRowsModified() !== 1) {
      const current = await this.get(id, token);
      throw new BadRequestException(`Invalid transition ${current.status} -> ${target}`);
    }
    if (isInitialAssignment) this.db.run('UPDATE WorkOrder SET assigneeId=(SELECT id FROM Employee WHERE LOWER(username)=LOWER(?)) WHERE id=?', [assignee!.trim(), id]);
    if (target === 'Cancelled') this.db.run('UPDATE WorkOrder SET assigneeId=NULL WHERE id=?', [id]);
    this.insertEvent(id, actor.username, this.transitionAction(order.status, target), order.status, target, now);
    this.persist();
    return this.get(id, token);
  }

  employees(canFulfill?: boolean): Array<{ username: string; displayName: string; canCreate: boolean; canFulfill: boolean }> {
    const employees = this.all<Employee>(`SELECT e.username,e.firstName,e.lastName,
      MAX(r.canCreate) AS canCreate,MAX(r.canFulfill) AS canFulfill
      FROM Employee e JOIN EmployeeRoles er ON er.EmployeeId=e.id JOIN Role r ON r.id=er.RoleId
      GROUP BY e.username,e.firstName,e.lastName ORDER BY e.lastName,e.firstName`);
    return employees.filter(employee => canFulfill === undefined || Boolean(employee.canFulfill) === canFulfill)
      .map(employee => ({ username: employee.username, displayName: `${employee.firstName} ${employee.lastName}`, canCreate: Boolean(employee.canCreate), canFulfill: Boolean(employee.canFulfill) }));
  }

  login(username: string): { token: string; user: ReturnType<WorkOrdersService['profile']> } {
    if (process.env.DEMO_LOGIN_ENABLED !== 'true') throw new ForbiddenException('Demo employee login is disabled; set DEMO_LOGIN_ENABLED=true for local demos and acceptance testing');
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
    for (const [username, firstName, lastName] of employees) {
      this.db.run('INSERT OR IGNORE INTO Employee(username,firstName,lastName) VALUES (?,?,?)', [username, firstName, lastName]);
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
    this.db.run(`CREATE TABLE IF NOT EXISTS WorkOrderEvent (
      sequence INTEGER PRIMARY KEY AUTOINCREMENT, id TEXT NOT NULL UNIQUE,
      workOrderId TEXT NOT NULL REFERENCES WorkOrder(id) ON DELETE CASCADE,
      actor TEXT NOT NULL REFERENCES Employee(username), action TEXT NOT NULL,
      fromStatus TEXT, toStatus TEXT NOT NULL, occurredAt TEXT NOT NULL
    )`);
    this.db.run('CREATE INDEX IF NOT EXISTS IX_WorkOrderEvent_WorkOrderSequence ON WorkOrderEvent(workOrderId,sequence)');
    this.db.run("INSERT OR IGNORE INTO SchemaMigration(version, appliedAt) VALUES (5, datetime('now'))");
  }

  private applyWorkOrderNumberMigration(): void {
    const legacy = this.all<{ id: string }>('SELECT id FROM WorkOrder WHERE length(number)>7');
    for (const row of legacy) {
      this.db.run('UPDATE WorkOrder SET number=? WHERE id=?', [crypto.randomUUID().replaceAll('-', '').slice(0, 7).toUpperCase(), row.id]);
    }
    this.db.run(`CREATE TRIGGER IF NOT EXISTS TR_WorkOrder_NumberLength_Insert
      BEFORE INSERT ON WorkOrder WHEN length(NEW.number)>7
      BEGIN SELECT RAISE(ABORT, 'WorkOrder.Number must be 7 characters or fewer'); END`);
    this.db.run(`CREATE TRIGGER IF NOT EXISTS TR_WorkOrder_NumberLength_Update
      BEFORE UPDATE OF number ON WorkOrder WHEN length(NEW.number)>7
      BEGIN SELECT RAISE(ABORT, 'WorkOrder.Number must be 7 characters or fewer'); END`);
    this.db.run("INSERT OR IGNORE INTO SchemaMigration(version, appliedAt) VALUES (6, datetime('now'))");
  }

  private applyStatusCodeMigration(): void {
    const codes: Record<string, string> = { Draft: 'DRT', Assigned: 'ASD', InProgress: 'IPG', Complete: 'CMP', Cancelled: 'CNL' };
    for (const [name, code] of Object.entries(codes)) this.db.run('UPDATE WorkOrder SET status=? WHERE status=?', [code, name]);
    this.db.run(`CREATE TRIGGER IF NOT EXISTS TR_WorkOrder_StatusLength_Insert
      BEFORE INSERT ON WorkOrder WHEN length(NEW.status)>3
      BEGIN SELECT RAISE(ABORT, 'WorkOrder.Status must be 3 characters or fewer'); END`);
    this.db.run(`CREATE TRIGGER IF NOT EXISTS TR_WorkOrder_StatusLength_Update
      BEFORE UPDATE OF status ON WorkOrder WHEN length(NEW.status)>3
      BEGIN SELECT RAISE(ABORT, 'WorkOrder.Status must be 3 characters or fewer'); END`);
    this.db.run("INSERT OR IGNORE INTO SchemaMigration(version, appliedAt) VALUES (7, datetime('now'))");
  }

  private applyRelationalIdentityMigration(): void {
    const columns = (table: string) => this.all<{ name: string }>(`PRAGMA table_info(${table})`).map(column => column.name);
    const addColumn = (table: string, name: string, ddl: string) => { if (!columns(table).includes(name)) this.db.run(`ALTER TABLE ${table} ADD COLUMN ${name} ${ddl}`); };
    addColumn('Employee', 'id', 'TEXT');
    addColumn('Employee', 'emailAddress', "TEXT NOT NULL DEFAULT ''");
    addColumn('Employee', 'preferredLanguage', "TEXT NOT NULL DEFAULT 'en-US'");
    addColumn('Role', 'id', 'TEXT');
    this.db.run('CREATE UNIQUE INDEX IF NOT EXISTS UX_Employee_Id ON Employee(id)');
    this.db.run('CREATE UNIQUE INDEX IF NOT EXISTS UX_Role_Id ON Role(id)');
    addColumn('WorkOrder', 'creatorId', 'TEXT REFERENCES Employee(id)');
    addColumn('WorkOrder', 'assigneeId', 'TEXT REFERENCES Employee(id)');
    addColumn('WorkOrderAttachment', 'uploadedById', 'TEXT REFERENCES Employee(id)');
    for (const employee of this.all<{ username: string }>("SELECT username FROM Employee WHERE id IS NULL OR id=''")) {
      this.db.run('UPDATE Employee SET id=? WHERE username=?', [crypto.randomUUID(), employee.username]);
    }
    for (const role of this.all<{ name: string }>("SELECT name FROM Role WHERE id IS NULL OR id=''")) {
      this.db.run('UPDATE Role SET id=? WHERE name=?', [crypto.randomUUID(), role.name]);
    }
    this.db.run(`CREATE TABLE IF NOT EXISTS EmployeeRoles (
      EmployeeId TEXT NOT NULL REFERENCES Employee(id) ON DELETE CASCADE,
      RoleId TEXT NOT NULL REFERENCES Role(id) ON DELETE CASCADE,
      PRIMARY KEY(EmployeeId,RoleId))`);
    if (this.all<{ name: string }>("SELECT name FROM sqlite_master WHERE type='table' AND name='EmployeeRole'").length) {
      for (const link of this.all<{ username: string; roleName: string }>('SELECT username,roleName FROM EmployeeRole')) {
        this.db.run(`INSERT OR IGNORE INTO EmployeeRoles(EmployeeId,RoleId)
          SELECT e.id,r.id FROM Employee e JOIN Role r ON r.name=? WHERE e.username=?`, [link.roleName, link.username]);
      }
      this.db.run('DROP TABLE EmployeeRole');
    }
    const roles: Array<[string,string]> = [
      ['hsimpson','Manager'],['tlovejoy','Minister'],['nflanders','Deacon'],['nflanders','Parishioner'],
      ['hlovejoy','Parishioner'],['gwillie','Groundskeeper'],['demo.tech','Fulfillment'],['demo.user','Manager'],
    ];
    for (const [username, role] of roles) this.db.run(`INSERT OR IGNORE INTO EmployeeRoles(EmployeeId,RoleId)
      SELECT e.id,r.id FROM Employee e JOIN Role r ON r.name=? WHERE e.username=?`, [role, username]);
    this.db.run("UPDATE WorkOrder SET creatorId=(SELECT id FROM Employee WHERE LOWER(username)=LOWER(creator)) WHERE creatorId IS NULL");
    this.db.run("UPDATE WorkOrder SET assigneeId=(SELECT id FROM Employee WHERE LOWER(username)=LOWER(assignee)) WHERE assignee IS NOT NULL AND assigneeId IS NULL");
    this.db.run("UPDATE WorkOrderAttachment SET uploadedById=(SELECT id FROM Employee WHERE LOWER(username)=LOWER(uploadedBy)) WHERE uploadedById IS NULL");
    this.db.run(`CREATE TRIGGER IF NOT EXISTS TR_WorkOrder_CreatorId_Required_Insert BEFORE INSERT ON WorkOrder
      WHEN NEW.creatorId IS NULL BEGIN SELECT RAISE(ABORT,'CreatorId is required'); END`);
    this.db.run(`CREATE TRIGGER IF NOT EXISTS TR_WorkOrder_CreatorId_Required_Update BEFORE UPDATE OF creatorId ON WorkOrder
      WHEN NEW.creatorId IS NULL BEGIN SELECT RAISE(ABORT,'CreatorId is required'); END`);
    this.db.run(`CREATE TRIGGER IF NOT EXISTS TR_WorkOrderAttachment_UploadedById_Required_Insert BEFORE INSERT ON WorkOrderAttachment
      WHEN NEW.uploadedById IS NULL BEGIN SELECT RAISE(ABORT,'UploadedById is required'); END`);
    this.db.run("INSERT OR IGNORE INTO SchemaMigration(version, appliedAt) VALUES (8, datetime('now'))");
  }

  private attachmentsFor(ids: string[]): Map<string, Attachment[]> {
    const attachments = new Map<string, Attachment[]>();
    for (let offset = 0; offset < ids.length; offset += 400) {
      const batch = ids.slice(offset, offset + 400);
      const rows = this.all<Attachment>(`SELECT a.id,a.workOrderId,a.fileName,a.contentType,a.fileSize,a.uploadedBy,
        e.firstName || ' ' || e.lastName AS uploadedByName,a.uploadedDate
        FROM WorkOrderAttachment a JOIN Employee e ON e.username=a.uploadedBy
        WHERE a.workOrderId IN (${batch.map(() => '?').join(',')}) ORDER BY a.uploadedDate,a.id`, ...batch);
      for (const row of rows) attachments.set(row.workOrderId, [...(attachments.get(row.workOrderId) ?? []), row]);
    }
    return attachments;
  }

  private insertEvent(workOrderId: string, actor: string, action: string, fromStatus: string|null, toStatus: string, occurredAt: string): void {
    this.db.run('INSERT INTO WorkOrderEvent(id,workOrderId,actor,action,fromStatus,toStatus,occurredAt) VALUES (?,?,?,?,?,?,?)',
      [crypto.randomUUID(), workOrderId, actor, action, fromStatus, toStatus, occurredAt]);
  }

  private transitionAction(from: string, to: string): string {
    if (to === 'Assigned' && from === 'Draft') return 'Assigned';
    if (to === 'Assigned') return 'Shelved';
    return to;
  }

  private normalizeStatus(status: string): string {
    const canonical = ['Draft', 'Assigned', 'InProgress', 'Complete', 'Cancelled'].find(candidate => candidate.toLowerCase() === status.trim().toLowerCase());
    if (!canonical) throw new BadRequestException(`Unknown work-order status: ${status}`);
    return canonical;
  }

  private statusCode(status: string): string {
    return ({ Draft: 'DRT', Assigned: 'ASD', InProgress: 'IPG', Complete: 'CMP', Cancelled: 'CNL' } as Record<string,string>)[status];
  }

  private displayStatus(code: string): string {
    return ({ DRT: 'Draft', ASD: 'Assigned', IPG: 'InProgress', CMP: 'Complete', CNL: 'Cancelled' } as Record<string,string>)[code] ?? code;
  }

  private employee(username: string): Employee | undefined {
    return this.one<Employee>(`SELECT e.username,e.firstName,e.lastName,
      MAX(r.canCreate) AS canCreate,MAX(r.canFulfill) AS canFulfill
      FROM Employee e JOIN EmployeeRoles er ON er.EmployeeId=e.id JOIN Role r ON r.id=er.RoleId
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
    order = { ...order, status: this.displayStatus(order.status) };
    let urgency = 'None';
    if (order.dueDate && ['Draft', 'Assigned', 'InProgress'].includes(order.status)) {
      const parts = new Intl.DateTimeFormat('en-US', { timeZone: 'America/Chicago', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
      const today = `${parts.find(p => p.type === 'year')!.value}-${parts.find(p => p.type === 'month')!.value}-${parts.find(p => p.type === 'day')!.value}`;
      urgency = order.dueDate === today ? 'DueToday' : order.dueDate < today ? 'Overdue' : 'None';
    }
    return { ...order, urgency, dueDateBadge: order.dueDate ? (urgency === 'DueToday' ? 'Due Today' : urgency === 'Overdue' ? 'Overdue' : 'On Track') : null };
  }

  private chicagoToday(): string {
    const parts = new Intl.DateTimeFormat('en-US', { timeZone: 'America/Chicago', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
    return `${parts.find(p => p.type === 'year')!.value}-${parts.find(p => p.type === 'month')!.value}-${parts.find(p => p.type === 'day')!.value}`;
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
