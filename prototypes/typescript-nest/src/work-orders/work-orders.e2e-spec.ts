import 'reflect-metadata';
import { INestApplication, ValidationPipe } from '@nestjs/common';
import { Test } from '@nestjs/testing';
import request from 'supertest';
import { AppModule } from '../app.module';

describe('Work order lifecycle (HTTP + SQLite)', () => {
  let app: INestApplication;
  let creatorAuth: string;
  let workerAuth: string;
  let readOnlyAuth: string;
  beforeAll(async () => {
    const module = await Test.createTestingModule({ imports: [AppModule] }).compile();
    app = module.createNestApplication();
    app.setGlobalPrefix('api');
    app.useGlobalPipes(new ValidationPipe({ whitelist: true, transform: true }));
    await app.init();
    creatorAuth = await login(app, 'hsimpson');
    workerAuth = await login(app, 'demo.tech');
    readOnlyAuth = await login(app, 'nflanders');
  });
  afterAll(async () => { await app.close(); });

  it('creates, assigns, begins and completes a work order, rejecting invalid moves', async () => {
    const dateParts = new Intl.DateTimeFormat('en-US', { timeZone: 'America/Chicago', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
    const chicagoDate = `${dateParts.find(part => part.type === 'year')!.value}-${dateParts.find(part => part.type === 'month')!.value}-${dateParts.find(part => part.type === 'day')!.value}`;
    const created = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth)
      .send({ title: `Plumbing repair ${Date.now()}`, description: 'Repair sink', instructions: 'Use side entrance', roomNumber: 'B-12', dueDate: chicagoDate }).expect(201);
    expect(created.body).toMatchObject({ status: 'Draft', creator: 'hsimpson', instructions: 'Use side entrance', roomNumber: 'B-12', dueDate: chicagoDate, urgency: 'DueToday', dueDateBadge: 'Due Today' });
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
  });

  it('validates due dates and the original room length limit', async () => {
    await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'bad date', dueDate: '2026-02-30' }).expect(400);
    await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'long room', roomNumber: 'R'.repeat(901) }).expect(400);
    await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'T'.repeat(301) }).expect(400);
    const saved = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth)
      .send({ title: 'truncated instructions', instructions: 'I'.repeat(4001), description: 'D'.repeat(4001) }).expect(201);
    expect(saved.body.instructions).toHaveLength(4000);
    expect(saved.body.description).toHaveLength(4000);
  });

  it('allows only one of two concurrent assignments to advance a draft', async () => {
    const created = await request(app.getHttpServer()).post('/api/work-orders').set('authorization', creatorAuth).send({ title: 'concurrent assignment' }).expect(201);
    const assignments = await Promise.all(['demo.tech', 'gwillie'].map(assignee =>
      request(app.getHttpServer()).post(`/api/work-orders/${created.body.id}/transitions`).set('authorization', creatorAuth).send({ status: 'Assigned', assignee })));
    expect(assignments.map(result => result.status).sort()).toEqual([201, 400]);
    const saved = await request(app.getHttpServer()).get(`/api/work-orders/${created.body.id}`).set('authorization', creatorAuth).expect(200);
    expect(['demo.tech', 'gwillie']).toContain(saved.body.assignee);
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
});

async function login(app: INestApplication, username: string): Promise<string> {
  const response = await request(app.getHttpServer()).post('/api/auth/login').send({ username }).expect(201);
  return `Bearer ${response.body.token}`;
}
