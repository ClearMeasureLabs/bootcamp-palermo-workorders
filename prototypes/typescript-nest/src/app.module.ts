import { Module } from '@nestjs/common';
import { HealthController } from './health.controller';
import { AuthenticationController, WorkOrdersController } from './work-orders/work-orders.controller';
import { WorkOrdersService } from './work-orders/work-orders.service';

@Module({ controllers: [HealthController, WorkOrdersController, AuthenticationController], providers: [WorkOrdersService] })
export class AppModule {}
