BEGIN TRANSACTION
GO
PRINT N'Migrating Cancelled (CNL) work orders to Draft (DFT) status'
GO
UPDATE [dbo].[WorkOrder] SET [Status] = 'DFT' WHERE [Status] = 'CNL'
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
