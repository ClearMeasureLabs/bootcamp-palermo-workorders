BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Adding Description column to [dbo].[WorkOrderAttachment]'
	ALTER TABLE [dbo].[WorkOrderAttachment] ADD [Description] NVARCHAR(500) NULL
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION
	THROW
END CATCH
GO
