-- Create AuditEntry table for tracking work order changes
CREATE TABLE [dbo].[AuditEntry]
(
	[Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
	[WorkOrderId] UNIQUEIDENTIFIER NOT NULL,
	[EmployeeId] UNIQUEIDENTIFIER NULL,
	[EmployeeName] NVARCHAR(100) NULL,
	[EntryDate] DATETIME2 NOT NULL,
	[BeginStatus] CHAR(3) NULL,
	[EndStatus] CHAR(3) NULL,
	[Action] NVARCHAR(50) NULL,
	CONSTRAINT [FK_AuditEntry_WorkOrder] FOREIGN KEY ([WorkOrderId]) REFERENCES [dbo].[WorkOrder]([Id]),
	CONSTRAINT [FK_AuditEntry_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee]([Id])
);

-- Create index for faster queries by WorkOrderId
CREATE INDEX [IX_AuditEntry_WorkOrderId] ON [dbo].[AuditEntry]([WorkOrderId]);
