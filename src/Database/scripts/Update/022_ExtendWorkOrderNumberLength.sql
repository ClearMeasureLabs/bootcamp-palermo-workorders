BEGIN TRANSACTION
GO

PRINT N'Extending [Number] column in [dbo].[WorkOrder] from NVARCHAR(5) to NVARCHAR(7)'
GO

ALTER TABLE [dbo].[WorkOrder] ALTER COLUMN [Number] NVARCHAR(7) NOT NULL
GO

IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO

PRINT 'The column length update succeeded'
COMMIT TRANSACTION
