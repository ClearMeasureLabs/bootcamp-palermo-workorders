BEGIN TRANSACTION
GO
IF COL_LENGTH('dbo.WorkOrder', 'Instructions') IS NULL
BEGIN
	ALTER TABLE [dbo].[WorkOrder]
		ADD [Instructions] NVARCHAR(4000) NULL;
END
GO
PRINT 'Ensured Instructions column exists on dbo.WorkOrder'
COMMIT TRANSACTION
GO

