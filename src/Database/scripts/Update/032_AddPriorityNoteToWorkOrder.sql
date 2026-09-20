BEGIN TRANSACTION
GO
PRINT N'Adding PriorityNote column to [dbo].[WorkOrder]'
GO
ALTER TABLE [dbo].[WorkOrder] ADD [PriorityNote] nvarchar(200) NULL
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
