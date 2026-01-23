BEGIN TRANSACTION
GO
PRINT N'Adding Instructions column to [dbo].[WorkOrder]'
GO
ALTER TABLE [dbo].[WorkOrder] ADD [Instructions] NVARCHAR(4000) NULL
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
