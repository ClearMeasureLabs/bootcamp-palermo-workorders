import 'reflect-metadata';
import { INestApplication, ValidationPipe } from '@nestjs/common';
import { Test } from '@nestjs/testing';
import request from 'supertest';
import { rmSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import crypto from 'node:crypto';
import { Database } from 'sql.js';
import { AppModule } from '../app.module';
import { WorkOrdersService } from './work-orders.service';

describe('Work order lifecycle (HTTP + SQLite)', () => {
  let app: INestApplication;
  let creatorAuth: string;
  let workerAuth: string;
  let readOnlyAuth: string;
  let testDatabase: string;
  beforeAll(async () => {
    process.env.DEMO_LOGIN_ENABLED = 'true';
    testDatabase = path.join(os.tmpdir(), `typescript-nest-test-${crypto.randomUUID()}.db`);
    process.env.DATABASE_URL = `file:${testDatabase}`;
    const module = await Test.createTestingModule({ imports: [AppModule] }).compile();
    app = module.createNestApplication();
    app.setGlobalPrefix('api');
    app.useGlobalPipes(new ValidationPipe({ whitelist: true, transform: true }));
    await app.init();
    creatorAuth = await login(app, 'hsimpson');
    workerAuth = await login(app, 'demo.tech');
    readOnlyAuth = await login(app, 'nflanders');
  });
  afterAll(async () => { await app.close(); rmSync(testDatabase, { force: true }); });

  it('keeps passwordless employee login disabled unless explicitly opted in', async () => {
    const original = process.env.DEMO_LOGIN_ENABLED;
    delete process.env.DEMO_LOGIN_ENABLED;
    try { await request(app.getHttpServer()).post('/api/auth/login').send({ username: 'hsimpson' }).expect(403); }
    finally { process.env.DEMO_LOGIN_ENABLED = original ?? 'true'; }
  });

  it('applies GUID employee/role links and constrained source status and number storage', async () => {
    const created = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth)
      .send({ title: 'Schema migration verification' }).expect(201);
    const db = (app.get(WorkOrdersService) as unknown as { db: Database }).db;
    const columns = (table: string) => db.exec(`PRAGMA table_info(${table})`)[0].values.map(row => row[1]);
    const tables = db.exec("SELECT name FROM sqlite_master WHERE type='table'").flatMap(result => result.values.map(row => row[0]));
    const workOrder = db.exec('SELECT number,status,creatorId FROM WorkOrder WHERE id=?', [created.body.id])[0].values[0];
    expect(columns('WorkOrder')).toEqual(expect.arrayContaining(['creatorId','assigneeId']));
    expect(columns('Employee')).toEqual(expect.arrayContaining(['id','emailAddress','preferredLanguage']));
    expect(tables).toContain('EmployeeRoles');
    expect(tables).not.toContain('EmployeeRole');
    expect(workOrder[0]).toMatch(/^[A-F0-9]{7}$/);
    expect(workOrder[1]).toBe('DRT');
    expect(workOrder[2]).toBeTruthy();
    expect(db.exec('PRAGMA foreign_key_check')).toEqual([]);
  });

  it('creates, assigns, begins and completes a work order, rejecting invalid moves', async () => {
    const dateParts = new Intl.DateTimeFormat('en-US', { timeZone: 'America/Chicago', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
    const chicagoDate = `${dateParts.find(part => part.type === 'year')!.value}-${dateParts.find(part => part.type === 'month')!.value}-${dateParts.find(part => part.type === 'day')!.value}`;
    const created = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth)
      .send({ title: `Plumbing repair ${Date.now()}`, description: 'Repair sink', instructions: 'Use side entrance', roomNumber: 'B-12', dueDate: chicagoDate }).expect(201);
    expect(created.body).toMatchObject({ status: 'Draft', creator: 'hsimpson', instructions: 'Use side entrance', roomNumber: 'B-12', dueDate: chicagoDate, urgency: 'DueToday', dueDateBadge: 'Due Today' });
    expect(created.body.number).toMatch(/^[A-F0-9]{7}$/);
    const id = created.body.id;
    expect((await request(app.getHttpServer()).get('/api/work-orders').set('authorization', creatorAuth).query({ q: 'side entrance' }).expect(200)).body).toHaveLength(1);
    expect((await request(app.getHttpServer()).get('/api/work-orders').set('authorization', creatorAuth).query({ q: 'B-12' }).expect(200)).body).toHaveLength(1);
    expect((await request(app.getHttpServer()).get('/api/work-orders').set('authorization', creatorAuth).query({ assignee: 'demo.tech' }).expect(200)).body).toHaveLength(0);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).set('authorization', creatorAuth).send({ status: 'Complete' }).expect(400);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).set('authorization', creatorAuth).send({ status: 'Assigned', assignee: 'hlovejoy' }).expect(400);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).set('authorization', readOnlyAuth).send({ status: 'Assigned', assignee: 'demo.tech' }).expect(403);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).set('authorization', creatorAuth).send({ status: 'Assigned', assignee: 'demo.tech' }).expect(201);
    expect((await request(app.getHttpServer()).get('/api/work-orders').set('authorization', creatorAuth).query({ assignee: 'DEMO.TECH' }).expect(200)).body).toHaveLength(1);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).set('authorization', creatorAuth).send({ status: 'InProgress' }).expect(403);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).set('authorization', workerAuth).send({ status: 'InProgress' }).expect(201);
    const done = await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).set('authorization', workerAuth).send({ status: 'Complete' }).expect(201);
    expect(done.body).toMatchObject({ status: 'Complete', assignee: 'demo.tech', roomNumber: 'B-12', instructions: 'Use side entrance', dueDate: chicagoDate, urgency: 'None' });
    expect(done.body.completedAt).toBeTruthy();
    const events = await request(app.getHttpServer()).get(`/api/work-orders/${id}/history`).set('authorization', creatorAuth).expect(200);
    expect(events.body.map((event: { action: string }) => event.action)).toEqual(['Created', 'Assigned', 'InProgress', 'Complete']);
    expect(events.body[1]).toMatchObject({ actor: 'hsimpson', actorName: 'Homer Simpson', fromStatus: 'Draft', toStatus: 'Assigned' });
  });

  it('validates due dates and the original room length limit', async () => {
    await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'bad date', dueDate: '2026-02-30' }).expect(400);
    await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'long room', roomNumber: 'R'.repeat(901) }).expect(400);
    await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'T'.repeat(301) }).expect(400);
    const saved = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth)
      .send({ title: 'truncated instructions', instructions: 'I'.repeat(4001), description: 'D'.repeat(4001) }).expect(201);
    expect(saved.body.instructions).toHaveLength(4000);
    expect(saved.body.description).toHaveLength(4000);
    const noDueDate = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth)
      .send({ title: 'empty due date', dueDate: '' }).expect(201);
    expect(noDueDate.body.dueDate).toBeNull();
  });

  it('allows only the creator to update a draft work order', async () => {
    const created = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth)
      .send({ title: 'Draft to edit' }).expect(201);
    const updated = await request(app.getHttpServer()).patch(`/api/work-orders/${created.body.id}`).set('authorization', creatorAuth)
      .send({ title: 'Draft edited', description: 'Updated description', instructions: 'Use side entrance', roomNumber: 'A-12', dueDate: '2026-10-09' }).expect(200);
    expect(updated.body).toMatchObject({ title: 'Draft edited', description: 'Updated description', instructions: 'Use side entrance', roomNumber: 'A-12', dueDate: '2026-10-09' });
    await request(app.getHttpServer()).patch(`/api/work-orders/${created.body.id}`).set('authorization', readOnlyAuth).send({ title: 'Unauthorized change' }).expect(403);
    await request(app.getHttpServer()).post(`/api/work-orders/${created.body.id}/transitions`).set('authorization', creatorAuth)
      .send({ status: 'Assigned', assignee: 'demo.tech' }).expect(201);
    await request(app.getHttpServer()).patch(`/api/work-orders/${created.body.id}`).set('authorization', creatorAuth).send({ title: 'No longer editable' }).expect(400);
    const events = await request(app.getHttpServer()).get(`/api/work-orders/${created.body.id}/history`).set('authorization', creatorAuth).expect(200);
    expect(events.body.map((event: { action: string }) => event.action)).toEqual(['Created', 'Updated', 'Assigned']);
  });

  it('allows only one of two concurrent assignments to advance a draft', async () => {
    const created = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'concurrent assignment' }).expect(201);
    const assignments = await Promise.all(['demo.tech', 'gwillie'].map(assignee =>
      request(app.getHttpServer()).post(`/api/work-orders/${created.body.id}/transitions`).set('authorization', creatorAuth).send({ status: 'Assigned', assignee })));
    expect(assignments.map(result => result.status).sort()).toEqual([201, 400]);
    const saved = await request(app.getHttpServer()).get(`/api/work-orders/${created.body.id}`).set('authorization', creatorAuth).expect(200);
    expect(['demo.tech', 'gwillie']).toContain(saved.body.assignee);
  });

  it('clears assignment fields when the creator cancels an assigned order', async () => {
    const created = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'cancel assigned order' }).expect(201);
    await request(app.getHttpServer()).post(`/api/work-orders/${created.body.id}/transitions`).set('authorization', creatorAuth)
      .send({ status: 'Assigned', assignee: 'demo.tech' }).expect(201);
    const cancelled = await request(app.getHttpServer()).post(`/api/work-orders/${created.body.id}/transitions`).set('authorization', creatorAuth)
      .send({ status: 'Cancelled' }).expect(201);
    expect(cancelled.body).toMatchObject({ status: 'Cancelled', assignee: null, assignedAt: null });
    expect((await request(app.getHttpServer()).get('/api/work-orders').set('authorization', creatorAuth).query({ assignee: 'demo.tech' }).expect(200)).body)
      .not.toContainEqual(expect.objectContaining({ id: created.body.id }));
  });

  it('filters by creator, status and overdue due date and returns all status counts', async () => {
    const todayParts = new Intl.DateTimeFormat('en-US', { timeZone: 'America/Chicago', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
    const today = `${todayParts.find(part => part.type === 'year')!.value}-${todayParts.find(part => part.type === 'month')!.value}-${todayParts.find(part => part.type === 'day')!.value}`;
    const overdueDate = new Date(`${today}T12:00:00.000Z`); overdueDate.setUTCDate(overdueDate.getUTCDate() - 1);
    const overdue = overdueDate.toISOString().slice(0, 10);
    const before = await request(app.getHttpServer()).get('/api/work-orders/status-counts').set('authorization', creatorAuth).expect(200);
    expect(Object.keys(before.body).sort()).toEqual(['Assigned', 'Cancelled', 'Complete', 'Draft', 'InProgress'].sort());
    const past = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'overdue filter case', dueDate: overdue }).expect(201);
    await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'future filter case', dueDate: '2099-12-31' }).expect(201);
    const filtered = await request(app.getHttpServer()).get('/api/work-orders').set('authorization', creatorAuth)
      .query({ creator: 'HSIMPSON', status: 'draft', overdueOnly: 'true' }).expect(200);
    expect(filtered.body.map((order: { id: string }) => order.id)).toEqual([past.body.id]);
    expect(filtered.body[0].attachments).toEqual([]);
    expect((await request(app.getHttpServer()).get('/api/work-orders/status-counts').set('authorization', creatorAuth).expect(200)).body.Draft)
      .toBe(before.body.Draft + 2);
    await request(app.getHttpServer()).get('/api/work-orders').set('authorization', creatorAuth).query({ status: 'Unknown' }).expect(400);
  });

  it('validates the demo employee picker, role capability, and revocable login session', async () => {
    const employees = await request(app.getHttpServer()).get('/api/employees').expect(200);
    expect(employees.body.find((employee: { username: string }) => employee.username === 'hsimpson').displayName).toBe('HOMER SIMPSON');
    expect((await request(app.getHttpServer()).get('/api/employees').query({ canFulfill: 'true' }).expect(200)).body.map((e: { username: string }) => e.username)).toContain('demo.tech');
    await request(app.getHttpServer()).post('/api/auth/login').send({ username: 'not-an-employee' }).expect(401);
    await request(app.getHttpServer()).post('/api/work-orders').send({ title: 'no session' }).expect(401);
    await request(app.getHttpServer()).post('/api/work-orders').set('authorization', readOnlyAuth).send({ title: 'no create permission' }).expect(403);
    const session = await request(app.getHttpServer()).post('/api/auth/login').send({ username: 'hsimpson' }).expect(201);
    await request(app.getHttpServer()).post('/api/auth/logout').set('authorization', `Bearer ${session.body.token}`).expect(201);
    await request(app.getHttpServer()).get('/api/auth/me').set('authorization', `Bearer ${session.body.token}`).expect(401);
  });

  it('adds and displays persisted attachment metadata without storing a binary file', async () => {
    const created = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'attachment metadata' }).expect(201);
    await request(app.getHttpServer()).get(`/api/work-orders/${created.body.id}/attachments`).expect(401);
    const added = await request(app.getHttpServer()).post(`/api/work-orders/${created.body.id}/attachments`).set('authorization', creatorAuth)
      .send({ fileName: 'damage-photo.jpg', contentType: 'image/jpeg', fileSize: 2048 }).expect(201);
    expect(added.body).toMatchObject({ workOrderId: created.body.id, fileName: 'damage-photo.jpg', contentType: 'image/jpeg', fileSize: 2048, uploadedBy: 'hsimpson', uploadedByName: 'Homer Simpson' });
    expect(new Date(added.body.uploadedDate).toISOString()).toBe(added.body.uploadedDate);
    expect((await request(app.getHttpServer()).get(`/api/work-orders/${created.body.id}/attachments`).set('authorization', creatorAuth).expect(200)).body).toEqual([added.body]);
    await request(app.getHttpServer()).post(`/api/work-orders/${created.body.id}/attachments`).set('authorization', creatorAuth).send({ fileName: ' ' , fileSize: 0 }).expect(400);
    await request(app.getHttpServer()).post(`/api/work-orders/${created.body.id}/attachments`).set('authorization', creatorAuth).send({ fileName: 'bad-size', fileSize: -1 }).expect(400);
    const workerAdded = await request(app.getHttpServer()).post(`/api/work-orders/${created.body.id}/attachments`).set('authorization', workerAuth)
      .send({ fileName: 'service-report.pdf', contentType: 'application/pdf', fileSize: 90 }).expect(201);
    expect(workerAdded.body).toMatchObject({ uploadedBy: 'demo.tech', fileName: 'service-report.pdf' });
    expect((await request(app.getHttpServer()).get(`/api/work-orders/${created.body.id}/attachments`).set('authorization', creatorAuth).expect(200)).body).toHaveLength(2);
    const listed = await request(app.getHttpServer()).get('/api/work-orders').set('authorization', creatorAuth).query({ q: 'attachment metadata' }).expect(200);
    expect(listed.body[0].attachments.map((attachment: { fileName: string }) => attachment.fileName)).toEqual(['damage-photo.jpg', 'service-report.pdf']);
    const events = await request(app.getHttpServer()).get(`/api/work-orders/${created.body.id}/history`).set('authorization', creatorAuth).expect(200);
    expect(events.body.map((event: { action: string }) => event.action)).toEqual(['Created', 'Attachment added', 'Attachment added']);
  });
});

async function login(app: INestApplication, username: string): Promise<string> {
  const response = await request(app.getHttpServer()).post('/api/auth/login').send({ username }).expect(201);
  return `Bearer ${response.body.token}`;
}
