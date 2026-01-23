BEGIN TRANSACTION
GO
PRINT N'Creating [dbo].[WorkOrderAuditEntry] table'
GO
CREATE TABLE [dbo].[WorkOrderAuditEntry]
(
	[Id] UNIQUEIDENTIFIER NOT NULL,
	[WorkOrderId] UNIQUEIDENTIFIER NOT NULL,
	[EmployeeId] UNIQUEIDENTIFIER NOT NULL,
	[ArchivedEmployeeName] NVARCHAR(100) NOT NULL,
	[Date] DATETIME NOT NULL,
	[BeginStatus] CHAR(3) NULL,
	[EndStatus] CHAR(3) NULL,
	[ActionType] NVARCHAR(50) NOT NULL,
	[ActionDetails] NVARCHAR(500) NULL,
	CONSTRAINT [PK_WorkOrderAuditEntry] PRIMARY KEY CLUSTERED ([Id]),
	CONSTRAINT [FK_WorkOrderAuditEntry_WorkOrder] FOREIGN KEY ([WorkOrderId]) REFERENCES [dbo].[WorkOrder]([Id]),
	CONSTRAINT [FK_WorkOrderAuditEntry_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee]([Id])
)
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
CREATE NONCLUSTERED INDEX [IX_WorkOrderAuditEntry_WorkOrderId] ON [dbo].[WorkOrderAuditEntry]([WorkOrderId])
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
