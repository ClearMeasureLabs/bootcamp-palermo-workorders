import { Body, Controller, Get, Param, Post, Query } from '@nestjs/common';
import { IsOptional, IsString, Matches, MaxLength, MinLength } from 'class-validator';
import { CreateWorkOrder, WorkOrdersService } from './work-orders.service';

class CreateWorkOrderDto implements CreateWorkOrder {
  @IsString() @MinLength(1) title!: string;
  @IsString() @IsOptional() description = '';
  @IsString() @IsOptional() instructions = '';
  @IsString() @IsOptional() @MaxLength(900) roomNumber?: string;
  @IsString() @IsOptional() @Matches(/^\d{4}-\d{2}-\d{2}$/) dueDate?: string;
  @IsString() @IsOptional() creator?: string;
}

class TransitionDto {
  @IsString() status!: string;
  @IsString() @IsOptional() assignee?: string;
}

@Controller('work-orders')
export class WorkOrdersController {
  constructor(private readonly workOrders: WorkOrdersService) {}

  @Get() list(@Query('status') status?: string, @Query('assignee') assignee?: string, @Query('q') q?: string) {
    return this.workOrders.list({ status, assignee, q });
  }
  @Get(':id') get(@Param('id') id: string) { return this.workOrders.get(id); }
  @Post() create(@Body() dto: CreateWorkOrderDto) { return this.workOrders.create(dto); }
  @Post(':id/transitions') transition(@Param('id') id: string, @Body() dto: TransitionDto) {
    return this.workOrders.transition(id, dto.status, dto.assignee);
  }
}
