import 'reflect-metadata';
import { INestApplication, ValidationPipe } from '@nestjs/common';
import { Test } from '@nestjs/testing';
import request from 'supertest';
import { AppModule } from '../app.module';

describe('Work order lifecycle (HTTP + SQLite)', () => {
  let app: INestApplication;
  beforeAll(async () => {
    const module = await Test.createTestingModule({ imports: [AppModule] }).compile();
    app = module.createNestApplication();
    app.setGlobalPrefix('api');
    app.useGlobalPipes(new ValidationPipe({ whitelist: true, transform: true }));
    await app.init();
  });
  afterAll(async () => { await app.close(); });

  it('creates, assigns, begins and completes a work order, rejecting invalid moves', async () => {
    const dateParts = new Intl.DateTimeFormat('en-US', { timeZone: 'America/Chicago', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
    const chicagoDate = `${dateParts.find(part => part.type === 'year')!.value}-${dateParts.find(part => part.type === 'month')!.value}-${dateParts.find(part => part.type === 'day')!.value}`;
    const created = await request(app.getHttpServer()).post('/api/work-orders')
      .send({ title: `Plumbing repair ${Date.now()}`, description: 'Repair sink', instructions: 'Use side entrance', roomNumber: 'B-12', dueDate: chicagoDate }).expect(201);
    expect(created.body).toMatchObject({ status: 'Draft', instructions: 'Use side entrance', roomNumber: 'B-12', dueDate: chicagoDate, urgency: 'DueToday', dueDateBadge: 'Due Today' });
    const id = created.body.id;
    expect((await request(app.getHttpServer()).get('/api/work-orders').query({ q: 'side entrance' }).expect(200)).body).toHaveLength(1);
    expect((await request(app.getHttpServer()).get('/api/work-orders').query({ q: 'B-12' }).expect(200)).body).toHaveLength(1);
    expect((await request(app.getHttpServer()).get('/api/work-orders').query({ assignee: 'Alex Tech' }).expect(200)).body).toHaveLength(0);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).send({ status: 'Complete' }).expect(400);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).send({ status: 'Assigned', assignee: 'Alex Tech' }).expect(201);
    expect((await request(app.getHttpServer()).get('/api/work-orders').query({ assignee: 'alex tech' }).expect(200)).body).toHaveLength(1);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).send({ status: 'InProgress' }).expect(201);
    const done = await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).send({ status: 'Complete' }).expect(201);
    expect(done.body).toMatchObject({ status: 'Complete', assignee: 'Alex Tech', roomNumber: 'B-12', instructions: 'Use side entrance', dueDate: chicagoDate, urgency: 'None' });
    expect(done.body.completedAt).toBeTruthy();
  });

  it('validates due dates and the original room length limit', async () => {
    await request(app.getHttpServer()).post('/api/work-orders').send({ title: 'bad date', dueDate: '2026-02-30' }).expect(400);
    await request(app.getHttpServer()).post('/api/work-orders').send({ title: 'long room', roomNumber: 'R'.repeat(901) }).expect(400);
    const saved = await request(app.getHttpServer()).post('/api/work-orders')
      .send({ title: 'truncated instructions', instructions: 'I'.repeat(4001), description: 'D'.repeat(4001) }).expect(201);
    expect(saved.body.instructions).toHaveLength(4000);
    expect(saved.body.description).toHaveLength(4000);
  });

  it('allows only one of two concurrent assignments to advance a draft', async () => {
    const created = await request(app.getHttpServer()).post('/api/work-orders').send({ title: 'concurrent assignment' }).expect(201);
    const assignments = await Promise.all(['tech.one', 'tech.two'].map(assignee =>
      request(app.getHttpServer()).post(`/api/work-orders/${created.body.id}/transitions`).send({ status: 'Assigned', assignee })));
    expect(assignments.map(result => result.status).sort()).toEqual([201, 400]);
    const saved = await request(app.getHttpServer()).get(`/api/work-orders/${created.body.id}`).expect(200);
    expect(['tech.one', 'tech.two']).toContain(saved.body.assignee);
  });
});
