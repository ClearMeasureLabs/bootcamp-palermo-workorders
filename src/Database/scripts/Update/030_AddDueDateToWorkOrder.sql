BEGIN TRANSACTION
GO
PRINT N'Adding DueDate column to [dbo].[WorkOrder]'
GO
	ALTER TABLE [dbo].[WorkOrder] ADD [DueDate] date NULL
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
