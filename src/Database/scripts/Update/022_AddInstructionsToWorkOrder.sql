BEGIN TRANSACTION
GO
PRINT N'Adding Instructions column to [dbo].[WorkOrder]'
GO
ALTER TABLE [dbo].[WorkOrder] ADD [Instructions] NVARCHAR(4000) NULL
GO
PRINT N'Updating existing rows with default Instructions value'
GO
UPDATE [dbo].[WorkOrder]
SET [Instructions] = ''
WHERE [Instructions] IS NULL
GO
PRINT N'Adding default constraint for Instructions column'
GO
ALTER TABLE [dbo].[WorkOrder] ADD CONSTRAINT [DF_WorkOrder_Instructions] DEFAULT ('') FOR [Instructions]
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO

