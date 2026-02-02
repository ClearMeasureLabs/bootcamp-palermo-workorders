BEGIN TRANSACTION
GO
PRINT N'Creating [dbo].[AuditEntry]'
GO
CREATE TABLE [dbo].[AuditEntry]
(
	[Id] [uniqueidentifier] NOT NULL,
	[WorkOrderId] [uniqueidentifier] NOT NULL,
	[EmployeeId] [uniqueidentifier] NULL,
	[ArchivedEmployeeName] [nvarchar](200) NULL,
	[Date] [datetime] NOT NULL,
	[BeginStatus] [nchar](3) NULL,
	[EndStatus] [nchar](3) NULL,
	[Action] [nvarchar](50) NULL
)
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT N'Creating primary key [PK_AuditEntry] on [dbo].[AuditEntry]'
GO
ALTER TABLE [dbo].[AuditEntry] ADD CONSTRAINT [PK_AuditEntry] PRIMARY KEY CLUSTERED ([Id])
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT N'Adding foreign keys to [dbo].[AuditEntry]'
GO
ALTER TABLE [dbo].[AuditEntry] ADD
	CONSTRAINT [FK_AuditEntry_WorkOrder] FOREIGN KEY ([WorkOrderId]) REFERENCES [dbo].[WorkOrder] ([Id]),
	CONSTRAINT [FK_AuditEntry_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee] ([Id])
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT N'Creating index [IX_AuditEntry_WorkOrderId] on [dbo].[AuditEntry]'
GO
CREATE NONCLUSTERED INDEX [IX_AuditEntry_WorkOrderId] ON [dbo].[AuditEntry] ([WorkOrderId])
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
