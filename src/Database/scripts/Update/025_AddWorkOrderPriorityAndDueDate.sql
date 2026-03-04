BEGIN TRANSACTION
GO
PRINT N'Adding Priority and DueDate columns to [dbo].[WorkOrder]'
GO
ALTER TABLE [dbo].[WorkOrder] ADD [Priority] INT NOT NULL DEFAULT 1
GO
ALTER TABLE [dbo].[WorkOrder] ADD [DueDate] DATETIME2 NULL
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
