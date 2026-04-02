BEGIN TRANSACTION
GO
PRINT N'Adding [Instructions] to [dbo].[WorkOrder]'
GO
ALTER TABLE [dbo].[WorkOrder] ADD [Instructions] NVARCHAR(4000) NOT NULL CONSTRAINT [DF_WorkOrder_Instructions] DEFAULT ''
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
