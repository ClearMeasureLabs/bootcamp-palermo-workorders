BEGIN TRANSACTION
GO
PRINT N'Adding [Instructions] to [dbo].[WorkOrder]'
GO
ALTER TABLE [dbo].[WorkOrder] ADD [Instructions] nvarchar(4000) NULL
GO
ALTER TABLE [dbo].[WorkOrder] SET (LOCK_ESCALATION = TABLE)
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
