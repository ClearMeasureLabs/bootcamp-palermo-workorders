BEGIN TRANSACTION
GO
PRINT N'Adding [Instructions] to [dbo].[WorkOrder] when missing'
GO
IF NOT EXISTS (
	SELECT 1
	FROM sys.columns
	WHERE object_id = OBJECT_ID(N'dbo.WorkOrder', N'U')
		AND name = N'Instructions'
)
BEGIN
	ALTER TABLE [dbo].[WorkOrder] ADD [Instructions] NVARCHAR(4000) NULL
END
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
