BEGIN TRY
	BEGIN TRANSACTION
	PRINT N'Adding non-clustered index IX_WorkOrder_Status on [dbo].[WorkOrder]([Status])'
	IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WorkOrder_Status' AND object_id = OBJECT_ID(N'[dbo].[WorkOrder]'))
	BEGIN
		CREATE NONCLUSTERED INDEX [IX_WorkOrder_Status] ON [dbo].[WorkOrder] ([Status])
	END
	PRINT 'The database update succeeded'
	COMMIT TRANSACTION
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION
	THROW
END CATCH
GO
