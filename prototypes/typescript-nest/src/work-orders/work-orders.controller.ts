import { Body, Controller, Get, Headers, Param, Patch, Post, Query } from '@nestjs/common';
import { Transform } from 'class-transformer';
import { IsInt, IsOptional, IsString, Matches, MaxLength, Min, MinLength } from 'class-validator';
import { CreateWorkOrder, WorkOrdersService } from './work-orders.service';

class CreateWorkOrderDto implements CreateWorkOrder {
  @IsString() @MinLength(1) @MaxLength(300) title!: string;
  @IsString() @IsOptional() description = '';
  @IsString() @IsOptional() instructions = '';
  @IsString() @IsOptional() @MaxLength(900) roomNumber?: string;
  @Transform(({ value }) => value === '' ? undefined : value) @IsString() @IsOptional() @Matches(/^\d{4}-\d{2}-\d{2}$/) dueDate?: string;
}

class TransitionDto {
  @IsString() status!: string;
  @IsString() @IsOptional() assignee?: string;
}

class UpdateWorkOrderDto {
  @IsString() @IsOptional() @MaxLength(300) title?: string;
  @IsString() @IsOptional() @MaxLength(4000) description?: string;
  @IsString() @IsOptional() @MaxLength(4000) instructions?: string;
  @IsString() @IsOptional() @MaxLength(900) roomNumber?: string;
  @Transform(({ value }) => value === '' ? null : value) @IsOptional() @IsString() @Matches(/^\d{4}-\d{2}-\d{2}$/) dueDate?: string|null;
}

class AddAttachmentMetadataDto {
  @IsString() @MinLength(1) @MaxLength(500) fileName!: string;
  @IsString() @IsOptional() @MaxLength(200) contentType?: string;
  @IsInt() @Min(0) fileSize!: number;
}

@Controller('work-orders')
export class WorkOrdersController {
  constructor(private readonly workOrders: WorkOrdersService) {}

  @Get('status-counts') statusCounts(@Headers('authorization') authorization?: string) { return this.workOrders.statusCounts(bearer(authorization)); }
  @Get() list(@Headers('authorization') authorization?: string, @Query('status') status?: string, @Query('assignee') assignee?: string,
    @Query('creator') creator?: string, @Query('q') q?: string, @Query('overdueOnly') overdueOnly?: string) {
    return this.workOrders.list(bearer(authorization), { status, assignee, creator, q, overdueOnly: overdueOnly === 'true' });
  }
  @Get(':id/history') history(@Param('id') id: string, @Headers('authorization') authorization?: string) { return this.workOrders.history(id, bearer(authorization)); }
  @Get(':id') get(@Param('id') id: string, @Headers('authorization') authorization?: string) { return this.workOrders.get(id, bearer(authorization)); }
  @Get(':id/attachments') attachments(@Param('id') id: string, @Headers('authorization') authorization?: string) { return this.workOrders.attachments(id, bearer(authorization)); }
  @Post(':id/attachments') addAttachment(@Param('id') id: string, @Body() dto: AddAttachmentMetadataDto, @Headers('authorization') authorization?: string) {
    return this.workOrders.addAttachment(id, dto, bearer(authorization));
  }
  @Post() create(@Body() dto: CreateWorkOrderDto, @Headers('authorization') authorization?: string) { return this.workOrders.create(dto, bearer(authorization)); }
  @Patch(':id') update(@Param('id') id: string, @Body() dto: UpdateWorkOrderDto, @Headers('authorization') authorization?: string) {
    return this.workOrders.update(id, dto, bearer(authorization));
  }
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
