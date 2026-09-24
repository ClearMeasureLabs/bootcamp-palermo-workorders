import 'reflect-metadata';
import { ValidationPipe } from '@nestjs/common';
import { NestFactory } from '@nestjs/core';
import express, { NextFunction, Request, Response } from 'express';
import path from 'node:path';
import { AppModule } from './app.module';

async function bootstrap() {
  const app = await NestFactory.create(AppModule);
  app.use(express.static(path.join(process.cwd(), 'public')));
  app.use((request: Request, response: Response, next: NextFunction) => {
    if (request.path.startsWith('/api/') || path.extname(request.path)) return next();
    response.sendFile(path.join(process.cwd(), 'public', 'index.html'));
  });
  app.setGlobalPrefix('api');
  app.useGlobalPipes(new ValidationPipe({ whitelist: true, transform: true }));
  await app.listen(Number(process.env.PORT ?? 3000), '0.0.0.0');
}
void bootstrap();
