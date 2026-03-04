BEGIN TRANSACTION
GO
PRINT N'Creating [dbo].[WorkOrderAttachment]'
GO
CREATE TABLE [dbo].[WorkOrderAttachment]
(
	[Id]			UNIQUEIDENTIFIER	NOT NULL,
	[WorkOrderId]	UNIQUEIDENTIFIER	NOT NULL,
	[FileName]		NVARCHAR(500)		NOT NULL,
	[ContentType]	NVARCHAR(200)		NOT NULL,
	[FileSize]		BIGINT				NOT NULL,
	[UploadedById]	UNIQUEIDENTIFIER	NOT NULL,
	[UploadedDate]	DATETIME2			NOT NULL,
	CONSTRAINT [PK_WorkOrderAttachment] PRIMARY KEY CLUSTERED ([Id]),
	CONSTRAINT [FK_WorkOrderAttachment_WorkOrder] FOREIGN KEY ([WorkOrderId]) REFERENCES [dbo].[WorkOrder] ([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_WorkOrderAttachment_Employee] FOREIGN KEY ([UploadedById]) REFERENCES [dbo].[Employee] ([Id])
)
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
