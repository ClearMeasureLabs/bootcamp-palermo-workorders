import { Body, Controller, Get, Headers, Param, Post, Query } from '@nestjs/common';
import { IsOptional, IsString, Matches, MaxLength, MinLength } from 'class-validator';
import { CreateWorkOrder, WorkOrdersService } from './work-orders.service';

class CreateWorkOrderDto implements CreateWorkOrder {
  @IsString() @MinLength(1) @MaxLength(300) title!: string;
  @IsString() @IsOptional() description = '';
  @IsString() @IsOptional() instructions = '';
  @IsString() @IsOptional() @MaxLength(900) roomNumber?: string;
  @IsString() @IsOptional() @Matches(/^\d{4}-\d{2}-\d{2}$/) dueDate?: string;
}

class TransitionDto {
  @IsString() status!: string;
  @IsString() @IsOptional() assignee?: string;
}

@Controller('work-orders')
export class WorkOrdersController {
  constructor(private readonly workOrders: WorkOrdersService) {}

  @Get() list(@Headers('authorization') authorization?: string, @Query('status') status?: string, @Query('assignee') assignee?: string, @Query('q') q?: string) {
    return this.workOrders.list(bearer(authorization), { status, assignee, q });
  }
  @Get(':id') get(@Param('id') id: string, @Headers('authorization') authorization?: string) { return this.workOrders.get(id, bearer(authorization)); }
  @Post() create(@Body() dto: CreateWorkOrderDto, @Headers('authorization') authorization?: string) { return this.workOrders.create(dto, bearer(authorization)); }
  @Post(':id/transitions') transition(@Param('id') id: string, @Body() dto: TransitionDto, @Headers('authorization') authorization?: string) {
    return this.workOrders.transition(id, dto.status, dto.assignee, bearer(authorization));
  }
}

class LoginDto { @IsString() @MinLength(1) username!: string; }

@Controller()
export class AuthenticationController {
  constructor(private readonly workOrders: WorkOrdersService) {}

  @Get('employees') employees(@Query('canFulfill') canFulfill?: string) {
    return this.workOrders.employees(canFulfill === undefined ? undefined : canFulfill === 'true');
  }

  @Post('auth/login') login(@Body() dto: LoginDto) { return this.workOrders.login(dto.username); }
  @Get('auth/me') me(@Headers('authorization') authorization?: string) { return this.workOrders.me(bearer(authorization)); }
  @Post('auth/logout') logout(@Headers('authorization') authorization?: string) { return this.workOrders.logout(bearer(authorization)); }
}

function bearer(authorization?: string): string | undefined {
  const match = authorization?.match(/^Bearer\s+(.+)$/i);
  return match?.[1];
}
