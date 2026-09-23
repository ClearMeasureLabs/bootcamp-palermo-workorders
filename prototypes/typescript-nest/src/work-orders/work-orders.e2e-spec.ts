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
    const created = await request(app.getHttpServer()).post('/api/work-orders')
      .send({ title: `Plumbing repair ${Date.now()}`, description: 'Repair sink', roomNumber: 'B-12' }).expect(201);
    expect(created.body.status).toBe('Draft');
    const id = created.body.id;
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).send({ status: 'Complete' }).expect(400);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).send({ status: 'Assigned', assignee: 'Alex Tech' }).expect(201);
    await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).send({ status: 'InProgress' }).expect(201);
    const done = await request(app.getHttpServer()).post(`/api/work-orders/${id}/transitions`).send({ status: 'Complete' }).expect(201);
    expect(done.body).toMatchObject({ status: 'Complete', assignee: 'Alex Tech', roomNumber: 'B-12' });
    expect(done.body.completedAt).toBeTruthy();
  });
});
