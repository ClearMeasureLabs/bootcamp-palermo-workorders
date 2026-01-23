BEGIN TRANSACTION
GO
PRINT N'Creating AuditEntry table'
GO
CREATE TABLE [dbo].[AuditEntry]
(
	[Id] [uniqueidentifier] NOT NULL DEFAULT (newid()),
	[WorkOrderId] [uniqueidentifier] NOT NULL,
	[UserName] [nvarchar](100) NULL,
	[Timestamp] [datetime] NOT NULL,
	[Action] [nvarchar](50) NULL,
	[OldStatus] [nvarchar](50) NULL,
	[NewStatus] [nvarchar](50) NULL,
	[Details] [nvarchar](500) NULL,
	CONSTRAINT [PK_AuditEntry] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO
PRINT N'Adding foreign key constraint on WorkOrderId'
GO
ALTER TABLE [dbo].[AuditEntry]
	ADD CONSTRAINT [FK_AuditEntry_WorkOrder] FOREIGN KEY ([WorkOrderId]) REFERENCES [dbo].[WorkOrder] ([Id])
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
