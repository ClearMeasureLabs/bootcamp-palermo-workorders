BEGIN TRANSACTION
GO
PRINT N'Creating [dbo].[AuditEntry]'
GO
CREATE TABLE [dbo].[AuditEntry]
(
	[Id] [uniqueidentifier] NOT NULL,
	[WorkOrderId] [uniqueidentifier] NOT NULL,
	[EmployeeId] [uniqueidentifier] NULL,
	[ArchivedEmployeeName] [nvarchar](50) NULL,
	[Date] [datetime] NOT NULL,
	[BeginStatus] [char](3) NULL,
	[EndStatus] [char](3) NULL,
	[Action] [nvarchar](50) NOT NULL
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
PRINT N'Adding foreign key [FK_AuditEntry_WorkOrder] on [dbo].[AuditEntry]'
GO
ALTER TABLE [dbo].[AuditEntry] ADD CONSTRAINT [FK_AuditEntry_WorkOrder] 
	FOREIGN KEY ([WorkOrderId]) REFERENCES [dbo].[WorkOrder] ([Id]) ON DELETE CASCADE
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT N'Adding foreign key [FK_AuditEntry_Employee] on [dbo].[AuditEntry]'
GO
ALTER TABLE [dbo].[AuditEntry] ADD CONSTRAINT [FK_AuditEntry_Employee] 
	FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employee] ([Id]) ON DELETE SET NULL
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
