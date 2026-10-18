BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Adding EstimatedHours column to [dbo].[WorkOrder]'
	ALTER TABLE [dbo].[WorkOrder] ADD [EstimatedHours] DECIMAL(5,2) NULL
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION
	THROW
END CATCH
GO
