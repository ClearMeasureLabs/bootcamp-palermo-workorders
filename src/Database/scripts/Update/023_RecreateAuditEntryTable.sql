BEGIN TRANSACTION
GO
PRINT N'Creating AuditEntry table'
GO
CREATE TABLE [dbo].[AuditEntry]
(
	[Id] uniqueidentifier NOT NULL,
	[WorkOrderId] uniqueidentifier NOT NULL,
	[Sequence] int NOT NULL,
	[EmployeeId] uniqueidentifier NULL,
	[ArchivedEmployeeName] nvarchar(50) NULL,
	[Date] datetime NULL,
	[BeginStatus] char(3) NULL,
	[EndStatus] char(3) NULL
)  ON [PRIMARY]
GO
ALTER TABLE [dbo].[AuditEntry] ADD CONSTRAINT
	[PK_AuditEntry] PRIMARY KEY CLUSTERED 
	(
	[Id]
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

GO
ALTER TABLE [dbo].[AuditEntry] ADD CONSTRAINT
	[FK_AuditEntry_WorkOrder] FOREIGN KEY
	(
	[WorkOrderId]
	) REFERENCES [dbo].[WorkOrder]
	(
	[Id]
	) ON UPDATE NO ACTION 
	 ON DELETE NO ACTION 

GO
ALTER TABLE [dbo].[AuditEntry] ADD CONSTRAINT
	[FK_AuditEntry_Employee] FOREIGN KEY
	(
	[EmployeeId]
	) REFERENCES [dbo].[Employee]
	(
	[Id]
	) ON UPDATE NO ACTION 
	 ON DELETE NO ACTION 

GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
