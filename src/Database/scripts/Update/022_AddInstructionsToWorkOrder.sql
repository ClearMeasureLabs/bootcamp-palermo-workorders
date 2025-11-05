/*
	Add Instructions field to WorkOrder table
	Support for work order execution instructions (up to 4000 characters)
*/
BEGIN TRANSACTION
GO
PRINT N'Adding [dbo].[WorkOrder] Instructions column'
GO
ALTER TABLE [dbo].[WorkOrder] ADD
	Instructions nvarchar(4000) NULL
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
