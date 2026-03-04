BEGIN TRANSACTION
GO
PRINT N'Updating [Status] in [dbo].[WorkOrder]'
GO
UPDATE [dbo].[WorkOrder] SET [Status] = 'DRT' WHERE [Status] = 'DFT'
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
