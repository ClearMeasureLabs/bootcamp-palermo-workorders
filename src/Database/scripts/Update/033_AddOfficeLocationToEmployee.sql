BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Adding OfficeLocation column to [dbo].[Employee]'
	ALTER TABLE [dbo].[Employee] ADD [OfficeLocation] NVARCHAR(100) NULL
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION
	THROW
END CATCH
GO
