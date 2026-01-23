BEGIN TRANSACTION
GO
PRINT N'Creating AuditEntry table'
GO
CREATE TABLE [dbo].[AuditEntry]
(
	[Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	[WorkOrderId] UNIQUEIDENTIFIER NOT NULL,
	[EmployeeId] UNIQUEIDENTIFIER NULL,
	[EmployeeName] NVARCHAR(100) NULL,
	[Date] DATETIME NOT NULL,
	[BeginStatus] CHAR(3) NULL,
	[EndStatus] CHAR(3) NULL,
	[Action] NVARCHAR(50) NULL,
	CONSTRAINT [FK_AuditEntry_WorkOrder] FOREIGN KEY ([WorkOrderId]) REFERENCES [dbo].[WorkOrder]([Id]),
	CONSTRAINT [FK_AuditEntry_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee]([Id])
)
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT N'Creating index on AuditEntry.WorkOrderId'
GO
CREATE INDEX [IX_AuditEntry_WorkOrderId] ON [dbo].[AuditEntry]([WorkOrderId])
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
